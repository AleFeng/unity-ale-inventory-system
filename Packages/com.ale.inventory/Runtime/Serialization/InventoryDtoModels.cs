using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// 扁平 DTO 模型，用于导出 JSON / 二进制 与运行时加载。与运行时数据模型一一镜像，
    /// 区别在于 Unity 对象引用以 GUID 字符串承载（便于跨工程移植），而非 instanceID。
    /// 所有字段为 public 且类型受 JsonUtility 支持（基础类型 + 数组 + 嵌套 [Serializable]）。
    /// </summary>
    [Serializable]
    public class InventoryDatabaseDto
    {
        public int version = InventoryDtoMapper.Version;
        public EnumTypeDto[] enumTypes;
        public FunctionTagDto[] functionTags;
        public ItemTemplateDto[] itemTemplates;
        public ItemDto[] items;
    }

    [Serializable]
    public class AttributeValueDto
    {
        public int      type;
        public bool     isArray;
        public string   enumTypeRef;
        public int[]    ints;
        public float[]  floats;
        public string[] strings;
        public string[] objGuids;
        /// <summary>
        /// AnimationCurve 序列化数据。每个元素对应一条曲线，格式：
        /// 关键帧以 '|' 分隔，每帧 7 个值以 ',' 分隔：time,value,inTangent,outTangent,inWeight,outWeight,weightedMode。
        /// </summary>
        public string[] curveData;
    }

    [Serializable]
    public class AttributeDefinitionDto
    {
        public string id;
        public int type;
        public bool isArray;
        public string enumTypeRef;
        public AttributeValueDto defaultValue;
    }

    [Serializable]
    public class AttributeEntryDto
    {
        public string id;
        public AttributeValueDto value;
    }

    [Serializable]
    public class EnumItemDto
    {
        public string name;
        public int value;
        /// <summary>枚举项携带的自定义属性值。</summary>
        public AttributeEntryDto[] attributeValues;
    }

    [Serializable]
    public class EnumTypeDto
    {
        public string name;
        public EnumItemDto[] items;
        public int nextValue;
        /// <summary>枚举类型的属性字段定义（所有枚举项共享 schema）。</summary>
        public AttributeDefinitionDto[] attributes;
    }

    [Serializable]
    public class FunctionTagDto
    {
        public string name;
        public string description;
        public AttributeDefinitionDto[] attributes;
    }

    [Serializable]
    public class ItemTemplateDto
    {
        public string name;
        public AttributeDefinitionDto[] attributes;
        /// <summary>模板默认携带的功能标签名称列表（v4 新增）。</summary>
        public string[] tagRefs;
    }

    [Serializable]
    public class ItemDto
    {
        public string id;
        public string templateRef;
        public string[] tagRefs;
        public AttributeEntryDto[] values;
    }

    /// <summary>
    /// 运行时数据模型 与 DTO 之间的双向映射。导出时用 <see cref="IAssetRefResolver"/> 把对象引用转 GUID，
    /// 导入时反向解析。
    /// </summary>
    public static class InventoryDtoMapper
    {
        /// <summary>序列化格式版本号。</summary>
        public const int Version = 5;  // v5: AttributeValueDto 追加 curveData（AnimationCurve 支持）

        #region 导出：DB -> DTO

        public static InventoryDatabaseDto ToDto(InventoryDatabase db, IAssetRefResolver resolver)
        {
            resolver ??= NullAssetRefResolver.Instance;
            return new InventoryDatabaseDto
            {
                version = Version,
                enumTypes = ToArray(db.EnumTypes, e => ToDto(e, resolver)),
                functionTags = ToArray(db.FunctionTags, t => ToDto(t, resolver)),
                itemTemplates = ToArray(db.ItemTemplates, t => ToDto(t, resolver)),
                items = ToArrayFiltered(db.Items, i => !string.IsNullOrWhiteSpace(i.id), i => ToDto(i, resolver))
            };
        }

        private static EnumTypeDto ToDto(EnumType e, IAssetRefResolver resolver)
        {
            return new EnumTypeDto
            {
                name      = e.name,
                nextValue = e.nextValue,
                attributes = ToArray(e.attributes, a => ToDto(a, resolver)),
                items = ToArray(e.items, it => new EnumItemDto
                {
                    name  = it.name,
                    value = it.value,
                    attributeValues = ToArray(it.attributeValues,
                        av => new AttributeEntryDto { id = av.id, value = ToDto(av.value, resolver) })
                })
            };
        }

        private static FunctionTagDto ToDto(FunctionTag t, IAssetRefResolver resolver)
        {
            return new FunctionTagDto
            {
                name = t.name,
                // 描述现为 Text；导出纯文本 fallback（本地化引用与 displayNameText 一样不入导出）。
                description = t.descriptionText != null ? t.descriptionText.GetTextValue(0) : null,
                attributes = ToArray(t.attributes, a => ToDto(a, resolver))
            };
        }

        private static ItemTemplateDto ToDto(ItemTemplate t, IAssetRefResolver resolver)
        {
            return new ItemTemplateDto
            {
                name = t.name,
                attributes = ToArray(t.attributes, a => ToDto(a, resolver)),
                tagRefs = t.tagRefs != null ? t.tagRefs.ToArray() : Array.Empty<string>()
            };
        }

        private static ItemDto ToDto(Item item, IAssetRefResolver resolver)
        {
            return new ItemDto
            {
                id = item.id,
                templateRef = item.templateRef,
                tagRefs = item.tagRefs != null ? item.tagRefs.ToArray() : Array.Empty<string>(),
                values = ToArray(item.values, v => new AttributeEntryDto { id = v.id, value = ToDto(v.value, resolver) })
            };
        }

        private static AttributeDefinitionDto ToDto(AttributeDefinition d, IAssetRefResolver resolver)
        {
            return new AttributeDefinitionDto
            {
                id = d.id,
                type = (int)d.type,
                isArray = d.isArray,
                enumTypeRef = d.enumTypeRef,
                defaultValue = ToDto(d.defaultValue, resolver)
            };
        }

        private static AttributeValueDto ToDto(AttributeValue v, IAssetRefResolver resolver)
        {
            if (v == null) return new AttributeValueDto();

            string[] guids = null;
            if (v.Type.IsObjectBacked())
            {
                var raw = v.RawObjects;
                guids = new string[raw.Count];
                for (int i = 0; i < raw.Count; i++)
                    // 有实时引用（直接模式）→ 经解析器转 GUID 并登记进分组；
                    // 无实时引用（IS_ADDRESSABLE 下 AssetReference 授权，objRefs 槽为 null）→ 直接用授权 GUID。
                    guids[i] = raw[i] != null ? resolver.ToGuid(raw[i]) : v.GetObjAddress(i);
            }

            string[] curveData = null;
            if (v.Type.IsAnimationCurveBacked())
            {
                var raw = v.RawCurves;
                curveData = new string[raw.Count];
                for (int i = 0; i < raw.Count; i++)
                    curveData[i] = SerializeCurve(raw[i]);
            }

            return new AttributeValueDto
            {
                type       = (int)v.Type,
                isArray    = v.IsArray,
                enumTypeRef = v.EnumTypeRef,
                ints       = v.RawInts.ToArray(),
                floats     = v.RawFloats.ToArray(),
                strings    = v.RawStrings.ToArray(),
                objGuids   = guids     ?? Array.Empty<string>(),
                curveData  = curveData ?? Array.Empty<string>()
            };
        }

        // ─── AnimationCurve 序列化辅助 ────────────────────────────────────────────

        private static string SerializeCurve(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < curve.length; i++)
            {
                if (i > 0) sb.Append('|');
                var k = curve.keys[i];
                sb.Append(k.time.ToString("R", CultureInfo.InvariantCulture));     sb.Append(',');
                sb.Append(k.value.ToString("R", CultureInfo.InvariantCulture));    sb.Append(',');
                sb.Append(k.inTangent.ToString("R", CultureInfo.InvariantCulture)); sb.Append(',');
                sb.Append(k.outTangent.ToString("R", CultureInfo.InvariantCulture)); sb.Append(',');
                sb.Append(k.inWeight.ToString("R", CultureInfo.InvariantCulture)); sb.Append(',');
                sb.Append(k.outWeight.ToString("R", CultureInfo.InvariantCulture)); sb.Append(',');
                sb.Append((int)k.weightedMode);
            }
            return sb.ToString();
        }

        private static AnimationCurve DeserializeCurve(string s)
        {
            var curve = new AnimationCurve();
            if (string.IsNullOrEmpty(s)) return curve;
            foreach (var frame in s.Split('|'))
            {
                var vals = frame.Split(',');
                if (vals.Length < 7) continue;
                if (!float.TryParse(vals[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float t))  continue;
                if (!float.TryParse(vals[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float v))  continue;
                if (!float.TryParse(vals[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float it)) continue;
                if (!float.TryParse(vals[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float ot)) continue;
                if (!float.TryParse(vals[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float iw)) continue;
                if (!float.TryParse(vals[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float ow)) continue;
                if (!int.TryParse(vals[6], out int wm)) continue;
                var key = new Keyframe(t, v, it, ot, iw, ow) { weightedMode = (WeightedMode)wm };
                curve.AddKey(key);
            }
            return curve;
        }

        #endregion

        #region 导入：DTO -> DB（写入给定的 InventoryDatabase 实例）

        public static void FromDto(InventoryDatabaseDto dto, InventoryDatabase target, IAssetRefResolver resolver)
        {
            resolver ??= NullAssetRefResolver.Instance;

            target.EnumTypes.Clear();
            target.FunctionTags.Clear();
            target.ItemTemplates.Clear();
            target.Items.Clear();

            if (dto == null) return;

            if (dto.enumTypes != null)
                foreach (var e in dto.enumTypes) target.EnumTypes.Add(FromDto(e, resolver));
            if (dto.functionTags != null)
                foreach (var t in dto.functionTags) target.FunctionTags.Add(FromDto(t, resolver));
            if (dto.itemTemplates != null)
                foreach (var t in dto.itemTemplates) target.ItemTemplates.Add(FromDto(t, resolver));
            if (dto.items != null)
                foreach (var i in dto.items) target.Items.Add(FromDto(i, resolver));
        }

        private static EnumType FromDto(EnumTypeDto dto, IAssetRefResolver resolver)
        {
            var e = new EnumType(dto.name) { nextValue = dto.nextValue };
            if (dto.attributes != null)
                foreach (var a in dto.attributes)
                    e.attributes.Add(FromDto(a, resolver));
            if (dto.items != null)
                foreach (var it in dto.items)
                {
                    var item = new EnumItem(it.name, it.value);
                    if (it.attributeValues != null)
                        foreach (var av in it.attributeValues)
                            item.attributeValues.Add(new AttributeEntry(av.id, FromDto(av.value, resolver)));
                    e.items.Add(item);
                }
            return e;
        }

        private static FunctionTag FromDto(FunctionTagDto dto, IAssetRefResolver resolver)
        {
            var t = new FunctionTag(dto.name, dto.description);
            if (dto.attributes != null)
                foreach (var a in dto.attributes)
                    t.attributes.Add(FromDto(a, resolver));
            return t;
        }

        private static ItemTemplate FromDto(ItemTemplateDto dto, IAssetRefResolver resolver)
        {
            var t = new ItemTemplate(dto.name);
            if (dto.attributes != null)
                foreach (var a in dto.attributes)
                    t.attributes.Add(FromDto(a, resolver));
            if (dto.tagRefs != null)
                t.tagRefs = new List<string>(dto.tagRefs);
            return t;
        }

        private static Item FromDto(ItemDto dto, IAssetRefResolver resolver)
        {
            var item = new Item(dto.id, dto.templateRef);
            if (dto.tagRefs != null) item.tagRefs = new List<string>(dto.tagRefs);
            if (dto.values != null)
                foreach (var v in dto.values)
                    item.values.Add(new AttributeEntry(v.id, FromDto(v.value, resolver)));
            return item;
        }

        private static AttributeDefinition FromDto(AttributeDefinitionDto dto, IAssetRefResolver resolver)
        {
            return new AttributeDefinition
            {
                id = dto.id,
                type = (EFieldType)dto.type,
                isArray = dto.isArray,
                enumTypeRef = dto.enumTypeRef,
                defaultValue = FromDto(dto.defaultValue, resolver)
            };
        }

        private static AttributeValue FromDto(AttributeValueDto dto, IAssetRefResolver resolver)
        {
            var v = new AttributeValue();
            if (dto == null) return v;

            var type = (EFieldType)dto.type;

            List<Object> objs = null;
            List<string> addresses = null;
            if (type.IsObjectBacked() && dto.objGuids != null)
            {
                objs = new List<Object>(dto.objGuids.Length);
                foreach (var guid in dto.objGuids)
                    objs.Add(resolver.FromGuid(guid));

                // 同时把原始 GUID/地址保留下来：运行时（NullResolver）对象引用为 null，
                // 此地址供 Addressable 取用门面按需异步加载。
                addresses = new List<string>(dto.objGuids);
            }

            List<AnimationCurve> curves = null;
            if (type.IsAnimationCurveBacked() && dto.curveData != null)
            {
                curves = new List<AnimationCurve>(dto.curveData.Length);
                foreach (var s in dto.curveData)
                    curves.Add(DeserializeCurve(s));
            }

            v.SetRaw(type, dto.isArray, dto.enumTypeRef,
                dto.ints, dto.floats, dto.strings, objs,
                curveList: curves, addressList: addresses);
            return v;
        }

        #endregion

        private static TOut[] ToArray<TIn, TOut>(List<TIn> source, Func<TIn, TOut> map)
        {
            if (source == null) return Array.Empty<TOut>();
            var result = new TOut[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = map(source[i]);
            return result;
        }

        /// <summary>带过滤器的 ToArray，仅映射满足条件的元素。</summary>
        private static TOut[] ToArrayFiltered<TIn, TOut>(List<TIn> source, Func<TIn, bool> filter, Func<TIn, TOut> map)
        {
            if (source == null) return Array.Empty<TOut>();
            var list = new List<TOut>(source.Count);
            foreach (var item in source)
                if (filter(item)) list.Add(map(item));
            return list.ToArray();
        }
    }
}

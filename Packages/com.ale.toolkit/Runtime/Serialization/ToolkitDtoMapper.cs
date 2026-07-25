using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using Ale.Toolkit.Runtime;

namespace Ale.Toolkit.Runtime.Serialization
{
    /// <summary>
    /// Toolkit 通用序列化映射：运行时属性系统类型（<see cref="AttributeValue"/> / <see cref="AttributeDefinition"/> /
    /// <see cref="AttributeEntry"/> / <see cref="GroupTag"/> / <see cref="ConfigTemplateBase"/> / <see cref="SortPriority"/>）
    /// 与其 DTO 的双向映射，以及各宿主映射器共用的辅助（数组映射、色点、曲线、对象引用转 GUID）。
    ///
    /// <para>导出时用 <see cref="IAssetRefResolver"/> 把对象引用转 GUID，导入时反向解析。宿主插件的领域映射器
    /// （如库存的 <c>InventoryDtoMapper</c>）调用本类完成属性系统部分，自身只处理领域类型的组装。</para>
    /// </summary>
    public static class ToolkitDtoMapper
    {
        #region 通用数组 / 色点辅助

        /// <summary>列表 -> 数组，逐元素映射（null 视为空）。</summary>
        public static TOut[] ToArray<TIn, TOut>(List<TIn> source, Func<TIn, TOut> map)
        {
            if (source == null) return Array.Empty<TOut>();
            var result = new TOut[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = map(source[i]);
            return result;
        }

        /// <summary>带过滤器的 ToArray，仅映射满足条件的元素。</summary>
        public static TOut[] ToArrayFiltered<TIn, TOut>(List<TIn> source, Func<TIn, bool> filter, Func<TIn, TOut> map)
        {
            if (source == null) return Array.Empty<TOut>();
            var list = new List<TOut>(source.Count);
            foreach (var item in source)
                if (filter(item)) list.Add(map(item));
            return list.ToArray();
        }

        /// <summary>字符串引用列表 -> 数组（null 视为空）。</summary>
        public static string[] ToArray(List<string> source)
            => source != null ? source.ToArray() : Array.Empty<string>();

        /// <summary>数组 -> 字符串引用列表（null 视为空，始终返回可写列表）。</summary>
        public static List<string> FromDto(string[] source)
            => source != null ? new List<string>(source) : new List<string>();

        /// <summary>颜色 -> RGBA 浮点数组。</summary>
        public static float[] ToDto(Color c) => new[] { c.r, c.g, c.b, c.a };

        /// <summary>RGBA 浮点数组 -> 颜色；缺省 / 长度不足时返回 <paramref name="fallback"/>。</summary>
        public static Color FromDto(float[] rgba, Color fallback)
            => rgba != null && rgba.Length >= 4 ? new Color(rgba[0], rgba[1], rgba[2], rgba[3]) : fallback;

        /// <summary>
        /// 单个 Unity 对象引用 -> GUID：有实时引用时经解析器转 GUID，
        /// 否则回退到已存的 Addressable 授权地址（约定同 <see cref="AttributeValue"/> 的对象槽）。
        /// </summary>
        public static string ObjToGuid(Object obj, string address, IAssetRefResolver resolver)
            => obj != null ? resolver.ToGuid(obj) : address;

        #endregion

        #region 属性值 / 定义 / 条目

        public static AttributeValueDto ToDto(AttributeValue v, IAssetRefResolver resolver)
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

        public static AttributeValue FromDto(AttributeValueDto dto, IAssetRefResolver resolver)
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

        /// <summary>Text 型属性值的导入：DTO 缺省时给出一个空的 <see cref="EFieldType.Text"/> 值而非默认 Int。</summary>
        public static AttributeValue TextFromDto(AttributeValueDto dto, IAssetRefResolver resolver)
            => dto != null ? FromDto(dto, resolver) : new AttributeValue(EFieldType.Text);

        public static AttributeDefinitionDto ToDto(AttributeDefinition d, IAssetRefResolver resolver)
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

        public static AttributeDefinition FromDto(AttributeDefinitionDto dto, IAssetRefResolver resolver)
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

        /// <summary>属性值条目列表 -> DTO 数组。</summary>
        public static AttributeEntryDto[] ToDto(List<AttributeEntry> source, IAssetRefResolver resolver)
            => ToArray(source, e => new AttributeEntryDto { id = e.id, value = ToDto(e.value, resolver) });

        /// <summary>DTO 数组 -> 属性值条目，追加进 <paramref name="dest"/>（null 视为空）。</summary>
        public static void FromDto(AttributeEntryDto[] source, List<AttributeEntry> dest, IAssetRefResolver resolver)
        {
            if (source == null) return;
            foreach (var e in source)
                dest.Add(new AttributeEntry(e.id, FromDto(e.value, resolver)));
        }

        #endregion

        #region 模板公共字段 / 分组标签

        /// <summary>把模板公共字段（名称 / 色点 / 属性字段）写入 DTO，供各模板 ToDto 复用。</summary>
        public static void FillTemplateDto(ConfigTemplateDto dto, ConfigTemplateBase src, IAssetRefResolver resolver)
        {
            dto.name       = src.name;
            dto.color      = ToDto(src.color);
            dto.attributes = ToArray(src.attributes, a => ToDto(a, resolver));
        }

        /// <summary>把 DTO 的模板公共字段写回运行时模板，供各模板 FromDto 复用。</summary>
        public static void FillTemplate(ConfigTemplateBase dest, ConfigTemplateDto dto, IAssetRefResolver resolver)
        {
            dest.name  = dto.name;
            dest.color = FromDto(dto.color, Color.gray);
            dest.attributes.Clear();
            if (dto.attributes != null)
                foreach (var a in dto.attributes)
                    dest.attributes.Add(FromDto(a, resolver));
        }

        /// <summary>分组标签（各系统同形）-> DTO。</summary>
        public static GroupTagDto ToDto(GroupTag t, IAssetRefResolver resolver)
        {
            return new GroupTagDto
            {
                id          = t.id,
                displayName = ToDto(t.displayName, resolver),
                description = ToDto(t.description, resolver),
                color       = ToDto(t.color)
            };
        }

        /// <summary>DTO -> 指定类型的分组标签（各系统的标签除类型外无差异，故用一个泛型工厂）。</summary>
        public static T FromDto<T>(GroupTagDto dto, IAssetRefResolver resolver) where T : GroupTag, new()
        {
            return new T
            {
                id          = dto.id,
                displayName = TextFromDto(dto.displayName, resolver),
                description = TextFromDto(dto.description, resolver),
                color       = FromDto(dto.color, Color.gray)
            };
        }

        #endregion

        #region 整理排序

        /// <summary>整理条件列表 -> DTO 数组。</summary>
        public static SortPriorityDto[] ToDto(List<SortPriority> source)
            => ToArray(source, sp => new SortPriorityDto { field = sp.field, ascending = sp.ascending });

        /// <summary>DTO 数组 -> 整理条件，追加进 <paramref name="dest"/>（null 视为空）。</summary>
        public static void FromDto(SortPriorityDto[] source, List<SortPriority> dest)
        {
            if (source == null) return;
            foreach (var sp in source)
                dest.Add(new SortPriority(sp.field, sp.ascending));
        }

        #endregion

        #region AnimationCurve 序列化辅助（toolkit 内部）

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
    }
}

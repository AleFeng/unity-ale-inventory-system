using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using Ale.Toolkit.Runtime;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// 运行时数据模型 与 DTO 之间的双向映射。导出时用 <see cref="IAssetRefResolver"/> 把对象引用转 GUID，
    /// 导入时反向解析。
    ///
    /// <para>本分部承载：版本号、顶层 <see cref="ToDto"/> / <see cref="FromDto"/>、属性系统与道具系统的映射，
    /// 以及各分部共用的辅助方法。其余系统各有一个分部文件：<c>InventoryDtoMapper.Inventory.cs</c> /
    /// <c>.Shop.cs</c> / <c>.Crafting.cs</c> / <c>.Equipment.cs</c> / <c>.Skill.cs</c>。</para>
    /// </summary>
    public static partial class InventoryDtoMapper
    {
        /// <summary>
        /// 序列化格式版本号。
        /// <list type="bullet">
        ///   <item>v5：AttributeValueDto 追加 curveData（AnimationCurve 支持）。</item>
        ///   <item>v6：导出补齐——<b>数据库的全部 20 个列表均已纳入</b>（新增 仓库 / 整理选项 / 数字格式 /
        ///         商店 / 制作 / 装备 / 技能），并补上道具系统此前静默丢弃的字段（模板色点、
        ///         weight / stackLimit / hideInInventory、功能标签的 UI 显示配置）。</item>
        /// </list>
        /// </summary>
        public const int Version = 6;

        /// <summary>首个包含仓库 / 商店 / 制作 / 装备 / 技能等扩展数据块的格式版本（二进制读取按此做向后兼容判断）。</summary>
        internal const int VersionWithAllSystems = 6;

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
                items = ToArrayFiltered(db.Items, i => !string.IsNullOrWhiteSpace(i.id), i => ToDto(i, resolver)),

                inventoryTemplates   = ToArray(db.InventoryTemplates, t => ToDto(t, resolver)),
                inventories          = ToArray(db.Inventories, inv => ToDto(inv, resolver)),
                sortOptionAttributes = ToArray(db.SortOptionAttributes, a => ToDto(a, resolver)),
                sortOptions          = ToArray(db.SortOptions, so => ToDto(so, resolver)),
                numberFormatConfigs  = ToArray(db.NumberFormatConfigs, c => ToDto(c, resolver)),

                shopTemplates = ToArray(db.ShopTemplates, t => ToDto(t, resolver)),
                shops         = ToArray(db.Shops, s => ToDto(s, resolver)),

                craftingGroupTags          = ToArray(db.CraftingGroupTags, t => ToDto(t, resolver)),
                craftingBlueprintTemplates = ToArray(db.CraftingBlueprintTemplates, t => ToDto(t, resolver)),
                craftingBlueprints         = ToArray(db.CraftingBlueprints, b => ToDto(b, resolver)),

                equipmentGroupTags      = ToArray(db.EquipmentGroupTags, t => ToDto(t, resolver)),
                equipmentGroupTemplates = ToArray(db.EquipmentGroupTemplates, t => ToDto(t, resolver)),
                equipmentGroups         = ToArray(db.EquipmentGroups, g => ToDto(g, resolver)),

                skillGroupTags = ToArray(db.SkillGroupTags, t => ToDto(t, resolver)),
                skillTemplates = ToArray(db.SkillTemplates, t => ToDto(t, resolver)),
                skills         = ToArray(db.Skills, s => ToDto(s, resolver)),

                localizationTableCollectionGuid = db.LocalizationTableCollectionGuid
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
                    attributeValues = ToDto(it.attributeValues, resolver)
                })
            };
        }

        private static FunctionTagDto ToDto(FunctionTag t, IAssetRefResolver resolver)
        {
            return new FunctionTagDto
            {
                name = t.name,
                // v5 及更早唯一的描述载体：纯文本 fallback。v6 起 descriptionText 才是完整来源，
                // 本字段仍写出以兼容只认旧格式的消费方。
                description = t.descriptionText != null ? t.descriptionText.GetTextValue(0) : null,
                attributes  = ToArray(t.attributes, a => ToDto(a, resolver)),

                displayNameText = ToDto(t.displayNameText, resolver),
                descriptionText = ToDto(t.descriptionText, resolver),
                backgroundSpriteGuid = ObjToGuid(t.backgroundSprite, t.backgroundSpriteAddress, resolver),
                backgroundColor = ToDto(t.backgroundColor),
                hideInUI        = t.hideInUI
            };
        }

        private static ItemTemplateDto ToDto(ItemTemplate t, IAssetRefResolver resolver)
        {
            var dto = new ItemTemplateDto
            {
                tagRefs         = ToArray(t.tagRefs),
                weight          = t.weight,
                stackLimit      = t.stackLimit,
                hideInInventory = t.hideInInventory
            };
            FillTemplateDto(dto, t, resolver);   // 名称 / 色点 / 属性字段
            return dto;
        }

        private static ItemDto ToDto(Item item, IAssetRefResolver resolver)
        {
            return new ItemDto
            {
                id = item.id,
                templateRef = item.templateRef,
                tagRefs = ToArray(item.tagRefs),
                values = ToDto(item.values, resolver),
                weight          = item.weight,
                stackLimit      = item.stackLimit,
                hideInInventory = item.hideInInventory
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
            target.InventoryTemplates.Clear();
            target.Inventories.Clear();
            target.SortOptionAttributes.Clear();
            target.SortOptions.Clear();
            target.NumberFormatConfigs.Clear();
            target.ShopTemplates.Clear();
            target.Shops.Clear();
            target.CraftingGroupTags.Clear();
            target.CraftingBlueprintTemplates.Clear();
            target.CraftingBlueprints.Clear();
            target.EquipmentGroupTags.Clear();
            target.EquipmentGroupTemplates.Clear();
            target.EquipmentGroups.Clear();
            target.SkillGroupTags.Clear();
            target.SkillTemplates.Clear();
            target.Skills.Clear();
            target.LocalizationTableCollectionGuid = null;

            if (dto == null) return;

            if (dto.enumTypes != null)
                foreach (var e in dto.enumTypes) target.EnumTypes.Add(FromDto(e, resolver));
            if (dto.functionTags != null)
                foreach (var t in dto.functionTags) target.FunctionTags.Add(FromDto(t, resolver));
            if (dto.itemTemplates != null)
                foreach (var t in dto.itemTemplates) target.ItemTemplates.Add(FromDto(t, resolver));
            if (dto.items != null)
                foreach (var i in dto.items) target.Items.Add(FromDto(i, resolver));

            if (dto.inventoryTemplates != null)
                foreach (var t in dto.inventoryTemplates) target.InventoryTemplates.Add(FromDto(t, resolver));
            if (dto.inventories != null)
                foreach (var inv in dto.inventories) target.Inventories.Add(FromDto(inv, resolver));
            if (dto.sortOptionAttributes != null)
                foreach (var a in dto.sortOptionAttributes) target.SortOptionAttributes.Add(FromDto(a, resolver));
            if (dto.sortOptions != null)
                foreach (var so in dto.sortOptions) target.SortOptions.Add(FromDto(so, resolver));
            if (dto.numberFormatConfigs != null)
                foreach (var c in dto.numberFormatConfigs) target.NumberFormatConfigs.Add(FromDto(c, resolver));

            if (dto.shopTemplates != null)
                foreach (var t in dto.shopTemplates) target.ShopTemplates.Add(FromDto(t, resolver));
            if (dto.shops != null)
                foreach (var s in dto.shops) target.Shops.Add(FromDto(s, resolver));

            if (dto.craftingGroupTags != null)
                foreach (var t in dto.craftingGroupTags) target.CraftingGroupTags.Add(FromDto<CraftingGroupTag>(t, resolver));
            if (dto.craftingBlueprintTemplates != null)
                foreach (var t in dto.craftingBlueprintTemplates) target.CraftingBlueprintTemplates.Add(FromDto(t, resolver));
            if (dto.craftingBlueprints != null)
                foreach (var b in dto.craftingBlueprints) target.CraftingBlueprints.Add(FromDto(b, resolver));

            if (dto.equipmentGroupTags != null)
                foreach (var t in dto.equipmentGroupTags) target.EquipmentGroupTags.Add(FromDto<EquipmentGroupTag>(t, resolver));
            if (dto.equipmentGroupTemplates != null)
                foreach (var t in dto.equipmentGroupTemplates) target.EquipmentGroupTemplates.Add(FromDto(t, resolver));
            if (dto.equipmentGroups != null)
                foreach (var g in dto.equipmentGroups) target.EquipmentGroups.Add(FromDto(g, resolver));

            if (dto.skillGroupTags != null)
                foreach (var t in dto.skillGroupTags) target.SkillGroupTags.Add(FromDto<SkillGroupTag>(t, resolver));
            if (dto.skillTemplates != null)
                foreach (var t in dto.skillTemplates) target.SkillTemplates.Add(FromDto(t, resolver));
            if (dto.skills != null)
                foreach (var s in dto.skills) target.Skills.Add(FromDto(s, resolver));

            target.LocalizationTableCollectionGuid = dto.localizationTableCollectionGuid;
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
                    FromDto(it.attributeValues, item.attributeValues, resolver);
                    e.items.Add(item);
                }
            return e;
        }

        private static FunctionTag FromDto(FunctionTagDto dto, IAssetRefResolver resolver)
        {
            // 描述：v6 起以 descriptionText 为准；缺省（v5 及更早的数据）回退到纯文本 description。
            var t = new FunctionTag(dto.name, dto.description);
            if (dto.descriptionText != null) t.descriptionText = FromDto(dto.descriptionText, resolver);
            t.displayNameText = TextFromDto(dto.displayNameText, resolver);

            t.backgroundSpriteAddress = dto.backgroundSpriteGuid;
            t.backgroundSprite        = resolver.FromGuid(dto.backgroundSpriteGuid) as Sprite;
            t.backgroundColor         = FromDto(dto.backgroundColor, Color.white);
            t.hideInUI                = dto.hideInUI;

            if (dto.attributes != null)
                foreach (var a in dto.attributes)
                    t.attributes.Add(FromDto(a, resolver));
            return t;
        }

        private static ItemTemplate FromDto(ItemTemplateDto dto, IAssetRefResolver resolver)
        {
            var t = new ItemTemplate
            {
                tagRefs         = FromDto(dto.tagRefs),
                weight          = dto.weight,
                stackLimit      = dto.stackLimit,
                hideInInventory = dto.hideInInventory
            };
            FillTemplate(t, dto, resolver);   // 名称 / 色点 / 属性字段
            return t;
        }

        private static Item FromDto(ItemDto dto, IAssetRefResolver resolver)
        {
            var item = new Item(dto.id, dto.templateRef)
            {
                tagRefs         = FromDto(dto.tagRefs),
                weight          = dto.weight,
                stackLimit      = dto.stackLimit,
                hideInInventory = dto.hideInInventory
            };
            FromDto(dto.values, item.values, resolver);
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

        #region 共用辅助

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

        /// <summary>字符串引用列表 -> 数组（null 视为空）。</summary>
        private static string[] ToArray(List<string> source)
            => source != null ? source.ToArray() : Array.Empty<string>();

        /// <summary>数组 -> 字符串引用列表（null 视为空，始终返回可写列表）。</summary>
        private static List<string> FromDto(string[] source)
            => source != null ? new List<string>(source) : new List<string>();

        /// <summary>属性值条目列表 -> DTO 数组。</summary>
        private static AttributeEntryDto[] ToDto(List<AttributeEntry> source, IAssetRefResolver resolver)
            => ToArray(source, e => new AttributeEntryDto { id = e.id, value = ToDto(e.value, resolver) });

        /// <summary>DTO 数组 -> 属性值条目，追加进 <paramref name="dest"/>（null 视为空）。</summary>
        private static void FromDto(AttributeEntryDto[] source, List<AttributeEntry> dest, IAssetRefResolver resolver)
        {
            if (source == null) return;
            foreach (var e in source)
                dest.Add(new AttributeEntry(e.id, FromDto(e.value, resolver)));
        }

        /// <summary>Text 型属性值的导入：DTO 缺省时给出一个空的 <see cref="EFieldType.Text"/> 值而非默认 Int。</summary>
        private static AttributeValue TextFromDto(AttributeValueDto dto, IAssetRefResolver resolver)
            => dto != null ? FromDto(dto, resolver) : new AttributeValue(EFieldType.Text);

        private static float[] ToDto(Color c) => new[] { c.r, c.g, c.b, c.a };

        /// <summary>RGBA 浮点数组 -> 颜色；缺省 / 长度不足时返回 <paramref name="fallback"/>。</summary>
        private static Color FromDto(float[] rgba, Color fallback)
            => rgba != null && rgba.Length >= 4 ? new Color(rgba[0], rgba[1], rgba[2], rgba[3]) : fallback;

        /// <summary>把模板公共字段（名称 / 色点 / 属性字段）写入 DTO，供各模板 ToDto 复用。</summary>
        private static void FillTemplateDto(ConfigTemplateDto dto, ConfigTemplateBase src, IAssetRefResolver resolver)
        {
            dto.name       = src.name;
            dto.color      = ToDto(src.color);
            dto.attributes = ToArray(src.attributes, a => ToDto(a, resolver));
        }

        /// <summary>把 DTO 的模板公共字段写回运行时模板，供各模板 FromDto 复用。</summary>
        private static void FillTemplate(ConfigTemplateBase dest, ConfigTemplateDto dto, IAssetRefResolver resolver)
        {
            dest.name  = dto.name;
            dest.color = FromDto(dto.color, Color.gray);
            dest.attributes.Clear();
            if (dto.attributes != null)
                foreach (var a in dto.attributes)
                    dest.attributes.Add(FromDto(a, resolver));
        }

        /// <summary>
        /// 分组标签（制作 / 装备 / 技能三系统同形）-> DTO。
        /// </summary>
        private static GroupTagDto ToDto(GroupTag t, IAssetRefResolver resolver)
        {
            return new GroupTagDto
            {
                id          = t.id,
                displayName = ToDto(t.displayName, resolver),
                description = ToDto(t.description, resolver),
                color       = ToDto(t.color)
            };
        }

        /// <summary>DTO -> 指定类型的分组标签（三系统的标签除类型外无差异，故用一个泛型工厂）。</summary>
        private static T FromDto<T>(GroupTagDto dto, IAssetRefResolver resolver) where T : GroupTag, new()
        {
            return new T
            {
                id          = dto.id,
                displayName = TextFromDto(dto.displayName, resolver),
                description = TextFromDto(dto.description, resolver),
                color       = FromDto(dto.color, Color.gray)
            };
        }

        /// <summary>
        /// 单个 Unity 对象引用 -> GUID：有实时引用时经解析器转 GUID，
        /// 否则回退到已存的 Addressable 授权地址（约定同 <see cref="AttributeValue"/> 的对象槽）。
        /// </summary>
        private static string ObjToGuid(Object obj, string address, IAssetRefResolver resolver)
            => obj != null ? resolver.ToGuid(obj) : address;

        /// <summary>整理条件列表 -> DTO 数组。</summary>
        private static SortPriorityDto[] ToDto(List<SortPriority> source)
            => ToArray(source, sp => new SortPriorityDto { field = sp.field, ascending = sp.ascending });

        /// <summary>DTO 数组 -> 整理条件，追加进 <paramref name="dest"/>（null 视为空）。</summary>
        private static void FromDto(SortPriorityDto[] source, List<SortPriority> dest)
        {
            if (source == null) return;
            foreach (var sp in source)
                dest.Add(new SortPriority(sp.field, sp.ascending));
        }

        #endregion
    }
}

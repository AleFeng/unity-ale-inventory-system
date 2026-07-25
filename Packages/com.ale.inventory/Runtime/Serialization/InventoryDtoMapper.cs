using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Ale.Toolkit.Runtime;
using Ale.Toolkit.Runtime.Serialization;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// 运行时数据模型 与 DTO 之间的双向映射。导出时用 <see cref="IAssetRefResolver"/> 把对象引用转 GUID，
    /// 导入时反向解析。
    ///
    /// <para>本分部承载：版本号、顶层 <see cref="ToDto"/> / <see cref="FromDto"/>、道具系统（枚举 / 功能标签 /
    /// 道具）的映射，以及各分部对 toolkit 通用映射 <see cref="ToolkitDtoMapper"/> 的转发封装。其余系统各有一个
    /// 分部文件：<c>InventoryDtoMapper.Inventory.cs</c> / <c>.Shop.cs</c> / <c>.Crafting.cs</c> /
    /// <c>.Equipment.cs</c> / <c>.Skill.cs</c>。属性系统 / 分组标签 / 模板公共字段 / 整理排序等通用映射已下沉到
    /// <see cref="ToolkitDtoMapper"/>；本类保留同名 <c>private</c> 转发以使各分部调用点不变。</para>
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

        #endregion

        #region 共用辅助（转发 toolkit 通用映射 ToolkitDtoMapper）

        // 属性系统 / 分组标签 / 模板公共字段 / 整理排序等通用映射已下沉到 ToolkitDtoMapper。
        // 下方保留同名 private 转发，使本类各分部（Inventory / Shop / Crafting / Equipment / Skill）
        // 与道具系统映射的调用点无需改动。

        private static TOut[] ToArray<TIn, TOut>(List<TIn> source, Func<TIn, TOut> map)
            => ToolkitDtoMapper.ToArray(source, map);

        private static TOut[] ToArrayFiltered<TIn, TOut>(List<TIn> source, Func<TIn, bool> filter, Func<TIn, TOut> map)
            => ToolkitDtoMapper.ToArrayFiltered(source, filter, map);

        private static string[] ToArray(List<string> source)
            => ToolkitDtoMapper.ToArray(source);

        private static List<string> FromDto(string[] source)
            => ToolkitDtoMapper.FromDto(source);

        private static AttributeValueDto ToDto(AttributeValue v, IAssetRefResolver resolver)
            => ToolkitDtoMapper.ToDto(v, resolver);

        private static AttributeValue FromDto(AttributeValueDto dto, IAssetRefResolver resolver)
            => ToolkitDtoMapper.FromDto(dto, resolver);

        private static AttributeValue TextFromDto(AttributeValueDto dto, IAssetRefResolver resolver)
            => ToolkitDtoMapper.TextFromDto(dto, resolver);

        private static AttributeDefinitionDto ToDto(AttributeDefinition d, IAssetRefResolver resolver)
            => ToolkitDtoMapper.ToDto(d, resolver);

        private static AttributeDefinition FromDto(AttributeDefinitionDto dto, IAssetRefResolver resolver)
            => ToolkitDtoMapper.FromDto(dto, resolver);

        private static AttributeEntryDto[] ToDto(List<AttributeEntry> source, IAssetRefResolver resolver)
            => ToolkitDtoMapper.ToDto(source, resolver);

        private static void FromDto(AttributeEntryDto[] source, List<AttributeEntry> dest, IAssetRefResolver resolver)
            => ToolkitDtoMapper.FromDto(source, dest, resolver);

        private static float[] ToDto(Color c)
            => ToolkitDtoMapper.ToDto(c);

        private static Color FromDto(float[] rgba, Color fallback)
            => ToolkitDtoMapper.FromDto(rgba, fallback);

        private static void FillTemplateDto(ConfigTemplateDto dto, ConfigTemplateBase src, IAssetRefResolver resolver)
            => ToolkitDtoMapper.FillTemplateDto(dto, src, resolver);

        private static void FillTemplate(ConfigTemplateBase dest, ConfigTemplateDto dto, IAssetRefResolver resolver)
            => ToolkitDtoMapper.FillTemplate(dest, dto, resolver);

        private static GroupTagDto ToDto(GroupTag t, IAssetRefResolver resolver)
            => ToolkitDtoMapper.ToDto(t, resolver);

        private static T FromDto<T>(GroupTagDto dto, IAssetRefResolver resolver) where T : GroupTag, new()
            => ToolkitDtoMapper.FromDto<T>(dto, resolver);

        private static string ObjToGuid(Object obj, string address, IAssetRefResolver resolver)
            => ToolkitDtoMapper.ObjToGuid(obj, address, resolver);

        private static SortPriorityDto[] ToDto(List<SortPriority> source)
            => ToolkitDtoMapper.ToDto(source);

        private static void FromDto(SortPriorityDto[] source, List<SortPriority> dest)
            => ToolkitDtoMapper.FromDto(source, dest);

        #endregion
    }
}

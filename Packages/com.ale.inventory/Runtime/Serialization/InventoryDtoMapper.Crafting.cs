namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryDtoMapper"/> 的制作系统分部：蓝图模板 / 蓝图（含产出 / 消耗条目与属性字段显示配置）。
    /// 分组标签走共用的 <see cref="GroupTagDto"/>（见核心分部）。自 v6 起纳入 JSON / 二进制导出。
    /// </summary>
    public static partial class InventoryDtoMapper
    {
        #region 导出：DB -> DTO

        private static CraftingBlueprintTemplateDto ToDto(CraftingBlueprintTemplate t, IAssetRefResolver resolver)
        {
            var dto = new CraftingBlueprintTemplateDto
            {
                craftTime          = t.craftTime,
                maxCraftCount      = t.maxCraftCount,
                craftInventoryRefs = ToArray(t.craftInventoryRefs),
                numberFormatRef    = t.numberFormatRef,
                sortPriorities     = ToDto(t.sortPriorities),
                sortTiebreakers    = ToDto(t.sortTiebreakers),
                attributeDisplays  = ToArray(t.attributeDisplays, ad => ToDto(ad))
            };
            FillTemplateDto(dto, t, resolver);   // 名称 / 色点 / 属性字段
            return dto;
        }

        private static CraftingBlueprintDto ToDto(CraftingBlueprint b, IAssetRefResolver resolver)
        {
            return new CraftingBlueprintDto
            {
                id              = b.id,
                templateRef     = b.templateRef,
                displayText     = ToDto(b.displayText, resolver),
                descriptionText = ToDto(b.descriptionText, resolver),
                primaryGroupTag    = b.primaryGroupTag,
                secondaryGroupTags = ToArray(b.secondaryGroupTags),
                outputs            = ToArray(b.outputs, o => ToDto(o)),
                inputs             = ToArray(b.inputs,  i => ToDto(i)),
                craftTime          = b.craftTime,
                maxCraftCount      = b.maxCraftCount,
                craftInventoryRefs = ToArray(b.craftInventoryRefs),
                numberFormatRef    = b.numberFormatRef,
                attributeDisplays  = ToArray(b.attributeDisplays, ad => ToDto(ad)),
                values             = ToDto(b.values, resolver)
            };
        }

        private static CraftingItemAmountDto ToDto(CraftingItemAmount a)
            => new CraftingItemAmountDto { itemId = a.itemId, count = a.count };

        private static CraftingAttributeDisplayDto ToDto(CraftingAttributeDisplay ad)
            => new CraftingAttributeDisplayDto { label = ad.label, attrId = ad.attrId };

        #endregion

        #region 导入：DTO -> DB

        private static CraftingBlueprintTemplate FromDto(CraftingBlueprintTemplateDto dto, IAssetRefResolver resolver)
        {
            var t = new CraftingBlueprintTemplate
            {
                craftTime          = dto.craftTime,
                maxCraftCount      = dto.maxCraftCount,
                craftInventoryRefs = FromDto(dto.craftInventoryRefs),
                numberFormatRef    = dto.numberFormatRef
            };
            FillTemplate(t, dto, resolver);   // 名称 / 色点 / 属性字段
            FromDto(dto.sortPriorities,  t.sortPriorities);
            FromDto(dto.sortTiebreakers, t.sortTiebreakers);
            if (dto.attributeDisplays != null)
                foreach (var ad in dto.attributeDisplays) t.attributeDisplays.Add(FromDto(ad));
            return t;
        }

        private static CraftingBlueprint FromDto(CraftingBlueprintDto dto, IAssetRefResolver resolver)
        {
            var b = new CraftingBlueprint(dto.id, dto.templateRef)
            {
                displayText     = TextFromDto(dto.displayText, resolver),
                descriptionText = TextFromDto(dto.descriptionText, resolver),
                primaryGroupTag    = dto.primaryGroupTag,
                secondaryGroupTags = FromDto(dto.secondaryGroupTags),
                craftTime          = dto.craftTime,
                maxCraftCount      = dto.maxCraftCount,
                craftInventoryRefs = FromDto(dto.craftInventoryRefs),
                numberFormatRef    = dto.numberFormatRef
            };
            if (dto.outputs != null)
                foreach (var o in dto.outputs) b.outputs.Add(FromDto(o));
            if (dto.inputs != null)
                foreach (var i in dto.inputs) b.inputs.Add(FromDto(i));
            if (dto.attributeDisplays != null)
                foreach (var ad in dto.attributeDisplays) b.attributeDisplays.Add(FromDto(ad));
            FromDto(dto.values, b.values, resolver);
            return b;
        }

        private static CraftingItemAmount FromDto(CraftingItemAmountDto dto)
            => new CraftingItemAmount(dto.itemId, dto.count);

        private static CraftingAttributeDisplay FromDto(CraftingAttributeDisplayDto dto)
            => new CraftingAttributeDisplay(dto.label, dto.attrId);

        #endregion
    }
}

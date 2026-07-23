namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryDtoMapper"/> 的仓库系统分部：仓库模板 / 仓库 / 整理选项 / 数字格式配置。
    /// 自 v6 起纳入 JSON / 二进制导出（此前这些列表被静默丢弃）。
    /// </summary>
    public static partial class InventoryDtoMapper
    {
        #region 导出：DB -> DTO

        private static InventoryTemplateDto ToDto(InventoryTemplate t, IAssetRefResolver resolver)
        {
            var dto = new InventoryTemplateDto
            {
                capacity            = t.capacity,
                weightLimit         = t.weightLimit,
                allowPutTagRefs     = ToArray(t.allowPutTagRefs),
                allowTakeTagRefs    = ToArray(t.allowTakeTagRefs),
                allowOperateTagRefs = ToArray(t.allowOperateTagRefs),
                filterTagRefs       = ToArray(t.filterTagRefs),
                showAllFilterTab    = t.showAllFilterTab,
                autoSort            = t.autoSort,
                dragSort            = t.dragSort,
                numberFormatRef     = t.numberFormatRef,
                sortPriorities      = ToDto(t.sortPriorities),
                sortTiebreakers     = ToDto(t.sortTiebreakers)
            };
            FillTemplateDto(dto, t, resolver);   // 名称 / 色点 / 属性字段
            return dto;
        }

        private static InventoryDto ToDto(Inventory inv, IAssetRefResolver resolver)
        {
            return new InventoryDto
            {
                id              = inv.id,
                templateRef     = inv.templateRef,
                displayNameText = ToDto(inv.displayNameText, resolver),
                descriptionText = ToDto(inv.descriptionText, resolver),
                capacity            = inv.capacity,
                weightLimit         = inv.weightLimit,
                allowPutTagRefs     = ToArray(inv.allowPutTagRefs),
                allowTakeTagRefs    = ToArray(inv.allowTakeTagRefs),
                allowOperateTagRefs = ToArray(inv.allowOperateTagRefs),
                filterTagRefs       = ToArray(inv.filterTagRefs),
                showAllFilterTab    = inv.showAllFilterTab,
                autoSort            = inv.autoSort,
                dragSort            = inv.dragSort,
                numberFormatRef     = inv.numberFormatRef,
                sortPriorities      = ToDto(inv.sortPriorities),
                sortTiebreakers     = ToDto(inv.sortTiebreakers),
                values              = ToDto(inv.values, resolver)
            };
        }

        private static SortOptionDto ToDto(SortOption so, IAssetRefResolver resolver)
        {
            return new SortOptionDto
            {
                field           = so.field,
                displayName     = ToDto(so.displayName, resolver),
                ignoreIds       = ToArray(so.ignoreIds),
                attributeValues = ToDto(so.attributeValues, resolver)
            };
        }

        private static NumberFormatConfigDto ToDto(NumberFormatConfig c, IAssetRefResolver resolver)
        {
            return new NumberFormatConfigDto
            {
                name = c.name,
                locales = ToArray(c.locales, loc => new NumberFormatLocaleDto
                {
                    languageCode = loc.languageCode,
                    rules = ToArray(loc.rules, r => new NumberFormatRuleDto
                    {
                        threshold     = r.threshold,
                        divisor       = r.divisor,
                        suffixText    = ToDto(r.suffixText, resolver),
                        decimalPlaces = r.decimalPlaces
                    })
                })
            };
        }

        #endregion

        #region 导入：DTO -> DB

        private static InventoryTemplate FromDto(InventoryTemplateDto dto, IAssetRefResolver resolver)
        {
            var t = new InventoryTemplate
            {
                capacity            = dto.capacity,
                weightLimit         = dto.weightLimit,
                allowPutTagRefs     = FromDto(dto.allowPutTagRefs),
                allowTakeTagRefs    = FromDto(dto.allowTakeTagRefs),
                allowOperateTagRefs = FromDto(dto.allowOperateTagRefs),
                filterTagRefs       = FromDto(dto.filterTagRefs),
                showAllFilterTab    = dto.showAllFilterTab,
                autoSort            = dto.autoSort,
                dragSort            = dto.dragSort,
                numberFormatRef     = dto.numberFormatRef
            };
            FillTemplate(t, dto, resolver);   // 名称 / 色点 / 属性字段
            FromDto(dto.sortPriorities,  t.sortPriorities);
            FromDto(dto.sortTiebreakers, t.sortTiebreakers);
            return t;
        }

        private static Inventory FromDto(InventoryDto dto, IAssetRefResolver resolver)
        {
            var inv = new Inventory(dto.id, dto.templateRef)
            {
                displayNameText = TextFromDto(dto.displayNameText, resolver),
                descriptionText = TextFromDto(dto.descriptionText, resolver),
                capacity            = dto.capacity,
                weightLimit         = dto.weightLimit,
                allowPutTagRefs     = FromDto(dto.allowPutTagRefs),
                allowTakeTagRefs    = FromDto(dto.allowTakeTagRefs),
                allowOperateTagRefs = FromDto(dto.allowOperateTagRefs),
                filterTagRefs       = FromDto(dto.filterTagRefs),
                showAllFilterTab    = dto.showAllFilterTab,
                autoSort            = dto.autoSort,
                dragSort            = dto.dragSort,
                numberFormatRef     = dto.numberFormatRef
            };
            FromDto(dto.sortPriorities,  inv.sortPriorities);
            FromDto(dto.sortTiebreakers, inv.sortTiebreakers);
            FromDto(dto.values, inv.values, resolver);
            return inv;
        }

        private static SortOption FromDto(SortOptionDto dto, IAssetRefResolver resolver)
        {
            var so = new SortOption(dto.field)
            {
                displayName = TextFromDto(dto.displayName, resolver),
                ignoreIds   = FromDto(dto.ignoreIds)
            };
            FromDto(dto.attributeValues, so.attributeValues, resolver);
            return so;
        }

        private static NumberFormatConfig FromDto(NumberFormatConfigDto dto, IAssetRefResolver resolver)
        {
            var c = new NumberFormatConfig { name = dto.name };
            if (dto.locales == null) return c;

            foreach (var locDto in dto.locales)
            {
                var loc = new NumberFormatLocale { languageCode = locDto.languageCode };
                if (locDto.rules != null)
                    foreach (var r in locDto.rules)
                        loc.rules.Add(new NumberFormatRule
                        {
                            threshold     = r.threshold,
                            divisor       = r.divisor,
                            suffixText    = TextFromDto(r.suffixText, resolver),
                            decimalPlaces = r.decimalPlaces
                        });
                c.locales.Add(loc);
            }
            return c;
        }

        #endregion
    }
}

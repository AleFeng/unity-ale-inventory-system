using System.Collections.Generic;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryDtoMapper"/> 的商店系统分部：商店模板 / 商店 / 商品组 / 商品 / 刷新计划。
    /// 自 v6 起纳入 JSON / 二进制导出（此前这些列表被静默丢弃）。
    ///
    /// <para>商品组与商品的 <c>guid</c> 随导出保留——它是每玩家交易进度的存档键，
    /// 丢了会让老存档的「已交易次数」挂到别的商品上。</para>
    /// </summary>
    public static partial class InventoryDtoMapper
    {
        #region 导出：DB -> DTO

        private static ShopTemplateDto ToDto(ShopTemplate t, IAssetRefResolver resolver)
        {
            var dto = new ShopTemplateDto
            {
                shopType           = (int)t.shopType,
                tradeInventoryRefs = ToArray(t.tradeInventoryRefs),
                tradeTagRefs       = ToArray(t.tradeTagRefs),
                filterTagRefs      = ToArray(t.filterTagRefs),
                showAllFilterTab   = t.showAllFilterTab,
                numberFormatRef    = t.numberFormatRef,
                priceAttrSource    = t.priceAttrSource,
                sortPriorities     = ToDto(t.sortPriorities),
                sortTiebreakers    = ToDto(t.sortTiebreakers),
                groups             = ToArray(t.groups, g => ToDto(g))
            };
            FillTemplateDto(dto, t, resolver);   // 名称 / 色点 / 属性字段
            return dto;
        }

        private static ShopDto ToDto(Shop s, IAssetRefResolver resolver)
        {
            return new ShopDto
            {
                id              = s.id,
                templateRef     = s.templateRef,
                displayNameText = ToDto(s.displayNameText, resolver),
                descriptionText = ToDto(s.descriptionText, resolver),
                shopType           = (int)s.shopType,
                tradeInventoryRefs = ToArray(s.tradeInventoryRefs),
                tradeTagRefs       = ToArray(s.tradeTagRefs),
                filterTagRefs      = ToArray(s.filterTagRefs),
                showAllFilterTab   = s.showAllFilterTab,
                numberFormatRef    = s.numberFormatRef,
                priceAttrSource    = s.priceAttrSource,
                sortPriorities     = ToDto(s.sortPriorities),
                sortTiebreakers    = ToDto(s.sortTiebreakers),
                groups             = ToArray(s.groups, g => ToDto(g)),
                values             = ToDto(s.values, resolver)
            };
        }

        private static ShopCommodityGroupDto ToDto(ShopCommodityGroup g)
        {
            return new ShopCommodityGroupDto
            {
                guid        = g.guid,
                name        = g.name,
                description = g.description,
                refresh     = ToDto(g.refresh),
                commodities = ToArray(g.commodities, c => ToDto(c))
            };
        }

        private static ShopCommodityDto ToDto(ShopCommodity c)
        {
            return new ShopCommodityDto
            {
                guid            = c.guid,
                itemId          = c.itemId,
                count           = c.count,
                priceMultiplier = c.priceMultiplier,
                tradeLimit      = c.tradeLimit,
                overrideRefresh = c.overrideRefresh,
                refresh         = ToDto(c.refresh)
            };
        }

        private static ShopRefreshScheduleDto ToDto(ShopRefreshSchedule r)
        {
            r ??= new ShopRefreshSchedule();
            return new ShopRefreshScheduleDto
            {
                refreshType = (int)r.refreshType,
                timeType    = (int)r.timeType,
                timeZoneId  = r.timeZoneId,
                hour        = r.hour,
                minute      = r.minute,
                dayOfWeek   = r.dayOfWeek,
                dayOfMonth  = r.dayOfMonth
            };
        }

        #endregion

        #region 导入：DTO -> DB

        private static ShopTemplate FromDto(ShopTemplateDto dto, IAssetRefResolver resolver)
        {
            var t = new ShopTemplate
            {
                shopType           = (ShopType)dto.shopType,
                tradeInventoryRefs = FromDto(dto.tradeInventoryRefs),
                tradeTagRefs       = FromDto(dto.tradeTagRefs),
                filterTagRefs      = FromDto(dto.filterTagRefs),
                showAllFilterTab   = dto.showAllFilterTab,
                numberFormatRef    = dto.numberFormatRef,
                priceAttrSource    = dto.priceAttrSource
            };
            FillTemplate(t, dto, resolver);   // 名称 / 色点 / 属性字段
            FromDto(dto.sortPriorities,  t.sortPriorities);
            FromDto(dto.sortTiebreakers, t.sortTiebreakers);
            FromDto(dto.groups, t.groups);
            return t;
        }

        private static Shop FromDto(ShopDto dto, IAssetRefResolver resolver)
        {
            var s = new Shop(dto.id, dto.templateRef)
            {
                displayNameText = TextFromDto(dto.displayNameText, resolver),
                descriptionText = TextFromDto(dto.descriptionText, resolver),
                shopType           = (ShopType)dto.shopType,
                tradeInventoryRefs = FromDto(dto.tradeInventoryRefs),
                tradeTagRefs       = FromDto(dto.tradeTagRefs),
                filterTagRefs      = FromDto(dto.filterTagRefs),
                showAllFilterTab   = dto.showAllFilterTab,
                numberFormatRef    = dto.numberFormatRef,
                priceAttrSource    = dto.priceAttrSource
            };
            FromDto(dto.sortPriorities,  s.sortPriorities);
            FromDto(dto.sortTiebreakers, s.sortTiebreakers);
            FromDto(dto.groups, s.groups);
            FromDto(dto.values, s.values, resolver);
            return s;
        }

        /// <summary>DTO 数组 -> 商品组，追加进 <paramref name="dest"/>（null 视为空）。</summary>
        private static void FromDto(ShopCommodityGroupDto[] source, List<ShopCommodityGroup> dest)
        {
            if (source == null) return;
            foreach (var dto in source)
            {
                var g = new ShopCommodityGroup
                {
                    guid        = dto.guid,
                    name        = dto.name,
                    description = dto.description,
                    refresh     = FromDto(dto.refresh)
                };
                if (dto.commodities != null)
                    foreach (var c in dto.commodities)
                        g.commodities.Add(new ShopCommodity
                        {
                            guid            = c.guid,
                            itemId          = c.itemId,
                            count           = c.count,
                            priceMultiplier = c.priceMultiplier,
                            tradeLimit      = c.tradeLimit,
                            overrideRefresh = c.overrideRefresh,
                            refresh         = FromDto(c.refresh)
                        });
                dest.Add(g);
            }
        }

        private static ShopRefreshSchedule FromDto(ShopRefreshScheduleDto dto)
        {
            if (dto == null) return new ShopRefreshSchedule();
            return new ShopRefreshSchedule
            {
                refreshType = (ShopRefreshType)dto.refreshType,
                timeType    = (ShopTimeType)dto.timeType,
                timeZoneId  = dto.timeZoneId,
                hour        = dto.hour,
                minute      = dto.minute,
                dayOfWeek   = dto.dayOfWeek,
                dayOfMonth  = dto.dayOfMonth
            };
        }

        #endregion
    }
}

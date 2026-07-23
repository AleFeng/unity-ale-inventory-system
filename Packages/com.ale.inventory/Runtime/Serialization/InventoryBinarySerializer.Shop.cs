using System.IO;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryBinarySerializer"/> 的商店系统分部：商店模板 / 商店两个列表的二进制块读写
    /// （含商品组 / 商品 / 刷新计划；v6 起写出）。
    /// </summary>
    public static partial class InventoryBinarySerializer
    {
        #region 导出

        private static void WriteShopBlock(BinaryWriter w, InventoryDatabaseDto dto)
        {
            WriteArray(w, dto.shopTemplates, WriteShopTemplate);
            WriteArray(w, dto.shops, WriteShop);
        }

        private static void WriteShopTemplate(BinaryWriter w, ShopTemplateDto t)
        {
            WriteStr(w, t.name);
            WriteFloatArray(w, t.color);
            WriteArray(w, t.attributes, WriteDefinition);

            WriteShopConfig(w, t.shopType, t.tradeInventoryRefs, t.tradeTagRefs, t.filterTagRefs,
                t.showAllFilterTab, t.numberFormatRef, t.priceAttrSource,
                t.sortPriorities, t.sortTiebreakers, t.groups);
        }

        private static void WriteShop(BinaryWriter w, ShopDto s)
        {
            WriteStr(w, s.id);
            WriteStr(w, s.templateRef);
            WriteValue(w, s.displayNameText);
            WriteValue(w, s.descriptionText);

            WriteShopConfig(w, s.shopType, s.tradeInventoryRefs, s.tradeTagRefs, s.filterTagRefs,
                s.showAllFilterTab, s.numberFormatRef, s.priceAttrSource,
                s.sortPriorities, s.sortTiebreakers, s.groups);

            WriteEntries(w, s.values);
        }

        /// <summary>商店与商店模板共有的那套配置项（对应运行时的 <c>IShopConfig</c>），两处按同一顺序读写。</summary>
        private static void WriteShopConfig(BinaryWriter w, int shopType,
            string[] tradeInventoryRefs, string[] tradeTagRefs, string[] filterTagRefs,
            bool showAllFilterTab, string numberFormatRef, string priceAttrSource,
            SortPriorityDto[] sortPriorities, SortPriorityDto[] sortTiebreakers,
            ShopCommodityGroupDto[] groups)
        {
            w.Write(shopType);
            WriteStrArray(w, tradeInventoryRefs);
            WriteStrArray(w, tradeTagRefs);
            WriteStrArray(w, filterTagRefs);
            w.Write(showAllFilterTab);
            WriteStr(w, numberFormatRef);
            WriteStr(w, priceAttrSource);
            WriteSortPriorities(w, sortPriorities);
            WriteSortPriorities(w, sortTiebreakers);
            WriteArray(w, groups, WriteCommodityGroup);
        }

        private static void WriteCommodityGroup(BinaryWriter w, ShopCommodityGroupDto g)
        {
            WriteStr(w, g.guid);
            WriteStr(w, g.name);
            WriteStr(w, g.description);
            WriteRefresh(w, g.refresh);
            WriteArray(w, g.commodities, WriteCommodity);
        }

        private static void WriteCommodity(BinaryWriter w, ShopCommodityDto c)
        {
            WriteStr(w, c.guid);
            WriteStr(w, c.itemId);
            w.Write(c.count);
            w.Write(c.priceMultiplier);
            w.Write(c.tradeLimit);
            w.Write(c.overrideRefresh);
            WriteRefresh(w, c.refresh);
        }

        private static void WriteRefresh(BinaryWriter w, ShopRefreshScheduleDto r)
        {
            r ??= new ShopRefreshScheduleDto();
            w.Write(r.refreshType);
            w.Write(r.timeType);
            WriteStr(w, r.timeZoneId);
            w.Write(r.hour);
            w.Write(r.minute);
            w.Write(r.dayOfWeek);
            w.Write(r.dayOfMonth);
        }

        #endregion

        #region 导入

        private static void ReadShopBlock(BinaryReader r, InventoryDatabaseDto dto)
        {
            dto.shopTemplates = ReadArray(r, ReadShopTemplate);
            dto.shops         = ReadArray(r, ReadShop);
        }

        private static ShopTemplateDto ReadShopTemplate(BinaryReader r)
        {
            var t = new ShopTemplateDto
            {
                name       = ReadStr(r),
                color      = ReadFloatArray(r),
                attributes = ReadArray(r, ReadDefinition),

                shopType           = r.ReadInt32(),
                tradeInventoryRefs = ReadStrArray(r),
                tradeTagRefs       = ReadStrArray(r),
                filterTagRefs      = ReadStrArray(r),
                showAllFilterTab   = r.ReadBoolean(),
                numberFormatRef    = ReadStr(r),
                priceAttrSource    = ReadStr(r),
                sortPriorities     = ReadSortPriorities(r),
                sortTiebreakers    = ReadSortPriorities(r),
                groups             = ReadArray(r, ReadCommodityGroup)
            };
            return t;
        }

        private static ShopDto ReadShop(BinaryReader r)
        {
            return new ShopDto
            {
                id              = ReadStr(r),
                templateRef     = ReadStr(r),
                displayNameText = ReadValue(r),
                descriptionText = ReadValue(r),

                shopType           = r.ReadInt32(),
                tradeInventoryRefs = ReadStrArray(r),
                tradeTagRefs       = ReadStrArray(r),
                filterTagRefs      = ReadStrArray(r),
                showAllFilterTab   = r.ReadBoolean(),
                numberFormatRef    = ReadStr(r),
                priceAttrSource    = ReadStr(r),
                sortPriorities     = ReadSortPriorities(r),
                sortTiebreakers    = ReadSortPriorities(r),
                groups             = ReadArray(r, ReadCommodityGroup),

                values             = ReadEntries(r)
            };
        }

        private static ShopCommodityGroupDto ReadCommodityGroup(BinaryReader r)
        {
            return new ShopCommodityGroupDto
            {
                guid        = ReadStr(r),
                name        = ReadStr(r),
                description = ReadStr(r),
                refresh     = ReadRefresh(r),
                commodities = ReadArray(r, ReadCommodity)
            };
        }

        private static ShopCommodityDto ReadCommodity(BinaryReader r)
        {
            return new ShopCommodityDto
            {
                guid            = ReadStr(r),
                itemId          = ReadStr(r),
                count           = r.ReadInt32(),
                priceMultiplier = r.ReadSingle(),
                tradeLimit      = r.ReadInt32(),
                overrideRefresh = r.ReadBoolean(),
                refresh         = ReadRefresh(r)
            };
        }

        private static ShopRefreshScheduleDto ReadRefresh(BinaryReader r)
        {
            return new ShopRefreshScheduleDto
            {
                refreshType = r.ReadInt32(),
                timeType    = r.ReadInt32(),
                timeZoneId  = ReadStr(r),
                hour        = r.ReadInt32(),
                minute      = r.ReadInt32(),
                dayOfWeek   = r.ReadInt32(),
                dayOfMonth  = r.ReadInt32()
            };
        }

        #endregion
    }
}

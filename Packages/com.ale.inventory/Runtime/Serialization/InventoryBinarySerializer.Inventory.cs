using System.IO;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryBinarySerializer"/> 的仓库系统分部：仓库模板 / 仓库 / 整理选项 schema /
    /// 整理选项 / 数字格式配置五个列表的二进制块读写（v6 起写出）。
    /// </summary>
    public static partial class InventoryBinarySerializer
    {
        #region 导出

        private static void WriteInventoryBlock(BinaryWriter w, InventoryDatabaseDto dto)
        {
            WriteArray(w, dto.inventoryTemplates, WriteInventoryTemplate);
            WriteArray(w, dto.inventories, WriteInventory);
            WriteArray(w, dto.sortOptionAttributes, WriteDefinition);
            WriteArray(w, dto.sortOptions, WriteSortOption);
            WriteArray(w, dto.numberFormatConfigs, WriteNumberFormatConfig);
        }

        private static void WriteInventoryTemplate(BinaryWriter w, InventoryTemplateDto t)
        {
            WriteStr(w, t.name);
            WriteFloatArray(w, t.color);
            WriteArray(w, t.attributes, WriteDefinition);

            w.Write(t.capacity);
            w.Write(t.weightLimit);
            WriteStrArray(w, t.allowPutTagRefs);
            WriteStrArray(w, t.allowTakeTagRefs);
            WriteStrArray(w, t.allowOperateTagRefs);
            WriteStrArray(w, t.filterTagRefs);
            w.Write(t.showAllFilterTab);
            w.Write(t.autoSort);
            w.Write(t.dragSort);
            WriteStr(w, t.numberFormatRef);
            WriteSortPriorities(w, t.sortPriorities);
            WriteSortPriorities(w, t.sortTiebreakers);
        }

        private static void WriteInventory(BinaryWriter w, InventoryDto inv)
        {
            WriteStr(w, inv.id);
            WriteStr(w, inv.templateRef);
            WriteValue(w, inv.displayNameText);
            WriteValue(w, inv.descriptionText);

            w.Write(inv.capacity);
            w.Write(inv.weightLimit);
            WriteStrArray(w, inv.allowPutTagRefs);
            WriteStrArray(w, inv.allowTakeTagRefs);
            WriteStrArray(w, inv.allowOperateTagRefs);
            WriteStrArray(w, inv.filterTagRefs);
            w.Write(inv.showAllFilterTab);
            w.Write(inv.autoSort);
            w.Write(inv.dragSort);
            WriteStr(w, inv.numberFormatRef);
            WriteSortPriorities(w, inv.sortPriorities);
            WriteSortPriorities(w, inv.sortTiebreakers);
            WriteEntries(w, inv.values);
        }

        private static void WriteSortOption(BinaryWriter w, SortOptionDto so)
        {
            WriteStr(w, so.field);
            WriteValue(w, so.displayName);
            WriteStrArray(w, so.ignoreIds);
            WriteEntries(w, so.attributeValues);
        }

        private static void WriteNumberFormatConfig(BinaryWriter w, NumberFormatConfigDto c)
        {
            WriteStr(w, c.name);
            WriteArray(w, c.locales, (bw, loc) =>
            {
                WriteStr(bw, loc.languageCode);
                WriteArray(bw, loc.rules, (bw2, rule) =>
                {
                    bw2.Write(rule.threshold);
                    bw2.Write(rule.divisor);
                    WriteValue(bw2, rule.suffixText);
                    bw2.Write(rule.decimalPlaces);
                });
            });
        }

        #endregion

        #region 导入

        private static void ReadInventoryBlock(BinaryReader r, InventoryDatabaseDto dto)
        {
            dto.inventoryTemplates   = ReadArray(r, ReadInventoryTemplate);
            dto.inventories          = ReadArray(r, ReadInventory);
            dto.sortOptionAttributes = ReadArray(r, ReadDefinition);
            dto.sortOptions          = ReadArray(r, ReadSortOption);
            dto.numberFormatConfigs  = ReadArray(r, ReadNumberFormatConfig);
        }

        private static InventoryTemplateDto ReadInventoryTemplate(BinaryReader r)
        {
            return new InventoryTemplateDto
            {
                name       = ReadStr(r),
                color      = ReadFloatArray(r),
                attributes = ReadArray(r, ReadDefinition),

                capacity            = r.ReadInt32(),
                weightLimit         = r.ReadSingle(),
                allowPutTagRefs     = ReadStrArray(r),
                allowTakeTagRefs    = ReadStrArray(r),
                allowOperateTagRefs = ReadStrArray(r),
                filterTagRefs       = ReadStrArray(r),
                showAllFilterTab    = r.ReadBoolean(),
                autoSort            = r.ReadBoolean(),
                dragSort            = r.ReadBoolean(),
                numberFormatRef     = ReadStr(r),
                sortPriorities      = ReadSortPriorities(r),
                sortTiebreakers     = ReadSortPriorities(r)
            };
        }

        private static InventoryDto ReadInventory(BinaryReader r)
        {
            return new InventoryDto
            {
                id              = ReadStr(r),
                templateRef     = ReadStr(r),
                displayNameText = ReadValue(r),
                descriptionText = ReadValue(r),

                capacity            = r.ReadInt32(),
                weightLimit         = r.ReadSingle(),
                allowPutTagRefs     = ReadStrArray(r),
                allowTakeTagRefs    = ReadStrArray(r),
                allowOperateTagRefs = ReadStrArray(r),
                filterTagRefs       = ReadStrArray(r),
                showAllFilterTab    = r.ReadBoolean(),
                autoSort            = r.ReadBoolean(),
                dragSort            = r.ReadBoolean(),
                numberFormatRef     = ReadStr(r),
                sortPriorities      = ReadSortPriorities(r),
                sortTiebreakers     = ReadSortPriorities(r),
                values              = ReadEntries(r)
            };
        }

        private static SortOptionDto ReadSortOption(BinaryReader r)
        {
            return new SortOptionDto
            {
                field           = ReadStr(r),
                displayName     = ReadValue(r),
                ignoreIds       = ReadStrArray(r),
                attributeValues = ReadEntries(r)
            };
        }

        private static NumberFormatConfigDto ReadNumberFormatConfig(BinaryReader r)
        {
            return new NumberFormatConfigDto
            {
                name = ReadStr(r),
                locales = ReadArray(r, br => new NumberFormatLocaleDto
                {
                    languageCode = ReadStr(br),
                    rules = ReadArray(br, br2 => new NumberFormatRuleDto
                    {
                        threshold     = br2.ReadInt64(),
                        divisor       = br2.ReadDouble(),
                        suffixText    = ReadValue(br2),
                        decimalPlaces = br2.ReadInt32()
                    })
                })
            };
        }

        #endregion
    }
}

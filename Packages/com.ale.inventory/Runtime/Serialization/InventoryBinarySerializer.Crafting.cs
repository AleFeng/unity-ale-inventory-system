using System.IO;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryBinarySerializer"/> 的制作系统分部：分组标签 / 蓝图模板 / 蓝图
    /// 三个列表的二进制块读写（v6 起写出）。
    /// </summary>
    public static partial class InventoryBinarySerializer
    {
        #region 导出

        private static void WriteCraftingBlock(BinaryWriter w, InventoryDatabaseDto dto)
        {
            WriteArray(w, dto.craftingGroupTags, WriteGroupTag);
            WriteArray(w, dto.craftingBlueprintTemplates, WriteBlueprintTemplate);
            WriteArray(w, dto.craftingBlueprints, WriteBlueprint);
        }

        private static void WriteBlueprintTemplate(BinaryWriter w, CraftingBlueprintTemplateDto t)
        {
            WriteStr(w, t.name);
            WriteFloatArray(w, t.color);
            WriteArray(w, t.attributes, WriteDefinition);

            w.Write(t.craftTime);
            w.Write(t.maxCraftCount);
            WriteStrArray(w, t.craftInventoryRefs);
            WriteStr(w, t.numberFormatRef);
            WriteSortPriorities(w, t.sortPriorities);
            WriteSortPriorities(w, t.sortTiebreakers);
            WriteArray(w, t.attributeDisplays, WriteCraftingDisplay);
        }

        private static void WriteBlueprint(BinaryWriter w, CraftingBlueprintDto b)
        {
            WriteStr(w, b.id);
            WriteStr(w, b.templateRef);
            WriteValue(w, b.displayText);
            WriteValue(w, b.descriptionText);

            WriteStr(w, b.primaryGroupTag);
            WriteStrArray(w, b.secondaryGroupTags);
            WriteArray(w, b.outputs, WriteItemAmount);
            WriteArray(w, b.inputs,  WriteItemAmount);
            w.Write(b.craftTime);
            w.Write(b.maxCraftCount);
            WriteStrArray(w, b.craftInventoryRefs);
            WriteStr(w, b.numberFormatRef);
            WriteArray(w, b.attributeDisplays, WriteCraftingDisplay);
            WriteEntries(w, b.values);
        }

        private static void WriteItemAmount(BinaryWriter w, CraftingItemAmountDto a)
        {
            WriteStr(w, a.itemId);
            w.Write(a.count);
        }

        private static void WriteCraftingDisplay(BinaryWriter w, CraftingAttributeDisplayDto ad)
        {
            WriteStr(w, ad.label);
            WriteStr(w, ad.attrId);
        }

        #endregion

        #region 导入

        private static void ReadCraftingBlock(BinaryReader r, InventoryDatabaseDto dto)
        {
            dto.craftingGroupTags          = ReadArray(r, ReadGroupTag);
            dto.craftingBlueprintTemplates = ReadArray(r, ReadBlueprintTemplate);
            dto.craftingBlueprints         = ReadArray(r, ReadBlueprint);
        }

        private static CraftingBlueprintTemplateDto ReadBlueprintTemplate(BinaryReader r)
        {
            return new CraftingBlueprintTemplateDto
            {
                name       = ReadStr(r),
                color      = ReadFloatArray(r),
                attributes = ReadArray(r, ReadDefinition),

                craftTime          = r.ReadSingle(),
                maxCraftCount      = r.ReadInt32(),
                craftInventoryRefs = ReadStrArray(r),
                numberFormatRef    = ReadStr(r),
                sortPriorities     = ReadSortPriorities(r),
                sortTiebreakers    = ReadSortPriorities(r),
                attributeDisplays  = ReadArray(r, ReadCraftingDisplay)
            };
        }

        private static CraftingBlueprintDto ReadBlueprint(BinaryReader r)
        {
            return new CraftingBlueprintDto
            {
                id              = ReadStr(r),
                templateRef     = ReadStr(r),
                displayText     = ReadValue(r),
                descriptionText = ReadValue(r),

                primaryGroupTag    = ReadStr(r),
                secondaryGroupTags = ReadStrArray(r),
                outputs            = ReadArray(r, ReadItemAmount),
                inputs             = ReadArray(r, ReadItemAmount),
                craftTime          = r.ReadSingle(),
                maxCraftCount      = r.ReadInt32(),
                craftInventoryRefs = ReadStrArray(r),
                numberFormatRef    = ReadStr(r),
                attributeDisplays  = ReadArray(r, ReadCraftingDisplay),
                values             = ReadEntries(r)
            };
        }

        private static CraftingItemAmountDto ReadItemAmount(BinaryReader r)
            => new CraftingItemAmountDto { itemId = ReadStr(r), count = r.ReadInt32() };

        private static CraftingAttributeDisplayDto ReadCraftingDisplay(BinaryReader r)
            => new CraftingAttributeDisplayDto { label = ReadStr(r), attrId = ReadStr(r) };

        #endregion
    }
}

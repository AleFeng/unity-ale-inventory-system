using System.IO;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryBinarySerializer"/> 的装备系统分部：分组标签 / 装备组模板 / 装备组
    /// 三个列表的二进制块读写（含嵌套三层的槽位结构；v6 起写出）。
    /// </summary>
    public static partial class InventoryBinarySerializer
    {
        #region 导出

        private static void WriteEquipmentBlock(BinaryWriter w, InventoryDatabaseDto dto)
        {
            WriteArray(w, dto.equipmentGroupTags, WriteGroupTag);
            WriteArray(w, dto.equipmentGroupTemplates, WriteEquipmentGroupTemplate);
            WriteArray(w, dto.equipmentGroups, WriteEquipmentGroup);
        }

        private static void WriteEquipmentGroupTemplate(BinaryWriter w, EquipmentGroupTemplateDto t)
        {
            WriteStr(w, t.name);
            WriteFloatArray(w, t.color);
            WriteArray(w, t.attributes, WriteDefinition);

            WriteStrArray(w, t.equipmentInventoryRefs);
            WriteArray(w, t.slotLists, WriteSlotList);
            WriteArray(w, t.attributeDisplays, WriteEquipmentDisplay);
            WriteSortPriorities(w, t.sortPriorities);
            WriteSortPriorities(w, t.sortTiebreakers);
        }

        private static void WriteEquipmentGroup(BinaryWriter w, EquipmentGroupDto g)
        {
            WriteStr(w, g.id);
            WriteStr(w, g.templateRef);
            WriteValue(w, g.displayNameText);
            WriteValue(w, g.descriptionText);

            WriteStrArray(w, g.equipmentInventoryRefs);
            WriteArray(w, g.slotLists, WriteSlotList);
            WriteArray(w, g.attributeDisplays, WriteEquipmentDisplay);
            WriteSortPriorities(w, g.sortPriorities);
            WriteSortPriorities(w, g.sortTiebreakers);
            WriteEntries(w, g.values);
        }

        private static void WriteSlotList(BinaryWriter w, EquipmentSlotListDto sl)
        {
            WriteStr(w, sl.id);
            WriteStr(w, sl.displayName);
            WriteStr(w, sl.description);
            WriteStrArray(w, sl.requiredTags);
            WriteArray(w, sl.enumConstraints, (bw, c) =>
            {
                WriteStr(bw, c.enumTypeRef);
                WriteIntArray(bw, c.allowedValues);
            });
            WriteArray(w, sl.slots, (bw, s) =>
            {
                WriteStr(bw, s.id);
                WriteStr(bw, s.displayName);
                WriteArray(bw, s.filters, (bw2, f) =>
                {
                    WriteStr(bw2, f.attrId);
                    WriteValue(bw2, f.value);
                });
            });
        }

        private static void WriteEquipmentDisplay(BinaryWriter w, EquipmentAttributeDisplayDto ad)
        {
            WriteStr(w, ad.attrId);
            WriteStr(w, ad.groupTag);
            WriteValue(w, ad.label);
            WriteStr(w, ad.enumLabelAttrId);
        }

        #endregion

        #region 导入

        private static void ReadEquipmentBlock(BinaryReader r, InventoryDatabaseDto dto)
        {
            dto.equipmentGroupTags      = ReadArray(r, ReadGroupTag);
            dto.equipmentGroupTemplates = ReadArray(r, ReadEquipmentGroupTemplate);
            dto.equipmentGroups         = ReadArray(r, ReadEquipmentGroup);
        }

        private static EquipmentGroupTemplateDto ReadEquipmentGroupTemplate(BinaryReader r)
        {
            return new EquipmentGroupTemplateDto
            {
                name       = ReadStr(r),
                color      = ReadFloatArray(r),
                attributes = ReadArray(r, ReadDefinition),

                equipmentInventoryRefs = ReadStrArray(r),
                slotLists              = ReadArray(r, ReadSlotList),
                attributeDisplays      = ReadArray(r, ReadEquipmentDisplay),
                sortPriorities         = ReadSortPriorities(r),
                sortTiebreakers        = ReadSortPriorities(r)
            };
        }

        private static EquipmentGroupDto ReadEquipmentGroup(BinaryReader r)
        {
            return new EquipmentGroupDto
            {
                id              = ReadStr(r),
                templateRef     = ReadStr(r),
                displayNameText = ReadValue(r),
                descriptionText = ReadValue(r),

                equipmentInventoryRefs = ReadStrArray(r),
                slotLists              = ReadArray(r, ReadSlotList),
                attributeDisplays      = ReadArray(r, ReadEquipmentDisplay),
                sortPriorities         = ReadSortPriorities(r),
                sortTiebreakers        = ReadSortPriorities(r),
                values                 = ReadEntries(r)
            };
        }

        private static EquipmentSlotListDto ReadSlotList(BinaryReader r)
        {
            return new EquipmentSlotListDto
            {
                id           = ReadStr(r),
                displayName  = ReadStr(r),
                description  = ReadStr(r),
                requiredTags = ReadStrArray(r),
                enumConstraints = ReadArray(r, br => new EquipmentEnumConstraintDto
                {
                    enumTypeRef   = ReadStr(br),
                    allowedValues = ReadIntArray(br)
                }),
                slots = ReadArray(r, br => new EquipmentSlotDto
                {
                    id          = ReadStr(br),
                    displayName = ReadStr(br),
                    filters     = ReadArray(br, br2 => new EquipmentSlotFilterDto
                    {
                        attrId = ReadStr(br2),
                        value  = ReadValue(br2)
                    })
                })
            };
        }

        private static EquipmentAttributeDisplayDto ReadEquipmentDisplay(BinaryReader r)
        {
            return new EquipmentAttributeDisplayDto
            {
                attrId          = ReadStr(r),
                groupTag        = ReadStr(r),
                label           = ReadValue(r),
                enumLabelAttrId = ReadStr(r)
            };
        }

        #endregion
    }
}

using System.IO;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryBinarySerializer"/> 的技能系统分部：分组标签 / 技能模板 / 技能
    /// 三个列表的二进制块读写（v6 起写出）。
    /// </summary>
    public static partial class InventoryBinarySerializer
    {
        #region 导出

        private static void WriteSkillBlock(BinaryWriter w, InventoryDatabaseDto dto)
        {
            WriteArray(w, dto.skillGroupTags, WriteGroupTag);
            WriteArray(w, dto.skillTemplates, WriteSkillTemplate);
            WriteArray(w, dto.skills, WriteSkill);
        }

        private static void WriteSkillTemplate(BinaryWriter w, SkillTemplateDto t)
        {
            WriteStr(w, t.name);
            WriteFloatArray(w, t.color);
            WriteArray(w, t.attributes, WriteDefinition);

            WriteValue(w, t.displayText);
            WriteValue(w, t.descriptionText);
            WriteStr(w, t.iconGuid);
            WriteStr(w, t.primaryGroupTag);
            WriteStrArray(w, t.secondaryGroupTags);
        }

        private static void WriteSkill(BinaryWriter w, SkillDto s)
        {
            WriteStr(w, s.id);
            WriteStr(w, s.templateRef);
            WriteValue(w, s.displayText);
            WriteValue(w, s.descriptionText);
            WriteStr(w, s.iconGuid);
            WriteStr(w, s.primaryGroupTag);
            WriteStrArray(w, s.secondaryGroupTags);
            WriteEntries(w, s.values);
        }

        #endregion

        #region 导入

        private static void ReadSkillBlock(BinaryReader r, InventoryDatabaseDto dto)
        {
            dto.skillGroupTags = ReadArray(r, ReadGroupTag);
            dto.skillTemplates = ReadArray(r, ReadSkillTemplate);
            dto.skills         = ReadArray(r, ReadSkill);
        }

        private static SkillTemplateDto ReadSkillTemplate(BinaryReader r)
        {
            return new SkillTemplateDto
            {
                name       = ReadStr(r),
                color      = ReadFloatArray(r),
                attributes = ReadArray(r, ReadDefinition),

                displayText        = ReadValue(r),
                descriptionText    = ReadValue(r),
                iconGuid           = ReadStr(r),
                primaryGroupTag    = ReadStr(r),
                secondaryGroupTags = ReadStrArray(r)
            };
        }

        private static SkillDto ReadSkill(BinaryReader r)
        {
            return new SkillDto
            {
                id              = ReadStr(r),
                templateRef     = ReadStr(r),
                displayText     = ReadValue(r),
                descriptionText = ReadValue(r),
                iconGuid        = ReadStr(r),
                primaryGroupTag = ReadStr(r),
                secondaryGroupTags = ReadStrArray(r),
                values             = ReadEntries(r)
            };
        }

        #endregion
    }
}

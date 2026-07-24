using UnityEngine;
using Ale.Toolkit.Runtime;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryDtoMapper"/> 的技能系统分部：技能模板 / 技能。
    /// 分组标签走共用的 <see cref="GroupTagDto"/>（见核心分部）。自 v6 起纳入 JSON / 二进制导出
    /// （含图标——按与属性值对象槽相同的约定转 GUID / Addressable 地址）。
    /// </summary>
    public static partial class InventoryDtoMapper
    {
        #region 导出：DB -> DTO

        private static SkillTemplateDto ToDto(SkillTemplate t, IAssetRefResolver resolver)
        {
            var dto = new SkillTemplateDto
            {
                displayText     = ToDto(t.displayText, resolver),
                descriptionText = ToDto(t.descriptionText, resolver),
                iconGuid        = ObjToGuid(t.icon, t.iconAddress, resolver),
                primaryGroupTag    = t.primaryGroupTag,
                secondaryGroupTags = ToArray(t.secondaryGroupTags)
            };
            FillTemplateDto(dto, t, resolver);   // 名称 / 色点 / 属性字段
            return dto;
        }

        private static SkillDto ToDto(Skill s, IAssetRefResolver resolver)
        {
            return new SkillDto
            {
                id              = s.id,
                templateRef     = s.templateRef,
                displayText     = ToDto(s.displayText, resolver),
                descriptionText = ToDto(s.descriptionText, resolver),
                iconGuid        = ObjToGuid(s.icon, s.iconAddress, resolver),
                primaryGroupTag    = s.primaryGroupTag,
                secondaryGroupTags = ToArray(s.secondaryGroupTags),
                values             = ToDto(s.values, resolver)
            };
        }

        #endregion

        #region 导入：DTO -> DB

        private static SkillTemplate FromDto(SkillTemplateDto dto, IAssetRefResolver resolver)
        {
            var t = new SkillTemplate
            {
                displayText     = TextFromDto(dto.displayText, resolver),
                descriptionText = TextFromDto(dto.descriptionText, resolver),
                icon            = resolver.FromGuid(dto.iconGuid) as Sprite,
                iconAddress     = dto.iconGuid,
                primaryGroupTag    = dto.primaryGroupTag,
                secondaryGroupTags = FromDto(dto.secondaryGroupTags)
            };
            FillTemplate(t, dto, resolver);   // 名称 / 色点 / 属性字段
            return t;
        }

        private static Skill FromDto(SkillDto dto, IAssetRefResolver resolver)
        {
            var s = new Skill(dto.id, dto.templateRef)
            {
                displayText     = TextFromDto(dto.displayText, resolver),
                descriptionText = TextFromDto(dto.descriptionText, resolver),
                icon            = resolver.FromGuid(dto.iconGuid) as Sprite,
                iconAddress     = dto.iconGuid,
                primaryGroupTag    = dto.primaryGroupTag,
                secondaryGroupTags = FromDto(dto.secondaryGroupTags)
            };
            FromDto(dto.values, s.values, resolver);
            return s;
        }

        #endregion
    }
}

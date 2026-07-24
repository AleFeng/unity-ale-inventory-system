using System.Collections.Generic;
using Ale.Toolkit.Runtime;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// <see cref="InventoryDtoMapper"/> 的装备系统分部：装备组模板 / 装备组，
    /// 含嵌套三层的槽位结构（槽位列表 → 装备槽 → 槽级过滤条件）与装备属性字段显示配置。
    /// 分组标签走共用的 <see cref="GroupTagDto"/>（见核心分部）。自 v6 起纳入 JSON / 二进制导出。
    /// </summary>
    public static partial class InventoryDtoMapper
    {
        #region 导出：DB -> DTO

        private static EquipmentGroupTemplateDto ToDto(EquipmentGroupTemplate t, IAssetRefResolver resolver)
        {
            var dto = new EquipmentGroupTemplateDto
            {
                equipmentInventoryRefs = ToArray(t.equipmentInventoryRefs),
                slotLists              = ToArray(t.slotLists, sl => ToDto(sl, resolver)),
                attributeDisplays      = ToArray(t.attributeDisplays, ad => ToDto(ad, resolver)),
                sortPriorities         = ToDto(t.sortPriorities),
                sortTiebreakers        = ToDto(t.sortTiebreakers)
            };
            FillTemplateDto(dto, t, resolver);   // 名称 / 色点 / 属性字段
            return dto;
        }

        private static EquipmentGroupDto ToDto(EquipmentGroup g, IAssetRefResolver resolver)
        {
            return new EquipmentGroupDto
            {
                id              = g.id,
                templateRef     = g.templateRef,
                displayNameText = ToDto(g.displayNameText, resolver),
                descriptionText = ToDto(g.descriptionText, resolver),
                equipmentInventoryRefs = ToArray(g.equipmentInventoryRefs),
                slotLists              = ToArray(g.slotLists, sl => ToDto(sl, resolver)),
                attributeDisplays      = ToArray(g.attributeDisplays, ad => ToDto(ad, resolver)),
                sortPriorities         = ToDto(g.sortPriorities),
                sortTiebreakers        = ToDto(g.sortTiebreakers),
                values                 = ToDto(g.values, resolver)
            };
        }

        private static EquipmentSlotListDto ToDto(EquipmentSlotList sl, IAssetRefResolver resolver)
        {
            return new EquipmentSlotListDto
            {
                id           = sl.id,
                displayName  = sl.displayName,
                description  = sl.description,
                requiredTags = ToArray(sl.requiredTags),
                enumConstraints = ToArray(sl.enumConstraints, c => new EquipmentEnumConstraintDto
                {
                    enumTypeRef   = c.enumTypeRef,
                    allowedValues = c.allowedValues != null ? c.allowedValues.ToArray() : System.Array.Empty<int>()
                }),
                slots = ToArray(sl.slots, s => new EquipmentSlotDto
                {
                    id          = s.id,
                    displayName = s.displayName,
                    filters     = ToArray(s.filters, f => new EquipmentSlotFilterDto
                    {
                        attrId = f.attrId,
                        value  = ToDto(f.value, resolver)
                    })
                })
            };
        }

        private static EquipmentAttributeDisplayDto ToDto(EquipmentAttributeDisplay ad, IAssetRefResolver resolver)
        {
            return new EquipmentAttributeDisplayDto
            {
                attrId          = ad.attrId,
                groupTag        = ad.groupTag,
                label           = ToDto(ad.label, resolver),
                enumLabelAttrId = ad.enumLabelAttrId
            };
        }

        #endregion

        #region 导入：DTO -> DB

        private static EquipmentGroupTemplate FromDto(EquipmentGroupTemplateDto dto, IAssetRefResolver resolver)
        {
            var t = new EquipmentGroupTemplate
            {
                equipmentInventoryRefs = FromDto(dto.equipmentInventoryRefs)
            };
            FillTemplate(t, dto, resolver);   // 名称 / 色点 / 属性字段
            FromDto(dto.slotLists, t.slotLists, resolver);
            FromDto(dto.attributeDisplays, t.attributeDisplays, resolver);
            FromDto(dto.sortPriorities,  t.sortPriorities);
            FromDto(dto.sortTiebreakers, t.sortTiebreakers);
            return t;
        }

        private static EquipmentGroup FromDto(EquipmentGroupDto dto, IAssetRefResolver resolver)
        {
            var g = new EquipmentGroup(dto.id, dto.templateRef)
            {
                displayNameText = TextFromDto(dto.displayNameText, resolver),
                descriptionText = TextFromDto(dto.descriptionText, resolver),
                equipmentInventoryRefs = FromDto(dto.equipmentInventoryRefs)
            };
            FromDto(dto.slotLists, g.slotLists, resolver);
            FromDto(dto.attributeDisplays, g.attributeDisplays, resolver);
            FromDto(dto.sortPriorities,  g.sortPriorities);
            FromDto(dto.sortTiebreakers, g.sortTiebreakers);
            FromDto(dto.values, g.values, resolver);
            return g;
        }

        /// <summary>DTO 数组 -> 槽位列表（含装备槽与槽级过滤条件），追加进 <paramref name="dest"/>。</summary>
        private static void FromDto(EquipmentSlotListDto[] source, List<EquipmentSlotList> dest, IAssetRefResolver resolver)
        {
            if (source == null) return;
            foreach (var dto in source)
            {
                var sl = new EquipmentSlotList(dto.id, dto.displayName)
                {
                    description  = dto.description,
                    requiredTags = FromDto(dto.requiredTags)
                };
                if (dto.enumConstraints != null)
                    foreach (var c in dto.enumConstraints)
                        sl.enumConstraints.Add(new EquipmentEnumConstraint(c.enumTypeRef)
                        {
                            allowedValues = c.allowedValues != null ? new List<int>(c.allowedValues) : new List<int>()
                        });
                if (dto.slots != null)
                    foreach (var s in dto.slots)
                    {
                        var slot = new EquipmentSlot(s.id, s.displayName);
                        if (s.filters != null)
                            foreach (var f in s.filters)
                                slot.filters.Add(new EquipmentSlotFilter(f.attrId, FromDto(f.value, resolver)));
                        sl.slots.Add(slot);
                    }
                dest.Add(sl);
            }
        }

        /// <summary>DTO 数组 -> 装备属性字段显示配置，追加进 <paramref name="dest"/>。</summary>
        private static void FromDto(EquipmentAttributeDisplayDto[] source, List<EquipmentAttributeDisplay> dest,
            IAssetRefResolver resolver)
        {
            if (source == null) return;
            foreach (var dto in source)
                dest.Add(new EquipmentAttributeDisplay(dto.attrId, dto.groupTag, null, dto.enumLabelAttrId)
                {
                    label = TextFromDto(dto.label, resolver)
                });
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 装备组模板。作为创建新装备组的蓝本，承载一整套装备组可配置项的默认值
    /// （槽位列表 / 装备槽 / 道具限制 / 装备属性字段）以及自定义属性字段定义（schema），同时用于分类筛选。
    /// 与 <see cref="EquipmentGroup"/> 共享 <see cref="IEquipmentConfig"/>，使两者配置项一致、编辑器复用同一套绘制。
    /// 从模板创建装备组时会深拷贝这些配置项（见 EquipmentListPanel.AddFromTemplate）。
    /// </summary>
    [Serializable]
    public class EquipmentGroupTemplate : IEquipmentConfig
    {
        /// <summary>模板名称。</summary>
        public string name;

        /// <summary>模板标识颜色（用于列表中的圆形色点，便于快速区分来源）。</summary>
        public Color color = Color.gray;

        /// <summary>模板所定义的自定义属性字段（装备组据此协调其属性值集合）。</summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        // ── 装备组可配置项（默认值，创建装备组时复制）────────────────────────────────
        /// <summary>装备仓库（装备系统 / 装备 UI 可直接交互的玩家仓库 ID 列表；卸下时从 Index0 起找第一个放得下的仓库）。</summary>
        public List<string> equipmentInventoryRefs = new List<string>();

        /// <summary>槽位列表（每个槽位列表含多个装备槽与道具限制）。</summary>
        public List<EquipmentSlotList> slotLists = new List<EquipmentSlotList>();

        /// <summary>装备属性字段列表（指定哪些道具属性作为装备组的总属性加成）。</summary>
        public List<EquipmentAttributeDisplay> attributeDisplays = new List<EquipmentAttributeDisplay>();

        // ── 整理排序（创建装备组时复制为默认值；装备组可独立编辑。应用于候选列表 UiwEquipmentCandidateList 的显示排序）──
        /// <summary>排序条件（候选道具列表按此排序；本列表有排序栏时玩家可选，否则以首条为默认排序）。</summary>
        public List<SortPriority> sortPriorities = new List<SortPriority>();

        /// <summary>整理优先级（排序条件值相同时，依次按此列表比较直至值不同）。</summary>
        public List<SortPriority> sortTiebreakers = new List<SortPriority>();

        // ── IEquipmentConfig（映射到上述序列化字段，供编辑器共享绘制）────────────────
        List<string> IEquipmentConfig.EquipmentInventoryRefs => equipmentInventoryRefs;
        List<EquipmentSlotList> IEquipmentConfig.SlotLists => slotLists;
        List<EquipmentAttributeDisplay> IEquipmentConfig.AttributeDisplays => attributeDisplays;
        List<SortPriority> IEquipmentConfig.SortPriorities => sortPriorities;
        List<SortPriority> IEquipmentConfig.SortTiebreakers => sortTiebreakers;

        public EquipmentGroupTemplate()
        {
        }

        public EquipmentGroupTemplate(string nameArg)
        {
            name = nameArg;
        }

        /// <summary>深拷贝。</summary>
        public EquipmentGroupTemplate Clone()
        {
            var clone = new EquipmentGroupTemplate(name) { color = color };
            clone.equipmentInventoryRefs = new List<string>(equipmentInventoryRefs);
            foreach (var attr in attributes)        clone.attributes.Add(attr.Clone());
            foreach (var sl in slotLists)           clone.slotLists.Add(sl.Clone());
            foreach (var ad in attributeDisplays)   clone.attributeDisplays.Add(ad.Clone());
            foreach (var sp in sortPriorities)      clone.sortPriorities.Add(sp.Clone());
            foreach (var sp in sortTiebreakers)     clone.sortTiebreakers.Add(sp.Clone());
            return clone;
        }
    }
}

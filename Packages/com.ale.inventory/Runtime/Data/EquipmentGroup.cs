using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 装备组。描述一整套装备的槽位结构（例如一个角色身上穿的整组装备的槽位结构）。
    /// 携带唯一 <see cref="id"/>、来源模板引用、若干槽位列表（每个含多个装备槽与道具限制）、
    /// 装备属性字段列表（指定哪些道具属性作为装备组总属性加成），以及来自模板的自定义属性值。
    ///
    /// <para>装备组是配置目录；运行时的装备/卸下行为与属性加成汇总由后续阶段的 EquipmentRuntimeManager 执行。</para>
    ///
    /// <para>继承 <see cref="AttributeOwner"/> 以复用带缓存的 O(1) <see cref="AttributeOwner.GetEntry"/>
    /// 与 <see cref="AttributeOwner.GetAttributeValue{T}"/> / <see cref="AttributeOwner.SetAttributeValue{T}"/>。</para>
    /// </summary>
    [Serializable]
    public class EquipmentGroup : AttributeOwner, IEquipmentConfig
    {
        /// <summary>唯一标识。</summary>
        public string id;
        
        /// <summary>显示名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；为空时回退 <see cref="id"/>）。</summary>
        public AttributeValue displayNameText = new AttributeValue(EFieldType.Text);

        /// <summary>描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；正式游戏配置数据，UI 显示为「描述」）。</summary>
        public AttributeValue descriptionText = new AttributeValue(EFieldType.Text);

        /// <summary>来源装备组模板名称（可为空）。</summary>
        public string templateRef;

        /// <summary>装备仓库（装备系统 / 装备 UI 可直接交互的玩家仓库 ID 列表；卸下时从 Index0 起找第一个放得下的仓库）。</summary>
        public List<string> equipmentInventoryRefs = new List<string>();

        /// <summary>槽位列表（每个槽位列表含多个装备槽与道具限制）。</summary>
        public List<EquipmentSlotList> slotLists = new List<EquipmentSlotList>();

        /// <summary>装备属性字段列表（指定哪些道具属性作为装备组的总属性加成，UI 上按分组标签分组显示）。</summary>
        public List<EquipmentAttributeDisplay> attributeDisplays = new List<EquipmentAttributeDisplay>();

        /// <summary>整理排序 - 排序条件（应用于可装备道具候选列表 UiwEquipmentCandidateList 的显示排序）。</summary>
        public List<SortPriority> sortPriorities = new List<SortPriority>();

        /// <summary>整理排序 - 整理优先级（排序条件值相同时，依次按此列表比较直至值不同）。</summary>
        public List<SortPriority> sortTiebreakers = new List<SortPriority>();

        /// <summary>来自模板的自定义属性值。</summary>
        public List<AttributeEntry> values = new List<AttributeEntry>();

        // 实现基类 AttributeOwner 的抽象属性，将 values 列表暴露给基类的懒加载字典缓存。
        protected override List<AttributeEntry> AttributeEntries => values;

        // ── IEquipmentConfig（映射到上述序列化字段，供编辑器共享绘制）────────────────
        List<string> IEquipmentConfig.EquipmentInventoryRefs => equipmentInventoryRefs;
        List<EquipmentSlotList> IEquipmentConfig.SlotLists => slotLists;
        List<EquipmentAttributeDisplay> IEquipmentConfig.AttributeDisplays => attributeDisplays;
        List<SortPriority> IEquipmentConfig.SortPriorities => sortPriorities;
        List<SortPriority> IEquipmentConfig.SortTiebreakers => sortTiebreakers;

        public EquipmentGroup()
        {
        }

        public EquipmentGroup(string newId, string newTemplateRef = null)
        {
            id          = newId;
            templateRef = newTemplateRef;
        }

        /// <summary>
        /// 根据当前模板协调自定义属性值集合：
        /// 为模板新增字段追加默认值条目；移除模板已不存在的字段条目；已存在字段保留现有值
        /// （类型 / 数组形态 / 枚举类型引用变化时重置为新类型默认值）。
        /// </summary>
        public void RebuildAttributes(InventoryDatabase db)
        {
            if (!db) return;

            var template = db.GetEquipmentGroupTemplate(templateRef);
            AttributeSync.Sync(values, template != null ? template.attributes : null);

            // values 已完整重建，使缓存失效；下次 GetEntry 调用时将从最终状态重建字典。
            InvalidateEntryCache();
        }

        /// <summary>深拷贝。</summary>
        public EquipmentGroup Clone()
        {
            var clone = new EquipmentGroup(id, templateRef);
            clone.displayNameText = displayNameText != null ? displayNameText.Clone() : new AttributeValue(EFieldType.Text);
            clone.descriptionText = descriptionText != null ? descriptionText.Clone() : new AttributeValue(EFieldType.Text);
            clone.equipmentInventoryRefs = new List<string>(equipmentInventoryRefs);
            foreach (var sl in slotLists)         clone.slotLists.Add(sl.Clone());
            foreach (var ad in attributeDisplays) clone.attributeDisplays.Add(ad.Clone());
            foreach (var sp in sortPriorities)    clone.sortPriorities.Add(sp.Clone());
            foreach (var sp in sortTiebreakers)   clone.sortTiebreakers.Add(sp.Clone());
            foreach (var e in values)             clone.values.Add(e.Clone());
            return clone;
        }
    }
}

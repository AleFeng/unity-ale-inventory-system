using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 单个装备组的运行时状态：该装备组各槽位当前已装备的道具。
    /// 由 <see cref="EquipmentRuntimeManager"/> 维护，并作为存档单元（<see cref="EquipmentRuntimeManager.GetSaveData"/>）。
    /// 仅记录「已装备」的槽位；空槽不入列表。
    /// </summary>
    [Serializable]
    public class RuntimeEquipmentState
    {
        /// <summary>对应 <see cref="EquipmentGroup.id"/>。</summary>
        public string groupId;

        /// <summary>已装备的槽位条目（槽位 ID → 道具 ID）。</summary>
        public List<EquippedSlotEntry> slots = new List<EquippedSlotEntry>();

        public RuntimeEquipmentState() { }

        public RuntimeEquipmentState(string groupId)
        {
            this.groupId = groupId;
        }

        /// <summary>深拷贝。</summary>
        public RuntimeEquipmentState Clone()
        {
            var clone = new RuntimeEquipmentState(groupId);
            foreach (var s in slots) clone.slots.Add(s.Clone());
            return clone;
        }
    }

    /// <summary>装备组中一个已装备槽位：槽位 ID + 已装备道具 ID。</summary>
    [Serializable]
    public class EquippedSlotEntry
    {
        /// <summary>对应 <see cref="EquipmentSlot.id"/>。</summary>
        public string slotId;

        /// <summary>已装备道具的 ID（引用 <see cref="Item.id"/>）。</summary>
        public string itemId;

        public EquippedSlotEntry() { }

        public EquippedSlotEntry(string slotId, string itemId)
        {
            this.slotId = slotId;
            this.itemId = itemId;
        }

        /// <summary>深拷贝。</summary>
        public EquippedSlotEntry Clone() => new EquippedSlotEntry(slotId, itemId);
    }
}

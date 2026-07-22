using System;
using System.Collections.Generic;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 单个仓库的运行时状态：有序的格子列表（顺序即显示顺序）。
    /// 由 <see cref="InventoryRuntimeManager"/> 统一创建与管理。
    /// </summary>
    [Serializable]
    public class RuntimeInventoryState
    {
        /// <summary>对应 <see cref="Inventory.id"/>。</summary>
        public string inventoryId;

        /// <summary>有序格子列表（顺序即 UI 显示顺序）。</summary>
        public List<RuntimeItemSlot> itemSlots = new List<RuntimeItemSlot>();

        public RuntimeInventoryState() { }

        public RuntimeInventoryState(string inventoryId)
        {
            this.inventoryId = inventoryId;
        }

        /// <summary>按 <see cref="RuntimeItemSlot.slotId"/> 查找格子，未找到返回 null。</summary>
        public RuntimeItemSlot GetSlot(string slotId)
        {
            foreach (var s in itemSlots)
                if (s.slotId == slotId) return s;
            return null;
        }

        /// <summary>查找第一个 <see cref="RuntimeItemSlot.itemId"/> 匹配的格子，未找到返回 null。</summary>
        public RuntimeItemSlot FindByItemId(string itemId)
        {
            foreach (var s in itemSlots)
                if (s.itemId == itemId) return s;
            return null;
        }
    }
}

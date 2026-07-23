using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
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

        /// <summary>
        /// 按 <see cref="RuntimeItemSlot.slotId"/> 查找格子，未找到（或 slotId 为空）返回 null。
        /// <para>拖放 / 装备等路径上高频调用，故用索引循环而非 <c>List.Find(lambda)</c>——
        /// 后者的谓词捕获 slotId，每次调用都要分配一个闭包对象与一个委托。</para>
        /// </summary>
        public RuntimeItemSlot GetSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;
            for (int i = 0; i < itemSlots.Count; i++)
                if (itemSlots[i].slotId == slotId) return itemSlots[i];
            return null;
        }

        /// <summary>查找第一个 <see cref="RuntimeItemSlot.itemId"/> 匹配的格子，未找到返回 null。</summary>
        public RuntimeItemSlot FindByItemId(string itemId)
        {
            for (int i = 0; i < itemSlots.Count; i++)
                if (itemSlots[i].itemId == itemId) return itemSlots[i];
            return null;
        }
    }
}

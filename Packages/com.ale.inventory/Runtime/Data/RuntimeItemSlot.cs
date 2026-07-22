using System;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 仓库中一个格子的运行时状态。
    /// 每格记录 <see cref="slotId"/>（唯一标识，用于存档与拖拽定位）、
    /// <see cref="itemId"/>（引用 <see cref="InventoryDatabase"/> 中的 <see cref="Item.id"/>）
    /// 和 <see cref="count"/>（当前数量，受 <see cref="Item.stackLimit"/> 约束）。
    /// </summary>
    [Serializable]
    public class RuntimeItemSlot
    {
        /// <summary>格子唯一 ID（GUID），用于存档与拖拽定位。</summary>
        public string slotId;

        /// <summary>引用 <see cref="InventoryDatabase.Items"/> 中的 <see cref="Item.id"/>。</summary>
        public string itemId;

        /// <summary>当前数量。受 <see cref="Item.stackLimit"/> 约束（0 = 无上限，1 = 不可堆叠，>1 = 具体上限）。</summary>
        public int count;

        public RuntimeItemSlot() { }

        public RuntimeItemSlot(string slotId, string itemId, int count)
        {
            this.slotId   = slotId;
            this.itemId   = itemId;
            this.count = count;
        }

        /// <summary>深拷贝。</summary>
        public RuntimeItemSlot Clone() => new RuntimeItemSlot(slotId, itemId, count);
    }
}

using System;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 仓库道具格子的通用 UI 事件总线（静态）。背包 / 仓库 UI 中任意道具格子被右键点击时，
    /// 经此广播（携带 仓库 ID + 道具 ID），供上层（如 <see cref="UiwEquipmentView"/> 在打开时订阅，
    /// 实现「右键道具自动装备」）订阅，而无需逐格 / 逐列表 / 逐视图地向上层层转发事件。
    ///
    /// <para>说明：为通用通知机制（不耦合装备概念），订阅方需自行按 仓库 ID 过滤并在适当生命周期取消订阅。</para>
    /// </summary>
    public static class UiwInventoryItemEvents
    {
        /// <summary>某仓库道具格子被右键点击。参数为 (仓库 ID, 道具 ID)。</summary>
        public static event Action<string, string> ItemRightClicked;

        /// <summary>由道具格子在右键点击时调用，广播右键事件（道具 ID 为空时忽略）。</summary>
        public static void RaiseItemRightClicked(string inventoryId, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            ItemRightClicked?.Invoke(inventoryId, itemId);
        }
    }
}

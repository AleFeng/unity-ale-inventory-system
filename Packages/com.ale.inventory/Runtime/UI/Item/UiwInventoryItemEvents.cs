using System;
using System.Collections.Generic;
using Ale.Toolkit.Runtime.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 仓库道具格子的通用 UI 事件总线（静态）。承载两件事：
    /// <list type="bullet">
    ///   <item><see cref="CollectingItemMenu"/>：右键操作菜单的<b>条目贡献点</b>——上层系统（如
    ///         <see cref="UiwEquipmentView"/>）在自己打开时挂上去，往菜单里加自己的条目（「装备」）。</item>
    ///   <item><see cref="ItemRightClicked"/>：通用「道具右键」通知（携带 仓库 ID + 道具 ID）。</item>
    /// </list>
    ///
    /// <para><b>历史：</b>右键原本<b>直接</b>广播 <see cref="ItemRightClicked"/>，装备界面订阅它实现「右键道具自动装备」。
    /// 自「右键操作菜单（查看 / 使用 / 丢弃）」引入后，右键的语义改为<b>弹出菜单</b>，快速装备降级为菜单里的一个条目——
    /// 该条目被点击时仍广播本事件，故 <see cref="ItemRightClicked"/> 的既有订阅方（含包外的）无需改动，
    /// 只是触发时机由「右键即触发」变为「右键 → 点『装备』」。</para>
    ///
    /// <para>说明：两者都是通用通知机制（不耦合装备概念），订阅方需自行按 仓库 ID 过滤并在适当生命周期取消订阅。</para>
    /// </summary>
    public static class UiwInventoryItemEvents
    {
        /// <summary>
        /// 右键菜单条目的贡献委托：按目标道具往 <paramref name="entries"/> 里追加条目。
        /// <para>贡献方须自行判断「此刻是否适用」（界面是否显示、仓库是否匹配、道具是否合适），不适用就什么都不加。</para>
        /// </summary>
        /// <param name="target">被右键的道具（仓库 / 槽位 / 道具 ID / 数量）。</param>
        /// <param name="entries">菜单条目列表，追加到末尾（内置的 查看 / 使用 / 丢弃 已在其中）。</param>
        public delegate void ItemMenuEntryCollector(ItemContextTarget target, List<UiwContextMenuItem> entries);

        /// <summary>右键菜单正在组装条目：供上层追加自己的条目（如装备界面的「装备」）。</summary>
        public static event ItemMenuEntryCollector CollectingItemMenu;

        /// <summary>某仓库道具格子被右键点击。参数为 (仓库 ID, 道具 ID)。</summary>
        public static event Action<string, string> ItemRightClicked;

        /// <summary>由右键菜单在组装条目时调用，收集上层贡献的额外条目。</summary>
        public static void CollectItemMenuEntries(ItemContextTarget target, List<UiwContextMenuItem> entries)
        {
            if (entries == null) return;
            CollectingItemMenu?.Invoke(target, entries);
        }

        /// <summary>广播「道具右键」事件（道具 ID 为空时忽略）。现由右键菜单的「装备」条目调用。</summary>
        public static void RaiseItemRightClicked(string inventoryId, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            ItemRightClicked?.Invoke(inventoryId, itemId);
        }
    }
}

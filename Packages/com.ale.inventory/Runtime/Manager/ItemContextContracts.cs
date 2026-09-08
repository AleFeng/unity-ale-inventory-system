using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 一次道具操作所指向的目标：来源仓库 + 槽位 + 道具 + 当前数量。
    ///
    /// <para>右键菜单、详情弹窗、丢弃弹窗以及菜单条目的贡献方都吃这一个载荷，避免同一组四个参数
    /// 在各处逐个传递时顺序写错（<c>inventoryId</c> 与 <c>slotId</c> 都是 string，传反了编译器不会报）。</para>
    ///
    /// <para><b>按值拷贝</b>：格子会被虚拟列表随时回收复用，故弹窗必须在打开的那一刻把这几项拷走，
    /// 绝不持有格子引用。</para>
    /// </summary>
    public readonly struct ItemContextTarget
    {
        /// <summary>来源仓库 ID。</summary>
        public readonly string InventoryId;
        /// <summary>仓库槽位 ID。可能为空（悬停弹窗内的详情行、网格补位空格没有真实槽位）。</summary>
        public readonly string SlotId;
        /// <summary>道具 ID。</summary>
        public readonly string ItemId;
        /// <summary>该槽位当前数量。</summary>
        public readonly int Count;

        public ItemContextTarget(string inventoryId, string slotId, string itemId, int count)
        {
            InventoryId = inventoryId;
            SlotId      = slotId;
            ItemId      = itemId;
            Count       = count;
        }

        /// <summary>是否指向一个可操作的真实槽位（仓库 / 槽位 / 道具 ID 齐备且数量为正）。</summary>
        public bool IsValid =>
            !string.IsNullOrEmpty(InventoryId) &&
            !string.IsNullOrEmpty(SlotId) &&
            !string.IsNullOrEmpty(ItemId) &&
            Count > 0;
    }

    /// <summary>
    /// 道具右键操作菜单的运行时抽象。具体实现在 UI 层（<c>UiwItemContextMenu</c>）。
    ///
    /// <para>与 <see cref="IItemTooltip"/> 同理定义于 Runtime 程序集，使 <see cref="InventoryRuntimeManager"/>
    /// 能在不反向依赖 UI 程序集的前提下持有并对外提供全局唯一的菜单（依赖倒置）。</para>
    /// </summary>
    public interface IItemContextMenu
    {
        /// <summary>在光标处（屏幕坐标）弹出针对该道具的操作菜单。目标无效时应当不弹。</summary>
        void Show(ItemContextTarget target, Vector2 screenPos);

        /// <summary>关闭菜单。未打开时为无操作。</summary>
        void Hide();
    }

    /// <summary>
    /// 道具详情弹窗（右键菜单「查看」）的运行时抽象。具体实现在 UI 层（<c>UiwItemDetailPopup</c>）。
    /// <para>与悬停弹窗 <see cref="IItemTooltip"/> 的区别：本弹窗是<b>常驻的</b>，靠关闭按钮 / 遮罩 / ESC 收起，
    /// 不随光标移开自动消失。</para>
    /// </summary>
    public interface IItemDetailPopup
    {
        /// <summary>显示指定道具的详情。<paramref name="count"/> 为持有数量（显示在数量文本）。</summary>
        void Show(string itemId, int count);

        /// <summary>关闭弹窗。未打开时为无操作。</summary>
        void Hide();
    }

    /// <summary>
    /// 道具丢弃弹窗（右键菜单「丢弃」）的运行时抽象。具体实现在 UI 层（<c>UiwItemDiscardPopup</c>）。
    /// </summary>
    public interface IItemDiscardPopup
    {
        /// <summary>对该槽位弹出丢弃数量选择。数量上限取槽位当时的实际堆叠数。目标无效时应当不弹。</summary>
        void Show(ItemContextTarget target);

        /// <summary>关闭弹窗。未打开时为无操作。</summary>
        void Hide();
    }
}

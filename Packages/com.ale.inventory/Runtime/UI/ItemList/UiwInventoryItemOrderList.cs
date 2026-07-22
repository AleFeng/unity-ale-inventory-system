using System.Collections.Generic;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 仓库<b>顺序</b>道具列表（虚拟滚动，单列纵向）。以 <see cref="UiwInventoryItemDetail"/> 为格子，
    /// 逐行显示仓库中"有道具"的槽位（空槽不显示）；大量道具时仅渲染可见行 + 缓冲，滚动循环复用。
    ///
    /// <para>虚拟滚动引擎与单列纵向布局由基类 <see cref="UiwInventoryOrderList{TData,TCell}"/> 提供；
    /// 本类只负责"把 <see cref="RuntimeItemSlot"/> 显示到 <see cref="UiwInventoryItemDetail"/> 格子"，
    /// 并对接仓库上下文（仓库 ID）与数字格式。由 <see cref="UiwInventoryView"/> 驱动。</para>
    /// </summary>
    public class UiwInventoryItemOrderList : UiwInventoryOrderList<RuntimeItemSlot, UiwInventoryItemDetail>
    {
        private string             _inventoryId;   // 当前所属仓库 ID
        private NumberFormatLocale _numberFormat;  // 数字显示格式（千分位 / 万分位）

        /// <summary>
        /// 设置道具槽位数据列表并从顶部刷新显示（切换过滤页签 / 排序等回到顶部的场景）。
        /// 顺序列表不显示空槽（itemId 为空的槽位直接跳过）。
        /// </summary>
        public void SetItemSlotList(string inventoryId, List<RuntimeItemSlot> slots)
        {
            _inventoryId = inventoryId;
            SetItems(FilterNonEmpty(slots));
        }

        /// <summary>
        /// 增量更新道具槽位数据列表（<b>保留当前滚动位置</b>）。适用于仓库内容变化（数量增减、条目增删）
        /// 但不希望把滚动条复位到顶部、打断玩家操作的场景。走增量差异刷新：条目数不变时只重绑数据变化的可见行。
        /// </summary>
        public void UpdateItemSlotList(string inventoryId, List<RuntimeItemSlot> slots)
        {
            _inventoryId = inventoryId;
            RefreshItemsData(FilterNonEmpty(slots));
        }

        /// <summary>滚动到列表顶部。</summary>
        public void ScrollToTop() => ScrollToStart();

        /// <summary>
        /// 设置数字显示格式。立即同步到所有已创建实例，新实例经 <see cref="InitCell"/> 自动应用。
        /// 应在 <see cref="SetItemSlotList"/> 之前或同时调用。
        /// </summary>
        public void SetNumberFormat(NumberFormatLocale locale)
        {
            _numberFormat = locale;
            foreach (var inst in Instances)
                if (inst) inst.numberFormat = locale;
        }

        // ── 格子绑定 ──────────────────────────────────────────────────────────────
        
        /// <summary>
        /// 绑定 格子与道具槽位数据。由基类在格子实例化 / 滚动复用时调用。
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="slot"></param>
        protected override void BindCell(UiwInventoryItemDetail cell, RuntimeItemSlot slot)
            => cell.SetSlot(_inventoryId, slot);
        
        /// <summary>
        /// 清空 格子显示（格子被回收 / 复用时调用）。
        /// </summary>
        /// <param name="cell"></param>
        protected override void ClearCell(UiwInventoryItemDetail cell) => cell.SetEmpty();
        
        /// <summary>
        /// 初始化格子实例（格子被创建时调用）。设置数字格式。
        /// </summary>
        /// <param name="cell"></param>
        protected override void InitCell(UiwInventoryItemDetail cell) => cell.numberFormat = _numberFormat;

        /// <summary>
        /// 增量差异刷新时：格子当前显示（道具 ID + 数量）与新槽位一致则跳过重绑，避免无谓开销与图标闪烁。
        /// </summary>
        protected override bool NeedsRebind(UiwInventoryItemDetail cell, RuntimeItemSlot slot)
            => !cell.MatchesSlot(slot);

        // ── 辅助 ──────────────────────────────────────────────────────────────────

        /// <summary>过滤掉空槽（itemId 为空），顺序列表只显示有道具的槽位。</summary>
        private static List<RuntimeItemSlot> FilterNonEmpty(List<RuntimeItemSlot> slots)
            => (slots ?? new List<RuntimeItemSlot>()).FindAll(s => s != null && !string.IsNullOrEmpty(s.itemId));
    }
}

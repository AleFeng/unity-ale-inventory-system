using System;
using System.Collections.Generic;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 制作蓝图列表（虚拟滚动，单列纵向）。以 <see cref="UiwCraftingBlueprintCell"/> 为条目，
    /// 在通用顺序虚拟滚动列表 <see cref="UiwInventoryOrderList{TData,TCell}"/> 之上额外支持
    /// 「选中高亮 + 选中事件」。滚动引擎、对象池、视口监听等由基类统一提供，本类只负责
    /// 「把 <see cref="CraftingBlueprint"/> 显示到蓝图条目」并维护选中态。
    /// </summary>
    public class UiwCraftingBlueprintList : UiwInventoryOrderList<CraftingBlueprint, UiwCraftingBlueprintCell>
    {
        /// <summary>蓝图被选中事件（点击或外部设置触发点击时）。</summary>
        public event Action<CraftingBlueprint> OnBlueprintSelected;

        private string _selectedId;

        /// <summary>当前选中的蓝图（无则 null）。</summary>
        public CraftingBlueprint SelectedBlueprint
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedId)) return null;
                foreach (var bp in Items) if (bp != null && bp.id == _selectedId) return bp;
                return null;
            }
        }

        /// <summary>设置蓝图数据列表（全集）并经内建 排序 管线从顶部刷新显示（切换模板 / 过滤 / 排序时调用）。</summary>
        public void SetBlueprints(List<CraftingBlueprint> blueprints) => SetSourceItems(blueprints);

        /// <summary>仅设置选中蓝图并刷新高亮（不触发选中事件）。null/空 = 取消选中。</summary>
        public void SetSelectedById(string blueprintId)
        {
            _selectedId = blueprintId;
            RefreshSelectionHighlight();
        }

        /// <summary>滚动到列表顶部。</summary>
        public void ScrollToTop() => ScrollToStart();

        // ── 选中处理 ──────────────────────────────────────────────────────────────

        private void HandleCellClicked(CraftingBlueprint bp)
        {
            if (bp == null) return;
            _selectedId = bp.id;
            RefreshSelectionHighlight();
            OnBlueprintSelected?.Invoke(bp);
        }

        // 遍历全部实例（活跃条目各自持有其 Blueprint，空闲条目 Blueprint 为 null → 自然不高亮），
        // 仅更新选中高亮而不重绑数据。
        private void RefreshSelectionHighlight()
        {
            foreach (var cell in Instances)
                if (cell) cell.SetSelected(cell.Blueprint != null && cell.Blueprint.id == _selectedId);
        }

        // ── 格子绑定 ──────────────────────────────────────────────────────────────
        
        /// <summary>
        /// 绑定 蓝图条目格子。由基类在格子实例化 / 滚动复用时调用。
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="bp"></param>
        protected override void BindCell(UiwCraftingBlueprintCell cell, CraftingBlueprint bp)
            => cell.Bind(bp, bp != null && bp.id == _selectedId);
        
        /// <summary>
        /// 清空 蓝图条目格子。将 <see cref="UiwCraftingBlueprintCell"/> 清空并隐藏。
        /// </summary>
        /// <param name="cell"></param>
        protected override void ClearCell(UiwCraftingBlueprintCell cell) => cell.SetEmpty();
        
        /// <summary>
        /// 初始化蓝图条目格子实例（格子被创建时调用）。订阅点击事件。
        /// </summary>
        /// <param name="cell"></param>
        protected override void InitCell(UiwCraftingBlueprintCell cell) => cell.OnClicked += HandleCellClicked;
    }
}

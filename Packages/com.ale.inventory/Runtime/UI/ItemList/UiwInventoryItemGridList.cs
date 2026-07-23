using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 仓库<b>网格</b>道具列表（虚拟滚动，多列 / 多行）。以 <see cref="UiwInventoryItemCell"/> 为格子，
    /// 按仓库容量显示格子（空格 + 已有道具），大量道具时仅渲染可见区域 + 缓冲、滚动循环复用。
    ///
    /// <para>当所属仓库 <see cref="Inventory.dragSort"/> = true 时，按容量显示全部格子（含空格）并支持
    /// <b>拖拽换位 / 移动</b>（已适配虚拟滚动：格子的数据索引随绑定动态更新，拖到视口边缘自动滚动）；
    /// 否则仅显示有道具的格子、不响应整理拖拽。</para>
    ///
    /// <para>虚拟滚动引擎与网格布局（纵向 / 横向、自动跨轴数量）由基类
    /// <see cref="UiwInventoryGridList{TData,TCell}"/> 提供；本类只负责格子绑定、仓库上下文、数字格式与拖拽整理。
    /// 由 <see cref="UiwInventoryView"/> 驱动。</para>
    /// </summary>
    public class UiwInventoryItemGridList : UiwInventoryGridList<RuntimeItemSlot, UiwInventoryItemCell>
    {
        [Header("拖拽整理")]
        [Tooltip("拖拽中显示的幽灵图标：为 null 时退用复制源格子图标。")]
        public Image dragGhostImage;
        [Tooltip("拖拽到视口边缘时触发自动滚动的边缘区域占视口比例（0~0.5）。")]
        [Range(0f, 0.5f)] public float edgeScrollFraction = 0.12f;
        [Tooltip("拖拽边缘自动滚动速度（像素/秒）。")]
        public float edgeScrollSpeed = 800f;

        private string             _inventoryId;   // 当前所属仓库 ID
        private int                _capacity;      // 当前仓库容量（0=无限）
        private bool               _filtered;      // 是否过滤显示（非"全部"页签）：过滤时不展开容量、不响应整理拖拽
        private bool               _dragSort;      // 当前仓库是否启用拖拽整理
        private NumberFormatLocale _numberFormat;  // 数字显示格式

        private int        _dragSourceIndex = -1;  // 当前拖拽源数据索引，-1=无
        private GameObject _dragGhostGameObj;       // 拖拽幽灵实例
        private Vector2    _dragScreenPos;          // 最近一次拖拽指针屏幕位置（供边缘自动滚动）

        // 拖拽期间钉住源格子：边缘自动滚动会把源格滚出窗口，若被回收停用则收不到 OnDrag/OnEndDrag。
        protected override int PinnedDataIndex => _dragSourceIndex;

        #region 设置数据


        /// <summary>
        /// 设置道具槽位数据列表，并从仓库定义读取容量 / 拖拽整理配置后刷新网格。
        /// </summary>
        /// <param name="slots"></param>
        /// <param name="filtered">
        /// 是否处于过滤显示（过滤页签非"全部"）。为 true 时仅显示传入的（已筛选的）道具格，
        /// 不展开到容量、不响应整理拖拽——因为过滤视图下格子位置无法对应真实槽位。
        /// </param>
        /// <param name="inventoryId"></param>
        public void SetItemSlotList(string inventoryId, List<RuntimeItemSlot> slots, bool filtered = false)
            => SetItems(BuildDisplayList(inventoryId, slots, filtered));

        /// <summary>
        /// 增量差异刷新道具槽位（<b>保留当前滚动位置</b>）：只重绑数据发生变化的可见格，未变的格子不动。
        /// 用于仓库内容变化（拖拽换位 / 堆叠 / 数量增减）而不希望整表重建 + 回顶的场景。
        /// 参数含义同 <see cref="SetItemSlotList"/>。
        /// </summary>
        public void RefreshItemSlotList(string inventoryId, List<RuntimeItemSlot> slots, bool filtered = false)
            => RefreshItemsData(BuildDisplayList(inventoryId, slots, filtered));

        /// <summary>
        /// 由仓库定义读取容量 / 拖拽整理配置，构建网格显示用的槽位列表（拖拽整理模式补齐到容量含空槽）。
        /// 同步更新当前上下文字段（仓库 ID / 过滤 / 容量 / 拖拽整理），供 <see cref="SetItemSlotList"/>
        /// 与 <see cref="RefreshItemSlotList"/> 共用。
        /// </summary>
        private List<RuntimeItemSlot> BuildDisplayList(string inventoryId, List<RuntimeItemSlot> slots, bool filtered)
        {
            _inventoryId = inventoryId;
            _filtered    = filtered;
            slots ??= new List<RuntimeItemSlot>();

            var invDef = InventoryDataManager.Instance.GetInventory(inventoryId);
            _capacity = invDef?.capacity ?? 0;
            _dragSort = invDef?.dragSort ?? false;

            // 拖拽整理 + 有限容量 + 未过滤 → 展开到容量（含空槽，供拖拽落点）；否则仅显示传入槽位。
            int cellCount = (_dragSort && _capacity > 0 && !_filtered) ? _capacity : slots.Count;
            var display = new List<RuntimeItemSlot>(cellCount);
            for (int i = 0; i < cellCount; i++)
                display.Add(i < slots.Count ? slots[i] : new RuntimeItemSlot(null, null, 0));

            return display;
        }

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


        #endregion

        #region 格子绑定

        
        /// <summary>
        /// 绑定 格子与道具槽位数据。由基类在格子实例化 / 滚动复用时调用。
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="slot"></param>
        protected override void BindCell(UiwInventoryItemCell cell, RuntimeItemSlot slot)
        {
            if (slot == null || string.IsNullOrEmpty(slot.itemId))
                cell.SetEmpty();                 // 窗口内空槽 → 可见空态（作为拖拽落点）
            else
                cell.SetSlot(_inventoryId, slot);
            cell.SetIconAlpha(1f);               // 复用重置：清除对象池残留的半透明
        }
        
        /// <summary>
        /// 清空 格子显示（格子被回收 / 复用时调用）。
        /// </summary>
        /// <param name="cell"></param>
        protected override void ClearCell(UiwInventoryItemCell cell) => cell.ClearAndHide();
        
        /// <summary>
        /// 初始化格子实例（格子被创建时调用）。设置数字格式并绑定拖拽整理处理器。
        /// </summary>
        /// <param name="cell"></param>
        protected override void InitCell(UiwInventoryItemCell cell)
        {
            cell.numberFormat = _numberFormat;
            if (cell.dragHandler) cell.dragHandler.ItemGridList = this;
        }
        
        /// <summary>
        /// 绑定格子时 更新拖拽处理器的源数据索引（虚拟滚动复用格子时索引会随绑定动态更新）。
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="dataIndex"></param>
        protected override void OnCellAssigned(UiwInventoryItemCell cell, int dataIndex)
        {
            var handler = cell.dragHandler;
            if (!handler) return;
            handler.ItemGridList = this;
            handler.ItemCellIdx  = dataIndex;   // 关键：格子的数据索引随绑定动态更新（虚拟滚动复用）
            // 过滤显示下禁用整理拖拽：格子按筛选顺序紧凑排列，索引无法对应真实槽位。
            handler.enabled = _dragSort && !_filtered;
        }

        /// <summary>
        /// 增量差异刷新时：格子当前显示（道具 ID + 数量）与新槽位一致则跳过重绑，
        /// 避免图标异步重载闪烁与无谓开销。
        /// </summary>
        protected override bool NeedsRebind(UiwInventoryItemCell cell, RuntimeItemSlot slot)
            => !cell.MatchesSlot(slot);


        #endregion

        #region 拖拽整理（适配虚拟滚动；由 GridCellDragHandler 转发）


        /// <summary>事件：格子 开始拖拽。创建拖拽幽灵并记录拖拽源数据索引。</summary>
        internal void OnCellBeginDrag(int dataIndex, PointerEventData eventData)
        {
            // 空格（含容量补齐占位）不可拖拽。
            if (dataIndex < 0 || dataIndex >= Items.Count || Items[dataIndex] == null
                || string.IsNullOrEmpty(Items[dataIndex].itemId))
            { _dragSourceIndex = -1; return; }

            _dragSourceIndex = dataIndex;
            _dragScreenPos   = eventData.position;

            // 根 Canvas（幽灵挂最上层，避免被其它 UI 裁剪）。
            var canvas = UIUtility.ResolveRootCanvas(this);
            if (!canvas) { _dragSourceIndex = -1; return; }

            TryGetActiveCell(dataIndex, out var srcCell);

            if (dragGhostImage)
            {
                _dragGhostGameObj = Instantiate(dragGhostImage.gameObject, canvas.transform);
            }
            else
            {
                // 退用：复制源格子图标。
                _dragGhostGameObj = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
                _dragGhostGameObj.transform.SetParent(canvas.transform, false);
                var srcIcon = srcCell ? srcCell.iconImage : null;
                var img     = _dragGhostGameObj.GetComponent<Image>();
                if (srcIcon)
                {
                    img.sprite = srcIcon.sprite;
                    ((RectTransform)_dragGhostGameObj.transform).sizeDelta =
                        ((RectTransform)srcIcon.transform).rect.size;
                }
            }

            // 指针穿透幽灵：CanvasGroup + 所有 Image.raycastTarget=false，确保目标格子能收到 IDropHandler。
            var cg = _dragGhostGameObj.GetComponent<CanvasGroup>();
            if (!cg) cg = _dragGhostGameObj.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            foreach (var img in _dragGhostGameObj.GetComponentsInChildren<Image>(true))
                img.raycastTarget = false;

            _dragGhostGameObj.transform.SetAsLastSibling();
            _dragGhostGameObj.transform.position = eventData.position;

            // 源格子图标半透明，透明度由格子自身的 dragIconAlpha 配置。
            if (srcCell) srcCell.SetIconAlpha(srcCell.dragIconAlpha);
        }

        /// <summary>事件：格子 拖拽中。更新幽灵位置并记录指针位置（供边缘自动滚动）。</summary>
        internal void OnCellDrag(int dataIndex, PointerEventData eventData)
        {
            _dragScreenPos = eventData.position;
            if (_dragGhostGameObj) _dragGhostGameObj.transform.position = eventData.position;
        }

        /// <summary>事件：格子 放入。执行换位或移动到空格。</summary>
        internal void OnCellDrop(int targetDataIndex, PointerEventData eventData)
        {
            if (_dragSourceIndex < 0 || _dragSourceIndex == targetDataIndex) return;
            if (targetDataIndex < 0 || targetDataIndex >= Items.Count) return;
            if (!InventoryRuntimeManager.Instance) return;

            var src = Items[_dragSourceIndex];
            var tgt = Items[targetDataIndex];
            // 容量补齐的纯占位（无 slotId）不是有效落点，跳过。
            if (src == null || tgt == null || string.IsNullOrEmpty(src.slotId) || string.IsNullOrEmpty(tgt.slotId)) return;

            // 预分配槽位后目标槽始终存在（含空槽），统一用 StackOrSwapSlots：可堆叠优先堆叠，否则交换内容。
            // OnInventoryChanged 事件会触发 UiwInventoryView.RefreshItemList 自动刷新本网格。
            InventoryRuntimeManager.Instance.StackOrSwapSlots(_inventoryId, src.slotId, tgt.slotId);
        }

        /// <summary>
        /// 获取指定数据索引格子对应的真实仓库槽位信息（仓库 ID / 槽位 ID / 是否空格），供拖拽落点做精确落位
        /// （如从装备槽拖到本格卸下 / 交换）。索引越界或无对应槽位（纯占位）时返回 false。
        /// </summary>
        internal bool TryGetCellSlot(int dataIndex, out string inventoryId, out string slotId, out bool isEmpty)
        {
            inventoryId = _inventoryId; slotId = null; isEmpty = true;
            if (dataIndex < 0 || dataIndex >= Items.Count || Items[dataIndex] == null) return false;
            slotId  = Items[dataIndex].slotId;
            isEmpty = string.IsNullOrEmpty(Items[dataIndex].itemId);
            return !string.IsNullOrEmpty(slotId);
        }

        /// <summary>事件：格子 结束拖拽。销毁幽灵并重置拖拽状态。</summary>
        internal void OnCellEndDrag(int dataIndex, PointerEventData eventData)
        {
            // 恢复源格子图标透明度（源格可能已随自动滚动被回收 / 复用，取当前仍显示该索引的实例安全还原）。
            if (_dragSourceIndex >= 0 && TryGetActiveCell(_dragSourceIndex, out var srcCell) && srcCell)
                srcCell.SetIconAlpha(1f);

            if (_dragGhostGameObj) Destroy(_dragGhostGameObj);
            _dragGhostGameObj = null;
            _dragSourceIndex  = -1;

            // 解除钉住后，若源格已滚出窗口则立即回收（成功换位会另有 OnInventoryChanged 触发完整刷新）。
            ForceRefreshVisible();
        }


        #endregion

        #region 边缘自动滚动（拖拽中）


        private void Update()
        {
            if (_dragSourceIndex < 0) return;   // 仅拖拽中
            EdgeAutoScroll();
        }

        /// <summary>拖拽指针位于视口沿滚动主轴两端的边缘区域时，按帧自动滚动内容，使远处落点可达。</summary>
        private void EdgeAutoScroll()
        {
            if (!scrollRect || !scrollRect.viewport || !content) return;

            var vpRt = scrollRect.viewport;
            // 本方法在拖拽期间每帧执行：走 UIUtility 的带缓存解析，避免每帧 GetComponentInParent 逐级上溯。
            var cam  = UIUtility.ResolveCanvasCamera(vpRt);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(vpRt, _dragScreenPos, cam, out var local))
                return;

            var   rect = vpRt.rect;
            float dt   = Time.unscaledDeltaTime;
            bool  vertical = scrollDirection == EListScrollDirection.Vertical;

            if (vertical)
            {
                float edge = rect.height * edgeScrollFraction;
                if (local.y > rect.yMax - edge)      ScrollPrimary(-edgeScrollSpeed * dt); // 近顶 → 向起点
                else if (local.y < rect.yMin + edge) ScrollPrimary(+edgeScrollSpeed * dt); // 近底 → 向末尾
            }
            else
            {
                float edge = rect.width * edgeScrollFraction;
                if (local.x < rect.xMin + edge)      ScrollPrimary(-edgeScrollSpeed * dt); // 近左 → 向起点
                else if (local.x > rect.xMax - edge) ScrollPrimary(+edgeScrollSpeed * dt); // 近右 → 向末尾
            }
        }

        /// <summary>沿主轴滚动内容（amount&gt;0 向末尾 / &lt;0 向起点），钳制在有效范围后刷新可见格。</summary>
        private void ScrollPrimary(float amount)
        {
            var   pos = content.anchoredPosition;
            var   vp  = scrollRect.viewport.rect;
            if (scrollDirection == EListScrollDirection.Vertical)
            {
                float maxY = Mathf.Max(0f, content.sizeDelta.y - vp.height);
                pos.y = Mathf.Clamp(pos.y + amount, 0f, maxY);   // 向末尾 = content 上移 = y 增大
            }
            else
            {
                float maxX = Mathf.Max(0f, content.sizeDelta.x - vp.width);
                pos.x = Mathf.Clamp(pos.x - amount, -maxX, 0f);  // 向末尾 = content 左移 = x 减小
            }
            content.anchoredPosition = pos;
            UpdateVisibleCells();
        }


        #endregion

        #region 生命周期


        protected override void OnDisable()
        {
            base.OnDisable();
            // 停用时（可能在拖拽中）清理幽灵与拖拽状态。
            if (_dragGhostGameObj) Destroy(_dragGhostGameObj);
            _dragGhostGameObj = null;
            _dragSourceIndex  = -1;
        }
        #endregion

    }
}

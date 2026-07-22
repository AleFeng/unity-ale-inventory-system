using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 装备槽显示组件。继承 <see cref="UiwInventoryItemSlotBase"/> 复用图标 / 名称 / 品质背景 / 悬停弹窗 / 拖拽字段。
    /// 绑定到某装备组的某槽位，从 <see cref="EquipmentRuntimeManager"/> 读取该槽当前已装备的道具并显示；空槽可选显示槽位名作占位。
    ///
    /// <para>交互：左键点击 → <see cref="Clicked"/>；右键点击 → <see cref="RightClicked"/>；
    /// 已装备时可拖出（槽↔槽交换）；作为放置目标接收候选道具 / 其它装备槽的拖入（装备 / 交换），
    /// 拖拽悬停时按可否装入显示绿 / 红有效性。</para>
    /// </summary>
    public class UiwEquipmentSlot : UiwInventoryItemSlotBase,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        /// <summary>所属装备组 ID。</summary>
        public string GroupId { get; private set; }
        /// <summary>所属槽位列表 ID。</summary>
        public string SlotListId { get; private set; }
        /// <summary>所属槽位列表定义。</summary>
        public EquipmentSlotList SlotListDef { get; private set; }
        /// <summary>槽位 ID。</summary>
        public string SlotId { get; private set; }
        /// <summary>槽位配置定义。</summary>
        public EquipmentSlot SlotDef { get; private set; }

        /// <summary>左键点击事件（参数为本槽）。</summary>
        public event Action<UiwEquipmentSlot> Clicked;
        /// <summary>右键点击事件（参数为本槽）。</summary>
        public event Action<UiwEquipmentSlot> RightClicked;

        [Header("装备槽")]
        [Tooltip("空槽时用名称文本显示槽位名（占位）。")]
        public bool showSlotNameWhenEmpty = true;
        [Tooltip("选中指示物（可选；装备选择面板中标记当前选中的装备槽，初始建议隐藏）。")]
        public GameObject selectedIndicator;

        [Header("拖拽有效性")]
        [Tooltip("拖拽悬停时的有效性叠加图（可选，初始建议禁用）。绿 = 可装入，红 = 不可装入。")]
        public Image validityOverlay;
        [Tooltip("可装入颜色。")]
        public Color validColor = new Color(0.30f, 0.90f, 0.30f, 0.45f);
        [Tooltip("不可装入颜色。")]
        public Color invalidColor = new Color(0.90f, 0.30f, 0.30f, 0.45f);

        /// <summary>绑定到某装备组某槽位并刷新显示。</summary>
        public void Bind(string groupId, EquipmentSlotList slotListDef, EquipmentSlot slotDef)
        {
            GroupId     = groupId;
            SlotListDef = slotListDef;
            SlotListId  = slotListDef != null ? slotListDef.id : null;
            SlotDef     = slotDef;
            SlotId      = slotDef != null ? slotDef.id : null;
            SetSelected(false);
            HideValidity();
            Refresh();
        }

        /// <summary>根据 <see cref="EquipmentRuntimeManager"/> 当前已装备状态刷新显示。</summary>
        public void Refresh()
        {
            gameObject.SetActive(true);

            var mgr    = EquipmentRuntimeManager.Instance;
            string itemId = mgr != null ? mgr.GetEquipped(GroupId, SlotId) : null;
            if (string.IsNullOrEmpty(itemId)) { ShowEmpty(); return; }

            var item = InventoryDataManager.Instance != null ? InventoryDataManager.Instance.GetItem(itemId) : null;
            ApplyQualityBackground(item);
            ApplyIcon(item);
            ApplyName(item, itemId);
            SetTooltipItemId(itemId);
            ClearStackFull();
            if (countText) countText.text = string.Empty;   // 装备槽恒为 1 件，不显示数量
        }

        private void ShowEmpty()
        {
            ClearIcon();
            ClearStackFull();
            ClearNameAndQuality();
            SetTooltipItemId(null);
            if (countText) countText.text = string.Empty;
            if (showSlotNameWhenEmpty && nameText && SlotDef != null)
                nameText.text = string.IsNullOrEmpty(SlotDef.displayName) ? string.Empty : SlotDef.displayName;
        }

        /// <summary>设置选中指示物显示状态（由选择面板驱动）。</summary>
        public void SetSelected(bool selected)
        {
            if (selectedIndicator) selectedIndicator.SetActive(selected);
        }

        #region 点击

        // 覆写基类的「右键广播快速装备」：装备槽点击语义不同——左键选中 / 右键卸下（不广播，故不调用 base）。
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null) return;

            // 拖拽移动中松开：当「松手落点 == 拖拽源」时，Unity 会在 Drop / EndDrag 之前先派发 PointerClick。
            // 若此处当成点击处理（左键会打开选择面板并停用本槽），随后的 OnEndDrag 便收不到（本槽已停用），
            // 导致拖拽状态无法正常收尾（幽灵残留、来源图标不复位）。故拖拽中一律只收尾拖拽状态，不当作点击。
            if (UiwEquipmentDragContext.IsDragging)
            {
                UiwEquipmentDragContext.End();   // 复位来源图标 + 销毁幽灵（幂等；若随后 OnEndDrag 触发亦无副作用）
                HideValidity();
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)       Clicked?.Invoke(this);
            else if (eventData.button == PointerEventData.InputButton.Right)  RightClicked?.Invoke(this);
        }

        #endregion

        #region 拖拽源（已装备时可拖出，用于槽↔槽交换）

        public void OnBeginDrag(PointerEventData eventData)
        {
            var mgr = EquipmentRuntimeManager.Instance;
            string itemId = mgr != null ? mgr.GetEquipped(GroupId, SlotId) : null;
            if (string.IsNullOrEmpty(itemId)) return;   // 空槽不可拖出

            var canvas = GetComponentInParent<Canvas>();
            if (canvas) canvas = canvas.rootCanvas;
            Sprite icon = iconImage ? iconImage.sprite : null;

            // 来源图标半透明与结束复位由拖拽上下文统一处理（传入本槽作为来源）。
            UiwEquipmentDragContext.BeginFromSlot(GroupId, SlotId, itemId, this, icon, canvas, eventData.position);
        }

        public void OnDrag(PointerEventData eventData) => UiwEquipmentDragContext.UpdateGhost(eventData.position);

        public void OnEndDrag(PointerEventData eventData)
        {
            UiwEquipmentDragContext.End();   // 复位来源图标 + 销毁幽灵
            HideValidity();
        }

        #endregion

        #region 放置目标（接收装备 / 交换）

        public void OnDrop(PointerEventData eventData)
        {
            HideValidity();
            var eq = EquipmentRuntimeManager.Instance;
            if (eq == null) return;

            if (UiwEquipmentDragContext.IsDragging)
            {
                if (!string.IsNullOrEmpty(UiwEquipmentDragContext.SourceSlotId))
                {
                    // 来自装备槽：同组槽↔槽交换。
                    if (UiwEquipmentDragContext.SourceGroupId == GroupId
                        && UiwEquipmentDragContext.SourceSlotId != SlotId)
                        eq.SwapSlots(GroupId, UiwEquipmentDragContext.SourceSlotId, SlotId);
                }
                else if (!string.IsNullOrEmpty(UiwEquipmentDragContext.SourceInventoryId))
                {
                    // 来自候选道具 / 仓库：装备到本槽（占用则换下旧道具回来源仓库）。
                    eq.Equip(GroupId, SlotListId, SlotId,
                        UiwEquipmentDragContext.ItemId, UiwEquipmentDragContext.SourceInventoryId);
                }

                // 在落点（确定动作后）立即收尾：销毁幽灵并复位来源图标。
                // 装备 / 交换会同步刷新，可能停用来源候选格子导致其 OnEndDrag 不触发（幽灵残留），故不能只依赖来源的 OnEndDrag。
                UiwEquipmentDragContext.End();
            }
            // 背包网格格子（其自带 GridCellDragHandler 拖拽）拖到装备槽的装备，改由 GridCellDragHandler.OnEndDrag
            // 按落点处理——它经 OwnerCell 解析宿主格子，可正确支持拖拽组件挂在格子子物体上的情形。
        }

        #endregion

        #region 悬停高亮 + 拖拽有效性

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);   // 保留悬停高亮 + 详情弹窗
            if (UiwEquipmentDragContext.IsDragging)
            {
                // 装备内部拖拽（候选 / 装备槽）：拖到自身槽视为无效。
                bool selfSlot = UiwEquipmentDragContext.SourceGroupId == GroupId
                             && UiwEquipmentDragContext.SourceSlotId == SlotId;
                ShowValidity(!selfSlot && IsItemValidForSlot(UiwEquipmentDragContext.ItemId));
            }
            else if (TryGetBackpackDragItem(eventData, out var itemId))
            {
                // 背包网格格子拖拽中：按可否装入显示绿 / 红。
                ShowValidity(IsItemValidForSlot(itemId));
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            HideValidity();
        }

        private bool IsItemValidForSlot(string itemId)
        {
            var eq = EquipmentRuntimeManager.Instance;
            return eq != null && eq.ItemMatchesSlot(SlotListDef, SlotDef, itemId);
        }

        /// <summary>
        /// 若当前正拖拽一个背包网格格子（<see cref="UiwInventoryItemCell"/>），取其道具 ID 与来源仓库（仅用于悬停有效性预览）。
        /// pointerDrag 是拖拽组件（<see cref="GridCellDragHandler"/>）所在物体，其可能挂在格子子物体上——
        /// 故优先经其 <c>OwnerCell</c> 解析宿主格子，同物体时回退直接取组件。
        /// </summary>
        private static bool TryGetBackpackDragItem(PointerEventData eventData, out string itemId)
        {
            itemId = null;
            if (eventData == null || !eventData.pointerDrag) return false;
            var handler = eventData.pointerDrag.GetComponent<GridCellDragHandler>();
            var cell    = handler ? handler.OwnerCell : eventData.pointerDrag.GetComponent<UiwInventoryItemCell>();
            if (!cell || string.IsNullOrEmpty(cell.ItemId)) return false;
            itemId = cell.ItemId;
            return true;
        }

        private void ShowValidity(bool valid)
        {
            if (!validityOverlay) return;
            validityOverlay.enabled = true;
            validityOverlay.color   = valid ? validColor : invalidColor;
        }

        private void HideValidity()
        {
            if (validityOverlay) validityOverlay.enabled = false;
        }

        #endregion
    }
}

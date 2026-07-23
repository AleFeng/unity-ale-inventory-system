using UnityEngine;
using UnityEngine.EventSystems;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具格子拖拽中转组件（挂在 <see cref="UiwInventoryItemCell"/> 或其子物体上，并关联到格子的 dragHandler 字段）。
    /// 按格子所处上下文自动切换两种拖拽：
    /// <list type="bullet">
    ///   <item><b>已接入网格列表</b>（<see cref="UiwInventoryItemGridList"/> 设置了 <see cref="ItemGridList"/>，
    ///   并按仓库 dragSort 启停本组件）：把拖拽事件转发给网格列表整理；<b>结束拖拽</b>时按落点决定——
    ///   落到装备槽（<see cref="UiwEquipmentSlot"/>）则装备到该槽，落到道具格子（<see cref="UiwInventoryItemCell"/>）
    ///   或其它则交给网格列表做换位 / 移动。</item>
    ///   <item><b>未接入网格列表</b>（如装备选择面板的候选列表）：驱动「拖到装备槽装备」——
    ///   经 <see cref="UiwEquipmentDragContext"/> 起拖（带跟随光标幽灵），落到 <see cref="UiwEquipmentSlot"/> 装备。</item>
    /// </list>
    /// 两种情形下把格子拖到 <see cref="UiwEquipmentSlot"/> 都能装备（网格情形在本组件结束拖拽时按落点装备，
    /// 候选情形由本组件起的拖拽上下文）。
    ///
    /// <para><b>右键快速装备不在此处理</b>：由 <see cref="UiwInventoryItemCell"/> 广播「道具右键」事件
    /// （<see cref="UiwInventoryItemEvents.ItemRightClicked"/>），装备界面打开时由 <see cref="UiwEquipmentView"/> 订阅统一处理，
    /// 使格子在背包网格与装备候选列表中的交互保持一致。</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GridCellDragHandler : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        internal int                      ItemCellIdx;
        internal UiwInventoryItemGridList ItemGridList;

        private UiwInventoryItemCell _cell;
        private bool _draggingEquip;   // 本次拖拽是否由本组件驱动装备拖拽（否则转发给网格列表）

        /// <summary>
        /// 宿主道具格子（装备拖拽需读取其道具 / 来源仓库 / 图标）。
        /// 由 <see cref="UiwInventoryItemCell"/> 在其 Awake 中经 dragHandler 字段主动注册——
        /// 因此本组件可挂在格子的<b>子物体</b>上（而非必须与 <see cref="UiwInventoryItemCell"/> 同物体）；
        /// 未注册时再沿父级向上查找兜底（含未激活对象）。
        /// </summary>
        internal UiwInventoryItemCell OwnerCell
        {
            get
            {
                if (!_cell) _cell = GetComponentInParent<UiwInventoryItemCell>(true);
                return _cell;
            }
            set => _cell = value;
        }
        
        /// <summary>
        /// 拖拽开始：按格子所处上下文自动切换两种拖拽行为（见类注释）。
        /// </summary>
        /// <param name="eventData"></param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            _draggingEquip = false;

            // 已接入网格列表：转发给网格列表做整理拖拽（落点行为在 OnEndDrag 按悬停目标决定：装备槽→装备，道具格子→换位）。
            if (ItemGridList) { ItemGridList.OnCellBeginDrag(ItemCellIdx, eventData); return; }

            // 未接入网格列表：驱动装备拖拽（拖到装备槽装备）。
            var cell = OwnerCell;
            if (!cell || string.IsNullOrEmpty(cell.ItemId)) return;
            var canvas = UIUtility.ResolveRootCanvas(this);
            Sprite icon = cell.iconImage ? cell.iconImage.sprite : null;
            // 来源图标半透明与结束复位由拖拽上下文统一处理（传入 cell 作为来源）。
            UiwEquipmentDragContext.BeginFromInventory(cell.ItemId, cell.InventoryId, cell, icon, canvas, eventData.position);
            _draggingEquip = true;
        }
        
        /// <summary>
        /// 拖拽中：已接入网格列表则转发给网格列表整理；未接入则更新装备拖拽幽灵位置。
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrag(PointerEventData eventData)
        {
            if (ItemGridList) { ItemGridList.OnCellDrag(ItemCellIdx, eventData); return; }
            if (_draggingEquip) UiwEquipmentDragContext.UpdateGhost(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 已接入网格列表：按落点决定行为。
            // · 落到装备槽（UiwEquipmentSlot）→ 装备到该槽（背包格子拖到装备槽装备）。
            // · 落到道具格子（UiwInventoryItemCell）或其它 → 交给网格列表整理（换位由目标格子的 OnCellDrop 完成）。
            // 无论何种落点，都要让网格列表清理拖拽表现（销毁幽灵 / 复位），故 OnCellEndDrag 总要调用（其本身只清理、不整理）。
            if (ItemGridList)
            {
                var slot = ResolveHoveredSlot(eventData);
                ItemGridList.OnCellEndDrag(ItemCellIdx, eventData);
                if (slot) TryEquipToSlot(slot);
                return;
            }

            // 未接入网格列表（候选列表）：装备拖拽收尾（End 复位来源图标 + 销毁幽灵）。
            // 注：落到装备槽装备时，装备已在 UiwEquipmentSlot.OnDrop 中收尾（那里的刷新可能停用本格子导致收不到本回调）。
            if (!_draggingEquip) return;
            _draggingEquip = false;
            UiwEquipmentDragContext.End();
        }

        // 放置目标：
        // · 若正从装备槽拖出（UiwEquipmentDragContext 带来源装备槽）落到本道具格子 → 卸下到本空格 / 与本格道具交换；
        // · 否则为网格整理（把别的格子放到本格子上换位）。
        // 背包格子拖到装备槽的装备落点由本组件在 OnEndDrag 按悬停目标处理。
        public void OnDrop(PointerEventData eventData)
        {
            if (UiwEquipmentDragContext.IsDragging && !string.IsNullOrEmpty(UiwEquipmentDragContext.SourceSlotId))
            {
                HandleEquipSlotDropOnCell();
                return;
            }
            ItemGridList?.OnCellDrop(ItemCellIdx, eventData);
        }

        /// <summary>
        /// 从装备槽拖出的道具落到本道具格子：本格为空 → 卸下装备槽道具到本格；本格有道具 → 若该道具可装入源装备槽则交换，
        /// 否则不改动。仅网格列表格子支持（有真实仓库槽位）；候选列表等无对应槽位则仅取消拖拽。
        /// </summary>
        private void HandleEquipSlotDropOnCell()
        {
            string groupId     = UiwEquipmentDragContext.SourceGroupId;
            string equipSlotId = UiwEquipmentDragContext.SourceSlotId;
            var eq             = EquipmentRuntimeManager.Instance;

            if (eq != null && ItemGridList
                && ItemGridList.TryGetCellSlot(ItemCellIdx, out var invId, out var slotId, out var isEmpty))
            {
                if (isEmpty) eq.UnequipToSlot(groupId, equipSlotId, invId, slotId);        // 空格 → 卸下到本格
                else         eq.EquipFromSlotSwap(groupId, equipSlotId, invId, slotId);    // 有道具 → 可装入则交换，否则不改动
            }

            // 收尾：销毁幽灵并复位来源装备槽图标（落点即确定动作，不依赖来源 OnEndDrag）。
            UiwEquipmentDragContext.End();
        }

        /// <summary>解析拖拽结束时光标下的装备槽（与 <see cref="UiwEquipmentSlot"/>.OnDrop 收到的落点一致）。</summary>
        private static UiwEquipmentSlot ResolveHoveredSlot(PointerEventData eventData)
        {
            if (eventData == null) return null;
            // pointerCurrentRaycast.gameObject 即 EventSystem 派发 OnDrop 的落点物体（可能是槽的子图形），向上查找槽。
            var go = eventData.pointerCurrentRaycast.gameObject;
            if (!go) go = eventData.pointerEnter;
            return go ? go.GetComponentInParent<UiwEquipmentSlot>() : null;
        }

        /// <summary>把宿主格子的道具装备到指定装备槽（装备失败由管理器内部判定，无操作）。</summary>
        private void TryEquipToSlot(UiwEquipmentSlot slot)
        {
            var eq = EquipmentRuntimeManager.Instance;
            if (eq == null || !slot) return;
            var cell = OwnerCell;
            if (!cell || string.IsNullOrEmpty(cell.ItemId)) return;
            eq.Equip(slot.GroupId, slot.SlotListId, slot.SlotId, cell.ItemId, cell.InventoryId);
        }
    }
}

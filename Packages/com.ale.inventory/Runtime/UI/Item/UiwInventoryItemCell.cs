namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具格子网格显示组件（MonoBehaviour）。
    /// 信息密度介于 <see cref="UiwInventoryItemSimple"/>（仅图标+数量）与
    /// <see cref="UiwInventoryItemDetail"/>（完整横排布局）之间，
    /// 专为网格状背包的方形 Slot 设计。
    ///
    /// <para>显示内容：品质背景框、道具图标、道具名称、道具数量、悬停高亮边框、堆叠已满提示图标。</para>
    /// <para>图标 / 数量 / 数字格式 / 名称 / 品质 / 悬停弹窗 / 堆叠提示 / 道具标识（仓库·道具 ID）/ 右键快速装备
    /// 等公共字段与行为均继承自 <see cref="UiwInventoryItemSlotBase"/>。</para>
    /// </summary>
    public class UiwInventoryItemCell : UiwInventoryItemSlotBase
    {
        protected virtual void Awake()
        {
            // 拖拽组件（dragHandler）可能挂在本格子的子物体上（UI 排版需要）；主动把自己注册给它，
            // 以便装备拖拽能取到本格子——跨物体时 GridCellDragHandler.GetComponent 取不到本组件。
            if (dragHandler) dragHandler.OwnerCell = this;
        }

        /// <summary>将此格子绑定到指定仓库的指定 slot，刷新所有显示。</summary>
        public void SetSlot(string inventoryId, RuntimeItemSlot slot)
        {
            if (slot == null) { ClearAndHide(); return; }

            SetBoundSlot(inventoryId, slot.itemId, slot.count);   // 记录来源仓库 / 道具 ID / 数量 + 悬停弹窗目标（基类共用）

            var item = InventoryDataManager.Instance.GetItem(slot.itemId);

            ApplyQualityBackground(item);
            ApplyIcon(item);

            // 名称
            ApplyName(item, slot.itemId);

            // 数量
            if (countText) countText.text = FormatNumber(slot.count);

            // 先激活，再启动协程（StartCoroutine 要求 GameObject 处于 active 状态）
            gameObject.SetActive(true);

            bool isFull = item != null && item.stackLimit > 0 && slot.count >= item.stackLimit;
            SetStackFull(isFull, animate: true);
        }

        /// <summary>
        /// 格子 设置为空槽
        /// 保持 GameObject 激活，以便 dragSort 模式下作为拖放目标，
        /// 也确保此前被 <see cref="ClearAndHide"/> 隐藏的格子在重新成为空槽时恢复显示。
        /// </summary>
        public void SetEmpty()
        {
            gameObject.SetActive(true);
            ClearBoundSlot();   // 清除道具标识 + 悬停弹窗目标（基类共用）
            ClearIcon();
            ClearStackFull();
            ClearNameAndQuality();
            if (countText) countText.text = string.Empty;
        }

        /// <summary>
        /// 格子 清除数据并隐藏
        /// 超出所需格子数时使用。
        /// </summary>
        public void ClearAndHide()
        {
            SetEmpty();
            gameObject.SetActive(false);
        }
    }
}

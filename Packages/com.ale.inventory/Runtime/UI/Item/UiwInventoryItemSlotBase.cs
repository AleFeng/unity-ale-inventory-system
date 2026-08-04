using Ale.Toolkit.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具格子（槽位）显示组件的中间基类，供 <see cref="UiwInventoryItemCell"/> 和
    /// <see cref="UiwInventoryItemDetail"/> 继承。
    ///
    /// <para>名称 / 图标 / 数量 / 品质背景等显示字段与方法已统一上移至
    /// <see cref="UiwInventoryItemBase"/>；本中间基类在其基础上增加：</para>
    /// <list type="bullet">
    ///   <item>悬停高亮（<see cref="IPointerEnterHandler"/> / <see cref="IPointerExitHandler"/>）</item>
    ///   <item>堆叠已满提示（Tween 淡入淡出）</item>
    ///   <item>拖拽整理（<see cref="dragHandler"/>）</item>
    /// </list>
    /// </summary>
    public abstract class UiwInventoryItemSlotBase : UiwInventoryItemBase, IPointerClickHandler
    {
        protected override void OnDisable()
        {
            base.OnDisable();   // 关闭本格可能残留的详情弹窗（停用不会派发 PointerExit）
            // [B6] Tween 不随 GameObject 停用而自动停止（协程会被 Unity 自动停）：回收 / 停用时打断悬停 & 堆叠
            // 淡入并复位 alpha=0，否则正在淡入的高亮边框会残留到对象池复用后的另一个道具上。
            ClearHoverHighlight();
            ClearStackFull();
        }

        #region 道具标识 + 右键

        // 当前绑定的仓库 / 道具 ID 与显示数量（供右键事件携带 + 悬停弹窗 + 列表增量差异刷新；空槽时道具 ID 为空、数量 0）。
        // 由子类在绑定 / 清空道具时经 SetBoundSlot / ClearBoundSlot 维护。
        private string _inventoryId;
        private string _itemId;
        private int    _displayedCount;

        /// <summary>当前绑定的道具 ID（空槽为空）。供装备槽在拖拽落点时读取（背包拖拽装备）。</summary>
        public string ItemId => _itemId;
        /// <summary>当前绑定的仓库 ID。供装备槽在拖拽落点时读取（装备道具来源仓库）。</summary>
        public string InventoryId => _inventoryId;
        /// <summary>当前格子正显示的数量（空槽为 0）。供列表做增量差异刷新时判断本格数据是否变化。</summary>
        public int DisplayedCount => _displayedCount;

        /// <summary>
        /// 子类绑定道具时调用：记录来源仓库 / 道具 ID 与显示数量，并同步悬停弹窗的目标道具
        /// （<see cref="UiwInventoryItemBase.showDetailTooltip"/> 启用时据此弹窗）。
        /// </summary>
        protected void SetBoundSlot(string inventoryId, string itemId, int count = 0)
        {
            _inventoryId    = inventoryId;
            _itemId         = itemId;
            _displayedCount = count;
            SetTooltipItemId(itemId, count);
        }

        /// <summary>子类清空道具时调用：清除来源仓库 / 道具 ID / 显示数量 与悬停弹窗目标道具。</summary>
        protected void ClearBoundSlot()
        {
            _inventoryId    = null;
            _itemId         = null;
            _displayedCount = 0;
            SetTooltipItemId(null);
        }

        /// <summary>
        /// 判断本格<b>当前显示内容</b>是否与给定槽位一致（道具 ID + 数量，空槽统一按「无道具 / 0」比较）。
        /// 供列表增量差异刷新：一致则该格无需重绑，避免图标异步重载闪烁与无谓开销。
        /// </summary>
        public bool MatchesSlot(RuntimeItemSlot slot)
        {
            string newItemId = slot != null ? slot.itemId : null;
            int    newCount  = slot != null ? slot.count  : 0;
            string a = string.IsNullOrEmpty(_itemId)   ? null : _itemId;
            string b = string.IsNullOrEmpty(newItemId) ? null : newItemId;
            return a == b && _displayedCount == newCount;
        }

        /// <summary>
        /// 右键点击：广播通用「道具右键」事件（携带 仓库 ID + 道具 ID），供上层统一处理（右键快速装备）——
        /// 装备界面打开时由 <see cref="UiwEquipmentView"/> 订阅本事件自动装备到当前装备组，
        /// 使格子在背包网格 / 明细列表 / 装备候选列表中的右键交互一致。
        /// 子类可覆写以改用其它点击语义（如 <see cref="UiwEquipmentSlot"/> 的选中 / 卸下事件）。
        /// </summary>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
                UiwInventoryItemEvents.RaiseItemRightClicked(_inventoryId, _itemId);
        }

        #endregion

        #region 悬停高亮

        [Header("悬停高亮")]
        [Tooltip("悬停时淡入的高亮边框图片（通过 Color.a 控制透明度，初始 alpha=0）。")]
        public Image hoverBorder;
        [Tooltip("悬停高亮淡入淡出时长（秒）。")]
        public float hoverFadeDuration = 0.15f;

        private ToolkitTweenHandle _hoverFade; // 悬停高亮淡入淡出句柄，用于打断正在进行的动画

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);   // 保留基类的悬停详情弹窗能力
            if (!hoverBorder) return;
            _hoverFade.Kill();
            _hoverFade = ToolkitTween.FadeGraphic(hoverBorder, 1f, hoverFadeDuration);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (!hoverBorder) return;
            _hoverFade.Kill();
            _hoverFade = ToolkitTween.FadeGraphic(hoverBorder, 0f, hoverFadeDuration);
        }

        /// <summary>
        /// 立即取消悬停高亮（无动画，打断正在进行的淡入并将边框 alpha 归零）。
        /// 用于本物体即将被停用、Unity 不会派发 <see cref="OnPointerExit"/> 而导致高亮残留的场景。
        /// </summary>
        public void ClearHoverHighlight()
        {
            _hoverFade.Kill();
            if (!hoverBorder) return;
            var c = hoverBorder.color; c.a = 0f; hoverBorder.color = c;
        }

        #endregion

        #region 堆叠数量
        [Header("堆叠已满提示")]
        [Tooltip("堆叠已满时显示的提示图标（通过 Color.a 控制透明度，初始 alpha=0）。")]
        public Image stackFullIcon;
        [Tooltip("堆叠已满提示淡入淡出时长（秒）。")]
        public float stackFullFadeDuration = 0.15f;

        private ToolkitTweenHandle _stackFullFade; // 堆叠已满提示淡入淡出句柄，用于打断正在进行的动画

        /// <summary>设置堆叠已满图标的显示状态（可选淡入淡出动画）。</summary>
        protected void SetStackFull(bool isFull, bool animate)
        {
            if (!stackFullIcon) return;
            _stackFullFade.Kill();
            float target = isFull ? 1f : 0f;
            // GO 非激活时淡入不可见，直接设值
            if (animate && gameObject.activeInHierarchy)
                _stackFullFade = ToolkitTween.FadeGraphic(stackFullIcon, target, stackFullFadeDuration);
            else
            {
                var c = stackFullIcon.color; c.a = target; stackFullIcon.color = c;
            }
        }

        /// <summary>立即隐藏堆叠已满图标（无动画，安全调用不受激活状态影响）。</summary>
        protected void ClearStackFull()
        {
            if (!stackFullIcon) return;
            _stackFullFade.Kill();
            var c = stackFullIcon.color; c.a = 0f; stackFullIcon.color = c;
        }

        #endregion

        #region 拖拽整理

        [Header("拖拽整理")]
        [Tooltip("拖拽整理事件中转组件（预制体中配置，由 UiwInventoryItemGridList 控制启停）。")]
        public GridCellDragHandler dragHandler;
        [Tooltip("拖拽中图标的透明度（0=完全透明，1=不透明）。")]
        public float dragIconAlpha = 0.6f;

        /// <summary>设置图标 alpha（用于拖拽开始/结束状态切换）。</summary>
        public void SetIconAlpha(float alpha)
        {
            if (!iconImage) return;
            var c = iconImage.color;
            c.a = alpha;
            iconImage.color = c;
        }

        #endregion
    }
}

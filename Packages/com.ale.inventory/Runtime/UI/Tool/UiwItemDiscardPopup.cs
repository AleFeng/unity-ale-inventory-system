#if ATK_TMP
using UiText = TMPro.TMP_Text;
#else
using UiText = UnityEngine.UI.Text;
#endif

using Ale.Toolkit.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具丢弃弹窗（场景全局单例）：右键菜单「丢弃」的落点。中间一条 <see cref="Slider"/> 调整丢弃数量
    /// （最小 1，最大为<b>该槽位</b>当前堆叠数），底部「丢弃 / 取消」。
    ///
    /// <para>确认后经 <see cref="InventoryRuntimeManager.TryRemoveItem"/> 按槽位精确扣减；界面刷新无需本类操心——
    /// 该方法自会派发 <c>OnInventoryChanged</c>，各视图已订阅。</para>
    ///
    /// <para>预制体配置在 <see cref="InventoryRuntimeManager"/> 上，运行时由其全局实例化一次并经
    /// <see cref="InventoryRuntimeManager.ShowItemDiscardPopup"/> 统一对外调用（实现 <see cref="IItemDiscardPopup"/>）。
    /// 「遮罩 + 淡入淡出 + 关闭按钮 + ESC」整套外壳来自 <see cref="UiwModalPopupBase"/>。</para>
    /// </summary>
    public class UiwItemDiscardPopup : UiwModalPopupBase, IItemDiscardPopup
    {
        /// <summary>场景全局单例（最近一次 Awake 的实例）。</summary>
        public static UiwItemDiscardPopup Instance { get; private set; }

        #region 子组件

        [Header("子组件")]
        [Tooltip("要丢弃的道具预览（图标 / 名称 / 数量）。可空。")]
        public UiwInventoryItemSimple itemPreview;
        [Tooltip("丢弃数量滑杆。运行时强制 wholeNumbers 与 [1, 该槽堆叠数] 范围。")]
        public Slider amountSlider;
        [Tooltip("当前丢弃数量文本（按 amountFormat 渲染）。可空。")]
        public UiText amountText;
        [Tooltip("确认丢弃按钮。")]
        public Button confirmButton;
        [Tooltip("取消按钮（等同关闭）。")]
        public Button cancelButton;

        [Header("文案")]
        [Tooltip("数量文本格式：{0} = 当前选择数量，{1} = 该槽位总数量。")]
        public string amountFormat = "{0} / {1}";

        #endregion

        #region 状态

        private ItemContextTarget _target;
        private int               _max;      // 该槽位当前堆叠数（打开那一刻取的实时值）
        private int               _amount;   // 当前选择的丢弃数量

        /// <summary>当前选择的丢弃数量（未打开时为 0）。</summary>
        public int Amount => _amount;

        #endregion

        #region 生命周期

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void OnInit()
        {
            if (confirmButton) confirmButton.onClick.AddListener(Confirm);
            if (cancelButton)  cancelButton.onClick.AddListener(Close);
            if (amountSlider)
            {
                amountSlider.wholeNumbers = true;   // 数量必须取整：滑到 3.7 个没有意义
                amountSlider.onValueChanged.AddListener(HandleSliderChanged);
            }
        }

        #endregion

        #region 对外接口

        /// <summary>对该槽位弹出丢弃数量选择。目标无效、或该槽位已空时不弹。</summary>
        public void Show(ItemContextTarget target)
        {
            if (!target.IsValid) { Hide(); return; }

            var mgr = InventoryRuntimeManager.Instance;
            if (mgr == null) return;

            // 上限取「打开这一刻的实时槽位数量」而非菜单里带来的 Count：格子的显示值可能已经过时
            // （菜单弹出后仓库仍可被其它逻辑改动），按过时上限丢弃会让玩家看到与预期不符的结果。
            var slot = mgr.GetSlot(target.InventoryId, target.SlotId);
            int live = slot != null ? slot.count : 0;
            if (live <= 0) { Hide(); return; }

            _target = target;
            _max    = live;
            Open();
        }

        /// <summary>关闭弹窗。未打开时为无操作。</summary>
        public void Hide() => Close();

        #endregion

        #region 内容与交互

        /// <summary>写入预览与滑杆范围（此时根节点已激活、尚未淡入）。</summary>
        protected override void OnOpening()
        {
            if (itemPreview) itemPreview.SetItem(_target.ItemId, _max);

            // 默认选 1 而非全部：丢弃不可撤销，默认值应当是最保守的那个。
            SetAmount(1);

            if (amountSlider)
            {
                amountSlider.minValue = 1;
                amountSlider.maxValue = _max;
                amountSlider.SetValueWithoutNotify(_amount);
                // 只有 1 个时滑杆没有可调空间，置灰以示「数量已定」（确认按钮仍可用）。
                amountSlider.interactable = _max > 1;
            }

            RefreshAmountText();
        }

        /// <summary>完全隐藏后清空内容，释放预览的图标句柄。</summary>
        protected override void OnClosed()
        {
            if (itemPreview) itemPreview.SetEmpty();
            _target = default;
            _max    = 0;
            _amount = 0;
        }

        private void HandleSliderChanged(float value)
        {
            SetAmount(Mathf.RoundToInt(value));
            RefreshAmountText();
        }

        private void SetAmount(int value) => _amount = Mathf.Clamp(value, 1, Mathf.Max(1, _max));

        private void RefreshAmountText()
        {
            if (amountText) amountText.text = string.Format(amountFormat, _amount, _max);
        }

        /// <summary>确认丢弃：按槽位精确扣减选定数量，随后关闭弹窗。</summary>
        private void Confirm()
        {
            var mgr = InventoryRuntimeManager.Instance;
            if (mgr != null && _target.IsValid && _amount > 0)
                mgr.TryRemoveItem(_target.InventoryId, _target.SlotId, _amount);

            Close();
        }

        #endregion
    }
}

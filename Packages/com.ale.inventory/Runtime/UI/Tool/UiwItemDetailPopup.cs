using Ale.Toolkit.Runtime.UI;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具详情弹窗（场景全局单例）：右键菜单「查看」的落点。复用 <see cref="UiwInventoryItemDetail"/> 渲染道具详情，
    /// 与悬停弹窗 <see cref="UiwItemTooltip"/> 用的是同一个渲染组件，只是外壳不同。
    ///
    /// <para><b>与悬停弹窗的区别</b>：本弹窗是<b>常驻</b>的——靠右上角关闭按钮 / 点击遮罩 / ESC 收起，
    /// 不随光标移开自动消失；且会拦截射线（悬停弹窗刻意不拦，以免挡住下方条目的悬停判定）。</para>
    ///
    /// <para>预制体配置在 <see cref="InventoryRuntimeManager"/> 上，运行时由其全局实例化一次并经
    /// <see cref="InventoryRuntimeManager.ShowItemDetailPopup"/> 统一对外调用（实现 <see cref="IItemDetailPopup"/>）。
    /// 「遮罩 + 淡入淡出 + 关闭按钮 + ESC」整套外壳来自 <see cref="UiwModalPopupBase"/>。</para>
    /// </summary>
    public class UiwItemDetailPopup : UiwModalPopupBase, IItemDetailPopup
    {
        /// <summary>场景全局单例（最近一次 Awake 的实例）。</summary>
        public static UiwItemDetailPopup Instance { get; private set; }

        [Header("子组件")]
        [Tooltip("渲染道具详情的组件（复用列表格子 UiwInventoryItemDetail）。")]
        public UiwInventoryItemDetail detail;

        // 本次显示的道具：Show 时缓存，OnOpening（根节点已激活）时才写入渲染组件。
        private string _itemId;
        private int    _count;

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
            // 关掉内嵌详情自己的悬停弹窗：本弹窗会拦截射线，鼠标停在详情上会触发它的悬停逻辑，
            // 弹出一个内容完全相同的悬停弹窗压在本弹窗上。悬停弹窗那边没这个问题（它不拦射线）。
            if (detail) detail.showDetailTooltip = false;
        }

        #endregion

        #region 对外接口

        /// <summary>显示指定道具的详情。<paramref name="count"/> 为持有数量（显示在数量文本）。道具 ID 为空时等同 <see cref="Hide"/>。</summary>
        public void Show(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId)) { Hide(); return; }

            _itemId = itemId;
            _count  = count;
            Open();
        }

        /// <summary>关闭弹窗。未打开时为无操作。</summary>
        public void Hide() => Close();

        #endregion

        #region 内容

        /// <summary>写入详情内容（此时根节点已激活、尚未淡入）。</summary>
        protected override void OnOpening()
        {
            // 仓库 ID / 槽位 ID 传空：弹窗里的这一行只用于展示，不应再成为右键菜单的操作目标
            // （ItemContextTarget.IsValid 会因此判否，右键它不会再套一层菜单）。
            if (detail) detail.SetSlot(null, new RuntimeItemSlot(null, _itemId, _count));
        }

        /// <summary>完全隐藏后清空内容，释放图标句柄。</summary>
        protected override void OnClosed()
        {
            if (detail) detail.SetEmpty();
            _itemId = null;
            _count  = 0;
        }

        #endregion
    }
}

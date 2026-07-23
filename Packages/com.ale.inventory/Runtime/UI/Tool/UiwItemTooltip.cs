using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具信息悬停弹窗（场景全局单例）。复用 <see cref="UiwInventoryItemDetail"/> 渲染道具详情，全局共用一个实例，
    /// 避免大量弹窗预制体常驻内存影响性能。
    ///
    /// <para>预制体配置在 <see cref="InventoryRuntimeManager"/> 上，运行时由其全局实例化一次并经
    /// <see cref="InventoryRuntimeManager.ShowItemTooltip"/> / <see cref="InventoryRuntimeManager.HideItemTooltip"/>
    /// 统一对外调用（实现 <see cref="IItemTooltip"/>）；本组件 Awake 时也注册 <see cref="Instance"/> 供直接访问。</para>
    ///
    /// <para>光标定位、淡入淡出与「淡出期间的待显示队列」均来自
    /// <see cref="UiwTooltipBase{TPayload}"/>；本类只负责内容渲染。</para>
    /// </summary>
    public class UiwItemTooltip : UiwTooltipBase<RuntimeItemSlot>, IItemTooltip
    {
        /// <summary>场景全局单例（最近一次 Awake 的实例）。</summary>
        public static UiwItemTooltip Instance { get; private set; }

        [Header("子组件")]
        [Tooltip("渲染道具详情的组件（复用列表格子 UiwInventoryItemDetail）。")]
        public UiwInventoryItemDetail detail;

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

        #endregion

        #region 显示与内容

        /// <summary>在光标处（屏幕坐标）显示指定道具的详情弹窗并淡入。count 为持有数量（显示在数量文本）。itemId 为空等同 <see cref="UiwTooltipBase{TPayload}.Hide"/>。</summary>
        public void Show(string itemId, int count, Vector2 screenPos)
            => ShowTooltip(string.IsNullOrEmpty(itemId) ? null : new RuntimeItemSlot(null, itemId, count), screenPos);

        protected override void ApplyContent(RuntimeItemSlot payload)
        {
            if (detail) detail.SetSlot(null, payload);
        }

        protected override void ClearContent()
        {
            if (detail) detail.SetEmpty();
        }

        #endregion
    }
}

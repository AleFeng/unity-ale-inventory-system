using Ale.Toolkit.Runtime;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// <see cref="InventoryRuntimeManager"/> 的 UI 宿主分部：覆盖式 UI（弹窗 / 拖拽幽灵 / 下拉窗）的
    /// 根节点与 Layer 设置，以及全局唯一的道具悬停弹窗的持有与惰性实例化。
    ///
    /// <para>弹窗以接口（<see cref="IItemTooltip"/>）持有，
    /// 使本管理器不反向依赖 UI 程序集（依赖倒置）。</para>
    /// </summary>
    public partial class InventoryRuntimeManager
    {

        [Header("UI设置")]
        [Tooltip("弹窗、幽灵图标、下拉窗等覆盖式UI的根节点。为空则运行时自动查找场景中首个 Canvas。")]
        [SerializeField] private Transform coverUiRoot;

        [Tooltip("是否将覆盖式UI（弹窗 / 幽灵图标等）强制设置到下方指定的 Layer。\n" +
                 "当使用独立 UI 摄像机、且其 Culling Mask 仅渲染 UI 层时开启：弹窗 / 幽灵等会分配独立 Canvas，" +
                 "其 Layer 可能与父级不一致，需在实例化后重新指定，UI 摄像机方可渲染。")]
        [SerializeField] private bool applyCoverUiLayer;

        [Layer]
        [Tooltip("覆盖式UI 强制设置到的 Layer（如 UI）。仅当上方开关开启时生效。")]
        [SerializeField] private int coverUiLayer;

        /// <summary>
        /// 设置 覆盖UI根节点
        /// </summary>
        /// <param name="parent"></param>
        public void SetCoverUiRoot(Transform parent)
        {
            coverUiRoot = parent;
        }

        /// <summary>
        /// 设置 覆盖式UI 强制 Layer（同时开启强制开关）。layer 会被约束到 0~31。
        /// </summary>
        public void SetCoverUiLayer(int layer)
        {
            coverUiLayer      = CoverUiUtil.ClampLayer(layer);
            applyCoverUiLayer = true;
        }

        /// <summary>
        /// 按层名设置 覆盖式UI 强制 Layer（同时开启强制开关）。层名不存在则记警告且不改动。
        /// </summary>
        public void SetCoverUiLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[InventoryRuntimeManager] Layer 名称 \"{layerName}\" 不存在，未设置 覆盖式UI Layer。");
                return;
            }
            SetCoverUiLayer(layer);
        }

        /// <summary>关闭「覆盖式UI 强制 Layer」（此后不再改动覆盖式UI的 Layer）。</summary>
        public void DisableCoverUiLayer() => applyCoverUiLayer = false;

        /// <summary>
        /// 将指定 覆盖式UI 对象（及其所有子级）递归设置到配置的 Layer。
        /// 弹窗、幽灵图标等在实例化 / 创建后统一调用；未开启强制开关时为无操作。
        /// 递归是必要的：带独立 Canvas 的子级也须落到目标 Layer，UI 摄像机（仅渲染该 Layer）方可渲染。
        /// </summary>
        public void ApplyCoverUiLayer(GameObject go)
        {
            if (!go || !applyCoverUiLayer) return;
            CoverUiUtil.SetLayerRecursively(go, coverUiLayer);
        }

        #region 覆盖式UI挂件的惰性实例化

        /// <summary>
        /// 覆盖式 UI 挂件（悬停弹窗 / 右键菜单 / 各类弹窗）的惰性实例化通用实现：
        /// 按预制体实例化到 <see cref="coverUiRoot"/>、置于最上层、按需强制 Layer，再取其根节点上的接口实现。
        ///
        /// <para><paramref name="resolved"/> 使「未配置预制体」也只判定一次——否则每次调用都会重新尝试并重复告警。</para>
        ///
        /// <para>预制体根节点<b>允许以未激活状态保存</b>（弹窗正是如此）：<c>Instantiate</c> 出的实例同样未激活，
        /// <c>GetComponent</c> 照常取得到，实际的初始化在挂件自己首次显示时进行。</para>
        /// </summary>
        /// <param name="prefab">挂件预制体（可空）。</param>
        /// <param name="cache">缓存字段。</param>
        /// <param name="resolved">「是否已解析过」标记字段。</param>
        /// <param name="prefabField">预制体字段名，仅用于告警文案。</param>
        private T EnsureCoverUiWidget<T>(GameObject prefab, ref T cache, ref bool resolved, string prefabField)
            where T : class
        {
            if (resolved) return cache;
            resolved = true;

            if (!prefab) return cache = null;

            var parent = coverUiRoot ? coverUiRoot : CoverUiUtil.FindCanvasTransform();
            var go     = parent ? Instantiate(prefab, parent) : Instantiate(prefab);
            go.transform.SetAsLastSibling();   // 置于父级最上层渲染
            ApplyCoverUiLayer(go);             // 覆盖式UI：按需强制到指定 Layer（如 UI）

            cache = go.GetComponent<T>();
            if (cache == null)
                Debug.LogWarning($"[InventoryRuntimeManager] {prefabField} 根节点未实现 {typeof(T).Name}，该挂件不可用。");
            return cache;
        }

        #endregion

        #region 道具悬停弹窗

        [Header("道具悬停弹窗")]
        [Tooltip("道具悬停详情弹窗预制体：运行时由本管理器全局实例化一次。其根节点需实现 IItemTooltip（如 UI 层 UiwItemTooltip）。可空。")]
        [SerializeField] private GameObject itemTooltipPrefab;

        private IItemTooltip _itemTooltip;
        private bool         _itemTooltipResolved;

        /// <summary>
        /// 全局道具悬停弹窗（首次访问时按 <see cref="itemTooltipPrefab"/> 懒实例化一次）。
        /// 未配置预制体时为 null。UI 层经本管理器统一调用，将全局共用功能集中于此管理。
        /// </summary>
        public IItemTooltip ItemTooltip => EnsureItemTooltip();

        private IItemTooltip EnsureItemTooltip()
            => EnsureCoverUiWidget(itemTooltipPrefab, ref _itemTooltip, ref _itemTooltipResolved, nameof(itemTooltipPrefab));

        /// <summary>在光标处（屏幕坐标）显示指定道具的悬停详情弹窗（全局统一入口）。count 为持有数量（显示在数量文本）。</summary>
        public void ShowItemTooltip(string itemId, int count, Vector2 screenPos)
            => EnsureItemTooltip()?.Show(itemId, count, screenPos);

        /// <summary>隐藏（原位淡出）道具悬停弹窗。未实例化时为无操作。</summary>
        public void HideItemTooltip()
        {
            if (_itemTooltipResolved) _itemTooltip?.Hide();
        }

        #endregion

        #region 道具操作：右键菜单 / 详情弹窗 / 丢弃弹窗

        [Header("道具操作弹窗")]
        [Tooltip("道具右键操作菜单预制体（查看 / 使用 / 丢弃）。根节点需实现 IItemContextMenu（如 UI 层 UiwItemContextMenu）。可空。")]
        [SerializeField] private GameObject itemContextMenuPrefab;
        [Tooltip("道具详情弹窗预制体（右键菜单「查看」）。根节点需实现 IItemDetailPopup（如 UI 层 UiwItemDetailPopup）。可空。")]
        [SerializeField] private GameObject itemDetailPopupPrefab;
        [Tooltip("道具丢弃弹窗预制体（右键菜单「丢弃」）。根节点需实现 IItemDiscardPopup（如 UI 层 UiwItemDiscardPopup）。可空。")]
        [SerializeField] private GameObject itemDiscardPopupPrefab;

        private IItemContextMenu  _itemContextMenu;
        private bool              _itemContextMenuResolved;
        private IItemDetailPopup  _itemDetailPopup;
        private bool              _itemDetailPopupResolved;
        private IItemDiscardPopup _itemDiscardPopup;
        private bool              _itemDiscardPopupResolved;

        /// <summary>全局道具右键操作菜单（懒实例化；未配置预制体时为 null）。</summary>
        public IItemContextMenu ItemContextMenu => EnsureItemContextMenu();
        /// <summary>全局道具详情弹窗（懒实例化；未配置预制体时为 null）。</summary>
        public IItemDetailPopup ItemDetailPopup => EnsureItemDetailPopup();
        /// <summary>全局道具丢弃弹窗（懒实例化；未配置预制体时为 null）。</summary>
        public IItemDiscardPopup ItemDiscardPopup => EnsureItemDiscardPopup();

        private IItemContextMenu EnsureItemContextMenu()
            => EnsureCoverUiWidget(itemContextMenuPrefab, ref _itemContextMenu, ref _itemContextMenuResolved, nameof(itemContextMenuPrefab));

        private IItemDetailPopup EnsureItemDetailPopup()
            => EnsureCoverUiWidget(itemDetailPopupPrefab, ref _itemDetailPopup, ref _itemDetailPopupResolved, nameof(itemDetailPopupPrefab));

        private IItemDiscardPopup EnsureItemDiscardPopup()
            => EnsureCoverUiWidget(itemDiscardPopupPrefab, ref _itemDiscardPopup, ref _itemDiscardPopupResolved, nameof(itemDiscardPopupPrefab));

        /// <summary>在光标处弹出道具右键操作菜单（道具格子右键的统一入口）。目标无效时由菜单自行忽略。</summary>
        public void ShowItemContextMenu(ItemContextTarget target, Vector2 screenPos)
            => EnsureItemContextMenu()?.Show(target, screenPos);

        /// <summary>关闭道具右键操作菜单。未实例化时为无操作。</summary>
        public void HideItemContextMenu()
        {
            if (_itemContextMenuResolved) _itemContextMenu?.Hide();
        }

        /// <summary>显示道具详情弹窗（右键菜单「查看」）。</summary>
        public void ShowItemDetailPopup(string itemId, int count)
            => EnsureItemDetailPopup()?.Show(itemId, count);

        /// <summary>关闭道具详情弹窗。未实例化时为无操作。</summary>
        public void HideItemDetailPopup()
        {
            if (_itemDetailPopupResolved) _itemDetailPopup?.Hide();
        }

        /// <summary>对指定槽位弹出丢弃数量选择弹窗（右键菜单「丢弃」）。</summary>
        public void ShowItemDiscardPopup(ItemContextTarget target)
            => EnsureItemDiscardPopup()?.Show(target);

        /// <summary>关闭道具丢弃弹窗。未实例化时为无操作。</summary>
        public void HideItemDiscardPopup()
        {
            if (_itemDiscardPopupResolved) _itemDiscardPopup?.Hide();
        }

        #endregion

    }
}

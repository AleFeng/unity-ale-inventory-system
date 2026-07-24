using Ale.Toolkit.Runtime;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// <see cref="InventoryRuntimeManager"/> 的 UI 宿主分部：覆盖式 UI（弹窗 / 拖拽幽灵 / 下拉窗）的
    /// 根节点与 Layer 设置，以及全局唯一的道具 / 技能悬停弹窗的持有与惰性实例化。
    ///
    /// <para>弹窗以接口（<see cref="IItemTooltip"/> / <see cref="ISkillTooltip"/>）持有，
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
        {
            if (_itemTooltipResolved) return _itemTooltip;
            _itemTooltipResolved = true;

            if (!itemTooltipPrefab) return _itemTooltip = null;

            var parent = coverUiRoot ? coverUiRoot : CoverUiUtil.FindCanvasTransform();
            var go     = parent ? Instantiate(itemTooltipPrefab, parent) : Instantiate(itemTooltipPrefab);
            go.transform.SetAsLastSibling();   // 置于父级最上层渲染
            ApplyCoverUiLayer(go);             // 覆盖式UI：按需强制到指定 Layer（如 UI）
            _itemTooltip = go.GetComponent<IItemTooltip>();
            if (_itemTooltip == null)
                Debug.LogWarning("[InventoryRuntimeManager] itemTooltipPrefab 根节点未实现 IItemTooltip（如 UiwItemTooltip），悬停弹窗不可用。");
            return _itemTooltip;
        }

        /// <summary>在光标处（屏幕坐标）显示指定道具的悬停详情弹窗（全局统一入口）。count 为持有数量（显示在数量文本）。</summary>
        public void ShowItemTooltip(string itemId, int count, Vector2 screenPos)
            => EnsureItemTooltip()?.Show(itemId, count, screenPos);

        /// <summary>隐藏（原位淡出）道具悬停弹窗。未实例化时为无操作。</summary>
        public void HideItemTooltip()
        {
            if (_itemTooltipResolved) _itemTooltip?.Hide();
        }

        #endregion

        #region 技能悬停弹窗

        [Header("技能悬停弹窗")]
        [Tooltip("技能悬停详情弹窗预制体：运行时由本管理器全局实例化一次。其根节点需实现 ISkillTooltip（如 UI 层 UiwSkillTooltip）。可空。父节点复用上方「弹窗实例的父节点」。")]
        [SerializeField] private GameObject skillTooltipPrefab;

        private ISkillTooltip _skillTooltip;
        private bool          _skillTooltipResolved;

        /// <summary>
        /// 全局技能悬停弹窗（首次访问时按 <see cref="skillTooltipPrefab"/> 懒实例化一次）。
        /// 未配置预制体时为 null。UI 层经本管理器统一调用，将全局共用功能集中于此管理。
        /// </summary>
        public ISkillTooltip SkillTooltip => EnsureSkillTooltip();

        private ISkillTooltip EnsureSkillTooltip()
        {
            if (_skillTooltipResolved) return _skillTooltip;
            _skillTooltipResolved = true;

            if (!skillTooltipPrefab) return _skillTooltip = null;

            var parent = coverUiRoot ? coverUiRoot : CoverUiUtil.FindCanvasTransform();
            var go     = parent ? Instantiate(skillTooltipPrefab, parent) : Instantiate(skillTooltipPrefab);
            go.transform.SetAsLastSibling();   // 置于父级最上层渲染
            ApplyCoverUiLayer(go);             // 覆盖式UI：按需强制到指定 Layer（如 UI）
            _skillTooltip = go.GetComponent<ISkillTooltip>();
            if (_skillTooltip == null)
                Debug.LogWarning("[InventoryRuntimeManager] skillTooltipPrefab 根节点未实现 ISkillTooltip（如 UiwSkillTooltip），技能悬停弹窗不可用。");
            return _skillTooltip;
        }

        /// <summary>在光标处（屏幕坐标）显示指定技能的悬停详情弹窗（全局统一入口）。</summary>
        public void ShowSkillTooltip(Skill skill, Vector2 screenPos)
            => EnsureSkillTooltip()?.Show(skill, screenPos);

        /// <summary>隐藏（原位淡出）技能悬停弹窗。未实例化时为无操作。</summary>
        public void HideSkillTooltip()
        {
            if (_skillTooltipResolved) _skillTooltip?.Hide();
        }

        #endregion

    }
}

#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 道具显示组件基类。
    /// 持有图标、数量、名称、品质背景、数字格式等各子类共用的字段与辅助方法。
    /// <para>名称 / 品质背景为「可选显示能力」：预制体中未配置 <see cref="nameText"/> /
    /// <see cref="qualityBackground"/> 时，对应 Apply 方法静默跳过，不影响其它显示。</para>
    /// <para>悬停信息弹窗为「可选能力」：启用 <see cref="showDetailTooltip"/> 且子类绑定道具时调用过
    /// <see cref="SetTooltipItemId"/>，鼠标悬停即弹出该道具的详情。弹窗为全局唯一，经
    /// <see cref="InventoryRuntimeManager.ShowItemTooltip"/> / <see cref="InventoryRuntimeManager.HideItemTooltip"/> 统一调用。</para>
    /// </summary>
    public abstract class UiwInventoryItemBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region 道具信息
        
        [Header("道具信息")]
        [Tooltip("道具名称文本。预制体中未配置则不显示名称。")]
        public InventoryText nameText;
        [Tooltip("图标图片。")]
        public Image         iconImage;
        [Tooltip("品质背景框图片，Sprite 从对应品质枚举项的属性中读取。预制体中未配置则不显示品质背景。")]
        public Image         qualityBackground;
        [Tooltip("数量文本。")]
        public InventoryText countText;
        
        [Header("道具信息-属性字段ID")]
        [Tooltip("名称属性 ID。")]
        public string nameAttrId              = "名称";
        [Tooltip("图标属性 ID（存储 Sprite）。")]
        public string iconAttrId              = "图标";
        [Tooltip("品质属性 ID。")]
        public string qualityAttrId           = "品质";
        [Tooltip("品质背景属性 ID（从品质枚举项的该属性读取背景 Sprite）。")]
        public string qualityBackgroundAttrId = "背景框";
        
        /// <summary>
        /// 读取 item 名称写入 <see cref="nameText"/>。未配置 nameText 时静默跳过（即不显示名称）；
        /// 名称为空时回退到 <paramref name="fallback"/>（通常传道具 ID）。
        /// </summary>
        protected void ApplyName(Item item, string fallback = null)
        {
            if (!nameText) return;
            string display = item != null && !string.IsNullOrEmpty(nameAttrId)
                ? (item.GetEntry(nameAttrId)?.value?.AsString ?? fallback)
                : fallback;
            nameText.text = display ?? string.Empty;
        }
        
        // 图标 / 品质背景异步加载的世代号：每次绑定自增，回调据此丢弃过期结果，
        // 避免对象池快速复用时旧的异步加载回调覆盖新内容（Addressable 模式）。
        private int _iconGen;
        private int _qualityBgGen;

        /// <summary>
        /// 从 item 属性中读取图标 Sprite 并写入 iconImage：经 <see cref="InventoryAssets"/> 门面
        /// （直接模式同步赋值；Addressable 模式异步加载完成后赋值）。item 为 null / 无图标时清空。
        /// </summary>
        protected void ApplyIcon(Item item)
        {
            if (!iconImage) return;
            var owner = iconImage.gameObject;
            InventoryAssets.Release(owner);      // 释放上次绑定的句柄（对象池复用）
            int gen = ++_iconGen;

            var entry = item != null && !string.IsNullOrEmpty(iconAttrId) ? item.GetEntry(iconAttrId) : null;
            if (entry?.value == null) { iconImage.sprite = null; iconImage.enabled = false; return; }

            InventoryAssets.Bind<Sprite>(entry.value, owner, s =>
            {
                if (gen != _iconGen || !iconImage) return;   // 过期结果丢弃
                iconImage.sprite  = s;
                iconImage.enabled = s;
            });
        }

        /// <summary>清空图标显示（并释放异步句柄、作废未完成的加载回调）。</summary>
        protected void ClearIcon()
        {
            if (!iconImage) return;
            InventoryAssets.Release(iconImage.gameObject);
            _iconGen++;
            iconImage.sprite  = null;
            iconImage.enabled = false;
        }
        
        /// <summary>
        /// 从 item 的品质枚举属性中读取背景 Sprite 并写入 <see cref="qualityBackground"/>。
        /// 未配置 qualityBackground 时静默跳过（即不显示品质背景）。
        /// </summary>
        protected void ApplyQualityBackground(Item item)
        {
            if (!qualityBackground) return;
            var owner = qualityBackground.gameObject;
            InventoryAssets.Release(owner);
            int gen = ++_qualityBgGen;

            qualityBackground.enabled = true;
            if (item == null || string.IsNullOrEmpty(qualityAttrId)) { qualityBackground.sprite = null; return; }

            var qualityAv = item.GetEntry(qualityAttrId)?.value;
            if (qualityAv == null || string.IsNullOrEmpty(qualityBackgroundAttrId)) { qualityBackground.sprite = null; return; }

            int qVal     = qualityAv.AsEnumValue;
            var enumType = InventoryDataManager.Instance.GetEnumType(qualityAv.EnumTypeRef);
            var enumItem = enumType?.GetItemByValue(qVal);
            var bgEntry  = enumItem?.GetEntry(qualityBackgroundAttrId);
            if (bgEntry?.value == null) { qualityBackground.sprite = null; return; }

            InventoryAssets.Bind<Sprite>(bgEntry.value, owner, s =>
            {
                if (gen != _qualityBgGen || !qualityBackground) return;
                qualityBackground.sprite = s;
            });
        }

        /// <summary>清空名称与品质背景显示（供空态 / 对象池回收复用）。</summary>
        protected void ClearNameAndQuality()
        {
            if (nameText) nameText.text = string.Empty;
            if (qualityBackground)
            {
                InventoryAssets.Release(qualityBackground.gameObject);
                _qualityBgGen++;
                qualityBackground.sprite  = null;
                qualityBackground.enabled = false;
            }
        }

        #endregion

        #region 悬停信息弹窗

        [Header("悬停信息弹窗")]
        [Tooltip("启用后，鼠标悬停在本道具显示上时，经所属 View 的 itemTooltip 弹出该道具的详情弹窗。默认关闭。")]
        public bool showDetailTooltip;

        // 当前绑定道具 ID（由子类在绑定 / 清空道具时通过 SetTooltipItemId 设置），供悬停弹窗使用。
        private string _tooltipItemId;
        // 当前绑定道具的持有数量（随 SetTooltipItemId 一并记录），悬停时透传给弹窗显示。
        private int    _tooltipCount;
        // 本格当前是否正显示（由本格触发且尚未隐藏）详情弹窗。用于在本格因刷新被清空 / 停用时主动关闭弹窗——
        // 快速装备等会在悬停时移除本格道具并刷新（甚至停用本格），使本格收不到 PointerExit，导致弹窗残留。
        private bool _tooltipShown;

        /// <summary>
        /// 由子类在绑定 / 清空道具时调用，记录当前道具 ID 与持有数量供悬停弹窗使用（传 null 清空）。
        /// 若正悬停显示本格弹窗且道具发生变化（被移除 / 换成别的道具），主动关闭残留弹窗。
        /// </summary>
        protected void SetTooltipItemId(string itemId, int count = 0)
        {
            if (_tooltipShown && itemId != _tooltipItemId) HideTooltipIfShowing();
            _tooltipItemId = itemId;
            _tooltipCount  = count;
        }

        /// <summary>
        /// 鼠标进入：启用弹窗且已绑定道具时，经 <see cref="InventoryRuntimeManager"/> 在光标处显示该道具的详情弹窗。
        /// 子类可覆写以叠加其它悬停行为（覆写时请调用 base 以保留弹窗能力）。
        /// </summary>
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (!showDetailTooltip || string.IsNullOrEmpty(_tooltipItemId)) return;
            Vector2 pos = eventData != null ? eventData.position : Input.mousePosition;
            if (InventoryRuntimeManager.Instance)
            {
                InventoryRuntimeManager.Instance.ShowItemTooltip(_tooltipItemId, _tooltipCount, pos);
                _tooltipShown = true;
            }
        }

        /// <summary>鼠标移出：经 <see cref="InventoryRuntimeManager"/> 隐藏详情弹窗。子类覆写时请调用 base 以保留弹窗能力。</summary>
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (!showDetailTooltip) return;
            _tooltipShown = false;
            if (InventoryRuntimeManager.Instance)
                InventoryRuntimeManager.Instance.HideItemTooltip();
        }

        /// <summary>
        /// 本物体被停用时，若本格正显示着详情弹窗则关闭之：停用不会派发 <see cref="OnPointerExit"/>，
        /// 会导致弹窗残留（如快速装备后本格被隐藏、或所属面板被关闭）。子类覆写时请调用 base 以保留此清理。
        /// </summary>
        protected virtual void OnDisable() => HideTooltipIfShowing();

        /// <summary>若本格当前正显示详情弹窗，则隐藏并复位标记（仅隐藏由本格触发的弹窗，不误伤其它格）。</summary>
        private void HideTooltipIfShowing()
        {
            if (!_tooltipShown) return;
            _tooltipShown = false;
            if (InventoryRuntimeManager.Instance)
                InventoryRuntimeManager.Instance.HideItemTooltip();
        }

        #endregion

        #region 数字格式

        [Header("数字格式")]
        [Tooltip("数字显示格式（由 UiwInventoryView 根据当前仓库配置和语言自动赋值）。")]
        [HideInInspector] public NumberFormatLocale numberFormat;

        /// <summary>根据 locale 规则格式化数值；无 locale 时退回 ToString()。</summary>
        protected string FormatNumber(long value)
        {
            if (numberFormat == null) return value.ToString();
            // 后缀本地化已内建于 NumberFormatRule.ResolveSuffix()（Text：本地化优先、回退纯文本）。
            return numberFormat.Format(value);
        }

        #endregion
    }
}

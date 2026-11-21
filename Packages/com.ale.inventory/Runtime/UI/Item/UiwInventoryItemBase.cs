#if ATK_TMP
using UiText = TMPro.TMP_Text;
#else
using UiText = UnityEngine.UI.Text;
#endif

using Ale.Toolkit.Runtime.UI;
using Ale.Toolkit.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具显示组件基类。
    /// 持有图标、数量、名称、品质背景、数字格式等各子类共用的字段与辅助方法。
    /// <para>名称 / 品质背景为「可选显示能力」：预制体中未配置 <see cref="nameText"/> /
    /// <see cref="qualityBackground"/> 时，对应 Apply 方法静默跳过，不影响其它显示。</para>
    /// <para>悬停信息弹窗为「可选能力」：启用 <see cref="showDetailTooltip"/> 且子类绑定道具时调用过
    /// <see cref="SetTooltipItemId"/>，鼠标悬停即弹出该道具的详情。弹窗为全局唯一，经
    /// <see cref="InventoryRuntimeManager.ShowItemTooltip"/> / <see cref="InventoryRuntimeManager.HideItemTooltip"/> 统一调用；
    /// 进入 / 移出 / 停用三条路径由基类 <see cref="UiwHoverTooltipSource"/> 统一处理。</para>
    /// </summary>
    public abstract class UiwInventoryItemBase : UiwListFadeCell
    {
        #region 道具信息
        
        [Header("道具信息")]
        [Tooltip("道具名称文本。预制体中未配置则不显示名称。")]
        public UiText nameText;
        [Tooltip("图标图片。")]
        public Image         iconImage;
        [Tooltip("品质背景框图片，Sprite 从对应品质枚举项的属性中读取。预制体中未配置则不显示品质背景。")]
        public Image         qualityBackground;
        [Tooltip("数量文本。")]
        public UiText countText;
        
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
        
        // 图标 / 品质背景的异步绑定槽：内建代次守卫，避免对象池快速复用时旧的异步加载回调
        // 覆盖新内容（Addressable 模式）。见 <see cref="SpriteSlot"/>。
        private readonly SpriteSlot _iconSlot      = new SpriteSlot();
        private readonly SpriteSlot _qualityBgSlot = new SpriteSlot();

        /// <summary>
        /// 从 item 属性中读取图标 Sprite 并写入 iconImage：经 <see cref="InventoryAssets"/> 门面
        /// （直接模式同步赋值；Addressable 模式异步加载完成后赋值）。item 为 null / 无图标时清空。
        /// <para><paramref name="animate"/>=true：先杀旧淡入、把图标 alpha 置 0，待 Sprite 就位（可能异步）后淡入；
        /// false：即时置 alpha 1、不淡入（就地刷新 / 非首次显示）。</para>
        /// </summary>
        protected void ApplyIcon(Item item, bool animate = false)
        {
            var entry = item != null && !string.IsNullOrEmpty(iconAttrId) ? item.GetEntry(iconAttrId) : null;
            _iconFade.Kill(false);   // [B5] 先杀旧句柄（两条路径都要）
            if (animate)
            {
                SetImageAlpha(iconImage, 0f);   // [B5] 置 0 须在 Bind 之前（直接模式 onApplied 同步触发）
                _onIconApplied ??= OnIconApplied;
                _iconSlot.Bind(iconImage, entry?.value, onApplied: _onIconApplied);
            }
            else
            {
                SetImageAlpha(iconImage, 1f);
                _iconSlot.Bind(iconImage, entry?.value);
            }
        }

        /// <summary>清空图标显示（并释放异步句柄、作废未完成的加载回调、打断淡入）。</summary>
        protected void ClearIcon()
        {
            _iconFade.Kill(false);
            _iconSlot.Clear(iconImage);
        }

        /// <summary>
        /// 从 item 的品质枚举属性中读取背景 Sprite 并写入 <see cref="qualityBackground"/>。
        /// 未配置 qualityBackground 时静默跳过（即不显示品质背景）。
        /// <para>背景框常驻显示（enabled 恒为 true），未解析出 Sprite 时只是没有贴图 ——
        /// 故绑定时不让 <see cref="SpriteSlot"/> 代管 enabled。</para>
        /// </summary>
        protected void ApplyQualityBackground(Item item, bool animate = false)
        {
            if (!qualityBackground) return;
            qualityBackground.enabled = true;

            AttributeValue bgValue = null;
            if (item != null && !string.IsNullOrEmpty(qualityAttrId) && !string.IsNullOrEmpty(qualityBackgroundAttrId))
            {
                var qualityAv = item.GetEntry(qualityAttrId)?.value;
                if (qualityAv != null)
                {
                    var enumType = InventoryDataManager.Instance.GetEnumType(qualityAv.EnumTypeRef);
                    var enumItem = enumType?.GetItemByValue(qualityAv.AsEnumValue);
                    bgValue = enumItem?.GetEntry(qualityBackgroundAttrId)?.value;
                }
            }

            _qualityBgFade.Kill(false);   // [B5] 先杀旧句柄
            if (animate)
            {
                SetImageAlpha(qualityBackground, 0f);
                _onQualityBgApplied ??= OnQualityBgApplied;
                _qualityBgSlot.Bind(qualityBackground, bgValue, toggleEnabled: false, onApplied: _onQualityBgApplied);
            }
            else
            {
                SetImageAlpha(qualityBackground, 1f);
                _qualityBgSlot.Bind(qualityBackground, bgValue, toggleEnabled: false);
            }
        }

        /// <summary>清空名称与品质背景显示（供空态 / 对象池回收复用）。</summary>
        protected void ClearNameAndQuality()
        {
            if (nameText) nameText.text = string.Empty;
            _qualityBgFade.Kill(false);
            _qualityBgSlot.Clear(qualityBackground);
        }

        #endregion

        #region 图片淡入（Tween）

        [Header("图片淡入（Tween）")]
        [Tooltip("图标 / 背景框 图片就位（可能异步加载完成）后的淡入时长（秒）。")]
        public float imageFadeDuration = 0.15f;

        // 根淡入淡出（整格）已上提到 Toolkit 基类 UiwListFadeCell（rootFadeIn/OutDuration、PlayShowFade、
        // FadeOutAndHide、CancelRootFade、ResetRootVisible），由列表引擎默认 hook 驱动；本处仅保留
        // 图标 / 背景框 的「加载门控」逐图片淡入（inventory 专有）。

        // 图标 / 背景框 各自的在途淡入句柄（打断旧动画、避免重叠写 alpha）。
        private ToolkitTweenHandle _iconFade;
        private ToolkitTweenHandle _qualityBgFade;

        // 缓存的图片就位回调（避免每次绑定分配闭包）。
        private System.Action<bool> _onIconApplied;
        private System.Action<bool> _onQualityBgApplied;

        // 图标就位回调：有图 → 淡入；无图 → SpriteSlot 已禁用该 Image，复位 alpha 以防外部再启用。
        private void OnIconApplied(bool hasSprite)
        {
            if (!iconImage) return;
            _iconFade.Kill(false);
            if (hasSprite) _iconFade = ToolkitTween.FadeGraphic(iconImage, 1f, imageFadeDuration);
            else           SetImageAlpha(iconImage, 1f);
        }

        // 背景框就位回调：有图 → 淡入；无背景图 → 复位为实心底框（alpha 1，保留常驻底框观感）。
        private void OnQualityBgApplied(bool hasSprite)
        {
            if (!qualityBackground) return;
            _qualityBgFade.Kill(false);
            if (hasSprite) _qualityBgFade = ToolkitTween.FadeGraphic(qualityBackground, 1f, imageFadeDuration);
            else           SetImageAlpha(qualityBackground, 1f);
        }

        /// <summary>设置某图片的 alpha（不改变 RGB）。</summary>
        protected static void SetImageAlpha(Image img, float a)
        {
            if (!img) return;
            var c = img.color; c.a = a; img.color = c;
        }

        #endregion

        #region 功能标签

        [Header("功能标签")]
        [Tooltip("道具标签 预制体。")]
        public UiwTextLabel textTagsPrefab;
        [Tooltip("道具标签 容器。用于放置 多个功能标签的UI显示。未配置则不显示功能标签。")]
        public Transform itemTagsContainer;

        /// <summary>功能标签 UI 实例池（只增不减，多余的实例 SetActive(false) 复用）。</summary>
        private readonly UiwWidgetPool<UiwTextLabel> _labelPool = new UiwWidgetPool<UiwTextLabel>();

        private static readonly Action<UiwTextLabel> RecycleLabel = lbl =>
        {
            InventoryAssets.Release(lbl.gameObject);
            lbl.gameObject.SetActive(false);
        };

        // 功能标签背景异步加载世代号：每次刷新自增，回调据此丢弃过期结果（标签实例池化复用）。
        private int _tagGen;

        /// <summary>
        /// 显示道具的功能标签（item.tagRefs ∪ template.tagRefs，跳过 hideInUI 的标签）。
        /// 预制体未配置 <see cref="itemTagsContainer"/> / <see cref="textTagsPrefab"/> 时静默跳过。
        /// </summary>
        protected void ApplyItemLabels(Item item)
        {
            if (!itemTagsContainer || !textTagsPrefab) return;

            // 合并 item.tagRefs ∪ template.tagRefs（保序去重，item 自身优先）
            var tagNames = new List<string>();
            var seen     = new HashSet<string>();
            if (item?.tagRefs != null)
                foreach (var t in item.tagRefs) if (seen.Add(t)) tagNames.Add(t);
            var tmpl = item != null
                ? InventoryDataManager.Instance.GetTemplate(item.templateRef)
                : null;
            if (tmpl?.tagRefs != null)
                foreach (var t in tmpl.tagRefs) if (seen.Add(t)) tagNames.Add(t);

            int gen = ++_tagGen;
            _labelPool.Configure(textTagsPrefab, itemTagsContainer);
            _labelPool.Begin();
            foreach (var tagName in tagNames)
            {
                var functionTag = InventoryDataManager.Instance.GetTag(tagName);
                if (functionTag == null || functionTag.hideInUI) continue;

                string resolved = functionTag.displayNameText != null ? functionTag.displayNameText.ResolveText() : null;
                string text = string.IsNullOrEmpty(resolved) ? functionTag.name : resolved;

                var label = _labelPool.Next();
                if (!label) break;
                InventoryAssets.Release(label.gameObject);
                // 先按直接引用设背景 + 色 + 文本（直接模式即正确）；授权模式 backgroundSprite 为空，随后异步回填。
                label.Setup(functionTag.backgroundSpriteValue.GetObject(0) as Sprite, functionTag.backgroundColor, text);
                var ft = functionTag;
                InventoryAssets.Bind<Sprite>(ft.backgroundSpriteValue, label.gameObject, s =>
                {
                    if (gen != _tagGen || !label || !s) return;   // 过期 / 空结果丢弃（保留 Setup 已设的底图）
                    label.SetBackgroundSprite(s);
                });
            }

            int shown = _labelPool.ActiveCount;
            _labelPool.End(RecycleLabel);

            itemTagsContainer.gameObject.SetActive(shown > 0);
        }

        /// <summary>清空功能标签显示（空态 / 对象池回收时调用）。</summary>
        protected void ClearItemLabels()
        {
            if (itemTagsContainer) itemTagsContainer.gameObject.SetActive(false);
            _tagGen++;
            _labelPool.RecycleAll(RecycleLabel);
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

        protected override bool HoverTooltipEnabled    => showDetailTooltip;
        protected override bool HasHoverTooltipPayload => !string.IsNullOrEmpty(_tooltipItemId);

        protected override void ShowHoverTooltip(Vector2 screenPos)
            => InventoryRuntimeManager.Instance.ShowItemTooltip(_tooltipItemId, _tooltipCount, screenPos);

        protected override void HideHoverTooltip()
            => InventoryRuntimeManager.Instance.HideItemTooltip();

        /// <summary>
        /// 由子类在绑定 / 清空道具时调用，记录当前道具 ID 与持有数量供悬停弹窗使用（传 null 清空）。
        /// 若正悬停显示本格弹窗且道具发生变化（被移除 / 换成别的道具），主动关闭残留弹窗——
        /// 快速装备等会在悬停时移除本格道具并刷新，使本格收不到 PointerExit，导致弹窗残留。
        /// </summary>
        protected void SetTooltipItemId(string itemId, int count = 0)
        {
            if (HoverTooltipShown && itemId != _tooltipItemId) HideHoverTooltipIfShowing();
            _tooltipItemId = itemId;
            _tooltipCount  = count;
        }

        #endregion

        #region 数字格式

        [Header("数字格式")]
        [Tooltip("数字显示格式（由 UiwInventoryView 根据当前仓库配置和语言自动赋值）。")]
        [HideInInspector] public NumberFormatLocale numberFormat;

        /// <summary>
        /// 根据 locale 规则格式化数值；无 locale 时退回 ToString()。
        /// 后缀本地化已内建于 NumberFormatRule.ResolveSuffix()（Text：本地化优先、回退纯文本）。
        /// </summary>
        protected string FormatNumber(long value) => UIFormat.Number(numberFormat, value);

        #endregion
    }
}

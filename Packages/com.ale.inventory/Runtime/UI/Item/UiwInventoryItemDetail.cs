#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具格子详细信息显示组件（MonoBehaviour）。
    /// 由虚拟列表 <see cref="UiwInventoryItemOrderList"/> 统一驱动 <see cref="SetSlot"/> / <see cref="SetEmpty"/>。
    ///
    /// <para>显示内容：品质背景、图标、名称、描述、数量、价格货币列表、已购数量、悬停高亮、堆叠已满提示。</para>
    /// <para>图标 / 数量 / 数字格式 / 名称 / 品质 / 悬停 / 堆叠提示 等公共字段均继承自
    /// <see cref="UiwInventoryItemSlotBase"/>。</para>
    /// </summary>
    public class UiwInventoryItemDetail : UiwInventoryItemSlotBase
    {
        // ── Detail 专有属性字段 ID ──────────────────────────────────────────────────
        [Header("属性字段 ID")]
        [Tooltip("描述属性 ID。")]
        public string descAttrId          = "描述";
        [Tooltip("价格属性 ID。")]
        public string priceAttrId         = "货币ID:价格";
        [Tooltip("已购数量属性 ID（格式 \"已购/上限\"）。")]
        public string purchaseCountAttrId;

        // ── Detail 专有 UI 引用 ───────────────────────────────────────────────────
        [Header("信息")]
        [Tooltip("描述文本。")]
        public InventoryText descText;
        
        [Header("功能标签")]
        [Tooltip("道具标签 预制体。")]
        public UiwTextLabel textTagsPrefab;
        [Tooltip("道具标签 容器。用于放置 多个功能标签的UI显示。")]
        public Transform itemTagsContainer;
        
        [Header("价格")]
        [Tooltip("货币格子 预制体。")]
        public UiwInventoryItemSimple priceCurrencyPrefab;
        [Tooltip("货币格子 父容器；未挂载该容器、或道具无价格数据时整个价格区不显示。")]
        public Transform priceContainer;

        /// <summary>价格货币格子实例池（只增不减，多余的实例 SetActive(false) 复用）。</summary>
        private readonly UiwWidgetPool<UiwInventoryItemSimple> _pricePool = new UiwWidgetPool<UiwInventoryItemSimple>();

        /// <summary>功能标签 UI 实例池（只增不减，多余的实例 SetActive(false) 复用）。</summary>
        private readonly UiwWidgetPool<UiwTextLabel> _labelPool = new UiwWidgetPool<UiwTextLabel>();

        // 回收功能标签行：除隐藏外还要释放 Addressable 背景图句柄。无捕获，编译器缓存为静态委托。
        private static readonly Action<UiwTextLabel> RecycleLabel = lbl =>
        {
            InventoryAssets.Release(lbl.gameObject);
            lbl.gameObject.SetActive(false);
        };

        // ── 公开接口 ───────────────────────────────────────────────────────────────

        /// <summary>将此格子绑定到指定仓库的指定 slot，刷新所有显示。</summary>
        public void SetSlot(string inventoryId, RuntimeItemSlot slot)
        {
            if (slot == null) { SetEmpty(); return; }

            SetBoundSlot(inventoryId, slot.itemId, slot.count);   // 记录来源仓库 / 道具 ID / 数量 + 悬停弹窗目标（基类共用）

            var item = InventoryDataManager.Instance.GetItem(slot.itemId);

            ApplyQualityBackground(item);
            ApplyIcon(item);

            // 名称
            ApplyName(item, slot.itemId);

            // 描述（未挂载 descText 组件即不显示）
            if (descText)
                descText.text = (item != null && !string.IsNullOrEmpty(descAttrId))
                    ? (item.GetEntry(descAttrId)?.value?.AsString ?? string.Empty)
                    : string.Empty;

            // 数量
            if (countText) countText.text = FormatNumber(slot.count);

            // 价格（StringIntPair 数组：key = 货币道具 ID，value = 数量）
            if (priceContainer)
            {
                // 未挂载货币格子 Prefab 或道具无价格属性 → 价格区不显示
                bool show = priceCurrencyPrefab
                            && item != null && !string.IsNullOrEmpty(priceAttrId);
                if (show)
                {
                    var av    = item.GetAttributeValue(priceAttrId);
                    int count = av != null ? av.Count : 0;

                    _pricePool.Configure(priceCurrencyPrefab, priceContainer);
                    _pricePool.Begin();
                    for (int i = 0; i < count; i++)
                    {
                        var cell = _pricePool.Next();
                        if (!cell) break;
                        var (currencyId, amount) = av.GetStringIntPair(i);
                        cell.SetItem(currencyId, amount);
                    }
                    _pricePool.End();

                    priceContainer.gameObject.SetActive(count > 0);
                }
                else
                {
                    priceContainer.gameObject.SetActive(false);
                }
            }
            
            // 功能标签
            ApplyItemLabels(item);

            // 先激活，再启动协程（StartCoroutine 要求 GameObject 处于 active 状态）
            gameObject.SetActive(true);

            bool isFull = item != null && item.stackLimit > 0 && slot.count >= item.stackLimit;
            SetStackFull(isFull, animate: true);
        }

        /// <summary>将此格子设为空态（隐藏内容，清空数据）。</summary>
        public void SetEmpty()
        {
            ClearBoundSlot();   // 清除道具标识 + 悬停弹窗目标（基类共用）
            ClearIcon();
            ClearNameAndQuality();
            if (descText)           descText.text            = string.Empty;
            if (countText)          countText.text           = string.Empty;
            if (priceContainer)     priceContainer.gameObject.SetActive(false);
            if (itemTagsContainer) itemTagsContainer.gameObject.SetActive(false);
            _tagGen++;
            _labelPool.RecycleAll(RecycleLabel);
            _pricePool.RecycleAll();
            ClearStackFull();
            gameObject.SetActive(false);
        }

        // 右键快速装备（广播「道具右键」事件）已上移至基类 UiwInventoryItemSlotBase.OnPointerClick，本类无需重复实现。

        // ── 功能标签 ─────────────────────────────────────────────────────────────

        // 功能标签背景异步加载世代号：每次刷新自增，回调据此丢弃过期结果（标签实例池化复用）。
        private int _tagGen;

        private void ApplyItemLabels(Item item)
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
                label.Setup(functionTag.backgroundSprite, functionTag.backgroundColor, text);
                var ft = functionTag;
                InventoryAssets.Bind<Sprite>(ft.backgroundSprite, ft.backgroundSpriteAddress, label.gameObject, s =>
                {
                    if (gen != _tagGen || !label || !s) return;   // 过期 / 空结果丢弃（保留 Setup 已设的底图）
                    label.SetBackgroundSprite(s);
                });
            }

            int shown = _labelPool.ActiveCount;
            _labelPool.End(RecycleLabel);

            itemTagsContainer.gameObject.SetActive(shown > 0);
        }
    }
}

#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 商店道具明细显示组件（MonoBehaviour），对应背包列表项 <see cref="UiwInventoryItemDetail"/>。
    /// 在 <see cref="UiwInventoryItemBase"/>（图标 / 名称 / 品质背景 / 数字格式）基础上，增加单价（多货币）、
    /// 剩余可交易次数 / 持有量、以及购买/回收次数的 +/- 选择。由 <see cref="UiwShopViewBase"/> 统一驱动。
    ///
    /// <para>「售卖」按可交易次数（货币是否充足由商店总价栏体现，不在行内钳制）选择次数；
    /// 「回收」按 min(可交易次数, 持有量) 钳制。道具缺失 / 无法交易时整体置灰且不可交互。</para>
    /// </summary>
    public class UiwShopItemDetail : UiwInventoryItemBase
    {
        [Header("交易信息")]
        [Tooltip("剩余可交易次数（售卖）/ 持有量（回收）文本。")]
        public InventoryText remainingText;
        [Tooltip("本行小计文本（单价 × 次数；次数为 0 时清空）。可空。")]
        public InventoryText subtotalText;

        [Header("单价")]
        [Tooltip("单价货币格子 Prefab（UiwInventoryItemSimple，多货币逐个显示）。可空。")]
        public UiwInventoryItemSimple priceCurrencyPrefab;
        [Tooltip("单价货币格子父容器。可空。")]
        public Transform priceContainer;
        [Tooltip("无货币图标容器时的单价文本回退。可空。")]
        public InventoryText unitPriceText;

        [Header("购买次数")]
        [Tooltip("数字计数器组件（+/- 调整交易次数，含长按连发）。")]
        public UiwNumberCounter counter;
        [Tooltip("无限可交易次数时，可选次数的上限。")]
        public int maxQuantityPerOrder = 999;
        
        [Header("置灰")]
        [Tooltip("不可交易（道具缺失 / 未持有）时用于整体置灰的 CanvasGroup。可空。")]
        public CanvasGroup interactableGroup;
        [Range(0f, 1f)]
        [Tooltip("置灰时的不透明度。")]
        public float disabledAlpha = 0.4f;

        [Header("文本")]
        [Tooltip("售卖剩余次数格式：{0}=剩余, {1}=上限。")]
        public string sellRemainingFormat = "剩余 {0}/{1}";
        [Tooltip("回收持有量格式：{0}=持有量。")]
        public string recycleOwnedFormat = "持有 {0}";
        [Tooltip("无限次数显示文本。")]
        public string unlimitedText = "∞";

        // ── 运行时状态 ────────────────────────────────────────────────────────────
        private UiwShopViewBase _owner;
        private Shop          _shop;
        private ShopCommodity _commodity;
        private ShopCommodityEntry _entry;   // 所属数据模型条目（选中次数的真源，回写于此）
        private ShopType      _mode;
        private int           _maxTimes;
        private bool          _tradable;
        private Dictionary<string, int> _unitPrice = new Dictionary<string, int>();
        private readonly List<UiwInventoryItemSimple> _priceInstances = new List<UiwInventoryItemSimple>();
        
        /// <summary>本行绑定的商品配置。</summary>
        public ShopCommodity Commodity => _commodity;

        /// <summary>是否为合成商品（未配置商品组的默认回收，结算走 RecycleItem）。</summary>
        public bool Synthetic { get; private set; }

        /// <summary>当前选择的交易次数。</summary>
        public int Times => counter ? counter.Value : 0;

        /// <summary>本行单价（货币ID → 金额）的只读视图。</summary>
        public IReadOnlyDictionary<string, int> UnitPrice => _unitPrice;

        private void Awake()
        {
            if (counter) counter.OnValueChanged += OnCounterChanged;
        }

        private void OnDestroy()
        {
            if (counter) counter.OnValueChanged -= OnCounterChanged;
        }

        /// <summary>计数器值变化（用户调整次数）：更新小计并通知 owner 重算总价。</summary>
        private void OnCounterChanged(int value)
        {
            if (_entry != null) _entry.times = value;   // 回写数据模型（虚拟滚动下选中次数的真源）
            UpdateSubtotal();
            _owner?.OnCellTimesChanged(this);
        }

        /// <summary>绑定到指定商品数据条目并刷新显示（次数取自 <see cref="ShopCommodityEntry.times"/>）。</summary>
        public void Bind(UiwShopViewBase owner, Shop shop, ShopCommodityEntry entry, ShopType mode)
        {
            _owner     = owner;
            _shop      = shop;
            _entry     = entry;
            _commodity = entry != null ? entry.commodity : null;
            _mode      = mode;
            Synthetic  = entry != null && entry.synthetic;
            gameObject.SetActive(true);
            RefreshDisplay();   // 内部据 entry.times 设置计数器（并钳制到当前最大可交易次数）
        }

        /// <summary>设为空态（隐藏，从对象池回收时调用）。</summary>
        public void SetEmpty()
        {
            _commodity = null;
            _entry     = null;
            if (counter) counter.SetValue(0, notify: false);
            gameObject.SetActive(false);
        }

        /// <summary>设置交易次数（计数器钳制到 [0, 最大可交易]），变化时刷新本行并通知 owner 更新总价。</summary>
        public void SetTimes(int times)
        {
            if (counter) counter.SetValue(times);   // 变化经 OnCounterChanged 通知 owner
        }

        /// <summary>次数归零（同步数据模型，不触发 owner 回调；由 owner 批量重算）。</summary>
        public void ResetTimes()
        {
            if (_entry != null) _entry.times = 0;
            if (counter) counter.SetValue(0, notify: false);
            UpdateSubtotal();
        }

        /// <summary>重算剩余/持有、最大可交易、单价并刷新显示（响应刷新/库存变化）。</summary>
        public void RefreshDisplay()
        {
            if (_commodity == null) return;

            var dm      = InventoryDataManager.Instance;
            var shopMgr = ShopRuntimeManager.Instance;
            var item    = dm.GetItem(_commodity.itemId);

            // 品质背景 + 图标 + 名称 + 每次交易数量
            ApplyQualityBackground(item);
            ApplyIcon(item);
            ApplyName(item, _commodity.itemId);
            if (countText) countText.text = "×" + _commodity.count;

            // 单价（多货币）
            _unitPrice = shopMgr.GetUnitPrice(_shop, _commodity);
            ApplyUnitPrice();

            int remainingTrades = shopMgr.GetRemainingTrades(_shop, _commodity);
            _maxTimes = ShopTradeMath.MaxTimes(shopMgr, _shop, _commodity, _mode, maxQuantityPerOrder);
            _tradable = item != null && _maxTimes > 0;

            if (_mode == ShopType.Recycle)
            {
                int owned = shopMgr.GetOwnedCount(_shop, _commodity.itemId);
                if (remainingText) remainingText.text = string.Format(recycleOwnedFormat, owned);
            }
            else // 售卖 /（等价交换占位按售卖显示）
            {
                if (remainingText)
                    remainingText.text = _commodity.tradeLimit < 0
                        ? "剩余 " + unlimitedText
                        : string.Format(sellRemainingFormat, remainingTrades, _commodity.tradeLimit);
            }

            if (counter)
            {
                counter.SetRange(0, _maxTimes);
                // 据数据模型的次数显示，并钳制到当前最大可交易次数后回写（离屏期间上限可能已下降）。
                int t = _entry != null ? Mathf.Min(_entry.times, _maxTimes) : 0;
                if (_entry != null) _entry.times = t;
                counter.SetValue(t, notify: false);
                counter.SetInteractable(_tradable);
            }
            ApplyGray(!_tradable);
            UpdateSubtotal();
        }

        // ── 内部 ─────────────────────────────────────────────────────────────────

        private void ApplyUnitPrice()
        {
            if (priceContainer && priceCurrencyPrefab)
            {
                int i = 0;
                foreach (var kv in _unitPrice)
                {
                    while (_priceInstances.Count <= i)
                        _priceInstances.Add(Instantiate(priceCurrencyPrefab, priceContainer));
                    _priceInstances[i].numberFormat = numberFormat;
                    _priceInstances[i].SetItem(kv.Key, kv.Value);
                    i++;
                }
                for (int j = i; j < _priceInstances.Count; j++)
                    _priceInstances[j].gameObject.SetActive(false);
                priceContainer.gameObject.SetActive(_unitPrice.Count > 0);
            }

            if (unitPriceText)
                unitPriceText.text = BuildPriceString(_unitPrice, 1);
        }

        private void UpdateSubtotal()
        {
            if (subtotalText)
                subtotalText.text = Times > 0 ? BuildPriceString(_unitPrice, Times) : string.Empty;
        }

        /// <summary>构造「金额 货币ID」串（多货币以两空格分隔），金额按数字格式显示。</summary>
        private string BuildPriceString(Dictionary<string, int> price, int multiplier)
        {
            if (price == null || price.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            foreach (var kv in price)
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(FormatNumber((long)kv.Value * multiplier)).Append(' ').Append(kv.Key);
            }
            return sb.ToString();
        }

        private void ApplyGray(bool disabled)
        {
            if (interactableGroup)
            {
                interactableGroup.alpha          = disabled ? disabledAlpha : 1f;
                interactableGroup.interactable   = !disabled;
                interactableGroup.blocksRaycasts = !disabled;
            }
            // +/- 可用性由计数器按可交互状态与边界控制（见 UiwNumberCounter.SetInteractable）
        }

    }
}

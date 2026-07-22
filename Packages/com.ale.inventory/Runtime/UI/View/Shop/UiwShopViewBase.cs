#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 商店主界面基类（abstract MonoBehaviour）。承载全部「与商店类型无关」的通用逻辑：
    /// 商品组页签（含「全部」）、货币栏、商品列表（购物车式 +/- 选数）、实时总价、结算按钮等。
    ///
    /// <para>类型特定行为通过抽象/虚方法下沉到子类：
    /// <see cref="UiwSellShopView"/>（售卖）、<see cref="UiwRecycleShopView"/>（回收）、
    /// <see cref="UiwBarterShopView"/>（等价交换，占位）。每个商店类型对应一个挂了相应子类的预制体。
    /// 结构与 <see cref="UiwInventoryView"/> 对称。</para>
    ///
    /// <para>价格 / 货币 / 刷新 / 交易 一律经 <see cref="ShopRuntimeManager"/>；
    /// 仓库读写经 <see cref="InventoryRuntimeManager"/>。</para>
    /// </summary>
    public abstract class UiwShopViewBase : UiwViewBase
    {
        private void Start()
        {
            if (settleButton) settleButton.onClick.AddListener(BtnSettle);
        }

        private void OnDestroy() => Unsubscribe();   // 过滤 / 排序改由商品列表组件自管，此处无需再退订

        #region 类型挂钩（子类实现）
        /// <summary>本视图对应的商店类型。</summary>
        protected abstract ShopType ExpectedType { get; }

        /// <summary>结算（点击结算按钮触发）。售卖 / 回收 / 等价交换各自实现。</summary>
        protected abstract void Settle();

        /// <summary>
        /// 构建类型特定的「额外商品」（如回收店在无配置商品时基于交易仓库合成的可回收商品）。
        /// </summary>
        protected virtual void BuildExtraCommodities() { }

        /// <summary>
        /// 尝试由子类提供商品列表（返回 true 表示已填充 <paramref name="result"/>，跳过默认「按商品组」逻辑）。
        /// 默认返回 false，走基类按组填充。
        /// </summary>
        protected virtual bool TryProvideCommodities(
            ShopCommodityGroup activeGroup, List<KeyValuePair<ShopCommodity, bool>> result) => false;

        /// <summary>总价文本前缀。默认「售卖总价前缀」；回收子类覆写为「总收益前缀」。</summary>
        protected virtual string TotalPrefix => sellTotalPrefix;

        /// <summary>判断某货币的合计消耗是否超出持有量（仅售卖需要，用于变红）。默认不超出。</summary>
        protected virtual bool IsOverBudget(string currencyId, int amount) => false;

        /// <summary>结算按钮是否可用。默认「有选中条目即可」；等价交换子类覆写为始终不可用。</summary>
        protected virtual bool CanSettle(int selectedCount) => selectedCount > 0;

        /// <summary>
        /// 该商品是否可交易（用于过滤商品列表）。默认全部可交易（Sell/Barter 无作用）；
        /// 回收子类覆写为「按交易功能标签限制」。
        /// </summary>
        protected virtual bool IsCommodityTradeable(ShopCommodity commodity) => true;
        #endregion

        #region 打开与关闭

        protected Shop Shop;

        [Header("商店")]
        [Tooltip("要打开的商店 ID。可在 Inspector 预设：本视图始终使用该值，直到经 Open(shopId) 或 Inspector 改动。")]
        [SerializeField] private string shopId;
        /// <summary>当前商店 ID（Inspector 可预设默认；<see cref="Open(string)"/> 与代码可覆盖）。</summary>
        protected string ShopId { get => shopId; set => shopId = value; }

        /// <summary>打开指定商店界面（等价于设置 <see cref="ShopId"/> 后调用无参 <see cref="Open()"/>）。</summary>
        public void Open(string shopIdSet)
        {
            ShopId = shopIdSet;
            Open();
        }

        /// <summary>用当前 <see cref="ShopId"/> 打开商店界面（ShopId 可由 Inspector 预设，或经 <see cref="Open(string)"/> / 代码指定）。</summary>
        public override void Open()
        {
            Shop = InventoryDataManager.Instance.GetShop(ShopId);
            if (Shop == null)
            {
                Debug.LogWarning($"[{GetType().Name}] 未找到商店：{ShopId}");
                return;
            }
            if (Shop.shopType != ExpectedType)
                Debug.LogWarning($"[{GetType().Name}] 商店「{ShopId}」类型为 {Shop.shopType}，" +
                                 $"与本视图期望的 {ExpectedType} 不符；请使用对应类型的商店视图预制体。");

            base.Open();   // 激活面板（公共步骤）

            _db = InventoryDataManager.Instance.FindDatabaseForShop(ShopId);

            NumberFormat = ResolveNumberFormatLocale(
                InventoryDataManager.Instance.GetNumberFormatConfig(Shop.numberFormatRef));

            RefreshTitle();
            BuildExtraCommodities();
            BuildGroupTabs();
            ConfigureCommodityFilterSort();   // 过滤（功能标签）+ 排序，交由商品列表组件内建管线
            BuildCurrencyBar();

            Subscribe();

            // 有页签 → 选中首个（「全部」或首个商品组）；无任何页签（隐藏全部且无商品组）→ 直接显示全部商品。
            if (_groupTabs.Count > 0) SwitchGroup(0);
            else                      PopulateCommodities(null);
        }

        /// <summary>用上次打开的商店 ID 重新打开（供基类 <see cref="UiwViewBase.ToggleOpenClose"/>）。</summary>
        protected override void Reopen()
        {
            if (string.IsNullOrEmpty(ShopId))
            {
                Debug.LogWarning($"[{GetType().Name}] 切换失败：尚未指定商店；请先调用 Open(shopId)。");
                return;
            }
            Open(ShopId);
        }

        private void Subscribe()
        {
            if (ShopRuntimeManager.Instance != null)
                ShopRuntimeManager.Instance.OnShopChanged += OnShopChanged;
            if (InventoryRuntimeManager.Instance)
                InventoryRuntimeManager.Instance.OnInventoryChanged += OnInventoryChanged;
        }

        /// <summary>取消商店运行时事件订阅（由基类 <see cref="UiwViewBase.Close"/> 与本类 OnDestroy 调用）。</summary>
        protected override void Unsubscribe()
        {
            if (ShopRuntimeManager.Instance != null)
                ShopRuntimeManager.Instance.OnShopChanged -= OnShopChanged;
            if (InventoryRuntimeManager.Instance)
                InventoryRuntimeManager.Instance.OnInventoryChanged -= OnInventoryChanged;
        }

        #endregion

        #region 标题
        // titleLabel / ResolveTitleText 继承自 UiwViewBase。

        /// <summary>
        /// 刷新标题为当前商店的标题（属性字段优先，回退显示名 / ID；见 <see cref="UiwViewBase.ResolveTitleText"/>）。
        /// </summary>
        private void RefreshTitle()
        {
            if (!titleLabel) return;
            titleLabel.text = Shop != null
                ? ResolveTitleText(Shop.displayNameText != null ? Shop.displayNameText.ResolveText() : null, Shop.id)
                : string.Empty;
        }
        #endregion

        #region 商品组页签
        [Header("商品组页签")]
        [Tooltip("页签容器（groupTabPrefab 于此下）。")]
        public Transform       groupTabContainer;
        [Tooltip("页签 Prefab（UiwShopGroupTab）。")]
        public UiwShopGroupTab groupTabPrefab;
        [Tooltip("「全部」页签显示名。")]
        public string          allTabName = "全部";

        private readonly List<UiwShopGroupTab>     _groupTabs = new List<UiwShopGroupTab>();
        private readonly List<ShopCommodityGroup>  _tabGroups = new List<ShopCommodityGroup>(); // 与 _groupTabs 平行；null = 全部
        private readonly List<string>              _tabNames  = new List<string>();             // 与 _groupTabs 平行

        private void BuildGroupTabs()
        {
            foreach (var t in _groupTabs)
                if (t) Destroy(t.gameObject);
            _groupTabs.Clear();
            _tabGroups.Clear();
            _tabNames.Clear();

            if (!groupTabPrefab || !groupTabContainer) return;

            // 「全部」页签：由 showAllFilterTab 决定是否显示（关闭时默认选中第一个商品组）。
            if (Shop.showAllFilterTab)
                AddGroupTab(null, allTabName);
            foreach (var g in Shop.groups)
                AddGroupTab(g, string.IsNullOrEmpty(g.name) ? "未命名" : g.name);
        }

        private void AddGroupTab(ShopCommodityGroup group, string display)
        {
            int idx = _groupTabs.Count;
            var tab = Instantiate(groupTabPrefab, groupTabContainer);
            tab.SetData(group, display, false);

            var btn = tab.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => SwitchGroup(idx));

            _groupTabs.Add(tab);
            _tabGroups.Add(group);
            _tabNames.Add(display);
        }

        private void SwitchGroup(int index)
        {
            if (index < 0 || index >= _groupTabs.Count) return;

            for (int i = 0; i < _groupTabs.Count; i++)
                _groupTabs[i]?.SetData(_tabGroups[i], _tabNames[i], i == index);

            PopulateCommodities(_tabGroups[index]);
        }
        #endregion

        #region 商品列表
        [Header("商品列表")]
        [Tooltip("商品列表（虚拟滚动 UiwShopCommodityList）。")]
        public UiwShopCommodityList commodityList;

        // 商品数据模型（选中次数 times 的真源；虚拟滚动下不随格子回收丢失）。子类结算据此遍历。
        protected readonly List<ShopCommodityEntry> Entries = new List<ShopCommodityEntry>();
        // 类型特定的「额外商品」（如回收店无配置商品时基于交易仓库合成的可回收商品）。引用稳定，供 cell 持有。
        protected readonly List<ShopCommodity> SyntheticCommodities = new List<ShopCommodity>();

        /// <summary>本商店是否没有任何配置商品（所有商品组均为空）。</summary>
        protected bool ShopHasNoCommodities()
        {
            foreach (var g in Shop.groups)
                if (g.commodities.Count > 0) return false;
            return true;
        }

        protected void PopulateCommodities(ShopCommodityGroup activeGroup)
        {
            var list = new List<KeyValuePair<ShopCommodity, bool>>(); // value = synthetic

            // 子类优先提供（如回收店的合成商品）；未提供则走默认「按商品组」填充。
            if (!TryProvideCommodities(activeGroup, list))
            {
                if (activeGroup == null)
                {
                    foreach (var g in Shop.groups)
                        foreach (var c in g.commodities)
                            list.Add(new KeyValuePair<ShopCommodity, bool>(c, false));
                }
                else
                {
                    foreach (var c in activeGroup.commodities)
                        list.Add(new KeyValuePair<ShopCommodity, bool>(c, false));
                }
            }

            // 按「可交易」规则过滤（回收店按交易功能标签限制；Sell/Barter 默认全放行）。
            list.RemoveAll(kv => !IsCommodityTradeable(kv.Key));

            // 构建数据模型（次数归零，全集）；功能标签过滤 + 显示排序交由商品列表组件内建管线处理。
            // Entries 始终为全集（购物车 / 结算 / 总价均遍历它），列表仅按过滤 / 排序显示其子集。
            Entries.Clear();
            foreach (var kv in list)
                Entries.Add(new ShopCommodityEntry(kv.Key, kv.Value));

            if (commodityList)
            {
                commodityList.SetContext(this, Shop, Shop.shopType, NumberFormat);
                commodityList.SetSourceItems(Entries);   // 列表内部：过滤 → 排序 → 显示
            }

            RecomputeTotals();
        }

        protected void RefreshAllCells()
        {
            if (commodityList) commodityList.RefreshVisibleCells();
        }

        protected void ResetAllTimes()
        {
            foreach (var e in Entries) e.times = 0;
            if (commodityList) commodityList.RefreshVisibleCells();
        }
        #endregion

        #region 过滤 / 排序（委托给商品列表组件）
        // 过滤栏 / 排序栏引用已移到商品列表组件（UiwShopCommodityList 继承的 UiwInventoryListBase）上，
        // 过滤 / 排序管线由列表基类内建；本视图只负责一次性把「数据源 + 差异回调」配置给列表。

        private InventoryDatabase _db;   // 当前商店所属数据库（排序属性比较 / 显示名解析用）

        /// <summary>
        /// 配置商品列表的过滤（功能标签，数据源 <see cref="Shop.filterTagRefs"/>）+ 显示排序（<see cref="Shop.sortPriorities"/>）。
        /// 过滤栏 / 排序栏引用配置在商品列表组件上；本方法只注入差异回调与数据条件，交互后由列表自动重排显示。
        /// </summary>
        private void ConfigureCommodityFilterSort()
        {
            if (!commodityList) return;
            var dm = InventoryDataManager.Instance;

            // 过滤谓词：主页签 token 为空 = 全部；否则要求商品的道具含该功能标签。
            commodityList.ConfigureFilter(
                (e, primary, _) => string.IsNullOrEmpty(primary)
                    || (e?.commodity != null && dm != null && dm.ItemHasTag(e.commodity.itemId, primary)),
                Shop.filterTagRefs, showAll: true);

            // 显示排序（不写运行时）：排序键取商品的道具 ID，复用 CompareSlots 按道具属性比较。
            commodityList.ConfigureSort(
                e => e?.commodity != null ? e.commodity.itemId : null,
                _db, Shop.sortPriorities, Shop.sortTiebreakers);
        }
        #endregion

        #region 货币栏
        [Header("货币栏")]
        [Tooltip("货币栏组件（通用 UiwCurrencyBar）。货币道具 ID 直接在 UiwCurrencyBar 组件上配置（留空则自动从商品价格中收集）。")]
        public UiwCurrencyBar currencyBar;

        private readonly List<string> _currencyIds = new List<string>();

        private void BuildCurrencyBar()
        {
            if (!currencyBar) return;
            _currencyIds.Clear();
            CollectCurrencyIds(_currencyIds);
            currencyBar.Setup(_currencyIds, GetOwnedCurrency, NumberFormat);
        }

        private void UpdateCurrencyBar()
        {
            if (currencyBar) currencyBar.Refresh();
        }

        // 货币栏 getter：跨商店交易仓库统计某货币持有量。
        private int GetOwnedCurrency(string itemId)
            => ShopRuntimeManager.Instance != null
                ? ShopRuntimeManager.Instance.GetOwnedCount(Shop, itemId)
                : 0;

        // 收集货币 ID：UiwCurrencyBar 上配置的 currencyItemIds 优先，否则遍历所有商品单价的货币键去重。
        private void CollectCurrencyIds(List<string> result)
        {
            var configured = currencyBar ? currencyBar.currencyItemIds : null;
            if (configured != null && configured.Length > 0)
            {
                foreach (var id in configured)
                    if (!string.IsNullOrEmpty(id) && !result.Contains(id)) result.Add(id);
                return;
            }

            var shopMgr = ShopRuntimeManager.Instance;
            if (shopMgr == null) return;
            foreach (var g in Shop.groups)
                foreach (var c in g.commodities)
                    foreach (var kv in shopMgr.GetUnitPrice(Shop, c))
                        if (!result.Contains(kv.Key)) result.Add(kv.Key);
            foreach (var sc in SyntheticCommodities)
                foreach (var kv in shopMgr.GetUnitPrice(Shop, sc))
                    if (!result.Contains(kv.Key)) result.Add(kv.Key);
        }
        #endregion

        #region 总价与结算
        [Header("总价与结算")]
        [Tooltip("总价 / 总收益文本。")]
        public InventoryText totalLabel;
        [Tooltip("结算按钮。")]
        public Button        settleButton;
        [Tooltip("提示文本（数量自动下调、不支持等，可空）。")]
        public InventoryText hintLabel;
        [Tooltip("售卖总价超出持有货币时的颜色。")]
        public Color  overBudgetColor    = new Color(0.9f, 0.3f, 0.3f, 1f);
        [Tooltip("正常颜色。")]
        public Color  normalColor        = Color.white;
        [Tooltip("售卖总价前缀。")]
        public string sellTotalPrefix    = "总价：";
        [Tooltip("回收总收益前缀。")]
        public string recycleTotalPrefix = "总收益：";
        [Tooltip("数量自动下调提示。")]
        public string adjustHint         = "数量已自动调整（货币 / 容量不足），请再次点击结算。";
        [Tooltip("不支持交易提示。")]
        public string notSupportedHint   = "该商店类型暂不支持交易。";

        protected bool Settling;

        /// <summary>由 <see cref="UiwShopItemDetail"/> 在次数变化时回调，重算总价。</summary>
        public void OnCellTimesChanged(UiwShopItemDetail cell)
        {
            if (Settling) return;
            RecomputeTotals();
        }

        /// <summary>
        /// 重新计算总价 / 总收益，并更新总价文本与结算按钮状态。
        /// 前缀 / 超预算判断 / 结算可用性 由子类挂钩（<see cref="TotalPrefix"/> / <see cref="IsOverBudget"/> / <see cref="CanSettle"/>）决定。
        /// </summary>
        protected void RecomputeTotals()
        {
            var shopMgr = ShopRuntimeManager.Instance;
            int maxPerOrder = commodityList && commodityList.cellPrefab ? commodityList.cellPrefab.maxQuantityPerOrder : 999;

            var totals  = new Dictionary<string, int>();
            int selected = 0;
            foreach (var e in Entries)
            {
                if (e == null || e.commodity == null) continue;
                // 防御性钳制：离屏 entry 的次数可能因库存 / 可交易次数变化而超出当前上限。
                if (shopMgr != null)
                {
                    int max = ShopTradeMath.MaxTimes(shopMgr, Shop, e.commodity, Shop.shopType, maxPerOrder);
                    if (e.times > max) e.times = max;
                }
                if (e.times <= 0) continue;
                selected++;
                var unitPrice = shopMgr != null ? shopMgr.GetUnitPrice(Shop, e.commodity) : null;
                if (unitPrice != null)
                    foreach (var kv in unitPrice)
                    {
                        totals.TryGetValue(kv.Key, out var ex);
                        totals[kv.Key] = ex + kv.Value * e.times;
                    }
            }

            bool over = false;
            var sb = new StringBuilder();
            foreach (var kv in totals)
            {
                if (IsOverBudget(kv.Key, kv.Value)) over = true;
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(FmtNum(kv.Value)).Append(' ').Append(kv.Key);
            }

            if (totalLabel)
            {
                totalLabel.text  = TotalPrefix + (sb.Length > 0 ? sb.ToString() : "0");
                totalLabel.color = over ? overBudgetColor : normalColor;
            }
            if (settleButton)
                settleButton.interactable = CanSettle(selected);
        }

        /// <summary>结算按钮点击事件：转交子类 <see cref="Settle"/> 实现。</summary>
        private void BtnSettle()
        {
            if (Shop == null) return;
            Settle();
        }

        /// <summary>
        /// 结算后刷新所有格子、重置次数、更新货币栏、重新计算总价，并清空提示。
        /// </summary>
        protected void AfterSettle()
        {
            RefreshAllCells();
            ResetAllTimes();
            UpdateCurrencyBar();
            RecomputeTotals();
            ShowHint(string.Empty);
        }

        protected void ShowHint(string msg)
        {
            if (hintLabel) hintLabel.text = msg ?? string.Empty;
        }

        protected string FmtNum(long v) => NumberFormat != null ? NumberFormat.Format(v) : v.ToString();
        #endregion

        #region 事件
        private void OnShopChanged(string shopIdSet)
        {
            if (Settling || shopIdSet != ShopId) return;
            RefreshAllCells();
            UpdateCurrencyBar();
            RecomputeTotals();
        }

        private void OnInventoryChanged(string inventoryId)
        {
            if (Settling || Shop == null) return;
            if (!Shop.tradeInventoryRefs.Contains(inventoryId)) return;
            RefreshAllCells();
            UpdateCurrencyBar();
            RecomputeTotals();
        }
        #endregion

        #region 数字格式
        protected NumberFormatLocale NumberFormat;
        // GetCurrentLanguage / ResolveNumberFormatLocale 继承自 UiwViewBase。
        #endregion
    }
}

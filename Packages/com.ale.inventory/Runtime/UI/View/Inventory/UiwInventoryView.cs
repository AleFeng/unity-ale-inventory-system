#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 背包主界面（MonoBehaviour）。
    /// 整合多仓库页签切换、货币道具栏、虚拟列表、过滤按钮、排序下拉+升降序切换、自动整理。
    /// </summary>
    public class UiwInventoryView : UiwViewBase
    {
        private void Start()
        {
            // 视图切换按钮事件绑定（排序/过滤/货币 已抽到独立工具栏组件）
            if (viewModeToggleButton) viewModeToggleButton.onClick.AddListener(OnToggleViewMode);

            // 订阅工具栏组件事件
            if (filterBar)   filterBar.OnFilterChanged += OnFilterChanged;
            if (sortToolbar) { sortToolbar.OnSortChanged += OnSortChanged; sortToolbar.OnAutoSort += OnAutoSort; }

            // 初始 视图模式。无切换按钮时，判断 道具列表组件 是否配置。
            if (!viewModeToggleButton) _isOrderListMode = itemOrderList;
            // 应用 当前视图模式
            ApplyViewMode();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();   // 取消按打开订阅的 OnInventoryChanged
            // 工具栏 / 过滤栏事件随组件生命周期订阅（Start），此处一并取消
            if (filterBar)   filterBar.OnFilterChanged -= OnFilterChanged;
            if (sortToolbar) { sortToolbar.OnSortChanged -= OnSortChanged; sortToolbar.OnAutoSort -= OnAutoSort; }
        }

        #region 打开与关闭
        /// <summary>
        /// 打开背包界面。
        /// </summary>
        /// <param name="inventoryIdsSet">要显示的仓库 ID 数组（按顺序生成页签）。</param>
        /// <param name="defaultFilter">默认激活的过滤标签名，null = 全部。</param>
        public void Open(string[] inventoryIdsSet, string defaultFilter = null)
        {
            this.inventoryIds = inventoryIdsSet ?? new string[0];
            _activeFilter = defaultFilter;
            Open();
        }

        /// <summary>用当前缓存的仓库列表 / 过滤打开背包（构建页签、货币栏并订阅仓库变化）。</summary>
        public override void Open()
        {
            base.Open();   // 激活面板（公共步骤）

            BuildInventoryTabs();
            BuildCurrencyBar();

            // 订阅仓库变化事件
            if (InventoryRuntimeManager.Instance)
                InventoryRuntimeManager.Instance.OnInventoryChanged += OnInventoryChanged;

            if (inventoryIds != null && inventoryIds.Length > 0)
                SwitchInventoryTab(0);
        }

        /// <summary>取消本视图按打开订阅的运行时事件（由基类 <see cref="UiwViewBase.Close"/> 与本类 OnDestroy 调用）。</summary>
        protected override void Unsubscribe()
        {
            if (InventoryRuntimeManager.Instance)
                InventoryRuntimeManager.Instance.OnInventoryChanged -= OnInventoryChanged;
        }

        /// <summary>用上次打开的仓库列表重新打开（供基类 <see cref="UiwViewBase.ToggleOpenClose"/>）。</summary>
        protected override void Reopen()
        {
            if (inventoryIds == null || inventoryIds.Length == 0)
            {
                Debug.LogWarning("[UiwInventoryView] 切换失败：尚未指定仓库；请先调用 Open(inventoryIds)。");
                return;
            }
            Open(inventoryIds);
        }
        #endregion

        #region 标题
        // titleLabel / ResolveTitleText 继承自 UiwViewBase。

        /// <summary>
        /// 刷新标题为当前选中仓库的标题（属性字段优先，回退显示名 / ID；见 <see cref="UiwViewBase.ResolveTitleText"/>）。
        /// 由 <see cref="SwitchInventoryTab"/> 在切换页签时调用。
        /// </summary>
        private void RefreshTitle()
        {
            if (!titleLabel) return;
            if (_inventoryIdsActiveIndex < 0 || inventoryIds == null
                || _inventoryIdsActiveIndex >= inventoryIds.Length)
            {
                titleLabel.text = string.Empty;
                return;
            }
            string id  = inventoryIds[_inventoryIdsActiveIndex];
            var    inv = InventoryDataManager.Instance?.GetInventory(id);
            titleLabel.text = inv != null ? ResolveTitleText(inv.displayNameText != null ? inv.displayNameText.ResolveText() : null, id) : id;
        }
        #endregion

        #region 道具列表
        [Header("道具列表")]
        [Tooltip("顺序道具列表。")]
        public UiwInventoryItemOrderList itemOrderList;
        [Tooltip("网格道具列表。")]
        public UiwInventoryItemGridList itemGridList;

        [Header("视图切换")]
        [Tooltip("切换按钮（null = 自动使用非空的那个视图，不支持切换）。")]
        public Button        viewModeToggleButton;
        [Tooltip("切换按钮上的文本（可选）。")]
        public InventoryText viewModeToggleLabel;

        // 当前视图模式：true = 列表，false = 网格
        private bool _isOrderListMode = true;

        /// <summary>
        /// 刷新 道具列表显示。
        /// </summary>
        /// <param name="preserveScroll">
        /// true = 保留当前滚动位置（仓库内容变化时增量刷新，避免滚动条复位）；
        /// false = 全量重建并回到顶部（切换页签 / 过滤 / 排序 / 视图等）。
        /// </param>
        private void RefreshItemList(bool preserveScroll = false)
        {
            if (_inventoryIdsActiveIndex < 0 || inventoryIds == null
                                 || _inventoryIdsActiveIndex >= inventoryIds.Length) return;
            if (!InventoryRuntimeManager.Instance) return;
            if (!itemOrderList && !itemGridList) return;

            string invId = inventoryIds[_inventoryIdsActiveIndex];
            var    itemSlotsDisplay = new List<RuntimeItemSlot>(InventoryRuntimeManager.Instance.GetSlots(invId));
            var    dm    = InventoryDataManager.Instance;
            var    db    = dm.FindDatabaseForInventory(invId);

            var invDef    = GetActiveInventoryDef();
            bool isDragSort = invDef?.dragSort ?? false;
            
            // 显示过滤：根据选中页签的功能标签ID匹配。空槽移除，此状态下进行拖拽整理，不能够确定 道具格子的准确位置。
            if (!string.IsNullOrEmpty(_activeFilter))
                itemSlotsDisplay.RemoveAll(s => string.IsNullOrEmpty(s.itemId) || !dm.ItemHasTag(s.itemId, _activeFilter));

            // 非拖拽整理模式：
            if (!isDragSort)
            {
                // 移除空槽（仅显示有道具的格子）。
                // 过滤"仓库中隐藏"的道具。
                // 可拖拽整理模式下，不隐藏道具，需要明确知道一共有多少格子，每个格子里放了什么道具。
                itemSlotsDisplay.RemoveAll(s => string.IsNullOrEmpty(s.itemId) || dm.IsItemHiddenInList(s.itemId));
            }
            
            // autoSort：对显示列表本地排序（不写入运行时，每次刷新自动重排）
            if ((invDef?.autoSort ?? false) && _currentSortPriorities.Count > 0 && itemSlotsDisplay.Count > 1)
            {
                var autoSortPriorities = BuildCurrentSortPriorities();
                if (autoSortPriorities.Count > 0)
                    InventoryRuntimeManager.SortSlots(itemSlotsDisplay, autoSortPriorities, db);
            }

            // 按当前视图模式 分发到对应组件
            if (_isOrderListMode && itemOrderList)
            {
                // 内容变化走增量更新（保留滚动位置）；其余场景全量重建并回到顶部。
                if (preserveScroll) itemOrderList.UpdateItemSlotList(invId, itemSlotsDisplay);
                else                itemOrderList.SetItemSlotList(invId, itemSlotsDisplay);
            }
            else if (itemGridList)
            {
                // 过滤页签非"全部"时，传入 filtered=true：网格仅显示筛选后的道具格，隐藏被过滤掉的格子与空格。
                bool filtered = !string.IsNullOrEmpty(_activeFilter);
                // 内容变化（拖拽换位 / 堆叠 / 数量增减）走增量差异刷新：保留滚动位置，只重绑数据变化的可见格；
                // 切页 / 过滤 / 排序 / 切视图等则全量重建并回到起点。
                if (preserveScroll) itemGridList.RefreshItemSlotList(invId, itemSlotsDisplay, filtered);
                else                itemGridList.SetItemSlotList(invId, itemSlotsDisplay, filtered);
            }

            RefreshWeightDisplay();
        }

        /// <summary>
        /// 切换视图模式（列表 ↔ 网格）按钮回调。
        /// </summary>
        private void OnToggleViewMode()
        {
            _isOrderListMode = !_isOrderListMode;
            // 应用 当前视图模式
            ApplyViewMode();
            // 刷新 道具列表内容
            RefreshItemList();
        }
        
        /// <summary>
        /// 应用 当前视图模式：激活对应视图组件，隐藏另一个。
        /// </summary>
        private void ApplyViewMode()
        {
            if (viewModeToggleLabel) viewModeToggleLabel.text = _isOrderListMode ? "列表" : "网格";
            // 切换 列表/网格 组件显示
            if (itemOrderList) itemOrderList.gameObject.SetActive(_isOrderListMode);
            if (itemGridList) itemGridList.gameObject.SetActive(!_isOrderListMode);
        }
        #endregion
        
        #region 货币栏
        [Header("货币栏")]
        [Tooltip("货币栏组件（通用 UiwCurrencyBar）。货币道具 ID 直接在 UiwCurrencyBar 组件上配置。")]
        public UiwCurrencyBar currencyBar;

        /// <summary>
        /// 构建货币栏。货币 ID 取自 UiwCurrencyBar 组件自身的配置；
        /// 数字格式取第一个仓库的配置；持有量跨所有已打开仓库统计。
        /// </summary>
        private void BuildCurrencyBar()
        {
            if (!currencyBar) return;

            NumberFormatLocale currencyFmt = null;
            if (inventoryIds != null && inventoryIds.Length > 0)
            {
                var inv = InventoryDataManager.Instance.GetInventory(inventoryIds[0]);
                currencyFmt = ResolveNumberFormatLocale(
                    InventoryDataManager.Instance.GetNumberFormatConfig(inv?.numberFormatRef));
            }

            currencyBar.Setup(GetOwnedCurrencyTotal, currencyFmt);
        }

        /// <summary>统计某货币道具跨所有已打开仓库的持有总量。供货币栏 getter 使用。</summary>
        private int GetOwnedCurrencyTotal(string itemId)
        {
            int total = 0;
            if (inventoryIds != null && InventoryRuntimeManager.Instance)
                foreach (var invId in inventoryIds)
                    total += InventoryRuntimeManager.Instance.GetTotalCount(invId, itemId);
            return total;
        }
        #endregion
        
        #region 仓库页签
        [Header("仓库页签")]
        [Tooltip("仓库页签容器（tabPrefab 于此下）。")]
        public Transform       tabContainer;
        [Tooltip("仓库页签Prefab（UiwInventoryTab）。")]
        public UiwInventoryTab tabPrefab;
        
        // 仓库页签条（实例 / 取值 / 显示名 / 高亮由 UiwTabStrip 统一维护；下标与 inventoryIds 对应）
        private readonly UiwTabStrip<UiwInventoryTab, string> _tabStrip = new UiwTabStrip<UiwInventoryTab, string>();
        private readonly List<string> _tabLabels = new List<string>();   // 复用的显示名缓冲，避免每次重建都分配

        [Header("仓库")]
        [Tooltip("要显示的仓库 ID 列表（按顺序生成页签）。可在 Inspector 预设：本视图始终使用该值，直到经 Open(inventoryIds) 或 Inspector 改动。")]
        [SerializeField] private string[] inventoryIds; // 仓库ID列表

        private int _inventoryIdsActiveIndex = -1; // 激活的 仓库ID列表 下标
        
        /// <summary>
        /// 仓库ID数据 变化回调
        /// </summary>
        /// <param name="inventoryId"></param>
        private void OnInventoryChanged(string inventoryId)
        {
            if (_inventoryIdsActiveIndex < 0 || inventoryIds == null
                                 || _inventoryIdsActiveIndex >= inventoryIds.Length) return;

            if (inventoryId == inventoryIds[_inventoryIdsActiveIndex])
            {
                // 仓库内容变化（数量增减 / 条目增删）默认增量刷新并保留滚动位置；
                // 仅手动排序经 _resetScrollNextRefresh 标记要求刷新后回到顶部。
                bool preserve = !_resetScrollNextRefresh;
                _resetScrollNextRefresh = false;
                RefreshItemList(preserveScroll: preserve);
            }

            // 货币可能随仓库变化（如花费 / 获得），同步刷新货币栏
            if (currencyBar) currencyBar.Refresh();
        }
        
        /// <summary>
        /// 构建 仓库页签。
        /// 根据当前的 _inventoryIds 生成对应数量的页签实例，并设置按钮回调。
        /// </summary>
        private void BuildInventoryTabs()
        {
            _tabLabels.Clear();
            foreach (var id in inventoryIds)
                _tabLabels.Add(ResolveInventoryDisplayName(id));

            _tabStrip.Configure(tabPrefab, tabContainer,
                (tab, id, label, selected) => tab.SetData(id, label, selected),
                (index, _) => OnInventoryTabSelected(index));

            // 仅重建页签实例，不在此触发切换（打开流程稍后统一调 SwitchInventoryTab(0)）。
            _tabStrip.SetTabs(inventoryIds, _tabLabels, selectedIndex: 0, notify: false);
        }

        /// <summary>
        /// 切换 仓库页签（经页签条统一刷新高亮，随后回调 <see cref="OnInventoryTabSelected"/>）。
        /// </summary>
        /// <param name="index"></param>
        private void SwitchInventoryTab(int index)
        {
            // 未配置页签预制体 / 容器时页签条为空，此时仍需直接初始化视图内容（与页签无关）。
            if (!_tabStrip.Select(index)) OnInventoryTabSelected(index);
        }

        /// <summary>页签切换后的实际响应：同步激活下标、标题、数字格式、过滤与排序栏、道具列表。</summary>
        private void OnInventoryTabSelected(int index)
        {
            if (inventoryIds == null || index < 0 || index >= inventoryIds.Length) return;

            _inventoryIdsActiveIndex = index;
            _activeFilter            = null;

            // 更新标题为当前选中仓库名称
            RefreshTitle();

            var invDef = GetActiveInventoryDef();

            // 同步数字格式到道具列表 / 网格视图
            {
                var fmt = ResolveNumberFormatLocale(
                    InventoryDataManager.Instance.GetNumberFormatConfig(invDef?.numberFormatRef));
                if (itemOrderList) itemOrderList.SetNumberFormat(fmt);
                if (itemGridList) itemGridList.SetNumberFormat(fmt);
            }

            // 过滤页签栏 + 排序整理栏（通用工具栏组件）
            // showAllFilterTab 决定是否显示「全部」页签：关闭时默认选中第一个过滤标签。
            if (filterBar) filterBar.SetFilters(invDef?.filterTagRefs, invDef?.showAllFilterTab ?? true);
            RebuildSortToolbar();
            RefreshItemList();
        }
        
        /// <summary>
        /// 获取 当前页签 对应的仓库定义（Inventory）。
        /// </summary>
        /// <returns></returns>
        private Inventory GetActiveInventoryDef()
        {
            if (_inventoryIdsActiveIndex < 0 || inventoryIds == null
                                 || _inventoryIdsActiveIndex >= inventoryIds.Length) return null;
            return InventoryDataManager.Instance.GetInventory(inventoryIds[_inventoryIdsActiveIndex]);
        }

        /// <summary>
        /// 解析仓库的 UI 显示名称：取 <see cref="Inventory.displayNameText"/>（本地化优先、回退纯文本），为空时退回使用 <paramref name="inventoryId"/>。
        /// </summary>
        private static string ResolveInventoryDisplayName(string inventoryId)
        {
            var inv = InventoryDataManager.Instance?.GetInventory(inventoryId);
            string name = inv != null && inv.displayNameText != null ? inv.displayNameText.ResolveText() : null;
            return !string.IsNullOrEmpty(name) ? name : inventoryId;
        }

        // GetCurrentLanguage / ResolveNumberFormatLocale 继承自 UiwViewBase。
        #endregion
        
        #region 过滤页签-功能标签
        [Header("过滤页签-功能标签")]
        [Tooltip("过滤页签栏组件（通用 UiwFilterTabBar）。")]
        public UiwFilterTabBar filterBar;

        private string _activeFilter; // 激活的 过滤页签。null = 全部

        /// <summary>过滤页签栏回调：更新激活过滤标签并刷新道具列表。</summary>
        private void OnFilterChanged(string tagName)
        {
            _activeFilter = tagName;
            RefreshItemList();
        }
        #endregion
        
        #region 整理排序
        [Header("整理排序")]
        [Tooltip("排序整理栏组件（通用 UiwSortToolbar：下拉 + 升降序 + 自动整理；排序选项名称/忽略ID 映射在该组件上配置）。")]
        public UiwSortToolbar sortToolbar;

        // 排序条件选项（来自 Inventory.sortPriorities）。供 BuildCurrentSortPriorities 把下拉下标映射回字段。
        private readonly List<SortPriority> _currentSortPriorities = new List<SortPriority>();

        // 手动排序标记：置 true 后，下一次（由排序写入运行时触发的）OnInventoryChanged 刷新将回到顶部。
        private bool _resetScrollNextRefresh;

        /// <summary>
        /// 重建排序整理栏：从当前仓库整理列表(sortPriorities)取排序条件并交给 <see cref="UiwSortToolbar"/>
        /// （显示名解析在该组件内完成）；同时缓存排序条件供本类运行时排序映射使用。
        /// </summary>
        private void RebuildSortToolbar()
        {
            _currentSortPriorities.Clear();

            var invDef = GetActiveInventoryDef();
            if (invDef != null)
                _currentSortPriorities.AddRange(invDef.sortPriorities);

            if (!sortToolbar) return;

            var db = inventoryIds != null && _inventoryIdsActiveIndex >= 0
                     && _inventoryIdsActiveIndex < inventoryIds.Length
                ? InventoryDataManager.Instance.FindDatabaseForInventory(inventoryIds[_inventoryIdsActiveIndex])
                : null;

            sortToolbar.SetSortPriorities(_currentSortPriorities, db);
        }

        /// <summary>排序整理栏：排序条件 / 方向变化回调。</summary>
        private void OnSortChanged(int index, bool ascending) => SortCurrentInventory();

        /// <summary>
        /// 从 UI 当前状态构建排序优先级列表（主条件 + tiebreakers，全部使用排序栏当前方向）。
        /// </summary>
        private List<SortPriority> BuildCurrentSortPriorities()
        {
            if (_currentSortPriorities.Count == 0) return new List<SortPriority>();
            int  idx = sortToolbar ? Mathf.Clamp(sortToolbar.SortIndex, 0, _currentSortPriorities.Count - 1) : 0;
            bool asc = sortToolbar && sortToolbar.Ascending;
            var primary = new SortPriority(_currentSortPriorities[idx].field, asc);
            var invDef  = GetActiveInventoryDef();
            var allKeys = new List<SortPriority> { primary };
            foreach (var tb in invDef?.sortTiebreakers ?? new List<SortPriority>())
                allKeys.Add(new SortPriority(tb.field, asc));
            return allKeys;
        }

        /// <summary>
        /// 将当前仓库按 UI 选中的排序条件排序并写入运行时状态（可存档）。
        /// 由排序下拉框、升降序按钮、整理按钮调用。
        /// </summary>
        private void SortCurrentInventory()
        {
            if (_inventoryIdsActiveIndex < 0 || inventoryIds == null
                || _inventoryIdsActiveIndex >= inventoryIds.Length) return;
            if (!InventoryRuntimeManager.Instance) return;

            var priorities = BuildCurrentSortPriorities();
            if (priorities.Count > 0)
            {
                // 手动排序：顺序整体改变，刷新后回到顶部。SortInventory 会同步触发 OnInventoryChanged。
                _resetScrollNextRefresh = true;
                InventoryRuntimeManager.Instance.SortInventory(
                    inventoryIds[_inventoryIdsActiveIndex], priorities);
                _resetScrollNextRefresh = false; // 同步事件已消费；兜底清理
            }
            else
            {
                RefreshItemList(); // 无排序条件 → 全量刷新（回到顶部）
            }
        }

        #endregion

        #region 自动整理按钮
        /// <summary>
        /// 自动整理（排序整理栏「整理」按钮事件回调）。
        /// </summary>
        private void OnAutoSort()
        {
            if (_inventoryIdsActiveIndex < 0 || inventoryIds == null
                                 || _inventoryIdsActiveIndex >= inventoryIds.Length) return;

            // 使用 UI 当前选中的主条件 + tiebreakers + 升降序，写入运行时并触发刷新
            SortCurrentInventory();
        }
        #endregion

        #region 重量显示
        [Header("重量显示")]
        [Tooltip("重量文本: 显示格式 道具总重量/仓库重量上限。")]
        public InventoryText weightLabel;
        [Tooltip("超出重量上限 颜色。")]
        public Color overWeightColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        [Tooltip("未超出重量上限 颜色。")]
        public Color normalWeightColor = Color.white;
        [Tooltip("重量无上限时 显示文本。")]
        public string unlimitedWeightText = "∞";

        /// <summary>
        /// 刷新重量显示文本。由 RefreshItemList 调用，仓库内容变化时自动同步。
        /// </summary>
        private void RefreshWeightDisplay()
        {
            if (!weightLabel || !InventoryRuntimeManager.Instance) return;
            if (_inventoryIdsActiveIndex < 0 || inventoryIds == null
                || _inventoryIdsActiveIndex >= inventoryIds.Length) return;

            string invId   = inventoryIds[_inventoryIdsActiveIndex];
            float  current = InventoryRuntimeManager.Instance.GetTotalWeight(invId);
            float  limit   = InventoryRuntimeManager.Instance.GetWeightLimit(invId);

            bool   isUnlimited = limit <= 0f;
            string limitStr    = isUnlimited ? unlimitedWeightText : $"{limit:0.##}";

            weightLabel.text  = $"{current:0.##} / {limitStr}";
            weightLabel.color = (!isUnlimited && current > limit) ? overWeightColor : normalWeightColor;
        }
        #endregion
    }
}

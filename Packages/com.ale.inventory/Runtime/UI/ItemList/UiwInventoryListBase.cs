using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 虚拟滚动列表基类（轴无关引擎）。所有"以列表 / 网格显示大量条目"的仓库系统 UI 列表的共同基础：
    /// 仅为视口内可见的条目分配 <typeparamref name="TCell"/> 实例（上下 / 左右各 <see cref="bufferCount"/> 个缓冲），
    /// 滚动时只复用实例、更新数据与位置，不创建 / 销毁对象，保证大量条目下的滚动流畅度。
    ///
    /// <para>本基类只持有"虚拟滚动引擎"（对象池 + 视口尺寸监听 + 回收 / 复用循环），
    /// 具体的<b>布局策略</b>（一维纵向 / 二维网格、纵向 / 横向滚动、条目定位）由子类
    /// <see cref="UiwInventoryOrderList{TData,TCell}"/> / <see cref="UiwInventoryGridList{TData,TCell}"/> 实现；
    /// 具体的<b>格子绑定</b>（把某条数据显示到一个格子 / 清空格子）由各系统的叶子类实现。</para>
    ///
    /// <para>数据类型 <typeparamref name="TData"/> 与格子类型 <typeparamref name="TCell"/> 均为泛型，
    /// 因此同一套引擎可被仓库 / 制作 / 技能 / 商店 / 装备等系统以各自的条目脚本复用。</para>
    /// </summary>
    /// <typeparam name="TData">列表数据元素类型（如 <c>RuntimeItemSlot</c> / <c>Skill</c> / <c>CraftingBlueprint</c>）。</typeparam>
    /// <typeparam name="TCell">格子显示组件类型（如 <c>UiwInventoryItemCell</c> / <c>UiwSkillEntry</c>）。</typeparam>
    public abstract class UiwInventoryListBase<TData, TCell> : MonoBehaviour where TCell : Component
    {
        // ── Inspector 配置 ────────────────────────────────────────────────────────
        [Header("虚拟滚动")]
        [Tooltip("格子 Prefab（TCell 组件）。要求根节点带 RectTransform 且尺寸固定。")]
        public TCell cellPrefab;
        [Tooltip("所属 ScrollRect（其 viewport 用于测量可见区域、监听尺寸变化）。")]
        public ScrollRect scrollRect;
        [Tooltip("滚动内容根节点（格子实例挂于此下，尺寸由数据量与布局自动撑开）。")]
        public RectTransform content;
        [Tooltip("视口沿滚动方向两端各额外保留的格子缓冲数量（防止快速滚动露白）。")]
        public int bufferCount = 1;
        [Tooltip("每秒最多生成 / 分配的格子数量（限速）：把实例化与绑定（含图标异步加载）分摊到多帧，" +
                 "避免单帧一次性实例化 / 加载大量格子导致卡顿或资源加载堵塞。≤ 0 = 不限速（一帧填满）。")]
        public float spawnPerSecond = 30f;

        [Header("过滤 / 排序（可选，绑定到本列表）")]
        [Tooltip("主过滤页签栏（UiwFilterTabBar）。绑定后本列表按视图注入的过滤谓词过滤显示。可空。")]
        public UiwFilterTabBar filterBar;
        [Tooltip("副过滤页签栏（UiwFilterTabBar）。与主过滤栏为 AND 关系（如技能主 / 副分组）。可空。")]
        public UiwFilterTabBar secondaryFilterBar;
        [Tooltip("排序整理栏（UiwSortToolbar）。绑定后本列表按其选中条件对显示排序，或经视图写运行时。可空。")]
        public UiwSortToolbar sortToolbar;

        // ── 数据 ──────────────────────────────────────────────────────────────────
        /// <summary>当前显示的数据列表（运行态，不序列化）。子类叶子可直接读取用于绑定 / 拖拽落位。</summary>
        protected List<TData> Items = new List<TData>();

        // ── 对象池 ────────────────────────────────────────────────────────────────
        // 全量实例池（含活跃 + 空闲）。
        private List<TCell> _instances;
        // dataIndex → 当前显示该数据的活跃实例。
        private readonly Dictionary<int, TCell> _idxToInstance = new Dictionary<int, TCell>();
        // 未分配给任何数据索引的空闲实例。
        private readonly List<TCell> _freeInstances = new List<TCell>();
        // 上次计算出的首个可见数据索引；未变化时提前退出，避免无谓遍历。
        private int _lastFirstIndex = -1;
        // 临时缓冲，收集待回收的 key，避免遍历字典时修改字典。
        private readonly List<int> _tempRecycleIndices = new List<int>();
        // 临时缓冲，收集增量差异刷新中"数据已变、需重绑"的 key，避免遍历字典时修改字典。
        private readonly List<int> _tempRebindIndices = new List<int>();

        // ── 限速生成 / 分配 ───────────────────────────────────────────────────────
        // 目标实例池大小（= 当前视口所需实例数 InstancesNeeded），同时也是可见窗口跨度；实例按需惰性创建到此上限。
        private int _poolTarget;
        // 当前目标可见窗口（含缓冲）[first,last]，由 UpdateVisibleCells 设置，供限速填充 FillWindow 逐帧消费。
        private int _windowFirst;
        private int _windowLast = -1;
        // 窗口内是否仍有"未分配实例"的索引待填充（驱动 LateUpdate 限速填充；无待填充时提前退出，空转开销极低）。
        private bool _hasPendingFill;
        // 限速预算累加器（单位：格）。每帧累加 spawnPerSecond × 帧时长，处理 floor 个格子后扣减。
        private float _spawnBudget;
        // 限速预算封顶（秒）。取很小的值：打开界面那一帧 unscaledDeltaTime 往往很大，
        // 若按整秒封顶会让预算瞬间冲满、一帧爆发实例化整屏；限到约 0.1 秒的量即可平滑分摊。
        private const float SpawnBudgetMaxSeconds = 0.1f;
        // 当前填充方向：true=从窗口开头往后（视觉"从上往下"）；false=从窗口末尾往前（"从下往上"）。
        // 由 UpdateVisibleCells 依滚动方向设置，使格子按进入视口的先后顺序逐个浮现。
        private bool _fillAscending = true;

        /// <summary>全量实例池（含活跃 + 空闲）。供叶子做跨格统一操作（如设数字格式）。</summary>
        protected IReadOnlyList<TCell> Instances => (IReadOnlyList<TCell>)_instances ?? Array.Empty<TCell>();

        /// <summary>当前已实例化的格子数（限速下惰性创建，未必等于目标池大小 / 窗口跨度）。</summary>
        protected int InstanceCount => _instances?.Count ?? 0;

        /// <summary>取当前正显示某数据索引的活跃实例（不在可见窗口内则返回 false）。供叶子做拖拽落位等按索引取格子。</summary>
        protected bool TryGetActiveCell(int dataIndex, out TCell cell) => _idxToInstance.TryGetValue(dataIndex, out cell);

        // ── 生命周期 ──────────────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            MeasureCell();

            // 在 Viewport 上挂 ViewportSizeWatcher，监听尺寸变化（Rebuild 循环内只置脏，LateUpdate 处理）。
            if (scrollRect && scrollRect.viewport)
            {
                var watcher = scrollRect.viewport.GetComponent<ViewportSizeWatcher>();
                if (!watcher)
                    watcher = scrollRect.viewport.gameObject.AddComponent<ViewportSizeWatcher>();
                watcher.OnChanged += SetViewportDirty;
            }
        }

        protected virtual void OnEnable()
        {
            // 监听滚动回调，滚动时刷新可见格。
            if (scrollRect) scrollRect.onValueChanged.AddListener(OnScroll);
            SubscribeFilterSort();   // 绑定的过滤栏 / 排序栏事件（仅在激活时响应）
        }

        protected virtual void OnDisable()
        {
            // 取消滚动回调监听，避免在禁用时仍触发刷新。
            if (scrollRect) scrollRect.onValueChanged.RemoveListener(OnScroll);
            UnsubscribeFilterSort();
        }

        protected virtual void OnDestroy()
        {
            // 取消 Viewport 尺寸变化监听，避免在销毁时仍触发刷新。
            if (scrollRect && scrollRect.viewport)
            {
                var watcher = scrollRect.viewport.GetComponent<ViewportSizeWatcher>();
                if (watcher) watcher.OnChanged -= SetViewportDirty;
            }
            // 销毁所有实例，避免内存泄漏。
            DestroyAllInstances();
        }

        // ── 公共入口 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 设置数据列表并从<b>起点</b>重新显示（切换页签 / 过滤 / 排序等需要回到顶部 / 起始的场景）。
        /// </summary>
        public virtual void SetItems(IReadOnlyList<TData> items)
        {
            Items = items != null ? new List<TData>(items) : new List<TData>();
            if (!scrollRect || !content) return;

            RebuildLayout();       // 重算布局 + 撑开 content + 补齐实例 + 强制刷新
            RegainAllInstances();  // 使所有缓存索引失效，下次强制全部重绑
            ScrollToStart();       // 回到起点并立刻刷新可见格
        }

        /// <summary>
        /// 增量更新数据列表（<b>保留当前滚动位置</b>）。与 <see cref="SetItems"/> 的唯一区别：不回到起点，
        /// 只按当前滚动位置重新绑定可见格子。适用于内容变化但不希望打断玩家滚动的场景。
        /// </summary>
        public virtual void UpdateItems(IReadOnlyList<TData> items)
        {
            // 实例池尚未建立 → 退化为完整构建。
            if (_instances == null || _instances.Count == 0)
            {
                SetItems(items);
                return;
            }

            Items = items != null ? new List<TData>(items) : new List<TData>();
            SetContentSize(Items.Count);   // 条目增删 → content 尺寸随之变化（起点不动，滚动位置保持）
            RegainAllInstances();
            UpdateVisibleCells();
        }

        /// <summary>
        /// 增量差异刷新（<b>保留当前滚动位置</b>）：只对"当前显示内容与新数据不一致"的可见格重新绑定，
        /// 未变化的格子不动。用于仓库内容变化（拖拽换位 / 堆叠 / 数量增减）时避免重绑<b>全部</b>可见格
        /// （从而避免图标异步重载闪烁与无谓开销）。是否需要重绑由子类 <see cref="NeedsRebind"/> 判定
        /// （默认全部重绑，与 <see cref="UpdateItems"/> 行为一致；叶子可覆写以按数据比较跳过未变格）。
        /// </summary>
        public virtual void RefreshItemsData(IReadOnlyList<TData> items)
        {
            // 实例池尚未建立 → 退化为完整构建（回起点）。
            if (_instances == null || _instances.Count == 0)
            {
                SetItems(items);
                return;
            }

            int oldCount = Items?.Count ?? 0;
            Items = items != null ? new List<TData>(items) : new List<TData>();
            SetContentSize(Items.Count);   // 条目增删 → content 尺寸随之变化（起点不动，滚动位置保持）

            if (Items.Count != oldCount)
            {
                // 条目数量变化：可见窗口"数据索引 ↔ 格子"映射整体位移，无法逐格差分，
                // 退回"回收全部活跃格 + 按当前滚动位置重新分配"（仍保留滚动位置，不回顶）。
                RegainAllInstances();
                _lastFirstIndex = -1;
                UpdateVisibleCells();
                return;
            }

            // 条目数量不变（如拖拽换位 / 就地堆叠）：索引映射不变，只重绑"显示内容已变"的活跃格。
            _tempRebindIndices.Clear();
            foreach (var kv in _idxToInstance)
            {
                int idx = kv.Key;
                if (idx < 0 || idx >= Items.Count) continue;
                if (NeedsRebind(kv.Value, Items[idx])) _tempRebindIndices.Add(idx);
            }
            foreach (int idx in _tempRebindIndices)
            {
                var cell = _idxToInstance[idx];
                BindCell(cell, Items[idx]);
                OnCellAssigned(cell, idx);
            }
        }

        /// <summary>滚动到列表起点（纵向=顶部 / 横向=最左）。</summary>
        public void ScrollToStart()
        {
            if (content) content.anchoredPosition = Vector2.zero;
            UpdateVisibleCells();
        }

        // ── 过滤 / 排序（可选，绑定 filterBar / secondaryFilterBar / sortToolbar）──────────
        //
        // 统一封装原先散落在各视图（背包 / 商店 / 制作 / 技能）的「过滤栏 + 排序栏」样板：
        // 本列表持有工具栏引用并订阅其事件，维护一份「源数据全集」，经 过滤 → 排序 后显示。
        // 差异（过滤谓词 / 排序键 / 排序条件 / 是否写运行时）由视图通过下方 Configure* 注入的小回调提供。
        //
        //   视图侧接入：ConfigureFilter(谓词, 主/副页签token) + ConfigureSort(取itemId, db, 排序条件…)
        //              + 数据变化时 SetSourceItems(全集)；过滤 / 排序栏交互后由本列表自动重排显示。

        private List<TData> _sourceItems;                            // 未过滤未排序的源数据全集
        private Func<TData, string, string, bool> _filterPredicate;  // (data, 主token, 副token) → 保留？null=不按栏过滤
        private Func<TData, bool>                  _extraFilter;      // 搜索 / 其它过滤（视图自管状态）；AND 叠加；null=无
        private Func<TData, string>                _sortKeySelector;  // data → 用于属性比较的 itemId；null=不排序
        private InventoryDatabase                  _sortDatabase;
        private List<SortPriority>                 _sortPriorities  = new List<SortPriority>();
        private List<SortPriority>                 _sortTiebreakers = new List<SortPriority>();
        private Action<List<SortPriority>>         _sortWriteHandler; // 非空 = 写运行时模式（背包）：排序事件回调它，基类不做显示排序
        private bool                               _filterSortSubscribed;

        private void SubscribeFilterSort()
        {
            if (_filterSortSubscribed) return;
            _filterSortSubscribed = true;
            if (filterBar)          filterBar.OnFilterChanged          += HandleFilterChanged;
            if (secondaryFilterBar) secondaryFilterBar.OnFilterChanged += HandleFilterChanged;
            if (sortToolbar)      { sortToolbar.OnSortChanged += HandleSortChanged; sortToolbar.OnAutoSort += HandleAutoSort; }
        }

        private void UnsubscribeFilterSort()
        {
            if (!_filterSortSubscribed) return;
            _filterSortSubscribed = false;
            if (filterBar)          filterBar.OnFilterChanged          -= HandleFilterChanged;
            if (secondaryFilterBar) secondaryFilterBar.OnFilterChanged -= HandleFilterChanged;
            if (sortToolbar)      { sortToolbar.OnSortChanged -= HandleSortChanged; sortToolbar.OnAutoSort -= HandleAutoSort; }
        }

        private void HandleFilterChanged(string _) => RebuildFilteredSorted(preserveScroll: false);

        private void HandleSortChanged(int index, bool ascending)
        {
            if (_sortWriteHandler != null) { _sortWriteHandler(CurrentSortPriorities()); return; }
            RebuildFilteredSorted(preserveScroll: false);
        }

        private void HandleAutoSort()
        {
            if (_sortWriteHandler != null) { _sortWriteHandler(CurrentSortPriorities()); return; }
            RebuildFilteredSorted(preserveScroll: false);
        }

        /// <summary>
        /// 配置过滤：绑定过滤谓词并填充过滤栏页签（不立即触发，autoApply=false）。
        /// <paramref name="predicate"/>(data, 主页签token, 副页签token) 返回是否保留（token 为 null 表示「全部」，谓词内自行放行）。
        /// </summary>
        public void ConfigureFilter(Func<TData, string, string, bool> predicate,
            IReadOnlyList<string> primaryTokens, IReadOnlyList<string> secondaryTokens = null, bool showAll = true)
        {
            _filterPredicate = predicate;
            if (filterBar)          filterBar.SetFilters(primaryTokens, showAll, autoApply: false);
            if (secondaryFilterBar) secondaryFilterBar.SetFilters(secondaryTokens, showAll, autoApply: false);
        }

        /// <summary>设置额外过滤谓词（搜索 / 分组组件等，视图自管其状态）；传 null 清除。<paramref name="refresh"/> 决定是否立即重排显示。</summary>
        public void SetExtraFilter(Func<TData, bool> predicate, bool refresh = true)
        {
            _extraFilter = predicate;
            if (refresh) RebuildFilteredSorted(preserveScroll: false);
        }

        /// <summary>
        /// 配置排序：绑定排序键（data → itemId）+ 数据库 + 排序条件，并填充排序栏下拉（不立即触发）。
        /// <paramref name="writeRuntime"/> 非空 = 「写运行时」模式（背包）：排序事件回调它（由视图写入运行时并触发刷新），
        /// 基类不做显示排序；为空 = 「显示排序」模式（商店 / 制作）：基类按排序键就地排序显示。
        /// </summary>
        public void ConfigureSort(Func<TData, string> sortKeySelector, InventoryDatabase db,
            IReadOnlyList<SortPriority> priorities, IReadOnlyList<SortPriority> tiebreakers,
            Action<List<SortPriority>> writeRuntime = null)
        {
            _sortKeySelector  = sortKeySelector;
            _sortDatabase     = db;
            _sortPriorities   = priorities  != null ? new List<SortPriority>(priorities)  : new List<SortPriority>();
            _sortTiebreakers  = tiebreakers != null ? new List<SortPriority>(tiebreakers) : new List<SortPriority>();
            _sortWriteHandler = writeRuntime;
            if (sortToolbar) sortToolbar.SetSortPriorities(_sortPriorities, db);
        }

        /// <summary>当前排序 UI 选择对应的优先级（主条件 + tiebreakers，全部使用当前方向）。供「写运行时」视图取用。</summary>
        public List<SortPriority> CurrentSortPriorities()
        {
            if (_sortPriorities == null || _sortPriorities.Count == 0) return new List<SortPriority>();

            // 无排序栏（如装备候选列表）：以第一条排序条件为默认排序，沿用各自配置的方向（含 tiebreakers）。
            if (!sortToolbar)
            {
                var def = new List<SortPriority>
                    { new SortPriority(_sortPriorities[0].field, _sortPriorities[0].ascending) };
                if (_sortTiebreakers != null)
                    foreach (var tb in _sortTiebreakers)
                        def.Add(new SortPriority(tb.field, tb.ascending));
                return def;
            }

            // 有排序栏：主条件取下拉选中项，全部使用排序栏当前方向。
            int  idx = Mathf.Clamp(sortToolbar.SortIndex, 0, _sortPriorities.Count - 1);
            bool asc = sortToolbar.Ascending;
            var  all = new List<SortPriority> { new SortPriority(_sortPriorities[idx].field, asc) };
            if (_sortTiebreakers != null)
                foreach (var tb in _sortTiebreakers)
                    all.Add(new SortPriority(tb.field, asc));
            return all;
        }

        /// <summary>
        /// 设置「源数据全集」，经 过滤 → 排序 后显示。取代直接调 <see cref="SetItems"/> / <see cref="RefreshItemsData"/>：
        /// <paramref name="preserveScroll"/>=true 保留滚动位置（增量差异刷新，用于内容变化）；false 回到起点（切页 / 过滤 / 排序 / 切视图）。
        /// </summary>
        public void SetSourceItems(IReadOnlyList<TData> items, bool preserveScroll = false)
        {
            _sourceItems = items != null ? new List<TData>(items) : new List<TData>();
            RebuildFilteredSorted(preserveScroll);
        }

        /// <summary>当前源数据全集（未过滤未排序）。</summary>
        public IReadOnlyList<TData> SourceItems => _sourceItems;

        /// <summary>当前实际显示的数据（已过滤 + 排序）。</summary>
        public IReadOnlyList<TData> DisplayedItems => Items;

        /// <summary>把源数据全集经 过滤（主/副页签 + 额外谓词）→ 排序（显示排序模式）后交给显示。</summary>
        private void RebuildFilteredSorted(bool preserveScroll)
        {
            if (_sourceItems == null) return;
            var list = new List<TData>(_sourceItems);

            // 过滤栏（主 + 副，AND；谓词内部处理 null token = 全部）
            if (_filterPredicate != null && (filterBar || secondaryFilterBar))
            {
                string p = filterBar ? filterBar.ActiveFilter : null;
                string s = secondaryFilterBar ? secondaryFilterBar.ActiveFilter : null;
                list.RemoveAll(d => !_filterPredicate(d, p, s));
            }
            // 额外过滤（搜索 / 分组组件等）
            if (_extraFilter != null)
                list.RemoveAll(d => !_extraFilter(d));

            // 显示排序（「写运行时」模式不在此排序：源已是运行时顺序）
            if (_sortWriteHandler == null && _sortKeySelector != null && _sortDatabase && list.Count > 1)
            {
                var priorities = CurrentSortPriorities();
                if (priorities.Count > 0)
                {
                    list.Sort((a, b) => InventoryRuntimeManager.CompareSlots(
                        new RuntimeItemSlot(null, _sortKeySelector(a), 0),
                        new RuntimeItemSlot(null, _sortKeySelector(b), 0),
                        priorities, _sortDatabase));
                }
            }

            if (preserveScroll) RefreshItemsData(list);
            else                SetItems(list);
        }

        // ── 虚拟滚动核心 ──────────────────────────────────────────────────────────

        /// <summary>滚动回调。</summary>
        private void OnScroll(Vector2 _) => UpdateVisibleCells();

        /// <summary>
        /// 根据当前滚动位置，把滑出窗口的实例<b>即时</b>回收到空闲池（回收廉价），并把窗口内待分配的索引标记为
        /// "待填充"——实际的实例<b>生成 + 分配</b>由限速填充 <see cref="FillWindow"/>（经 <see cref="TickSpawn"/>
        /// 每帧按预算处理）完成，避免单帧一次性实例化 / 绑定大量格子。不限速（<see cref="spawnPerSecond"/> ≤ 0）时
        /// 本方法内即时填满，行为同旧逻辑。"首个可见索引"与"定位"交由布局策略解析。
        /// </summary>
        protected void UpdateVisibleCells()
        {
            if (Items == null || !content) return;
            if (_poolTarget <= 0) return;   // 尚未 RebuildLayout（无目标池），无窗口可算

            int first = Mathf.Max(0, ComputeFirstIndex(content.anchoredPosition));

            // 首个可见索引未变化 → 窗口未移动：无需重设窗口 / 回收；若仍有待填充，交由 LateUpdate 限速 tick 继续。
            if (first == _lastFirstIndex) return;

            // 填充方向：向起点滚动（first 变小）→ 从窗口末尾往前填（视觉"从下往上"逐个浮现）；
            // 向末尾滚动 / 重建后首次（_lastFirstIndex < 0）→ 从窗口开头往后填（"从上往下"）。
            _fillAscending = !(_lastFirstIndex >= 0 && first < _lastFirstIndex);
            _lastFirstIndex = first;

            int last = first + _poolTarget - 1;
            _windowFirst = first;
            _windowLast  = last;

            // Step 1：回收滑出 [first, last] 的活跃实例（钉住的索引除外，如拖拽源格子——
            // 停用它会导致 EventSystem 不再派发其 OnDrag/OnEndDrag，故拖拽期间保持其存活）。即时执行（廉价）。
            int pinned = PinnedDataIndex;
            _tempRecycleIndices.Clear();
            foreach (var kv in _idxToInstance)
                if ((kv.Key < first || kv.Key > last) && kv.Key != pinned)
                    _tempRecycleIndices.Add(kv.Key);
            foreach (int idx in _tempRecycleIndices)
            {
                ClearCell(_idxToInstance[idx]);
                _freeInstances.Add(_idxToInstance[idx]);
                _idxToInstance.Remove(idx);
            }

            // Step 2：标记窗口内待填充。限速由 LateUpdate 的 TickSpawn 逐帧消费预算填充；不限速则本帧填满。
            _hasPendingFill = true;
            if (spawnPerSecond <= 0f) FillWindow(int.MaxValue);
        }

        /// <summary>
        /// 填充当前窗口 [_windowFirst, _windowLast] 内"尚未分配实例"的数据索引：为其惰性取 / 创建格子实例、
        /// 定位并绑定。最多处理 <paramref name="max"/> 个（限速用；不限速传 <see cref="int.MaxValue"/>）。
        /// 扫描完整个窗口全部待填项则清除待填充标记，返回本次实际处理（生成 + 分配）的格子数。
        /// </summary>
        private int FillWindow(int max)
        {
            if (max <= 0) return 0;
            if (Items == null || _poolTarget <= 0) { _hasPendingFill = false; return 0; }

            int processed = 0;
            int count = _windowLast - _windowFirst + 1;
            for (int k = 0; k < count; k++)
            {
                // 按滚动方向决定处理次序：升序 → 从窗口开头往后；降序 → 从末尾往前，使格子按进入视口先后逐个浮现。
                int idx = _fillAscending ? _windowFirst + k : _windowLast - k;
                if (idx < 0 || idx >= Items.Count) continue;    // 超出数据范围
                if (_idxToInstance.ContainsKey(idx)) continue;    // 已分配，保持不动
                if (processed >= max) { _hasPendingFill = true; return processed; }   // 本帧预算耗尽，余量下帧继续

                var inst = TakeOrCreateInstance();
                if (!inst) { _hasPendingFill = true; return processed; }   // 无法取得实例（缺 prefab 等），下帧再试

                ((RectTransform)inst.transform).anchoredPosition = PositionOf(idx);
                BindCell(inst, Items[idx]);
                OnCellAssigned(inst, idx);
                _idxToInstance[idx] = inst;
                processed++;
            }
            _hasPendingFill = false;   // 扫描完整个窗口，已无待填充
            return processed;
        }

        /// <summary>
        /// 取一个空闲实例；空闲池为空则在目标池上限 <see cref="_poolTarget"/> 内<b>惰性新建</b>一个
        /// （新建同样计入限速预算——每次调用至多创建一个）。已达上限或缺 prefab 时返回 null。
        /// </summary>
        private TCell TakeOrCreateInstance()
        {
            if (_freeInstances.Count > 0)
            {
                int lastFree = _freeInstances.Count - 1;
                var reused   = _freeInstances[lastFree];
                _freeInstances.RemoveAt(lastFree);
                return reused;
            }

            if (_instances == null) _instances = new List<TCell>();
            if (!cellPrefab || !content) return null;
            if (_poolTarget > 0 && _instances.Count >= _poolTarget) return null;   // 已达目标池上限，不再新建

            var created = Instantiate(cellPrefab, content);
            SetupInstanceRect(created);
            InitCell(created);
            ClearCell(created);        // 新实例初始为空态（隐藏）
            _instances.Add(created);
            return created;
        }

        /// <summary>销毁所有实例，清空对象池与映射。</summary>
        private void DestroyAllInstances()
        {
            if (_instances == null) return;
            foreach (var inst in _instances)
                if (inst) Destroy(inst.gameObject);
            _instances.Clear();
            _idxToInstance.Clear();
            _freeInstances.Clear();
            _lastFirstIndex = -1;
            _windowLast     = -1;
            _hasPendingFill = false;
            _spawnBudget    = 0f;
        }

        /// <summary>
        /// 把所有活跃实例回收到空闲池并重置 <see cref="_lastFirstIndex"/>，
        /// 使下次 <see cref="UpdateVisibleCells"/> 强制重新分配全部可见格。数据变更 / 跨轴数量变化后调用。
        /// </summary>
        protected void RegainAllInstances()
        {
            foreach (var kv in _idxToInstance)
            {
                ClearCell(kv.Value);
                _freeInstances.Add(kv.Value);
            }
            _idxToInstance.Clear();
            _lastFirstIndex = -1;
        }

        // ── Viewport 尺寸变化监听 ─────────────────────────────────────────────────
        // OnRectTransformDimensionsChange 在 Canvas Rebuild 循环内同步触发，其中不能修改任何 UI 元素
        // （否则报 "rebuild list" 错误）。故只在其中置脏，由 LateUpdate 在 Rebuild 结束后安全处理。
        private bool _viewportSizeDirty;
        private void SetViewportDirty() => _viewportSizeDirty = true;

        protected virtual void LateUpdate()
        {
            if (_viewportSizeDirty)
            {
                _viewportSizeDirty = false;
                RebuildLayout();
            }
            TickSpawn();
        }

        /// <summary>
        /// 限速填充 tick（每帧）：按 <see cref="spawnPerSecond"/> 累积预算，处理窗口内待填充的格子
        /// （惰性生成 + 分配绑定），把单帧峰值分摊到多帧。无待填充时立即返回（空转开销极低）。
        /// </summary>
        private void TickSpawn()
        {
            if (!_hasPendingFill) return;

            // 不限速兜底（含运行时把 spawnPerSecond 改为 ≤ 0）：一次填满。
            if (spawnPerSecond <= 0f) { FillWindow(int.MaxValue); return; }

            _spawnBudget += spawnPerSecond * Time.unscaledDeltaTime;
            // 预算封顶（约 SpawnBudgetMaxSeconds 秒的量）：打开界面那一帧 unscaledDeltaTime 往往很大，
            // 若按整秒封顶会让预算瞬间冲满、一帧爆发实例化整屏；限到很小的量即可平滑分摊到多帧。
            float cap = Mathf.Max(1f, spawnPerSecond * SpawnBudgetMaxSeconds);
            if (_spawnBudget > cap) _spawnBudget = cap;

            int quota = Mathf.FloorToInt(_spawnBudget);
            if (quota <= 0) return;

            _spawnBudget -= FillWindow(quota);   // 扣减实际处理的格子数
        }

        /// <summary>
        /// 重算布局并刷新可见格：重算跨轴数量（网格）→ 撑开 content → 更新目标池大小 → 强制重算窗口。
        /// 实例按需惰性创建到目标池上限（受限速约束），可由视口尺寸变化（<see cref="LateUpdate"/>）
        /// 或数据变更（<see cref="SetItems"/>）触发。
        /// </summary>
        protected void RebuildLayout()
        {
            if (Items == null) return;
            if (!scrollRect || !scrollRect.viewport || !content) return;

            var vp = scrollRect.viewport.rect;
            if (vp.width <= 0f || vp.height <= 0f) return;

            RecomputeLayout(vp);                 // 网格：按视口重算跨轴数量（变化则内部 RegainAllInstances）
            SetContentSize(Items.Count);        // 撑开 content 尺寸
            _poolTarget = InstancesNeeded(vp);   // 目标池大小 = 可见窗口跨度；实例按需惰性创建到此上限

            _lastFirstIndex = -1;                // 绕过 early-return，强制重算窗口
            UpdateVisibleCells();
        }

        // ── 布局策略（子类实现：一维纵向 / 二维网格，纵向 / 横向） ───────────────────

        /// <summary>测量格子尺寸（从 <see cref="cellPrefab"/> 的 RectTransform 读取行高 / 列宽）。</summary>
        protected abstract void MeasureCell();

        /// <summary>按数据量撑开 <see cref="content"/> 的尺寸（纵向设高、横向设宽；网格按跨轴数量折行）。</summary>
        protected abstract void SetContentSize(int count);

        /// <summary>按视口尺寸计算所需实例数量（覆盖可见窗口 + 两端缓冲）。</summary>
        protected abstract int InstancesNeeded(Rect viewport);

        /// <summary>按 <see cref="content"/> 锚点位置计算首个可见数据索引（沿滚动主轴）。</summary>
        protected abstract int ComputeFirstIndex(Vector2 contentAnchoredPos);

        /// <summary>计算数据索引对应实例在 <see cref="content"/> 中的锚点位置。</summary>
        protected abstract Vector2 PositionOf(int index);

        /// <summary>设置新建实例的锚点 / 轴心 / 尺寸（纵向拉伸单列 / 固定尺寸网格格）。</summary>
        protected abstract void SetupInstanceRect(TCell inst);

        /// <summary>按视口重算布局参数（网格重算跨轴数量，变化则触发全量重建）；一维列表默认空实现。</summary>
        protected virtual void RecomputeLayout(Rect viewport) { }

        // ── 格子绑定（叶子实现：把数据显示到格子 / 清空格子） ───────────────────────

        /// <summary>把一条数据显示到一个格子（含"窗口内空槽"的可见空态处理，由叶子决定）。</summary>
        protected abstract void BindCell(TCell cell, TData data);

        /// <summary>把一个格子清空并隐藏（用于回收出窗口的实例）。</summary>
        protected abstract void ClearCell(TCell cell);

        /// <summary>新建实例后的一次性初始化钩子（如设数字格式、接线点击事件）。默认空实现。</summary>
        protected virtual void InitCell(TCell cell) { }

        /// <summary>实例被分配给某数据索引、完成绑定后的钩子（如配置拖拽 handler 的动态索引、刷新选中高亮）。默认空实现。</summary>
        protected virtual void OnCellAssigned(TCell cell, int dataIndex) { }

        /// <summary>
        /// 增量差异刷新（<see cref="RefreshItemsData"/>）时判断某活跃格是否需要重绑：
        /// 返回 false 则跳过该格（其当前显示与新数据一致）。<b>默认恒 true</b>（即全部重绑，行为同旧逻辑）；
        /// 叶子可覆写，比较"格子当前显示内容"与新数据以跳过未变格（避免图标异步重载闪烁）。
        /// </summary>
        protected virtual bool NeedsRebind(TCell cell, TData data) => true;

        /// <summary>
        /// 需在滚动回收时"钉住"（不回收 / 不停用）的数据索引，-1 表示无。默认 -1。
        /// 叶子可覆写以在拖拽期间钉住源格子（防止其被自动滚动回收后停用而收不到拖拽事件）。
        /// </summary>
        protected virtual int PinnedDataIndex => -1;

        /// <summary>强制在下次刷新时重新评估全部可见格（绕过"首索引未变"的提前返回）。用于解除钉住后立即回收。</summary>
        protected void ForceRefreshVisible()
        {
            _lastFirstIndex = -1;
            UpdateVisibleCells();
        }
    }
}

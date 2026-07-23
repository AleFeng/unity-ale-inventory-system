using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 制作主界面（MonoBehaviour）。整合 蓝图模板页签 + 蓝图名搜索 + 分组折叠页签 + 排序整理栏 +
    /// 蓝图虚拟列表 + 蓝图详情。过滤管线：模板 → 分组（主/副）→ 名称搜索；再按所选模板的「整理设置」排序。
    /// 结构与 <see cref="UiwInventoryView"/> 对称。
    /// </summary>
    public class UiwCraftingView : UiwViewBase
    {
        private void Start()
        {
            if (groupFilter)  groupFilter.OnGroupChanged += OnGroupChanged;
            if (searchInput)  searchInput.onValueChanged.AddListener(OnSearchChanged);
            // 排序栏事件已下沉到蓝图列表组件（UiwInventoryListBase）自管，此处不再订阅。
            if (blueprintList) blueprintList.OnBlueprintSelected += OnBlueprintSelected;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();   // 取消按打开订阅的 OnInventoryChanged
            // 工具栏 / 搜索 / 列表事件随组件生命周期订阅（Start），此处一并取消
            if (groupFilter)   groupFilter.OnGroupChanged -= OnGroupChanged;
            if (searchInput)   searchInput.onValueChanged.RemoveListener(OnSearchChanged);
            if (blueprintList) blueprintList.OnBlueprintSelected -= OnBlueprintSelected;
        }

        #region 打开与关闭

        /// <summary>打开制作界面（收集数据、构建模板页签并选中首个）。</summary>
        public override void Open()
        {
            base.Open();   // 激活面板（公共步骤）
            GatherData();

            if (InventoryRuntimeManager.Instance)
                InventoryRuntimeManager.Instance.OnInventoryChanged += OnInventoryChanged;

            BuildTemplateTabs();
            if (_templateTabs.Count > 0)
                SwitchTemplate(0);
            else
            {
                _currentTemplate = null;
                _currentTemplateObj = null;
                if (groupFilter) groupFilter.SetBlueprints(CollectTemplateBlueprints(), autoApply: false);
                ConfigureBlueprintSort();
                _activePrimary = null; _activeSub = null;
                RefilterAndShow();
            }
        }

        /// <summary>取消本视图按打开订阅的运行时事件（由基类 <see cref="UiwViewBase.Close"/> 与本类 OnDestroy 调用）。</summary>
        protected override void Unsubscribe()
        {
            if (InventoryRuntimeManager.Instance)
                InventoryRuntimeManager.Instance.OnInventoryChanged -= OnInventoryChanged;
        }

        /// <summary>重新打开制作界面（供基类 <see cref="UiwViewBase.ToggleOpenClose"/>）。</summary>
        protected override void Reopen() => Open();

        #endregion

        #region 数据收集

        private readonly List<CraftingBlueprint>         _allBlueprints = new List<CraftingBlueprint>();
        private readonly List<CraftingBlueprintTemplate> _templates     = new List<CraftingBlueprintTemplate>();
        private InventoryDatabase _db;   // 用于排序的数据库（取首个含制作数据的库）

        private void GatherData()
        {
            _allBlueprints.Clear();
            _templates.Clear();
            _db = null;

            var dm = InventoryDataManager.Instance;
            if (dm == null) return;

            foreach (var db in dm.Databases)
            {
                if (!db) continue;
                if (_db == null && db.CraftingBlueprints.Count > 0) _db = db;
                _templates.AddRange(db.CraftingBlueprintTemplates);
                _allBlueprints.AddRange(db.CraftingBlueprints);
            }
            if (_db == null && dm.Databases.Count > 0) _db = dm.Databases[0];
        }

        private CraftingBlueprintTemplate FindTemplate(string tempName)
        {
            if (string.IsNullOrEmpty(tempName)) return null;
            foreach (var temp in _templates) if (temp.name == tempName) return temp;
            return null;
        }

        #endregion

        #region 蓝图模板页签
        [Header("蓝图模板页签")]
        [Tooltip("模板页签容器。")]
        public Transform       templateTabContainer;
        [Tooltip("模板页签 Prefab（复用 UiwInventoryTab）。")]
        public UiwInventoryTab templateTabPrefab;
        [Tooltip("是否显示「全部」模板页签（显示所有蓝图）。")]
        public bool            showAllTemplateTab;
        [Tooltip("「全部」模板页签显示名。")]
        public string          allTemplateLabel = "全部";

        private readonly List<UiwInventoryTab> _templateTabs = new List<UiwInventoryTab>();
        private readonly List<string>          _templateRefs = new List<string>(); // 与 _templateTabs 平行；null = 全部
        // private int    _currentTemplateIndex = -1;
        private string _currentTemplate;                  // null = 全部
        private CraftingBlueprintTemplate _currentTemplateObj;

        private void BuildTemplateTabs()
        {
            foreach (var t in _templateTabs) if (t) Destroy(t.gameObject);
            _templateTabs.Clear();
            _templateRefs.Clear();

            if (!templateTabPrefab || !templateTabContainer) return;

            if (showAllTemplateTab) AddTemplateTab(null, allTemplateLabel);
            foreach (var t in _templates) AddTemplateTab(t.name, t.name);
        }

        private void AddTemplateTab(string templateRef, string display)
        {
            int idx = _templateTabs.Count;
            var tab = Instantiate(templateTabPrefab, templateTabContainer);
            tab.SetData(templateRef ?? string.Empty, display, false);
            var btn = tab.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => SwitchTemplate(idx));
            _templateTabs.Add(tab);
            _templateRefs.Add(templateRef);
        }

        private void SwitchTemplate(int index)
        {
            if (index < 0 || index >= _templateTabs.Count) return;

            // _currentTemplateIndex = index;
            _currentTemplate      = _templateRefs[index];
            _currentTemplateObj   = FindTemplate(_currentTemplate);

            for (int i = 0; i < _templateTabs.Count; i++)
                _templateTabs[i]?.SetData(_templateRefs[i] ?? string.Empty,
                    i == 0 && showAllTemplateTab ? allTemplateLabel : _templateRefs[i], i == index);

            // 重置搜索 + 分组筛选
            _search = string.Empty;
            if (searchInput) searchInput.SetTextWithoutNotify(string.Empty);
            _activePrimary = null; _activeSub = null;
            if (groupFilter) groupFilter.SetBlueprints(CollectTemplateBlueprints(), autoApply: false);

            ConfigureBlueprintSort();
            RefilterAndShow();
            RefreshTitle();
        }

        // 收集当前模板下的蓝图（全部模板时为所有蓝图），用于构建分组折叠页签。
        private List<CraftingBlueprint> CollectTemplateBlueprints()
        {
            var list = new List<CraftingBlueprint>();
            foreach (var bp in _allBlueprints)
            {
                if (bp == null) continue;
                if (!string.IsNullOrEmpty(_currentTemplate) && bp.templateRef != _currentTemplate) continue;
                list.Add(bp);
            }
            return list;
        }

        #endregion

        #region 标题
        // titleLabel 继承自 UiwViewBase。

        private void RefreshTitle()
        {
            if (!titleLabel) return;
            titleLabel.text = string.IsNullOrEmpty(_currentTemplate) ? allTemplateLabel : _currentTemplate;
        }
        #endregion

        #region 搜索
        [Header("搜索")]
        [Tooltip("蓝图名称搜索输入框（仅匹配 UI 显示的蓝图名称）。")]
        public InputField searchInput;

        private string _search = string.Empty;

        private void OnSearchChanged(string value)
        {
            _search = value;
            RefilterAndShow();
        }
        #endregion

        #region 分组折叠页签
        [Header("分组折叠页签")]
        [Tooltip("分组折叠页签组件（UiwCraftingGroupFilter）。")]
        public UiwCraftingGroupFilter groupFilter;

        private string _activePrimary; // null = 全部
        private string _activeSub;     // null = 整个主分组 / 全部

        private void OnGroupChanged(string primary, string sub)
        {
            _activePrimary = primary;
            _activeSub     = sub;
            RefilterAndShow();
        }
        #endregion

        #region 排序整理栏（委托给蓝图列表组件）
        // 排序栏引用已移到蓝图列表组件（UiwCraftingBlueprintList 继承的 UiwInventoryListBase）上，
        // 排序管线由列表基类内建；本视图只在模板切换时把「排序键 + 排序条件」配置给列表。

        /// <summary>配置蓝图列表的显示排序：排序键取蓝图主产物道具 ID，条件来自所选模板的整理设置。</summary>
        private void ConfigureBlueprintSort()
        {
            if (!blueprintList) return;
            var sps = _currentTemplateObj != null ? _currentTemplateObj.sortPriorities : null;
            var tbs = _currentTemplateObj != null ? _currentTemplateObj.sortTiebreakers : null;
            blueprintList.ConfigureSort(MainItemId, _db, sps, tbs);
        }
        #endregion

        #region 蓝图列表 / 详情
        [Header("蓝图列表 / 详情")]
        [Tooltip("蓝图虚拟列表组件。")]
        public UiwCraftingBlueprintList blueprintList;
        [Tooltip("蓝图详情面板组件。")]
        public UiwCraftingDetail detail;

        private void RefilterAndShow()
        {
            var list = new List<CraftingBlueprint>();
            foreach (var bp in _allBlueprints)
            {
                if (bp == null) continue;
                if (!string.IsNullOrEmpty(_currentTemplate) && bp.templateRef != _currentTemplate) continue;
                if (!string.IsNullOrEmpty(_activePrimary))
                {
                    if (bp.primaryGroupTag != _activePrimary) continue;
                    if (!string.IsNullOrEmpty(_activeSub) &&
                        (bp.secondaryGroupTags == null || !bp.secondaryGroupTags.Contains(_activeSub))) continue;
                }
                if (!string.IsNullOrEmpty(_search) && !NameMatches(bp, _search)) continue;
                list.Add(bp);
            }

            // 排序交由蓝图列表组件内建管线（显示排序）；本视图只喂过滤后的源数据。
            if (blueprintList) blueprintList.SetSourceItems(list);
            ApplySelection(blueprintList ? blueprintList.DisplayedItems : list);
        }

        // 选中保持：原选中仍在列表则保留并刷新详情；否则选中首个；空列表则清空详情。
        private void ApplySelection(IReadOnlyList<CraftingBlueprint> list)
        {
            CraftingBlueprint sel = blueprintList ? blueprintList.SelectedBlueprint : null;
            if (sel == null && list.Count > 0) sel = list[0];

            if (blueprintList) blueprintList.SetSelectedById(sel != null ? sel.id : null);
            if (detail) detail.Bind(sel, ResolveFmt(sel));
        }

        private void OnBlueprintSelected(CraftingBlueprint bp)
        {
            if (detail) detail.Bind(bp, ResolveFmt(bp));
        }

        private static string MainItemId(CraftingBlueprint bp)
            => bp != null && bp.PrimaryOutput != null ? bp.PrimaryOutput.itemId : null;

        private static bool NameMatches(CraftingBlueprint bp, string term)
        {
            string name = ResolveName(bp);
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ResolveName(CraftingBlueprint bp)
        {
            if (bp == null) return string.Empty;
            // 仅匹配 UI 显示名（displayText：本地化优先、回退纯文本），不含 id。
            return bp.displayText != null ? bp.displayText.ResolveText() : string.Empty;
        }

        #endregion

        #region 数字格式
        // GetCurrentLanguage / ResolveNumberFormatLocale 继承自 UiwViewBase。
        private NumberFormatLocale ResolveFmt(CraftingBlueprint bp)
            => bp == null
                ? null
                : ResolveNumberFormatLocale(InventoryDataManager.Instance.GetNumberFormatConfig(bp.numberFormatRef));
        #endregion

        #region 事件
        private void OnInventoryChanged(string inventoryId)
        {
            var sel = blueprintList ? blueprintList.SelectedBlueprint : null;
            if (sel != null && sel.craftInventoryRefs.Contains(inventoryId) && detail)
                detail.Refresh();
        }
        #endregion
    }
}

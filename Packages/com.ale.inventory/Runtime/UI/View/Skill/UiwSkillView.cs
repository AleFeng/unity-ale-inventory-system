#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 技能主界面（MonoBehaviour，继承 <see cref="UiwViewBase"/>）。
    /// 标题 + 搜索栏 + 主 / 副分组页签（各含「全部」）+ 网格 / 顺序双显示模式列表；技能来源可在四种间切换
    /// （数据库 / 装备组 / 仓库 / 角色已学）。悬停经 <see cref="UiwSkillTooltip"/> 显示详情。
    ///
    /// <para>技能采集由 <see cref="SkillCollector"/> 完成；来源对应的运行时变化事件触发自动刷新。
    /// 主 / 副分组页签各复用一个 <see cref="UiwFilterTabBar"/>，为**两个 AND 筛选条件**：技能需同时满足
    /// 「主分组 == 选中主页签」且「某副分组 == 选中副页签」才显示（选「全部」则该条件不过滤）。
    /// 页签仅按当前来源技能**实际配置到**的主 / 副分组标签生成（避免出现无技能的空标签页签），
    /// 并以分组标签「显示名」作为过滤 token（故各分组显示名应互不相同）。</para>
    /// </summary>
    public class UiwSkillView : UiwViewBase
    {
        [Header("技能来源")]
        [Tooltip("技能信息来源。")]
        public ESkillSource source = ESkillSource.InventoryDatabase;
        [Tooltip("来源 = Equipment 时的装备组 ID。")]
        public string equipmentGroupId;
        [Tooltip("来源 = Inventory 时的仓库 ID。")]
        public string inventoryId;
        [Tooltip("来源 = Character 时的角色 ID。")]
        public string characterId;
        [Tooltip("来源 = Equipment / Inventory 时，道具上存放技能 ID 的属性字段 ID（String / String 数组）。")]
        public string skillRefAttrId = "技能";

        [Header("标题")]
        [Tooltip("标题文本内容（写入继承的 titleLabel）。")]
        public string titleText = "技能";

        [Header("技能列表")]
        [Tooltip("顺序（列表）技能列表。")]
        public UiwSkillOrderList orderList;
        [Tooltip("网格技能列表。")]
        public UiwSkillGridList gridList;

        [Header("分组页签")]
        [Tooltip("是否显示「全部」页签。")]
        public bool showAllTab = true;
        // 主 / 副分组页签栏（UiwFilterTabBar）引用已移到技能列表组件（网格 / 顺序列表）上，
        // 与搜索一起构成列表基类内建的过滤管线；本视图只提供页签 token 与匹配谓词。

        [Header("搜索")]
        [Tooltip("搜索输入框（按名称 / ID 过滤）。")]
        public InputField searchInput;
        
        private string _search = string.Empty; // 当前搜索过滤 token（名称 / ID；null / 空 = 全部）
        private bool   _subscribed; // 是否已订阅事件（避免重复订阅）

        private readonly List<string> _primaryTokens   = new List<string>(); // 主分组页签 token（显示名）
        private readonly List<string> _secondaryTokens = new List<string>(); // 副分组页签 token（显示名）

        private void Start()
        {
            if (searchInput) searchInput.onValueChanged.AddListener(OnSearchChanged);
            // 主 / 副分组页签栏事件由技能列表组件（UiwInventoryListBase）自管，此处不再订阅。

            // 视图模式（含无切换按钮时自动采用已配置的那个视图）由基类统一处理。
            SetupViewModeToggle(orderList, gridList);
            ApplyViewMode();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            TeardownViewModeToggle();
            if (searchInput) searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        }

        #region 打开 / 关闭

        /// <summary>用当前序列化的来源配置打开技能界面。</summary>
        public override void Open()
        {
            base.Open();   // 激活面板（公共步骤）

            if (titleLabel) titleLabel.text = titleText;

            // 采集一次，页签构建与列表刷新共用（原实现各采集一遍，每次打开跑两趟）。
            var skills = SkillCollector.Collect(source, ConfigIdForSource(), skillRefAttrId);
            BuildGroupTabs(skills);
            Subscribe();
            Refresh(skills);
        }

        /// <summary>切换来源并打开（configId 依来源写入对应字段）。</summary>
        public void Open(ESkillSource newSource, string configId)
        {
            source = newSource;
            switch (newSource)
            {
                case ESkillSource.Equipment: equipmentGroupId = configId; break;
                case ESkillSource.Inventory: inventoryId      = configId; break;
                case ESkillSource.Character: characterId      = configId; break;
            }
            Open();
        }

        /// <summary>取消本视图按打开订阅的运行时事件（由基类 <see cref="UiwViewBase.Close"/> 与本类 OnDestroy 调用）。</summary>
        protected override void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            if (EquipmentRuntimeManager.Instance != null)
                EquipmentRuntimeManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
            if (InventoryRuntimeManager.Instance)
                InventoryRuntimeManager.Instance.OnInventoryChanged -= OnInventoryChanged;
            if (SkillRuntimeManager.Instance != null)
                SkillRuntimeManager.Instance.OnLearnedChanged -= OnLearnedChanged;
        }

        /// <summary>用上次的来源配置重新打开（供基类 <see cref="UiwViewBase.ToggleOpenClose"/>）。</summary>
        protected override void Reopen() => Open();
        
        /// <summary>
        /// 关闭 技能界面（由基类 <see cref="UiwViewBase.Close"/> 调用）。
        /// </summary>
        private void Subscribe()
        {
            Unsubscribe();
            switch (source)
            {
                case ESkillSource.Equipment:
                    if (EquipmentRuntimeManager.Instance != null)
                        EquipmentRuntimeManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
                    break;
                case ESkillSource.Inventory:
                    if (InventoryRuntimeManager.Instance)
                        InventoryRuntimeManager.Instance.OnInventoryChanged += OnInventoryChanged;
                    break;
                case ESkillSource.Character:
                    if (SkillRuntimeManager.Instance != null)
                        SkillRuntimeManager.Instance.OnLearnedChanged += OnLearnedChanged;
                    break;
                // InventoryDatabase：静态数据，无运行时变化事件，无需订阅。
            }
            _subscribed = true;
        }
        
        /// <summary>
        /// 刷新 技能列表（按当前来源、分组页签、搜索条件）。
        /// </summary>
        /// <param name="groupId"></param>
        private void OnEquipmentChanged(string groupId)  { if (groupId == equipmentGroupId) Refresh(); }
        /// <summary>
        /// 仓库 内容发生变化时调用（由 <see cref="InventoryRuntimeManager.OnInventoryChanged"/> 触发）。
        /// </summary>
        /// <param name="invId"></param>
        private void OnInventoryChanged(string invId)    { if (invId == inventoryId)        Refresh(); }
        /// <summary>
        /// 角色 技能变化事件回调（供订阅 <see cref="SkillRuntimeManager.OnLearnedChanged"/>）。
        /// </summary>
        /// <param name="charId"></param>
        private void OnLearnedChanged(string charId)     { if (charId == characterId)       Refresh(); }

        #endregion

        #region 分组页签

        /// <summary>
        /// 依当前来源技能实际配置到的主 / 副分组标签，生成主 / 副两栏页签（去掉无技能的空标签）。
        /// 由Open方法调用；运行时变化仅走 <see cref="Refresh"/> 重新过滤，不重建页签（避免重置选中）。
        /// </summary>
        /// <param name="skills">已采集的当前来源技能（由调用方提供，避免与 <see cref="Refresh"/> 重复采集）。</param>
        private void BuildGroupTabs(List<Skill> skills)
        {
            _primaryTokens.Clear();
            _secondaryTokens.Clear();

            var dm = InventoryDataManager.Instance;
            if (dm != null)
            {
                // 收集当前来源技能实际用到的主 / 副分组标签 ID。
                var primaryUsed   = new HashSet<string>();
                var secondaryUsed = new HashSet<string>();
                foreach (var s in skills)
                {
                    if (s == null) continue;
                    if (!string.IsNullOrEmpty(s.primaryGroupTag)) primaryUsed.Add(s.primaryGroupTag);
                    if (s.secondaryGroupTags != null)
                        foreach (var g in s.secondaryGroupTags)
                            if (!string.IsNullOrEmpty(g)) secondaryUsed.Add(g);
                }

                // 按数据库分组标签顺序，仅保留被实际使用的标签，转为显示名 token。
                foreach (var t in dm.GetAllSkillGroupTags())
                {
                    if (t == null || string.IsNullOrEmpty(t.id)) continue;
                    string disp = GroupDisplayOf(t);
                    if (primaryUsed.Contains(t.id)   && !_primaryTokens.Contains(disp))   _primaryTokens.Add(disp);
                    if (secondaryUsed.Contains(t.id) && !_secondaryTokens.Contains(disp)) _secondaryTokens.Add(disp);
                }
            }

            ConfigureListsFilter();
        }

        /// <summary>
        /// 把「主 / 副分组 AND 过滤谓词 + 页签 token」配置到网格 / 顺序两个技能列表组件上
        /// （两列表共用同一对过滤栏；仅当前激活的列表响应其事件）。
        /// </summary>
        private void ConfigureListsFilter()
        {
            Func<Skill, string, string, bool> predicate =
                (s, primary, secondary) => MatchesPrimary(s, primary) && MatchesSecondary(s, secondary);
            if (gridList)  gridList.ConfigureFilter(predicate, _primaryTokens, _secondaryTokens, showAllTab);
            if (orderList) orderList.ConfigureFilter(predicate, _primaryTokens, _secondaryTokens, showAllTab);
        }

        /// <summary>当前激活的技能列表（网格 / 顺序），以基类类型返回以调用过滤 / 数据入口。</summary>
        private UiwInventoryListBase<Skill, UiwSkillEntry> ActiveList()
            => GridMode
                ? (gridList  ? gridList  : orderList)
                : (orderList ? orderList : gridList);
        
        /// <summary>
        /// 获取 分组标签的显示名（若数据库中未配置显示名，则返回 ID）。
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static string GroupDisplayOf(SkillGroupTag tag)
            => tag == null ? string.Empty : tag.ResolveDisplayName();

        /// <summary>按 ID 解析分组标签显示名（用于把技能所属分组与页签 token 对齐）。</summary>
        private static string GroupDisplayOfId(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return string.Empty;
            var dm = InventoryDataManager.Instance;
            var tag = dm != null ? dm.GetSkillGroupTag(groupId) : null;
            return tag != null ? GroupDisplayOf(tag) : string.Empty;
        }

        #endregion

        #region 搜索
        
        /// <summary>
        /// 搜索栏 文本变化时，更新搜索关键字并刷新技能列表。
        /// </summary>
        /// <param name="value"></param>
        private void OnSearchChanged(string value)
        {
            _search = value ?? string.Empty;
            var active = ActiveList();

            // 启用「全部」且开始搜索时：主 / 副分组页签都切到「全部」（重建高亮，不触发回调），使搜索跨全部分组。
            if (showAllTab && !string.IsNullOrEmpty(_search) && active != null)
            {
                if (active.filterBar && !string.IsNullOrEmpty(active.filterBar.ActiveFilter))
                    active.filterBar.SetFilters(_primaryTokens, showAllTab, autoApply: false);
                if (active.secondaryFilterBar && !string.IsNullOrEmpty(active.secondaryFilterBar.ActiveFilter))
                    active.secondaryFilterBar.SetFilters(_secondaryTokens, showAllTab, autoApply: false);
            }

            // 搜索作为额外过滤（读取当前 _search），触发列表重排。
            active?.SetExtraFilter(s => MatchesSearch(s, _search), refresh: true);
        }

        #endregion

        #region 视图模式

        /// <summary>激活当前模式对应的技能列表组件，隐藏另一个（基类 <see cref="UiwViewBase"/> 驱动）。</summary>
        protected override void OnApplyViewMode(bool gridMode)
        {
            if (orderList) orderList.gameObject.SetActive(!gridMode);
            if (gridList)  gridList.gameObject.SetActive(gridMode);
        }

        /// <summary>切换模式后重新采集并刷新列表。</summary>
        protected override void OnViewModeChanged() => Refresh();

        #endregion

        #region 刷新

        /// <summary>按当前来源 / 主副分组 / 搜索采集并刷新技能列表。</summary>
        public void Refresh()
            => Refresh(SkillCollector.Collect(source, ConfigIdForSource(), skillRefAttrId));

        /// <summary>用已采集好的技能列表刷新（供 <see cref="Open"/> 与页签构建共用同一次采集）。</summary>
        private void Refresh(List<Skill> skills)
        {
            var active = ActiveList();
            if (active == null) return;

            // 源为采集到的全部技能；主 / 副分组（AND）+ 搜索 过滤由列表基类内建管线完成。
            active.SetExtraFilter(s => MatchesSearch(s, _search), refresh: false);
            active.SetSourceItems(skills);
        }
        
        /// <summary>
        /// 检查 技能是否匹配主分组页签 token。
        /// </summary>
        /// <returns></returns>
        private string ConfigIdForSource()
        {
            switch (source)
            {
                case ESkillSource.Equipment: return equipmentGroupId;
                case ESkillSource.Inventory: return inventoryId;
                case ESkillSource.Character: return characterId;
                default:                     return null;
            }
        }
        
        /// <summary>
        /// 检查 技能是否匹配主分组页签 token。
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool MatchesPrimary(Skill skill, string token)
        {
            if (string.IsNullOrEmpty(token)) return true;   // 全部
            return GroupDisplayOfId(skill.primaryGroupTag) == token;
        }
        
        /// <summary>
        /// 副分组 匹配条件：技能的任意副分组标签显示名与页签 token 相同即匹配。
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool MatchesSecondary(Skill skill, string token)
        {
            if (string.IsNullOrEmpty(token)) return true;   // 全部
            if (skill.secondaryGroupTags == null) return false;
            foreach (var g in skill.secondaryGroupTags)
                if (GroupDisplayOfId(g) == token) return true;
            return false;
        }
        
        /// <summary>
        /// 检查 技能是否匹配搜索条件（名称 / 描述 / 备注 / 技能 ID）。
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        private static bool MatchesSearch(Skill skill, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            string name = UiwSkillText.ResolveName(skill);
            if (!string.IsNullOrEmpty(name) &&
                name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(skill.id) &&
                skill.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        #endregion
    }
}

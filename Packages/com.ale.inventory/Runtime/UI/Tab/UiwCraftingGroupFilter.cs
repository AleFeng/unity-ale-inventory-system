using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 制作分组折叠页签（MonoBehaviour）。顶部恒有「全部」（跨所有主分组）；其下按蓝图出现的主分组生成
    /// 可折叠「下拉」标题，展开后第一项恒为「全部」（该主分组的全部条目），其后为该主分组下蓝图所带的副分组子页签。
    /// 主分组标题点击仅展开 / 折叠，不改变筛选；筛选由「全部」与各副分组项触发。
    /// 通过 <see cref="OnGroupChanged"/> 通知宿主当前筛选：(主分组ID, 副分组ID)，均为 null 表示「全部」；只有主分组时副分组为 null。
    /// 主 / 副分组顺序按「分组标签」定义顺序排列。
    ///
    /// <para>性能：整个分组列表仅在切换蓝图模板（<see cref="SetBlueprints"/>）时重建；之后的展开 / 折叠仅
    /// 显示 / 隐藏已建好的子项按钮并切换折叠图标，选中变化仅刷新各按钮高亮，均不再销毁 / 重建 UI。</para>
    /// </summary>
    public class UiwCraftingGroupFilter : MonoBehaviour
    {
        [Header("分组折叠页签")]
        [Tooltip("分组页签父容器（建议挂 VerticalLayoutGroup）。")]
        public Transform container;
        [Tooltip("分组页签 Prefab（UiwFoldTab：左侧图标 + 右侧名称文本的可点击页签）。")]
        public UiwFoldTab uiwFoldTabPrefab;
        [Tooltip("「全部」按钮显示名。")]
        public string    allLabel = "全部";
        [Tooltip("副分组子页签前缀（缩进，用以与主分组区分）。")]
        public string    subIndentText = "    ";

        [Header("折叠状态图标（主分组左侧）")]
        [Tooltip("主分组展开 / 折叠的文本前缀（仅当未配置折叠图标时回退使用）。")]
        public string    expandedPrefix  = "▾ ";
        public string    collapsedPrefix = "▸ ";
        [Tooltip("主分组展开时显示的图标（留空则回退为文本前缀 expandedPrefix）。")]
        public Sprite    expandedIcon;
        [Tooltip("主分组折叠时显示的图标（留空则回退为文本前缀 collapsedPrefix）。")]
        public Sprite    collapsedIcon;

        [Tooltip("选中时按钮 normalColor。")]
        public Color     activeColor   = new Color(1.00f, 0.85f, 0.30f, 1.00f);
        [Tooltip("未选中时按钮 normalColor。")]
        public Color     inactiveColor = Color.white;

        /// <summary>分组筛选变化事件。参数为 (主分组ID, 副分组ID)；均为 null = 全部；副为 null = 整个主分组。</summary>
        public event Action<string, string> OnGroupChanged;

        // 主分组节点。重建时（仅模板切换）填充其 UI 引用，之后展开 / 折叠与高亮均直接复用，不再重建。
        private class Main
        {
            public string ID;
            public readonly List<string> Subs = new List<string>();
            public bool Expanded;

            // UI 引用（由 BuildButtons 填充）。折叠图标 / 文本均通过 headerTab 访问。
            public UiwFoldTab HeaderTab;
            public readonly List<UiwFoldTab> ChildTabs = new List<UiwFoldTab>(); // 「全部」+ 各副分组（折叠时隐藏）
        }

        // 单个页签及其「是否选中」判定所需的元数据，用于增量刷新高亮（不重建）。
        private enum RowKind { All, MainHeader, MainAll, Sub }
        private class Row
        {
            public UiwFoldTab Tab;
            public RowKind    Kind;
            public string     Primary;  // MainHeader / MainAll / Sub 关联的主分组
            public string     Sub;      // Sub 关联的副分组
        }

        private readonly List<Main> _mains = new List<Main>();
        private readonly List<Row>  _rows  = new List<Row>();
        private string _activePrimary; // null = 全部
        private string _activeSub;     // null = 整个主分组 / 全部

        /// <summary>当前选中的主分组 ID（null = 全部）。</summary>
        public string ActivePrimary => _activePrimary;
        /// <summary>当前选中的副分组 ID（null = 整个主分组 / 全部）。</summary>
        public string ActiveSub => _activeSub;

        /// <summary>用一批蓝图构建分组树，重置选中为「全部」并刷新按钮。</summary>
        /// <param name="blueprintList"></param>
        /// <param name="autoApply">是否立即触发一次 <see cref="OnGroupChanged"/> 通知默认选中（全部）。</param>
        public void SetBlueprints(List<CraftingBlueprint> blueprintList, bool autoApply = true)
        {
            BuildTree(blueprintList);
            _activePrimary = null;
            _activeSub     = null;
            BuildButtons();   // 完整重建：仅在切换蓝图模板（即调用本方法）时进行
            if (autoApply) OnGroupChanged?.Invoke(null, null);
        }

        private void BuildTree(List<CraftingBlueprint> blueprints)
        {
            _mains.Clear();
            if (blueprints == null) return;

            var primarySet = new HashSet<string>();
            var subsByMain = new Dictionary<string, HashSet<string>>();
            foreach (var bp in blueprints)
            {
                if (bp == null || string.IsNullOrEmpty(bp.primaryGroupTag)) continue;
                primarySet.Add(bp.primaryGroupTag);
                if (!subsByMain.TryGetValue(bp.primaryGroupTag, out var set))
                    subsByMain[bp.primaryGroupTag] = set = new HashSet<string>();
                if (bp.secondaryGroupTags != null)
                    foreach (var s in bp.secondaryGroupTags)
                        if (!string.IsNullOrEmpty(s)) set.Add(s);
            }

            // 按「分组标签」定义顺序排列主 / 副分组
            var order = GroupTagOrder();
            foreach (var id in order)
            {
                if (!primarySet.Contains(id)) continue;
                var m = new Main { ID = id };
                var subSet = subsByMain[id];
                foreach (var sid in order)
                    if (subSet.Contains(sid)) m.Subs.Add(sid);
                _mains.Add(m);
            }
        }

        private static List<string> GroupTagOrder()
        {
            var list = new List<string>();
            var dm = InventoryDataManager.Instance;
            if (dm != null)
                foreach (var db in dm.Databases)
                    foreach (var g in db.CraftingGroupTags)
                        if (!string.IsNullOrEmpty(g.id) && !list.Contains(g.id)) list.Add(g.id);
            return list;
        }

        // 完整重建分组列表：销毁旧按钮，按 _mains 生成「全部」+ 各主分组标题 +（其下「全部」与副分组）。
        // 子项按当前展开状态显示 / 隐藏。仅在切换蓝图模板（SetBlueprints）时调用。
        private void BuildButtons()
        {
            Clear();
            if (!uiwFoldTabPrefab || !container) return;

            bool useIcon = expandedIcon || collapsedIcon;

            // 顶部「全部」：跨所有主分组显示全部条目
            var allTab = CreateTab(allLabel, SelectAll);
            _rows.Add(new Row { Tab = allTab, Kind = RowKind.All });

            foreach (var m in _mains)
            {
                var main = m;
                m.ChildTabs.Clear();

                // 主分组标题：图标模式只放组名（折叠状态用左侧 Image 表示）；否则用文本前缀、隐藏图标
                string headerText = useIcon
                    ? ResolveGroupName(m.ID)
                    : (m.Expanded ? expandedPrefix : collapsedPrefix) + ResolveGroupName(m.ID);
                m.HeaderTab = CreateTab(headerText, () => ToggleMain(main));
                if (useIcon) m.HeaderTab.SetIcon(m.Expanded ? expandedIcon : collapsedIcon);
                _rows.Add(new Row { Tab = m.HeaderTab, Kind = RowKind.MainHeader, Primary = m.ID });

                // 「全部」子项：该主分组全部条目（副分组 = null）
                var allSub = CreateTab(subIndentText + allLabel, () => SelectMainAll(main));
                m.ChildTabs.Add(allSub);
                _rows.Add(new Row { Tab = allSub, Kind = RowKind.MainAll, Primary = m.ID });

                // 各副分组子项
                foreach (var sid in m.Subs)
                {
                    var subId  = sid;
                    var subTab = CreateTab(subIndentText + ResolveGroupName(sid), () => SelectSub(main, subId));
                    m.ChildTabs.Add(subTab);
                    _rows.Add(new Row { Tab = subTab, Kind = RowKind.Sub, Primary = m.ID, Sub = sid });
                }

                // 初始按展开状态显示 / 隐藏子项
                foreach (var cb in m.ChildTabs)
                    if (cb) cb.gameObject.SetActive(m.Expanded);
            }

            RefreshHighlights();
        }

        // 实例化一个分组页签并设置文本与点击回调（图标默认隐藏；高亮由 RefreshHighlights 统一处理）。
        private UiwFoldTab CreateTab(string label, Action onClick)
        {
            var tab = Instantiate(uiwFoldTabPrefab, container);
            tab.SetLabel(label);
            tab.AddClickListener(onClick);
            return tab;
        }

        // 仅刷新所有已建页签的选中高亮（不重建、不增删）。
        private void RefreshHighlights()
        {
            foreach (var r in _rows)
            {
                if (!r.Tab) continue;
                r.Tab.SetNormalColor(IsActive(r) ? activeColor : inactiveColor);
            }
        }

        private bool IsActive(Row r)
        {
            switch (r.Kind)
            {
                case RowKind.All:        return _activePrimary == null;
                case RowKind.MainHeader: return _activePrimary == r.Primary;   // 该主分组为当前上下文即高亮
                case RowKind.MainAll:    return _activePrimary == r.Primary && _activeSub == null;
                case RowKind.Sub:        return _activePrimary == r.Primary && _activeSub == r.Sub;
                default:                 return false;
            }
        }

        private void Clear()
        {
            foreach (var r in _rows)
                if (r.Tab) Destroy(r.Tab.gameObject);
            _rows.Clear();
        }

        private void SelectAll()
        {
            _activePrimary = null;
            _activeSub     = null;
            RefreshHighlights();   // 仅刷新高亮，不重建
            OnGroupChanged?.Invoke(null, null);
        }

        // 主分组标题：仅展开 / 折叠 —— 显示 / 隐藏其子项并切换折叠图标，不重建、不改变筛选。
        private void ToggleMain(Main m)
        {
            m.Expanded = !m.Expanded;

            foreach (var cb in m.ChildTabs)
                if (cb) cb.gameObject.SetActive(m.Expanded);

            if (expandedIcon || collapsedIcon)
                m.HeaderTab.SetIcon(m.Expanded ? expandedIcon : collapsedIcon);
            else
                m.HeaderTab.SetLabel((m.Expanded ? expandedPrefix : collapsedPrefix) + ResolveGroupName(m.ID));
        }

        // 主分组下「全部」项：选中整个主分组（副分组 = null）。子项此时已展开可见，仅刷新高亮。
        private void SelectMainAll(Main m)
        {
            _activePrimary = m.ID;
            _activeSub     = null;
            RefreshHighlights();
            OnGroupChanged?.Invoke(m.ID, null);
        }

        private void SelectSub(Main m, string subId)
        {
            _activePrimary = m.ID;
            _activeSub     = subId;
            RefreshHighlights();
            OnGroupChanged?.Invoke(m.ID, subId);
        }

        private static string ResolveGroupName(string tagId)
        {
            var tag = InventoryDataManager.Instance?.GetCraftingGroupTag(tagId);
            return tag != null ? tag.ResolveDisplayName() : tagId;
        }
    }
}

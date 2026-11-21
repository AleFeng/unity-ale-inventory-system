using Ale.Toolkit.Runtime;
using Ale.Toolkit.Runtime.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 通用多维过滤（MonoBehaviour）：按分组标签（主分组 <c>primaryGroupTag</c> + 副分组 <c>secondaryGroupTags</c>）做多维正交过滤。
    /// 维度数、每一维比对哪个字段、单选还是多选、收哪一族标签，全部由调用方配置；本组件不认具体数据类型（蓝图、商品……），
    /// 只回答「某条目的标签能否通过当前筛选」（<see cref="Matches"/>）。
    /// <para>过滤逻辑：每一维「选中集合为空 = 不限；否则条目在该维的标签与选中集合有交集」，各维之间取与。
    /// 单选维度至多选一个标签（「全部」页签 = 不限）；多选维度点一下加入、再点移除，「清空」页签清空本维。</para>
    /// <para>与两级单选树 <see cref="UiwCraftingGroupFilter"/> 并存；<see cref="UiwCraftingView"/> 配了本组件时用它代替那棵树。</para>
    /// </summary>
    public class UiwGroupTagMultiFilter : MonoBehaviour
    {
        /// <summary>维度比对条目的哪个标签字段。</summary>
        public enum TagField
        {
            /// <summary>主分组（单个标签）。</summary>
            Primary,
            /// <summary>副分组（多个标签）。</summary>
            Secondary,
        }

        /// <summary>一个过滤维度。</summary>
        [Serializable]
        public class Dimension
        {
            [Tooltip("维度名（仅用于 Inspector 识别）。")]
            public string name;
            [Tooltip("比对条目的哪个标签字段：主分组（单个）或副分组（多个）。")]
            public TagField field = TagField.Secondary;
            [Tooltip("多选：可同时选多个标签，选中集合为空 = 不限。单选：至多一个，「全部」= 不限。")]
            public bool multiSelect;
            [Tooltip("本维收哪些标签：按 ID 前缀从 SetTags 给的标签里筛（如 tier_）。tagIds 非空时以 tagIds 为准。")]
            public string tagPrefix;
            [Tooltip("本维的标签 ID（按此顺序显示）。为空时按 tagPrefix 筛。")]
            public List<string> tagIds = new List<string>();
            [Tooltip("本维页签的父容器。未配置则本维不生成页签（仍可经代码选择）。")]
            public Transform container;
        }

        [Tooltip("过滤维度（按顺序显示与比对）。")]
        public List<Dimension> dimensions = new List<Dimension>();
        [Tooltip("页签预制体（需带 Button 组件）。")]
        public UiwTabButton tabPrefab;
        [Tooltip("单选维度「不限」页签的文字。")]
        public string allLabel = "全部";
        [Tooltip("多选维度「清空」页签的文字。")]
        public string clearLabel = "清空";

        /// <summary>任一维度的选中变化后触发（调用方据此重新过滤）。</summary>
        public event Action OnFilterChanged;

        private readonly List<List<string>>        _dimTags  = new List<List<string>>();
        private readonly List<HashSet<string>>     _selected = new List<HashSet<string>>();
        private readonly List<List<UiwTabButton>>  _tabs     = new List<List<UiwTabButton>>();
        private readonly Dictionary<string, string> _names   = new Dictionary<string, string>();

        /// <summary>某一维当前选中的标签 ID。</summary>
        public IReadOnlyCollection<string> GetSelected(int dim) => _selected[dim];

        /// <summary>某一维收的标签 ID（显示顺序）。</summary>
        public IReadOnlyList<string> GetDimensionTags(int dim) => _dimTags[dim];

        /// <summary>
        /// 用一组分组标签（ID + 显示名）确定各维收的标签并重建页签，所有维度重置为不限。不触发 <see cref="OnFilterChanged"/>。
        /// </summary>
        public void SetTags(IEnumerable<GroupTag> tags)
        {
            var all = new List<GroupTag>();
            if (tags != null)
                foreach (var t in tags)
                    if (t != null && !string.IsNullOrEmpty(t.id)) all.Add(t);

            _names.Clear();
            foreach (var t in all) _names[t.id] = t.ResolveDisplayName();

            _dimTags.Clear();
            _selected.Clear();
            foreach (var d in dimensions)
            {
                var ids = new List<string>();
                if (d.tagIds != null && d.tagIds.Count > 0) ids.AddRange(d.tagIds);
                else if (!string.IsNullOrEmpty(d.tagPrefix))
                    foreach (var t in all)
                        if (t.id.StartsWith(d.tagPrefix, StringComparison.Ordinal)) ids.Add(t.id);
                _dimTags.Add(ids);
                _selected.Add(new HashSet<string>());
            }
            RebuildTabs();
        }

        /// <summary>条目的标签能否通过当前筛选（各维取与；选中集合为空的维度不限）。</summary>
        public bool Matches(string primaryTag, IList<string> secondaryTags)
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                var sel = _selected[i];
                if (sel.Count == 0) continue;
                bool hit = dimensions[i].field == TagField.Primary
                    ? primaryTag != null && sel.Contains(primaryTag)
                    : Overlaps(sel, secondaryTags);
                if (!hit) return false;
            }
            return true;
        }

        /// <summary>点选标签：单选维度换成它（再点不取消）；多选维度加入或移除。不属于本维的标签忽略。</summary>
        public void Select(int dim, string tagId)
        {
            if (dim < 0 || dim >= _selected.Count || !_dimTags[dim].Contains(tagId)) return;
            var sel = _selected[dim];
            if (dimensions[dim].multiSelect)
            {
                if (!sel.Remove(tagId)) sel.Add(tagId);
            }
            else
            {
                if (sel.Count == 1 && sel.Contains(tagId)) return;
                sel.Clear();
                sel.Add(tagId);
            }
            Changed(dim);
        }

        /// <summary>本维不限（单选的「全部」、多选的「清空」）。</summary>
        public void Clear(int dim)
        {
            if (dim < 0 || dim >= _selected.Count || _selected[dim].Count == 0) return;
            _selected[dim].Clear();
            Changed(dim);
        }

        private void Changed(int dim)
        {
            RefreshTabs(dim);
            OnFilterChanged?.Invoke();
        }

        private static bool Overlaps(HashSet<string> selected, IList<string> tags)
        {
            if (tags == null) return false;
            foreach (var t in tags)
                if (selected.Contains(t)) return true;
            return false;
        }

        #region 页签

        private void RebuildTabs()
        {
            foreach (var list in _tabs)
                foreach (var tab in list)
                    if (tab)
                    {
                        if (Application.isPlaying) Destroy(tab.gameObject);
                        else DestroyImmediate(tab.gameObject);
                    }
            _tabs.Clear();

            for (int i = 0; i < dimensions.Count; i++)
            {
                var list = new List<UiwTabButton>();
                _tabs.Add(list);
                var d = dimensions[i];
                if (!tabPrefab || !d.container) continue;

                int dim = i;
                list.Add(NewTab(d.container, null, () => Clear(dim)));   // 首个页签：单选「全部」/ 多选「清空」
                foreach (var id in _dimTags[i])
                {
                    var tagId = id;
                    list.Add(NewTab(d.container, tagId, () => Select(dim, tagId)));
                }
                RefreshTabs(i);
            }
        }

        private UiwTabButton NewTab(Transform parent, string tagId, UnityAction onClick)
        {
            var tab = Instantiate(tabPrefab, parent);
            tab.SetData(tagId, null, false);
            var button = tab.GetComponent<Button>();
            if (button) button.onClick.AddListener(onClick);
            return tab;
        }

        private void RefreshTabs(int dim)
        {
            if (dim >= _tabs.Count) return;
            var list = _tabs[dim];
            var sel  = _selected[dim];
            bool multi = dimensions[dim].multiSelect;
            for (int k = 0; k < list.Count; k++)
            {
                var tab = list[k];
                if (!tab) continue;
                if (k == 0) tab.SetData(null, multi ? clearLabel : allLabel, !multi && sel.Count == 0);
                else        tab.SetData(tab.InventoryId, Name(tab.InventoryId), sel.Contains(tab.InventoryId));
            }
        }

        private string Name(string tagId) => _names.TryGetValue(tagId, out var n) && !string.IsNullOrEmpty(n) ? n : tagId;

        #endregion
    }
}

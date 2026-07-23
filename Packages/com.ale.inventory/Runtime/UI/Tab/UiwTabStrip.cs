using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 页签条（纯 C# 辅助类，非 MonoBehaviour）。管理「一排页签实例 + 与之平行的取值 / 显示名 + 单选高亮」，
    /// 由仓库页签（<see cref="UiwInventoryView"/>）、商店商品组页签（<see cref="UiwShopViewBase"/>）、
    /// 蓝图模板页签（<see cref="UiwCraftingView"/>）与过滤页签栏（<see cref="UiwFilterTabBar"/>）共用 ——
    /// 这四处此前各写了一遍「销毁旧的 → 逐个 Instantiate → 挂 onClick(idx) → 切换时整排重刷高亮」。
    ///
    /// <para><b>差异复用：</b><see cref="SetTabs"/> 不再无条件销毁重建 —— 数量不变时原地复用已有实例，
    /// 只按新的取值 / 显示名重新绑定；多则销毁尾部、少则补建。页签实例的下标始终等于它代表的条目下标，
    /// 因此补建时挂的 onClick 闭包在后续复用中一直有效，无需反复增删监听。</para>
    ///
    /// <para><b>与具体页签组件解耦：</b>绑定与点击都以委托传入，故对页签组件没有接口要求 ——
    /// <see cref="UiwInventoryTab"/> / <see cref="UiwShopGroupTab"/> 的 <c>SetData</c>、
    /// 乃至裸 <see cref="Button"/> 的「改文本 + 改 normalColor」都能接。</para>
    /// </summary>
    /// <typeparam name="TTab">页签组件类型。</typeparam>
    /// <typeparam name="TValue">每个页签代表的取值（可为 null，例如「全部」页签）。</typeparam>
    public sealed class UiwTabStrip<TTab, TValue> where TTab : Component
    {
        private readonly List<TTab>   _tabs   = new List<TTab>();
        private readonly List<TValue> _values = new List<TValue>();
        private readonly List<string> _labels = new List<string>();

        private TTab      _prefab;
        private Transform _container;

        // (页签实例, 取值, 显示名, 是否选中) → 写入页签显示
        private Action<TTab, TValue, string, bool> _bind;
        // (下标, 取值) → 宿主响应切换
        private Action<int, TValue> _onSelect;

        private int _selectedIndex = -1;

        #region 查询

        /// <summary>当前页签数量。</summary>
        public int Count => _tabs.Count;

        /// <summary>当前选中下标；无选中为 -1。</summary>
        public int SelectedIndex => _selectedIndex;

        /// <summary>当前选中页签的取值；无选中返回 default。</summary>
        public TValue SelectedValue
            => _selectedIndex >= 0 && _selectedIndex < _values.Count ? _values[_selectedIndex] : default;

        /// <summary>按下标取页签实例；越界返回 null。</summary>
        public TTab TabAt(int index) => index >= 0 && index < _tabs.Count ? _tabs[index] : null;

        /// <summary>按下标取页签取值；越界返回 default。</summary>
        public TValue ValueAt(int index) => index >= 0 && index < _values.Count ? _values[index] : default;

        #endregion

        #region 配置

        /// <summary>
        /// 绑定预制体、容器与两个回调。可重复调用（例如宿主重新 Open 时），已建实例不受影响。
        /// </summary>
        /// <param name="prefab">页签预制体。</param>
        /// <param name="container">页签父容器。</param>
        /// <param name="bind">把 (取值, 显示名, 是否选中) 写入页签实例。</param>
        /// <param name="onSelect">页签被选中后的回调（高亮已刷新完毕才调用）。</param>
        public void Configure(TTab prefab, Transform container,
            Action<TTab, TValue, string, bool> bind, Action<int, TValue> onSelect)
        {
            _prefab    = prefab;
            _container = container;
            _bind      = bind;
            _onSelect  = onSelect;
        }

        #endregion

        #region 重建与选中

        /// <summary>
        /// 按新的取值 / 显示名重建页签条（数量不变时原地复用实例）。
        /// </summary>
        /// <param name="values">各页签的取值，顺序即显示顺序。</param>
        /// <param name="labels">与 <paramref name="values"/> 平行的显示名；可为 null / 长度不足（缺的按 null 处理）。</param>
        /// <param name="selectedIndex">重建后的选中下标；越界时钳到 [0, Count-1]，空条为 -1。</param>
        /// <param name="notify">是否为该选中项触发一次 <c>onSelect</c>。</param>
        public void SetTabs(IReadOnlyList<TValue> values, IReadOnlyList<string> labels,
            int selectedIndex = 0, bool notify = true)
        {
            if (!_prefab || !_container) { Clear(); return; }

            int want = values?.Count ?? 0;

            // 多则销毁尾部
            for (int i = _tabs.Count - 1; i >= want; i--)
            {
                if (_tabs[i]) UnityEngine.Object.Destroy(_tabs[i].gameObject);
                _tabs.RemoveAt(i);
            }

            // 少则补建（下标 == 条目下标，故 onClick 闭包一次挂好即长期有效）
            for (int i = _tabs.Count; i < want; i++)
            {
                var tab = UnityEngine.Object.Instantiate(_prefab, _container);
                int idx = i;
                var btn = tab.GetComponent<Button>();
                if (btn) btn.onClick.AddListener(() => Select(idx));
                _tabs.Add(tab);
            }

            _values.Clear();
            _labels.Clear();
            for (int i = 0; i < want; i++)
            {
                _values.Add(values[i]);
                _labels.Add(labels != null && i < labels.Count ? labels[i] : null);
            }

            _selectedIndex = want == 0 ? -1 : Mathf.Clamp(selectedIndex, 0, want - 1);
            RefreshTabs();
            if (notify && _selectedIndex >= 0) _onSelect?.Invoke(_selectedIndex, _values[_selectedIndex]);
        }

        /// <summary>选中指定下标：刷新整排高亮后触发 <c>onSelect</c>。越界返回 false 且不做任何事。</summary>
        public bool Select(int index, bool notify = true)
        {
            if (index < 0 || index >= _tabs.Count) return false;
            _selectedIndex = index;
            RefreshTabs();
            if (notify) _onSelect?.Invoke(index, _values[index]);
            return true;
        }

        /// <summary>按取值选中（用 <see cref="EqualityComparer{T}.Default"/> 比较）。未找到返回 false。</summary>
        public bool SelectValue(TValue value, bool notify = true)
        {
            var cmp = EqualityComparer<TValue>.Default;
            for (int i = 0; i < _values.Count; i++)
                if (cmp.Equals(_values[i], value)) return Select(i, notify);
            return false;
        }

        /// <summary>用当前取值 / 显示名 / 选中态重刷全部页签显示（不增删实例）。</summary>
        public void RefreshTabs()
        {
            if (_bind == null) return;
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (!_tabs[i]) continue;
                _bind(_tabs[i], _values[i], _labels[i], i == _selectedIndex);
            }
        }

        /// <summary>销毁所有页签实例并清空取值。</summary>
        public void Clear()
        {
            foreach (var t in _tabs)
                if (t) UnityEngine.Object.Destroy(t.gameObject);
            _tabs.Clear();
            _values.Clear();
            _labels.Clear();
            _selectedIndex = -1;
        }

        #endregion
    }
}

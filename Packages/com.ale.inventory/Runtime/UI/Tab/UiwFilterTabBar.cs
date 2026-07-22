#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 过滤页签栏（MonoBehaviour）。以功能标签按钮形式呈现一组过滤项（可选首位「全部」），
    /// 管理选中高亮，并通过 <see cref="OnFilterChanged"/> 通知宿主。与具体系统解耦：
    /// 宿主传入标签名列表、订阅事件后自行刷新内容。供 <see cref="UiwInventoryView"/> 等共用。
    /// </summary>
    public class UiwFilterTabBar : MonoBehaviour
    {
        [Header("过滤页签")]
        [Tooltip("过滤按钮父容器。")]
        public Transform filterContainer;
        [Tooltip("过滤按钮 Prefab（含 Text/TMP_Text 子节点显示标签名）。")]
        public Button    filterButtonPrefab;
        [Tooltip("「全部」按钮显示名。")]
        public string    allLabel = "全部";
        [Tooltip("选中时按钮 normalColor。")]
        public Color     activeColor   = new Color(1.00f, 0.85f, 0.30f, 1.00f);
        [Tooltip("未选中时按钮 normalColor。")]
        public Color     inactiveColor = new Color(1.00f, 1.00f, 1.00f, 1.00f);

        /// <summary>过滤变化事件。参数为标签名，<c>null</c> = 全部。</summary>
        public event Action<string> OnFilterChanged;

        private readonly List<Button> _buttons   = new List<Button>();
        private readonly List<string> _tagValues = new List<string>(); // 与 _buttons 平行；null = 全部
        private string _activeFilter;

        /// <summary>当前激活的过滤标签（null = 全部）。</summary>
        public string ActiveFilter => _activeFilter;

        /// <summary>
        /// 重建过滤按钮：可选「全部」首位 + 各标签。
        /// 默认选中：显示「全部」时选「全部」(null)；否则选第一个标签（无标签时回退 null = 不过滤）。
        /// 并触发一次 <see cref="OnFilterChanged"/> 通知宿主默认选中项。
        /// </summary>
        /// <param name="tagNames">过滤标签名列表。</param>
        /// <param name="showAll">是否显示「全部」页签。false = 不显示，默认选中第一个标签。</param>
        /// <param name="autoApply">是否在重建后立即按默认选中项触发一次 <see cref="OnFilterChanged"/>。</param>
        public void SetFilters(IReadOnlyList<string> tagNames, bool showAll = true, bool autoApply = true)
        {
            Clear();
            if (!filterButtonPrefab || !filterContainer) return;

            if (showAll) AddButton(allLabel, null);
            if (tagNames != null)
                foreach (var tag in tagNames)
                    AddButton(tag, tag);

            // 默认选中项：有「全部」→ null（全部）；否则取第一个标签；都没有 → null（不过滤）。
            string defaultFilter = showAll
                ? null
                : (_tagValues.Count > 0 ? _tagValues[0] : null);

            _activeFilter = defaultFilter;
            UpdateHighlights();
            if (autoApply) ApplyFilter(defaultFilter, true);
        }

        /// <summary>销毁所有过滤按钮。</summary>
        public void Clear()
        {
            foreach (var btn in _buttons)
                if (btn) Destroy(btn.gameObject);
            _buttons.Clear();
            _tagValues.Clear();
            _activeFilter = null;
        }

        private void AddButton(string display, string tagName)
        {
            var btn = Instantiate(filterButtonPrefab, filterContainer);
            var txt = btn.GetComponentInChildren<InventoryText>();
            if (txt) txt.text = display;

            string captured = tagName;
            btn.onClick.AddListener(() => ApplyFilter(captured));
            _buttons.Add(btn);
            _tagValues.Add(tagName);
        }

        private void ApplyFilter(string tagName, bool forceSelectBtn = false)
        {
            _activeFilter = tagName;
            var selected = UpdateHighlights();
            OnFilterChanged?.Invoke(tagName);
            if (forceSelectBtn) selected?.Select();
        }

        // 按 _activeFilter 更新所有按钮 normalColor，返回选中的按钮。
        private Button UpdateHighlights()
        {
            Button selected = null;
            for (int i = 0; i < _buttons.Count; i++)
            {
                var btn = _buttons[i];
                if (!btn) continue;
                bool active = _tagValues[i] == _activeFilter;
                var colors = btn.colors;
                colors.normalColor = active ? activeColor : inactiveColor;
                btn.colors = colors;
                if (active) selected = btn;
            }
            return selected;
        }
    }
}

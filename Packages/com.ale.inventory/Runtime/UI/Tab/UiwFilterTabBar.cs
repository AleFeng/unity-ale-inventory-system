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

        // 页签实例 / 取值 / 显示名 / 高亮由 UiwTabStrip 统一维护（与仓库 / 商店 / 蓝图三处页签同源）。
        private readonly UiwTabStrip<Button, string> _tabStrip = new UiwTabStrip<Button, string>();

        private readonly List<string> _tagValues = new List<string>();   // null = 全部
        private readonly List<string> _tagLabels = new List<string>();   // 与 _tagValues 平行

        /// <summary>当前激活的过滤标签（null = 全部）。</summary>
        public string ActiveFilter => _tabStrip.SelectedValue;

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
            _tagValues.Clear();
            _tagLabels.Clear();

            if (showAll) { _tagValues.Add(null); _tagLabels.Add(allLabel); }
            if (tagNames != null)
                foreach (var tag in tagNames)
                {
                    _tagValues.Add(tag);
                    _tagLabels.Add(tag);
                }

            _tabStrip.Configure(filterButtonPrefab, filterContainer, BindButton,
                (_, tagName) => OnFilterChanged?.Invoke(tagName));

            // 默认选中首项：有「全部」→ 首项即「全部」(null)；否则为第一个标签。
            _tabStrip.SetTabs(_tagValues, _tagLabels, selectedIndex: 0, notify: autoApply);

            if (!autoApply) return;

            // 无任何页签（不显示「全部」且标签列表为空 / 未配置预制体）：页签条不会回调，
            // 此处补发一次「不过滤」通知，与原实现一致——否则宿主收不到初始过滤状态。
            if (_tabStrip.Count == 0) { OnFilterChanged?.Invoke(null); return; }

            // 重建后把焦点放到默认选中的按钮上。
            _tabStrip.TabAt(_tabStrip.SelectedIndex)?.Select();
        }

        /// <summary>销毁所有过滤按钮。</summary>
        public void Clear()
        {
            _tabStrip.Clear();
            _tagValues.Clear();
            _tagLabels.Clear();
        }

        // 写入按钮文本与选中高亮（normalColor）。
        private void BindButton(Button btn, string tagName, string display, bool selected)
        {
            var txt = btn.GetComponentInChildren<InventoryText>();
            if (txt) txt.text = display;

            var colors = btn.colors;
            colors.normalColor = selected ? activeColor : inactiveColor;
            btn.colors = colors;
        }
    }
}

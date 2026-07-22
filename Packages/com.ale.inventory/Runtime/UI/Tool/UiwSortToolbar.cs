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
    /// 排序整理栏（MonoBehaviour）。封装 排序条件下拉 + 升降序切换 + 自动整理按钮 三个控件，
    /// 通过 <see cref="OnSortChanged"/> / <see cref="OnAutoSort"/> 通知宿主。与具体系统解耦：
    /// 宿主传入排序项显示名、订阅事件后把下标映射回字段并执行排序。供 <see cref="UiwInventoryView"/> 等共用。
    /// </summary>
    public class UiwSortToolbar : MonoBehaviour
    {
        [Header("排序整理")]
        [Tooltip("排序条件下拉框。")]
        public Dropdown      sortDropdown;
        [Tooltip("升序/降序切换按钮。")]
        public Button        sortDirectionButton;
        [Tooltip("升序/降序按钮上的文本组件。")]
        public InventoryText sortDirectionLabel;
        [Tooltip("自动整理按钮。")]
        public Button        autoSortButton;
        [Tooltip("升序显示文本。")]
        public string        ascText  = "升序";
        [Tooltip("降序显示文本。")]
        public string        descText = "降序";

        /// <summary>排序条件 / 方向变化事件。参数为（下拉下标, 是否升序）。</summary>
        public event Action<int, bool> OnSortChanged;
        /// <summary>自动整理按钮点击事件。</summary>
        public event Action            OnAutoSort;

        private int  _index;
        private bool _ascending; // 默认降序（与原仓库 UI 行为一致）

        /// <summary>当前选中的排序条件下标。</summary>
        public int  SortIndex => _index;
        /// <summary>当前是否升序。</summary>
        public bool Ascending => _ascending;

        private void Awake()
        {
            if (sortDropdown)        sortDropdown.onValueChanged.AddListener(HandleDropdown);
            if (sortDirectionButton) sortDirectionButton.onClick.AddListener(HandleToggleDirection);
            if (autoSortButton)      autoSortButton.onClick.AddListener(() => OnAutoSort?.Invoke());
        }

        /// <summary>
        /// 设置排序条件下拉项（显示名）。重置选中为第 0 项；无项时隐藏下拉与升降序按钮。
        /// </summary>
        public void SetOptions(IReadOnlyList<string> displayNames)
        {
            bool has = displayNames != null && displayNames.Count > 0;

            if (sortDropdown)
            {
                sortDropdown.ClearOptions();
                sortDropdown.gameObject.SetActive(has);
            }
            if (sortDirectionButton) sortDirectionButton.gameObject.SetActive(has);
            if (!has) return;

            var options = new List<Dropdown.OptionData>(displayNames.Count);
            foreach (var n in displayNames)
                options.Add(new Dropdown.OptionData(n));
            sortDropdown.AddOptions(options);

            _index = 0;
            sortDropdown.value = _index;     // 与原 BuildSortDropdown 行为一致（可能触发一次排序）
            UpdateDirectionLabel();
        }

        /// <summary>
        /// 用一组排序条件（<see cref="SortPriority"/>）填充下拉项：显示名取自对应「整理选项」的内置
        /// <see cref="SortOption.displayName"/>（<see cref="SortOption.ResolveDisplayName"/>，无则用字段名本身）。
        /// 是 <see cref="SetOptions"/> 的便捷封装，把「排序条件 → 显示名」的解析收拢在本组件内。
        /// </summary>
        public void SetSortPriorities(IReadOnlyList<SortPriority> priorities, InventoryDatabase db)
        {
            if (priorities == null || priorities.Count == 0)
            {
                SetOptions(null);
                return;
            }

            var names = new List<string>(priorities.Count);
            foreach (var sp in priorities)
            {
                string displayName = sp.field;
                if (db)
                {
                    string n = db.GetSortOption(sp.field)?.ResolveDisplayName(null);
                    if (!string.IsNullOrEmpty(n)) displayName = n;
                }
                names.Add(displayName);
            }
            SetOptions(names);
        }

        private void HandleDropdown(int index)
        {
            _index = index;
            OnSortChanged?.Invoke(_index, _ascending);
        }

        private void HandleToggleDirection()
        {
            _ascending = !_ascending;
            UpdateDirectionLabel();
            OnSortChanged?.Invoke(_index, _ascending);
        }

        private void UpdateDirectionLabel()
        {
            if (sortDirectionLabel) sortDirectionLabel.text = _ascending ? ascText : descText;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 货币栏（MonoBehaviour）。与具体系统解耦的通用组件：宿主提供「货币 ID 列表」与
    /// 「按 ID 取持有量」的 getter，本组件只负责实例化货币格并刷新显示。
    /// 供 <see cref="UiwInventoryView"/>、<see cref="UiwShopViewBase"/> 等共用。
    /// </summary>
    public class UiwCurrencyBar : MonoBehaviour
    {
        [Header("货币栏")]
        [Tooltip("货币格子父容器（currencyPrefab 于此下实例化）。")]
        public Transform              currencyContainer;
        [Tooltip("货币格子 Prefab（UiwInventoryItemSimple）。")]
        public UiwInventoryItemSimple currencyPrefab;
        [Tooltip("货币道具 ID 列表（直接在本组件上配置）。可被带 ids 参数的 Setup 重载在运行时覆盖（如商店按商品价格自动收集）。")]
        public string[]               currencyItemIds;

        private readonly List<UiwInventoryItemSimple> _widgets = new List<UiwInventoryItemSimple>();
        private readonly List<string>                 _ids     = new List<string>();
        private Func<string, int>   _ownedGetter;
        private NumberFormatLocale   _numberFormat;

        /// <summary>
        /// 配置货币栏（使用本组件 Inspector 上配置的 <see cref="currencyItemIds"/>）：
        /// 持有量由 <paramref name="ownedGetter"/> 提供。会清空并重建货币格，随后刷新一次。
        /// </summary>
        public void Setup(Func<string, int> ownedGetter, NumberFormatLocale fmt)
            => Setup(currencyItemIds, ownedGetter, fmt);

        /// <summary>
        /// 配置货币栏：按 <paramref name="currencyIds"/> 建货币格（为 null 时退回本组件的
        /// <see cref="currencyItemIds"/>），持有量由 <paramref name="ownedGetter"/> 提供。
        /// 会清空并重建已有货币格，随后刷新一次。
        /// </summary>
        public void Setup(IReadOnlyList<string> currencyIds, Func<string, int> ownedGetter, NumberFormatLocale fmt)
        {
            _ownedGetter  = ownedGetter;
            _numberFormat = fmt;

            var ids = currencyIds ?? currencyItemIds;

            Clear();
            if (!currencyPrefab || !currencyContainer || ids == null) return;

            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id)) continue;
                var w = Instantiate(currencyPrefab, currencyContainer);
                w.numberFormat = _numberFormat;
                _widgets.Add(w);
                _ids.Add(id);
            }
            Refresh();
        }

        /// <summary>更新数字格式并刷新显示。</summary>
        public void SetNumberFormat(NumberFormatLocale fmt)
        {
            _numberFormat = fmt;
            foreach (var w in _widgets)
                if (w) w.numberFormat = fmt;
            Refresh();
        }

        /// <summary>按 getter 重新读取各货币持有量并刷新显示。</summary>
        public void Refresh()
        {
            if (_ownedGetter == null) return;
            for (int i = 0; i < _widgets.Count && i < _ids.Count; i++)
                if (_widgets[i]) _widgets[i].SetItem(_ids[i], _ownedGetter(_ids[i]));
        }

        /// <summary>销毁所有货币格。</summary>
        public void Clear()
        {
            foreach (var w in _widgets)
                if (w) Destroy(w.gameObject);
            _widgets.Clear();
            _ids.Clear();
        }
    }
}

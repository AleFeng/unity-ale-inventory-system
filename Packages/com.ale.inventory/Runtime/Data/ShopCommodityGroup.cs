using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 商品组。商店中以页签形式分组显示的一组商品，另有「全部」页签聚合显示。
    /// 持有组级刷新计划，商品可选择覆盖。
    /// </summary>
    [Serializable]
    public class ShopCommodityGroup
    {
        /// <summary>商品组名称（UI 页签显示）。</summary>
        public string name;

        /// <summary>商品组描述。</summary>
        public string description;

        /// <summary>组级刷新计划（商品未覆盖时生效）。</summary>
        public ShopRefreshSchedule refresh = new ShopRefreshSchedule();

        /// <summary>组内商品列表。</summary>
        public List<ShopCommodity> commodities = new List<ShopCommodity>();

        public ShopCommodityGroup Clone()
        {
            var clone = new ShopCommodityGroup
            {
                name        = name,
                description = description,
                refresh     = refresh?.Clone() ?? new ShopRefreshSchedule(),
            };
            foreach (var c in commodities)
                clone.commodities.Add(c.Clone());
            return clone;
        }
    }
}

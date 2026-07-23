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
        /// <summary>
        /// 稳定唯一键，作为每玩家交易进度的存档 Key（见 <see cref="ShopCommodityProgress.groupKey"/>）。
        /// 创建时分配、此后不变——组改名或被拖拽重排都不会让老存档的交易次数错位。
        /// <para>1.4.0 及更早的数据没有此字段，打开配置编辑器时由
        /// <see cref="InventoryDatabase.EnsureShopEntryGuids"/> 自动补发一次；
        /// 补发之前运行时回退到旧的「组名 / #组索引」键，行为与旧版完全一致。</para>
        /// </summary>
        public string guid;

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
                // guid 随拷贝保留：模板派生出的商店各有独立 shopId，交易进度按 shopId 分桶，不会互相串。
                guid        = guid,
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

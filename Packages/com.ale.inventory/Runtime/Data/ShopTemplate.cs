using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 商店模板。定义自定义属性字段 + 一整套商店可配置项的默认值，作为创建新商店的蓝本。
    /// 与 <see cref="Shop"/> 共享 <see cref="IShopConfig"/>，使两者的配置项一致且编辑器复用同一套绘制。
    /// </summary>
    [Serializable]
    public class ShopTemplate : IShopConfig
    {
        /// <summary>模板名称。</summary>
        public string name;

        /// <summary>模板标识颜色（用于列表中的圆形色点，便于快速区分来源）。</summary>
        public Color color = Color.gray;

        /// <summary>模板所定义的自定义属性字段。</summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        // ── 商店可配置项（默认值，创建商店时复制）────────────────────────────────
        /// <summary>商店类型（售卖 / 回收 / 等价交换）。</summary>
        public ShopType shopType = ShopType.Sell;

        /// <summary>交易仓库：与本商店交易时使用的仓库条目 ID 列表（可多选）。</summary>
        public List<string> tradeInventoryRefs = new List<string>();

        /// <summary>交易功能标签列表（仅 Recycle 生效：只回收含其中任一标签的道具；空 = 不限制）。</summary>
        public List<string> tradeTagRefs = new List<string>();

        /// <summary>过滤标签列表（UI 中以功能标签按钮形式呈现）。</summary>
        public List<string> filterTagRefs = new List<string>();

        /// <summary>是否在 UI 页签栏显示"全部"页签（创建商店时复制此值）。</summary>
        public bool showAllFilterTab = true;

        /// <summary>引用的数字格式配置名称（对应 InventoryDatabase.NumberFormatConfigs；空 = 不使用）。</summary>
        public string numberFormatRef;

        /// <summary>价格属性来源：一个类型为 StringIntPair(货币ID→价格) 的道具属性 ID。</summary>
        public string priceAttrSource;

        /// <summary>整理列表（UI 中以下拉菜单形式呈现，玩家可选择排序条件及升降序）。</summary>
        public List<SortPriority> sortPriorities = new List<SortPriority>();

        /// <summary>整理优先级（整理列表条件值相同时，依次按此列表比较直至值不同）。</summary>
        public List<SortPriority> sortTiebreakers = new List<SortPriority>();

        /// <summary>商品组列表。</summary>
        public List<ShopCommodityGroup> groups = new List<ShopCommodityGroup>();

        // ── IShopConfig（映射到上述序列化字段，供编辑器共享绘制）────────────────────
        ShopType IShopConfig.ShopType { get => shopType; set => shopType = value; }
        List<string> IShopConfig.TradeInventoryRefs => tradeInventoryRefs;
        List<string> IShopConfig.TradeTagRefs => tradeTagRefs;
        List<string> IShopConfig.FilterTagRefs => filterTagRefs;
        bool IShopConfig.ShowAllFilterTab { get => showAllFilterTab; set => showAllFilterTab = value; }
        string IShopConfig.NumberFormatRef { get => numberFormatRef; set => numberFormatRef = value; }
        string IShopConfig.PriceAttrSource { get => priceAttrSource; set => priceAttrSource = value; }
        List<ShopCommodityGroup> IShopConfig.Groups => groups;
        List<SortPriority> IShopConfig.SortPriorities => sortPriorities;
        List<SortPriority> IShopConfig.SortTiebreakers => sortTiebreakers;

        public ShopTemplate()
        {
        }

        public ShopTemplate(string nameArg)
        {
            name = nameArg;
        }

        public ShopTemplate Clone()
        {
            var clone = new ShopTemplate(name)
            {
                color           = color,
                shopType        = shopType,
                showAllFilterTab = showAllFilterTab,
                numberFormatRef = numberFormatRef,
                priceAttrSource = priceAttrSource,
                tradeInventoryRefs = new List<string>(tradeInventoryRefs),
                tradeTagRefs       = new List<string>(tradeTagRefs),
                filterTagRefs      = new List<string>(filterTagRefs),
            };
            foreach (var attr in attributes)
                clone.attributes.Add(attr.Clone());
            foreach (var sp in sortPriorities)
                clone.sortPriorities.Add(sp.Clone());
            foreach (var sp in sortTiebreakers)
                clone.sortTiebreakers.Add(sp.Clone());
            foreach (var g in groups)
                clone.groups.Add(g.Clone());
            return clone;
        }
    }
}

using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 商店可配置项的共享契约。<see cref="Shop"/>（实例）与 <see cref="ShopTemplate"/>（模板）共同实现，
    /// 使编辑器的配置绘制（类型 / 交易仓库 / 过滤 / 数字格式 / 价格属性来源 / 商品组）可在两者间复用。
    /// </summary>
    public interface IShopConfig
    {
        /// <summary>商店类型（售卖 / 回收 / 等价交换）。</summary>
        ShopType ShopType { get; set; }

        /// <summary>交易仓库（与本商店交易时使用的玩家仓库 ID 列表）。</summary>
        List<string> TradeInventoryRefs { get; }

        /// <summary>交易功能标签列表（仅 Recycle 生效：只回收含其中任一标签的道具；空 = 不限制）。</summary>
        List<string> TradeTagRefs { get; }

        /// <summary>过滤标签列表（UI 中以标签按钮形式呈现）。</summary>
        List<string> FilterTagRefs { get; }

        /// <summary>是否在 UI 页签栏显示"全部"页签（true = 显示并默认选中；false = 隐藏，默认选中第一项）。</summary>
        bool ShowAllFilterTab { get; set; }

        /// <summary>引用的数字格式配置名称（空 = 不使用）。</summary>
        string NumberFormatRef { get; set; }

        /// <summary>价格属性来源（一个 StringIntPair 类型的道具属性 ID）。</summary>
        string PriceAttrSource { get; set; }

        /// <summary>商品组列表。</summary>
        List<ShopCommodityGroup> Groups { get; }

        /// <summary>整理列表（UI 中以下拉菜单形式呈现，玩家可选择排序条件及升降序）。</summary>
        List<SortPriority> SortPriorities { get; }

        /// <summary>整理优先级（整理列表条件值相同时，依次按此列表比较直至值不同）。</summary>
        List<SortPriority> SortTiebreakers { get; }
    }
}

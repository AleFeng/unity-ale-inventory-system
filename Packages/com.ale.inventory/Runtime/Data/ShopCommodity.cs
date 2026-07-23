using System;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 商店商品条目。关联道具系统中的一个道具，描述其交易数量、价格倍率、可交易次数与刷新覆盖。
    /// 价格本身不在此存储——由所属 <see cref="Shop.priceAttrSource"/> 指向的道具属性提供，再乘以 <see cref="priceMultiplier"/>。
    /// </summary>
    [Serializable]
    public class ShopCommodity
    {
        /// <summary>
        /// 稳定唯一键，作为每玩家交易进度的存档 Key（见 <see cref="ShopCommodityProgress.commodityKey"/>）。
        /// 创建时分配、此后不变——商品在组内被拖拽重排不会让老存档的交易次数错位。
        /// <para>1.4.0 及更早的数据没有此字段，打开配置编辑器时由
        /// <see cref="InventoryDatabase.EnsureShopEntryGuids"/> 自动补发一次；
        /// 补发之前运行时回退到旧的「组内索引:道具ID」键，行为与旧版完全一致。
        /// 运行时合成的商品（回收店未配商品组时）不分配 guid，恒走回退路径。</para>
        /// </summary>
        public string guid;

        /// <summary>关联的道具 ID（道具系统）。</summary>
        public string itemId;

        /// <summary>每次交易获得 / 回收的道具数量。</summary>
        public int count = 1;

        /// <summary>价格倍率。1 = 原价；回收常用 &lt;1（如 0.5 = 半价回收）。</summary>
        public float priceMultiplier = 1f;

        /// <summary>每个刷新周期内可交易次数。-1 = 无限；刷新周期为「不刷新」时即终身上限。</summary>
        public int tradeLimit = -1;

        /// <summary>是否覆盖所属商品组的刷新计划，使用本商品自己的 <see cref="refresh"/>。</summary>
        public bool overrideRefresh;

        /// <summary>商品级刷新计划（仅当 <see cref="overrideRefresh"/> 为 true 时生效）。</summary>
        public ShopRefreshSchedule refresh = new ShopRefreshSchedule();

        public ShopCommodity Clone() => new ShopCommodity
        {
            guid            = guid,   // 随拷贝保留（理由同 ShopCommodityGroup.Clone）
            itemId          = itemId,
            count           = count,
            priceMultiplier = priceMultiplier,
            tradeLimit      = tradeLimit,
            overrideRefresh = overrideRefresh,
            refresh         = refresh?.Clone() ?? new ShopRefreshSchedule(),
        };
    }
}

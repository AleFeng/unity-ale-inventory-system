using System;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 单个商品的每玩家交易进度。记录「当前刷新周期内已交易次数」与「上次刷新时间」。
    /// 由 <see cref="ShopRuntimeManager"/> 维护并随存档持久化。
    /// </summary>
    [Serializable]
    public class ShopCommodityProgress
    {
        /// <summary>所属商品组的稳定键（组名，空名回退为 <c>#组索引</c>）。</summary>
        public string groupKey;

        /// <summary>商品在组内的稳定键（<c>组内索引:道具ID</c>）。</summary>
        public string commodityKey;

        /// <summary>当前刷新周期内已交易次数。</summary>
        public int tradedCount;

        /// <summary>
        /// 上次刷新时间的 <see cref="DateTime.Ticks"/>（与刷新计算所用时钟同一参考系）；0 = 尚未初始化。
        /// </summary>
        public long lastRefreshTicks;

        public ShopCommodityProgress()
        {
        }

        public ShopCommodityProgress(string groupKey, string commodityKey)
        {
            this.groupKey     = groupKey;
            this.commodityKey = commodityKey;
        }

        public ShopCommodityProgress Clone() => new ShopCommodityProgress
        {
            groupKey         = groupKey,
            commodityKey     = commodityKey,
            tradedCount      = tradedCount,
            lastRefreshTicks = lastRefreshTicks,
        };
    }
}

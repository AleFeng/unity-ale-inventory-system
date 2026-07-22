using System.Collections.Generic;

namespace InventorySystem.Runtime
{
    /// <summary>交易失败 / 部分成交的原因。</summary>
    public enum ShopTradeFailReason
    {
        /// <summary>无失败（成交）。</summary>
        None,
        /// <summary>商店不存在。</summary>
        ShopNotFound,
        /// <summary>商品无效（道具不存在 / 商品为空 / 在该商店中未找到）。</summary>
        CommodityInvalid,
        /// <summary>该商店类型不支持此操作（如对非售卖店购买、等价交换未实现）。</summary>
        NotSupported,
        /// <summary>货币不足（连一次都买不起）。</summary>
        NotEnoughCurrency,
        /// <summary>本周期可交易次数已用尽。</summary>
        TradeLimitReached,
        /// <summary>交易仓库容量不足，无法容纳购入道具（或回收所得货币）。</summary>
        InventoryFull,
        /// <summary>玩家未持有可回收的道具（连一次的数量都不够）。</summary>
        ItemNotOwned,
    }

    /// <summary>
    /// 一次交易（购买 / 回收）的结果。
    /// <para><see cref="AppliedTimes"/> 可能小于请求次数：受可交易次数、货币、仓库容量等约束自动下调。</para>
    /// </summary>
    public struct ShopTradeResult
    {
        /// <summary>是否至少成交了一次。</summary>
        public bool Success;

        /// <summary>实际成交的交易次数（可能因约束被下调）。</summary>
        public int AppliedTimes;

        /// <summary>失败 / 部分成交的原因（成交时为 <see cref="ShopTradeFailReason.None"/>）。</summary>
        public ShopTradeFailReason Reason;

        /// <summary>实际发生的货币流水（货币ID → 总额；购买为支出额，回收为收入额）。</summary>
        public Dictionary<string, int> CurrencyDelta;

        public static ShopTradeResult Fail(ShopTradeFailReason reason) => new ShopTradeResult
        {
            Success      = false,
            AppliedTimes = 0,
            Reason       = reason,
        };
    }
}

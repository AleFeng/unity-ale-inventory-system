namespace Ale.Inventory.Runtime
{
    /// <summary>商店类型。决定商店的交互方式；三种类型都需配置「交易仓库」。</summary>
    public enum ShopType
    {
        /// <summary>售卖：玩家用货币购买商店商品。</summary>
        Sell,
        /// <summary>回收：玩家出售背包道具给商店换取货币。</summary>
        Recycle,
        /// <summary>等价交换：双方交易列表按总价值互换（本期仅占位，不实现逻辑/UI）。</summary>
        Barter,
    }
}

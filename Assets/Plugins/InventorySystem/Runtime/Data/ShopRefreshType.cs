namespace InventorySystem.Runtime
{
    /// <summary>商店刷新周期。决定「可交易次数」按何种周期重置。</summary>
    public enum ShopRefreshType
    {
        /// <summary>不刷新：可交易次数为终身上限。</summary>
        不刷新,
        /// <summary>每日刷新。</summary>
        每日,
        /// <summary>每周刷新。</summary>
        每周,
        /// <summary>每月刷新。</summary>
        每月,
    }
}

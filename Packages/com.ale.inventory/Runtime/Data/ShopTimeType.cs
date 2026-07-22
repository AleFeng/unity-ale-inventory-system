namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 刷新时间类型。决定刷新边界依据哪种时钟计算。
    /// 三种时钟统一由 <see cref="InventoryRuntimeManager"/> 经已注册的获取器
    /// （<see cref="InventoryRuntimeManager.RegisterTimeGetter"/>）取得；
    /// 未注册时 fallback 到系统本地时间。
    /// </summary>
    public enum ShopTimeType
    {
        /// <summary>游戏时间（由游戏层时间系统提供）。</summary>
        游戏时间,
        /// <summary>本地时间（设备系统时间）。</summary>
        本地时间,
        /// <summary>服务器时间（由游戏层网络时间提供）。</summary>
        服务器时间,
    }
}

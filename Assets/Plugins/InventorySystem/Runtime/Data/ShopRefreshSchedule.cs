using System;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 刷新计划。描述「可交易次数」按何种周期、依据哪种时钟、在何时间点重置。
    /// 商品组持有一份；商品可选「覆盖刷新」持有自己的一份覆盖组级设置。
    /// </summary>
    [Serializable]
    public class ShopRefreshSchedule
    {
        /// <summary>刷新周期。</summary>
        public ShopRefreshType refreshType = ShopRefreshType.不刷新;

        /// <summary>刷新依据的时钟类型。</summary>
        public ShopTimeType timeType = ShopTimeType.本地时间;

        /// <summary>时区 ID（IANA / Windows 时区标识；空 = 使用时钟自身的本地时区）。</summary>
        public string timeZoneId;

        /// <summary>刷新时间点 - 小时（24 小时制，0-23）。</summary>
        public int hour;

        /// <summary>刷新时间点 - 分钟（0-59）。</summary>
        public int minute;

        /// <summary>每周刷新使用：星期几（0 = 周日 … 6 = 周六）。</summary>
        public int dayOfWeek;

        /// <summary>每月刷新使用：几号（1-31；超出当月天数时取当月最后一天）。</summary>
        public int dayOfMonth = 1;

        public ShopRefreshSchedule Clone() => new ShopRefreshSchedule
        {
            refreshType = refreshType,
            timeType    = timeType,
            timeZoneId  = timeZoneId,
            hour        = hour,
            minute      = minute,
            dayOfWeek   = dayOfWeek,
            dayOfMonth  = dayOfMonth,
        };
    }
}

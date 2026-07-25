using Ale.Toolkit.Runtime;
using Ale.Inventory.Runtime;

using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 枚举下拉显示名映射。**仅在「多语言设定 → 枚举值」勾选后生效**；
    /// 未勾选时 <c>ToolkitEditorL10n.TrEnum</c> 一律返回代码中的枚举原名。
    ///
    /// <para>中文名取自各枚举自身的 <c>[InspectorName]</c> 与 XML 文档；英文沿用枚举标识符
    /// （必要处加空格），日文按包内既有日文文档术语。</para>
    /// </summary>
    internal static partial class InventoryEditorL10nTables
    {
        static partial void RegisterEnums()
        {
            // ── ShopType（商店类型）──────────────────────────────────────────────
            AddEnum(ShopType.Sell,    "Sell",     "販売",     "售卖");
            AddEnum(ShopType.Recycle, "Buy-back", "買い取り", "回收");
            AddEnum(ShopType.Barter,  "Barter",   "等価交換", "等价交换");

            // ── ShopRefreshType（刷新周期）───────────────────────────────────────
            AddEnum(ShopRefreshType.Never,   "Never",   "更新しない", "不刷新");
            AddEnum(ShopRefreshType.Daily,   "Daily",   "毎日",       "每日");
            AddEnum(ShopRefreshType.Weekly,  "Weekly",  "毎週",       "每周");
            AddEnum(ShopRefreshType.Monthly, "Monthly", "毎月",       "每月");

            // ── ShopTimeType（刷新时间类型）──────────────────────────────────────
            AddEnum(ShopTimeType.GameTime,   "Game Time",   "ゲーム時間",   "游戏时间");
            AddEnum(ShopTimeType.LocalTime,  "Local Time",  "ローカル時間", "本地时间");
            AddEnum(ShopTimeType.ServerTime, "Server Time", "サーバー時間", "服务器时间");
        }
    }
}

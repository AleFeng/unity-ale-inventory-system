using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 刷新时间类型。决定刷新边界依据哪种时钟计算。
    /// 三种时钟统一由 <see cref="InventoryRuntimeManager"/> 经已注册的获取器
    /// （<see cref="InventoryRuntimeManager.RegisterTimeGetter"/>）取得；
    /// 未注册时 fallback 到系统本地时间。
    ///
    /// <para>枚举值<b>显式写死</b>，与 1.4.0 及之前的中文标识符版本逐一对应，
    /// 因此已有 <c>.asset</c> / <c>.prefab</c> 数据无需迁移。</para>
    ///
    /// <para><c>[InspectorName]</c> 仅在经 <c>SerializedProperty</c> 绘制时生效（默认 Inspector /
    /// <c>PropertyField</c>）；配置编辑器的商店刷新面板用 <c>EditorGUILayout.EnumPopup</c> 绘制，
    /// 下拉显示的是英文标识符。</para>
    /// </summary>
    public enum ShopTimeType
    {
        /// <summary>游戏时间（由游戏层时间系统提供）。</summary>
        [InspectorName("游戏时间")]   GameTime   = 0,
        /// <summary>本地时间（设备系统时间）。</summary>
        [InspectorName("本地时间")]   LocalTime  = 1,
        /// <summary>服务器时间（由游戏层网络时间提供）。</summary>
        [InspectorName("服务器时间")] ServerTime = 2,
    }
}

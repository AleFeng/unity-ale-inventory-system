using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 商店刷新周期。决定「可交易次数」按何种周期重置。
    ///
    /// <para>枚举值<b>显式写死</b>，与 1.4.0 及之前的中文标识符版本逐一对应，
    /// 因此已有 <c>.asset</c> 数据无需迁移。</para>
    ///
    /// <para><c>[InspectorName]</c> 仅在经 <c>SerializedProperty</c> 绘制时生效（默认 Inspector /
    /// <c>PropertyField</c>）；配置编辑器的商店刷新面板用 <c>EditorGUILayout.EnumPopup</c> 绘制，
    /// 下拉显示的是英文标识符。</para>
    /// </summary>
    public enum ShopRefreshType
    {
        /// <summary>不刷新：可交易次数为终身上限。</summary>
        [InspectorName("不刷新")] Never   = 0,
        /// <summary>每日刷新。</summary>
        [InspectorName("每日")]   Daily   = 1,
        /// <summary>每周刷新。</summary>
        [InspectorName("每周")]   Weekly  = 2,
        /// <summary>每月刷新。</summary>
        [InspectorName("每月")]   Monthly = 3,
    }
}

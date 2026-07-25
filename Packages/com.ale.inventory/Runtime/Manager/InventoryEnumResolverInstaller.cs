using Ale.Toolkit.Runtime;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 把库存的枚举类型集合接到 toolkit 的 <see cref="EnumTypeResolver"/> 上（运行时路径）。
    ///
    /// <para><see cref="AttributeValue"/> 在把枚举整数值转显示名时需要按枚举类型名找到 <see cref="EnumType"/>，
    /// 而枚举类型集合由 <see cref="InventoryDataManager"/> 持有。此处在每次播放开始时安装解析委托；
    /// 委托本身惰性求值（调用时才访问 <c>Instance</c>），不会提前创建单例。</para>
    ///
    /// <para>编辑器（非播放）路径由 <c>InventoryEnumResolverEditorInstaller</c> 以
    /// <c>[InitializeOnLoad]</c> 安装同一委托。</para>
    /// </summary>
    internal static class InventoryEnumResolverInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            EnumTypeResolver.Resolve = n => InventoryDataManager.Instance != null
                ? InventoryDataManager.Instance.GetEnumType(n)
                : null;
        }
    }
}

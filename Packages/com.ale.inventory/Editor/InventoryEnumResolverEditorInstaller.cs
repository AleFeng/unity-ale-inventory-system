using Ale.Inventory.Runtime;
using Ale.Toolkit.Runtime;
using UnityEditor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 编辑器（非播放）路径下把库存枚举类型集合接到 toolkit 的 <see cref="EnumTypeResolver"/> 上。
    ///
    /// <para><c>[RuntimeInitializeOnLoadMethod]</c> 只在进入播放时触发；编辑器内绘制 / 显示属性值也可能
    /// 需要解析枚举，故用 <c>[InitializeOnLoad]</c> 在编辑器加载时安装同一委托。委托惰性求值，
    /// 不会提前创建 <see cref="InventoryDataManager"/> 单例。</para>
    /// </summary>
    [InitializeOnLoad]
    internal static class InventoryEnumResolverEditorInstaller
    {
        static InventoryEnumResolverEditorInstaller()
        {
            EnumTypeResolver.Resolve = n => InventoryDataManager.Instance != null
                ? InventoryDataManager.Instance.GetEnumType(n)
                : null;
        }
    }
}

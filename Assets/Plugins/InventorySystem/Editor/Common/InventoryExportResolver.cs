using System;
using InventorySystem.Runtime.Serialization;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 导出资源解析器选择钩子。core 编辑器程序集对 Addressables 零依赖：
    /// 当启用 IS_ADDRESSABLE 宏时，受约束的 Addressable 编辑器程序集会通过
    /// <see cref="AddressableProvider"/> 注入自己的解析器（导出时把资源登记进 Addressable 分组并返回地址作 key）；
    /// 未启用时回退到默认的 <see cref="EditorAssetGuidResolver"/>（GUID:localFileId）。
    /// </summary>
    public static class InventoryExportResolver
    {
        /// <summary>
        /// 由 Addressable 编辑器程序集在 [InitializeOnLoad] 时赋值。
        /// 返回用于导出的解析器；为 null 表示未注入（宏未启用或包未装）。
        /// </summary>
        public static Func<IAssetRefResolver> AddressableProvider;

        /// <summary>
        /// 选择当前导出应使用的解析器。
        /// <paramref name="addressableEnabled"/> 通常为 <see cref="InventoryEditorPrefs.IsAddressableEnabled"/>。
        /// </summary>
        public static IAssetRefResolver Resolve(bool addressableEnabled)
        {
            if (addressableEnabled && AddressableProvider != null)
            {
                var resolver = AddressableProvider();
                if (resolver != null) return resolver;
            }
            return EditorAssetGuidResolver.Instance;
        }
    }
}

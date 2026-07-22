using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// 资源引用 <-> GUID 的转换抽象。导出时把 Unity 对象引用转为可移植的 GUID 字符串，
    /// 导入时反向解析。运行时使用 <see cref="NullAssetRefResolver"/>（不解析，引用保持为空）；
    /// 编辑器侧由 EditorAssetGuidResolver（基于 AssetDatabase）实现。
    /// </summary>
    public interface IAssetRefResolver
    {
        /// <summary>把对象引用转为 GUID 字符串（含子资源 fileId）。无法解析时返回空字符串。</summary>
        string ToGuid(Object obj);

        /// <summary>把 GUID 字符串解析回对象引用。无法解析时返回 null。</summary>
        Object FromGuid(string guid);
    }

    /// <summary>
    /// 空解析器：导出时所有引用记为空 GUID，导入时所有 GUID 解析为 null。
    /// 运行时（无 AssetDatabase）使用。
    /// </summary>
    public class NullAssetRefResolver : IAssetRefResolver
    {
        public static readonly NullAssetRefResolver Instance = new NullAssetRefResolver();

        public string ToGuid(Object obj) => string.Empty;
        public Object FromGuid(string guid) => null;
    }
}

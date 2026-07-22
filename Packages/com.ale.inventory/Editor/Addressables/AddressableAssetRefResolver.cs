using InventorySystem.Runtime.Serialization;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InventorySystem.Editor
{
    /// <summary>
    /// Addressable 模式下的导出解析器。把被引用资源转为其 GUID 作为运行时加载键
    /// （Addressables 内部以 GUID 建索引，加载稳定且无需手动指定地址）；图集子 Sprite 等子资源返回 <c>GUID[子名]</c>。
    /// <b>不会自动修改 AddressableAssetSettings</b>（既不建分组、也不建条目）：资源是否标记为 Addressable
    /// 由用户自行在 Addressables Groups 窗口中管理，遇到未标记的资源仅打警告提醒。
    ///
    /// 仅在启用 IS_ADDRESSABLE 宏时编译；[InitializeOnLoad] 时把自己注入 core 的
    /// <see cref="InventoryExportResolver.AddressableProvider"/> 钩子。
    /// </summary>
    [InitializeOnLoad]
    public sealed class AddressableAssetRefResolver : IAssetRefResolver
    {
        public static readonly AddressableAssetRefResolver Instance = new AddressableAssetRefResolver();

        static AddressableAssetRefResolver()
        {
            // 注入钩子：core 导出流程在 IS_ADDRESSABLE 启用时会取用此解析器。
            InventoryExportResolver.AddressableProvider = () => Instance;
        }

        // ── 导出：Object → Addressable key（不改动 AddressableAssetSettings；未标记则警告）──────

        public string ToGuid(Object obj)
        {
            return ToGuid(obj, warnIfUnregistered: true);
        }

        /// <summary>
        /// Object → Addressable key（GUID；子资源为 <c>GUID[子名]</c>）。
        /// 无论如何都<b>不会自动创建 Addressable 分组或条目</b>。
        /// <paramref name="warnIfUnregistered"/> 为 true 时（导出 / 拖拽授权流程）会检查资源是否已标记为 Addressable，
        /// 未标记则打警告提醒用户手动添加；为 false 时（迁移工具）仅取 GUID、完全不访问 AddressableAssetSettings。
        /// </summary>
        public string ToGuid(Object obj, bool warnIfUnregistered)
        {
            if (!obj) return string.Empty;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long _))
                return string.Empty;

            if (warnIfUnregistered)
            {
                var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                if (settings)
                {
                    // 不自动创建条目：仅检查资源是否已（显式或隐式）标记为 Addressable，未标记则警告提醒，
                    // 由用户自行在 Addressables Groups 窗口中添加（与「不自动建组」策略一致）。
                    if (settings.FindAssetEntry(guid, includeImplicit: true) == null)
                        Debug.LogWarning($"[InventoryAddressables] 资源「{obj.name}」（GUID: {guid}）尚未标记为 Addressable，未自动登记。" +
                                         "请在 Addressables Groups 窗口中手动添加为 Addressable，否则运行时无法按 GUID 加载。");
                }
                else
                {
                    Debug.LogWarning("[InventoryAddressables] 无法获取 AddressableAssetSettings：" + obj.name);
                }
            }

            // 子资源（如图集中的子 Sprite）：附加 [子名] 以便 Addressables 定位
            if (!AssetDatabase.IsMainAsset(obj))
                return $"{guid}[{obj.name}]";
            return guid;
        }

        // ── 导入（编辑器内偶尔用到）：key → Object ────────────────────────────────────

        public Object FromGuid(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            // GUID[子名]
            int lb = key.IndexOf('[');
            if (lb >= 0 && key.EndsWith("]"))
            {
                string guidPart = key.Substring(0, lb);
                string subName  = key.Substring(lb + 1, key.Length - lb - 2);
                string path     = AssetDatabase.GUIDToAssetPath(guidPart);
                if (string.IsNullOrEmpty(path)) return null;
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (a && a.name == subName) return a;
                return AssetDatabase.LoadMainAssetAtPath(path);
            }

            // 兼容旧的 GUID:localFileId 格式
            if (key.IndexOf(':') >= 0)
                return EditorAssetGuidResolver.Instance.FromGuid(key);

            // 纯 GUID
            string mainPath = AssetDatabase.GUIDToAssetPath(key);
            return string.IsNullOrEmpty(mainPath) ? null : AssetDatabase.LoadMainAssetAtPath(mainPath);
        }
    }
}

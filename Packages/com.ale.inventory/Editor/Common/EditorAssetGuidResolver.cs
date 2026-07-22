using InventorySystem.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 基于 AssetDatabase 的资源引用解析器。导出时把对象引用转为 "GUID:localFileId" 字符串
    /// （localFileId 用于区分同一资源文件下的子资源，如图集中的 Sprite），导入时反向解析。
    /// </summary>
    public class EditorAssetGuidResolver : IAssetRefResolver
    {
        public static readonly EditorAssetGuidResolver Instance = new EditorAssetGuidResolver();

        public string ToGuid(Object obj)
        {
            if (!obj) return string.Empty;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localId))
                return $"{guid}:{localId}";
            return string.Empty;
        }

        public Object FromGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            string guidPart = guid;
            long localId = 0;
            int sep = guid.IndexOf(':');
            if (sep >= 0)
            {
                guidPart = guid.Substring(0, sep);
                long.TryParse(guid.Substring(sep + 1), out localId);
            }

            string path = AssetDatabase.GUIDToAssetPath(guidPart);
            if (string.IsNullOrEmpty(path)) return null;

            // 在该资源路径下的所有子资源中，匹配 localFileId。
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (!a) continue;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out string _, out long id) && id == localId)
                    return a;
            }

            // 退化：直接加载主资源。
            return AssetDatabase.LoadMainAssetAtPath(path);
        }
    }
}

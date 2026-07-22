using UnityEngine;

namespace InventorySystem.Runtime.Serialization
{
    /// <summary>
    /// 仓库系统 JSON 序列化器。导出：DB -> 可读 JSON 文本；导入：JSON 文本 -> 新的 InventoryDatabase 实例。
    /// JSON 为单向导出格式：编辑流程始终基于 ScriptableObject，导出仅用于运行时/打包消费。
    /// </summary>
    public static class InventoryJsonSerializer
    {
        /// <summary>把数据库导出为带缩进的 JSON 文本。对象引用经 <paramref name="resolver"/> 转为 GUID。</summary>
        public static string Export(InventoryDatabase db, IAssetRefResolver resolver)
        {
            var dto = InventoryDtoMapper.ToDto(db, resolver);
            return JsonUtility.ToJson(dto, true);
        }

        /// <summary>
        /// 从 JSON 文本导入为一个新的 <see cref="InventoryDatabase"/> 实例（ScriptableObject.CreateInstance）。
        /// </summary>
        public static InventoryDatabase Import(string json, IAssetRefResolver resolver)
        {
            var db = ScriptableObject.CreateInstance<InventoryDatabase>();
            ImportInto(json, db, resolver);
            return db;
        }

        /// <summary>把 JSON 文本反序列化并写入已有的数据库实例（覆盖其内容）。</summary>
        public static void ImportInto(string json, InventoryDatabase target, IAssetRefResolver resolver)
        {
            if (string.IsNullOrEmpty(json) || target == null) return;
            var dto = JsonUtility.FromJson<InventoryDatabaseDto>(json);
            InventoryDtoMapper.FromDto(dto, target, resolver);
        }
    }
}

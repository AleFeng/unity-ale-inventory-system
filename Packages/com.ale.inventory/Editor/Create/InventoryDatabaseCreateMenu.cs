using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统数据文件创建菜单。替代 [CreateAssetMenu]，在创建时支持从配置的模板深拷贝数据。
    /// 模板路径存储于 EditorPrefs（键 <see cref="InventoryEditorPrefs.TemplateDatabasePath"/>），
    /// 可在欢迎窗口中设置。若未设置模板，则使用默认数据（SeedDefaults）。
    /// </summary>
    public static class InventoryDatabaseCreateMenu
    {
        [MenuItem("Assets/Create/Inventory System/Inventory Database", priority = 0)]
        public static void CreateDatabase() => CreateInventoryDatabase();

        private static void CreateInventoryDatabase()
        {
            string savePath = EditorUtility.SaveFilePanelInProject(
                "创建仓库系统数据文件", "InventoryDatabase", "asset",
                "请选择数据文件的保存位置");
            if (string.IsNullOrEmpty(savePath)) return;

            var newDb = ScriptableObject.CreateInstance<InventoryDatabase>();

            var templateDb = InventoryEditorPrefs.LoadTemplateDatabase();
            if (templateDb)
                newDb.CloneFrom(templateDb);
            else
                newDb.SeedDefaults();

            AssetDatabase.CreateAsset(newDb, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 选中新建的资产
            Selection.activeObject = newDb;
            EditorGUIUtility.PingObject(newDb);
        }
    }
}

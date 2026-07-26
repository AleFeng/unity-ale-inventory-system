using Ale.Inventory.Runtime;
using UnityEditor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统编辑器偏好设置的键名常量与持久化辅助方法。
    /// 所有 EditorPrefs 的读写统一经由此类，避免键名散落各处。
    ///
    /// <para>可选依赖宏（TMP / Localization / Addressables）已下沉为项目级全局设定，其宏名常量、启用状态
    /// 与包安装检测统一由 toolkit 的 <see cref="Ale.Toolkit.Editor.ToolkitDefines"/> 提供，本类不再持有。</para>
    /// </summary>
    public static class InventoryEditorPrefs
    {
        // ── EditorPrefs 键 ──────────────────────────────────────────────────────
        /// <summary>欢迎窗口是否在启动时自动显示。</summary>
        public const string WelcomeAutoShow       = "IS_WelcomeAutoShow";
        /// <summary>欢迎窗口本会话是否已显示过（SessionState 键，非 EditorPrefs）。</summary>
        public const string WelcomeShownThisSession = "IS_WelcomeShownThisSession";
        /// <summary>
        /// 旧版本：创建新数据文件时使用的模板资产路径（EditorPrefs 键）。
        /// 模板已改存项目级 <see cref="InventoryProjectSettings"/>（随仓库入库）；此键仅用于一次性迁移旧设置。
        /// </summary>
        public const string TemplateDatabasePath  = "IS_TemplateDatabasePath";
        /// <summary>上次打开的 InventoryDatabase 资产路径。</summary>
        public const string LastDatabasePath      = "InventorySystem.DatabasePath";
        /// <summary>编辑器 UI 显示语言（<c>EditorLanguage</c> 的整数值）。持久化经 <c>ToolkitEditorL10n</c>。</summary>
        public const string EditorLanguage        = "InventorySystem.Editor.Language";
        /// <summary>是否翻译枚举下拉值（默认关）。持久化经 <c>ToolkitEditorL10n</c>。</summary>
        public const string EditorTranslateEnums  = "InventorySystem.Editor.TranslateEnums";

        #region 模板数据库

        /// <summary>保存模板数据库到项目级设置（<see cref="InventoryProjectSettings"/>，随仓库入库）。</summary>
        public static void SaveTemplateDatabase(InventoryDatabase db)
        {
            var s = InventoryProjectSettings.instance;
            if (s.templateDatabase == db) return;
            s.templateDatabase = db;
            s.SaveSettings();
        }

        /// <summary>
        /// 从项目级设置加载模板数据库，未设置或已删除时返回 null。
        /// 首次调用时若项目级设置为空、而旧 EditorPrefs 键仍存路径，则一次性迁入并清除旧键。
        /// </summary>
        public static InventoryDatabase LoadTemplateDatabase()
        {
            var s = InventoryProjectSettings.instance;
            if (s.templateDatabase) return s.templateDatabase;

            // 一次性迁移旧设置：EditorPrefs 路径 → 项目级设置。
            string legacyPath = EditorPrefs.GetString(TemplateDatabasePath, string.Empty);
            if (string.IsNullOrEmpty(legacyPath)) return null;

            var db = AssetDatabase.LoadAssetAtPath<InventoryDatabase>(legacyPath);
            if (db)
            {
                s.templateDatabase = db;
                s.SaveSettings();
                EditorPrefs.DeleteKey(TemplateDatabasePath);
            }
            return db;
        }

        #endregion
    }
}

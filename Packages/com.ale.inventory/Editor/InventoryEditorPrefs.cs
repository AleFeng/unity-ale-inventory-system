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
        /// <summary>创建新数据文件时使用的模板资产路径。</summary>
        public const string TemplateDatabasePath  = "IS_TemplateDatabasePath";
        /// <summary>上次打开的 InventoryDatabase 资产路径。</summary>
        public const string LastDatabasePath      = "InventorySystem.DatabasePath";
        /// <summary>编辑器 UI 显示语言（<c>EditorLanguage</c> 的整数值）。持久化经 <c>ToolkitEditorL10n</c>。</summary>
        public const string EditorLanguage        = "InventorySystem.Editor.Language";
        /// <summary>是否翻译枚举下拉值（默认关）。持久化经 <c>ToolkitEditorL10n</c>。</summary>
        public const string EditorTranslateEnums  = "InventorySystem.Editor.TranslateEnums";

        #region 模板数据库

        /// <summary>保存模板数据库路径到 EditorPrefs。</summary>
        public static void SaveTemplateDatabase(InventoryDatabase db)
        {
            string path = db ? AssetDatabase.GetAssetPath(db) : string.Empty;
            EditorPrefs.SetString(TemplateDatabasePath, path);
        }

        /// <summary>从 EditorPrefs 加载模板数据库，未设置或已删除时返回 null。</summary>
        public static InventoryDatabase LoadTemplateDatabase()
        {
            string path = EditorPrefs.GetString(TemplateDatabasePath, string.Empty);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<InventoryDatabase>(path);
        }

        #endregion
    }
}

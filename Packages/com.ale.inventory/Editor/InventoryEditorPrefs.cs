using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统编辑器偏好设置的键名常量与持久化辅助方法。
    /// 所有 EditorPrefs 的读写统一经由此类，避免键名散落各处。
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

        // ── IS_LOCALIZATION 宏定义名 ──────────────────────────────────────────
        public const string Define_IsLocalization = "IS_LOCALIZATION";

        // ── IS_ADDRESSABLE 宏定义名 ───────────────────────────────────────────
        public const string Define_IsAddressable = "IS_ADDRESSABLE";

        // ── IS_TMP 宏定义名 ───────────────────────────────────────────────────
        /// <summary>启用后，道具 UI 文本组件使用 TMP_Text；未启用时使用 UnityEngine.UI.Text。</summary>
        public const string Define_IsTmp = "IS_TMP";

        // ── 包名（用于检测是否已安装）──────────────────────────────────────────
        public const string Package_Localization = "com.unity.localization";
        public const string Package_Addressables = "com.unity.addressables";
        /// <summary>TextMeshPro 包名（Unity 2021+ 已内置于 com.unity.ugui）。</summary>
        public const string Package_Tmp = "com.unity.ugui";

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

        #region 向导 TMP 默认字体

        /// <summary>向导生成 Prefab 时使用的默认 TMP 字体资产路径（EditorPrefs 键）。</summary>
        public const string WizardDefaultTmpFontPath = "IS_WizardDefaultTmpFontPath";

        /// <summary>保存向导默认 TMP 字体到 EditorPrefs。</summary>
        public static void SaveWizardDefaultTmpFont(TMPro.TMP_FontAsset font)
        {
            string path = font ? AssetDatabase.GetAssetPath(font) : string.Empty;
            EditorPrefs.SetString(WizardDefaultTmpFontPath, path);
        }

        /// <summary>从 EditorPrefs 加载向导默认 TMP 字体，未设置或已删除时返回 null。</summary>
        public static TMPro.TMP_FontAsset LoadWizardDefaultTmpFont()
        {
            string path = EditorPrefs.GetString(WizardDefaultTmpFontPath, string.Empty);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
        }

        #endregion

        #region IS_LOCALIZATION 宏状态

        /// <summary>当前是否在 PlayerSettings 中启用了 IS_LOCALIZATION 宏。</summary>
        public static bool IsLocalizationEnabled()
        {
            return IsDefineEnabled(Define_IsLocalization);
        }

        /// <summary>当前是否在 PlayerSettings 中启用了 IS_ADDRESSABLE 宏。</summary>
        public static bool IsAddressableEnabled()
        {
            return IsDefineEnabled(Define_IsAddressable);
        }

        /// <summary>当前是否在 PlayerSettings 中启用了 IS_TMP 宏。</summary>
        public static bool IsTmpEnabled()
        {
            return IsDefineEnabled(Define_IsTmp);
        }

        private static bool IsDefineEnabled(string define)
        {
            try
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
                return ContainsDefine(defines, define);
            }
            catch
            {
                return false;
            }
        }

        private static bool ContainsDefine(string defines, string define)
        {
            if (string.IsNullOrEmpty(defines)) return false;
            foreach (var d in defines.Split(';'))
                if (d.Trim() == define) return true;
            return false;
        }

        #endregion

        #region 包存在性

        /// <summary>
        /// 检测 UnityEngine.Localization 命名空间是否可用（即 com.unity.localization 包是否已安装）。
        /// </summary>
        public static bool IsLocalizationPackageInstalled()
        {
            return InventoryDefineUtils.HasNamespace("UnityEngine.Localization");
        }

        /// <summary>
        /// 检测 UnityEngine.AddressableAssets 命名空间是否可用（即 com.unity.addressables 包是否已安装）。
        /// </summary>
        public static bool IsAddressablePackageInstalled()
        {
            return InventoryDefineUtils.HasNamespace("UnityEngine.AddressableAssets");
        }

        /// <summary>
        /// 检测 TextMeshPro (TMPro) 命名空间是否可用。
        /// Unity 2021+ 已将 TMP 内置于 com.unity.ugui，通常始终可用。
        /// </summary>
        public static bool IsTmpPackageInstalled()
        {
            return InventoryDefineUtils.HasNamespace("TMPro");
        }

        #endregion
    }
}

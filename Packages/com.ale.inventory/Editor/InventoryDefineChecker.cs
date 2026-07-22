using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统编辑器加载检查器。
    /// 每次 Unity 启动/域重载时执行，负责：
    /// 1. 若本会话尚未显示过欢迎窗口且未禁用自动显示，则自动弹出欢迎窗口。
    /// 2. 检测 com.unity.localization 包是否存在，与 IS_LOCALIZATION 宏状态对比，
    ///    若两者不一致则在 Console 输出提示（不强制修改，保持手动控制优先）。
    /// </summary>
    [InitializeOnLoad]
    public static class InventoryDefineChecker
    {
        static InventoryDefineChecker()
        {
            // 延迟到编辑器完全就绪后执行，避免在域初始化期间操作 UI。
            EditorApplication.delayCall += OnDelayedInit;
        }

        private static void OnDelayedInit()
        {
            EditorApplication.delayCall -= OnDelayedInit;

            CheckWelcomeWindow();
            CheckLocalizationPackageConsistency();
        }

        /// <summary>判断是否需要自动弹出欢迎窗口并弹出。</summary>
        private static void CheckWelcomeWindow()
        {
            // 本会话已经显示过则跳过（SessionState 在重启 Unity 后重置）。
            if (SessionState.GetBool(InventoryEditorPrefs.WelcomeShownThisSession, false))
                return;

            SessionState.SetBool(InventoryEditorPrefs.WelcomeShownThisSession, true);

            // 用户禁用了自动显示则跳过。
            if (!EditorPrefs.GetBool(InventoryEditorPrefs.WelcomeAutoShow, true))
                return;

            InventoryWelcomeWindow.Open();
        }

        /// <summary>
        /// 包与宏状态一致性检查（仅提示，不自动修改）。
        /// 场景：用户卸载了 Localization 包但忘记关掉宏，或已安装包但从未启用宏。
        /// </summary>
        private static void CheckLocalizationPackageConsistency()
        {
            bool packageInstalled = InventoryEditorPrefs.IsLocalizationPackageInstalled();
            bool macroEnabled     = InventoryEditorPrefs.IsLocalizationEnabled();

            if (!packageInstalled && macroEnabled)
            {
                Debug.LogWarning(
                    "[InventorySystem] IS_LOCALIZATION 宏已启用，但 com.unity.localization 包未安装。\n" +
                    "LocalizedString 字段在运行时将无法解析。建议安装包或在欢迎窗口中关闭宏。\n" +
                    "（Tools > InventorySystem > Welcome）");
            }
        }
    }
}

using UnityEditor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统编辑器加载检查器。每次 Unity 启动 / 域重载时，若本会话尚未显示过欢迎窗口且未禁用自动显示，
    /// 则自动弹出库存欢迎窗口。
    ///
    /// <para>可选依赖宏（TMP / Localization / Addressables）已下沉为项目级全局设定，其旧宏自动迁移与
    /// 包 / 宏一致性检查由 toolkit 的 <c>ToolkitDefineChecker</c> 统一负责，本类不再处理。</para>
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
    }
}

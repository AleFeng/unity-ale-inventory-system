namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 通用编辑器设置 UI 的英 / 日译表：编辑器语言开关（多语言设定 / 枚举值）、可选依赖宏开关面板的
    /// 通用对话框 / 状态行（警告 / 确定 / 取消 / 已安装 / 未安装 / 等待重新编译）、启动自动显示开关。
    /// 与具体插件无关的通用文案，以中文原文为键。
    /// </summary>
    public static partial class ToolkitEditorL10n
    {
        static partial void RegisterDefines()
        {
            // ── 编辑器语言设定 ────────────────────────────────────────────────────
            Add("多语言设定", "Language Settings", "多言語設定");
            Add("枚举值",     "Enum Values",       "列挙値");
            Add("勾选后，类型下拉等枚举值也随语言切换；不勾选则保持代码中的英文原名。",
                "When checked, enum values such as type dropdowns also switch language; otherwise the original English names in code are kept.",
                "チェックすると、型ドロップダウンなどの列挙値も言語に合わせて切り替わります。チェックしない場合はコード内の英語の原名のままです。");

            // ── 宏开关面板：通用对话框 / 状态行 ───────────────────────────────────
            Add("警告", "Warning", "警告");
            Add("确定", "OK",      "OK");
            Add("取消", "Cancel",  "キャンセル");
            Add("  ✓ {0} 已安装", "  ✓ {0} installed", "  ✓ {0} インストール済み");
            Add("  ⚠ {0} 未安装（需通过 Package Manager 安装）",
                "  ⚠ {0} not installed (install via Package Manager)",
                "  ⚠ {0} 未インストール（Package Manager からインストール）");
            Add("  ⏳ 宏定义已更改，等待 Unity 重新编译…",
                "  ⏳ Define changed, waiting for Unity to recompile…",
                "  ⏳ マクロ定義が変更されました。Unity の再コンパイルを待っています…");

            // ── 启动自动显示 ──────────────────────────────────────────────────────
            Add("启动时自动显示", "Show on startup", "起動時に表示");
        }
    }
}

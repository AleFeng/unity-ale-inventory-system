namespace Ale.Inventory.Editor
{
    /// <summary>
    /// <see cref="InventoryWelcomeWindow"/> 的英 / 日译表。中文为源语言，故此处只登记英、日两栏；
    /// 未登记的条目在对应语言下自动回退中文。
    /// </summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterWelcome()
        {
            // ── 窗口标题 ──────────────────────────────────────────────────────────
            Add("Inventory 道具仓库系统",
                "Inventory System",
                "インベントリシステム");

            // ── 页眉 ──────────────────────────────────────────────────────────────
            Add("面向设计师的 道具与仓库 配置工具",
                "Item & inventory configuration tool for designers",
                "デザイナー向けアイテム・インベントリ設定ツール");

            // ── 多语言设定 ────────────────────────────────────────────────────────
            Add("多语言设定", "Language Settings", "多言語設定");
            Add("枚举值",     "Enum Values",       "列挙値");
            Add("勾选后，类型下拉等枚举值也随语言切换；不勾选则保持代码中的英文原名。",
                "When checked, enum values such as type dropdowns also switch language; otherwise the original English names in code are kept.",
                "チェックすると、型ドロップダウンなどの列挙値も言語に合わせて切り替わります。チェックしない場合はコード内の英語の原名のままです。");

            // ── 快捷操作 ──────────────────────────────────────────────────────────
            Add("快捷操作",              "Quick Actions",           "クイック操作");
            Add("创建新数据文件",        "Create New Data File",    "新規データファイル作成");
            Add("打开 Inventory Editor", "Open Inventory Editor",   "Inventory Editor を開く");
            Add("打开 Addressable工具窗口", "Open Addressable Tool", "Addressable ツールを開く");
            Add("打开 本地化工具窗口",   "Open Localization Tool",  "ローカライズツールを開く");
            Add("查看文档",              "View Docs",               "ドキュメントを見る");

            // ── 预制体生成 ────────────────────────────────────────────────────────
            Add("预制体生成",            "Prefab Generation",       "プレハブ生成");
            Add("生成全部（数据库 + 全部 Prefab）",
                "Generate All (Database + All Prefabs)",
                "すべて生成（データベース + 全 Prefab）");
            Add("{0}（{1}）", "{0} ({1})", "{0}（{1}）");
            Add("生成", "Generate", "生成");

            // ── 数据模板 ──────────────────────────────────────────────────────────
            Add("数据模板", "Data Template", "データテンプレート");
            Add("创建新数据文件时使用的模板（留空则使用默认数据）：",
                "Template used when creating a new data file (leave empty for default data):",
                "新規データファイル作成時に使うテンプレート（空欄でデフォルトデータ）：");
            Add("  包含：{0} 枚举类型  |  {1} 功能标签  |  {2} 道具模板  |  {3} 道具",
                "  Contains: {0} enum types  |  {1} function tags  |  {2} item templates  |  {3} items",
                "  内容：{0} 列挙型  |  {1} 機能タグ  |  {2} アイテムテンプレート  |  {3} アイテム");

            // ── 插件支持 ──────────────────────────────────────────────────────────
            Add("插件支持", "Plugin Support", "プラグインサポート");

            // TextMeshPro
            Add("启用后，道具 UI 脚本（Uiw 开头）的文本组件使用 TMP_Text；" +
                "未启用时使用 UnityEngine.UI.Text。Unity 2021+ 已内置 TextMeshPro，通常可直接启用。",
                "When enabled, text components of item UI scripts (Uiw prefix) use TMP_Text; " +
                "otherwise UnityEngine.UI.Text is used. TextMeshPro is built into Unity 2021+, so it can usually be enabled directly.",
                "有効にすると、アイテム UI スクリプト（Uiw 始まり）のテキストコンポーネントが TMP_Text を使用します。" +
                "無効時は UnityEngine.UI.Text を使用します。TextMeshPro は Unity 2021+ に内蔵されているため、通常はそのまま有効にできます。");
            Add("TMPro 命名空间未检测到。\n" +
                "请确认 TextMeshPro 已通过 Package Manager 安装。\n\n" +
                "确定要继续启用吗？",
                "The TMPro namespace was not detected.\n" +
                "Please make sure TextMeshPro is installed via Package Manager.\n\n" +
                "Enable anyway?",
                "TMPro 名前空間が検出されませんでした。\n" +
                "TextMeshPro が Package Manager 経由でインストールされているか確認してください。\n\n" +
                "このまま有効にしますか？");

            // Unity Localization
            Add("启用后，属性字段类型可选择 LocalizedString，支持 Unity Localization 多语言配置。",
                "When enabled, attribute field types can use LocalizedString for Unity Localization multi-language configuration.",
                "有効にすると、属性フィールドの型で LocalizedString を選択でき、Unity Localization による多言語設定に対応します。");
            Add("com.unity.localization 包尚未安装。\n" +
                "启用宏后，LocalizedString 字段将出现在编辑器中，但运行时无法解析。\n\n" +
                "确定要继续启用吗？",
                "The com.unity.localization package is not installed.\n" +
                "After enabling the define, LocalizedString fields will appear in the editor but cannot be resolved at runtime.\n\n" +
                "Enable anyway?",
                "com.unity.localization パッケージがインストールされていません。\n" +
                "マクロを有効にすると LocalizedString フィールドがエディターに表示されますが、実行時には解決できません。\n\n" +
                "このまま有効にしますか？");

            // Unity Addressable
            Add("启用后，属性系统的资源字段（Sprite/Prefab 等）在编辑器改用原生 AssetReference 选择器授权（仅存 GUID，" +
                "不硬引用、加载数据库不再一并载入资源）；运行时经 Addressable 按需异步加载、引用计数随宿主销毁自动卸载。" +
                "导出时自动把被引用资源登记进默认 Addressable 分组。" +
                "已有数据可用菜单 Tools/Inventory System/Addressables/资源引用迁移工具（带进度条与实时日志）在「Object 引用 ↔ AssetReference(GUID)」间批量互转。",
                "When enabled, asset fields of the attribute system (Sprite/Prefab, etc.) switch to the native AssetReference selector in the editor " +
                "(storing only the GUID, no hard references, so loading the database no longer loads the assets too); at runtime they are loaded " +
                "asynchronously on demand via Addressable and unloaded automatically by reference counting when the host is destroyed. " +
                "On export, referenced assets are automatically registered into the default Addressable group. " +
                "For existing data, use the menu Tools/Inventory System/Addressables/Asset Reference Migration Tool (with progress bar and live log) " +
                "to batch-convert between \"Object reference ↔ AssetReference(GUID)\".",
                "有効にすると、属性システムのアセットフィールド（Sprite/Prefab など）がエディターでネイティブの AssetReference セレクターに切り替わります" +
                "（GUID のみを保存し、ハード参照しないため、データベースを読み込んでもアセットは同時に読み込まれません）。実行時は Addressable で必要に応じて" +
                "非同期読み込みし、参照カウントによりホストの破棄時に自動でアンロードされます。エクスポート時には参照されたアセットが既定の Addressable グループへ自動登録されます。" +
                "既存データはメニュー Tools/Inventory System/Addressables/アセット参照移行ツール（進捗バーとリアルタイムログ付き）で" +
                "「Object 参照 ↔ AssetReference(GUID)」を一括変換できます。");
            Add("com.unity.addressables 包尚未安装。\n" +
                "启用宏后，运行时无法通过 Addressable 加载资源。\n\n" +
                "确定要继续启用吗？",
                "The com.unity.addressables package is not installed.\n" +
                "After enabling the define, assets cannot be loaded via Addressable at runtime.\n\n" +
                "Enable anyway?",
                "com.unity.addressables パッケージがインストールされていません。\n" +
                "マクロを有効にすると、実行時に Addressable でアセットを読み込めません。\n\n" +
                "このまま有効にしますか？");

            // ── 宏开关：字体折叠栏 ────────────────────────────────────────────────
            Add("TextMeshPro 设置", "TextMeshPro Settings", "TextMeshPro 設定");
            Add("默认字体", "Default Font", "デフォルトフォント");
            Add("生成测试 Prefab 时将此字体应用于所有 TMP 文本节点（留空则使用 TMP 默认字体）。",
                "Applies this font to all TMP text nodes when generating test prefabs (leave empty to use the TMP default font).",
                "テスト用 Prefab の生成時に、このフォントをすべての TMP テキストノードに適用します（空欄で TMP のデフォルトフォントを使用）。");
            Add("Unity Localization 设置", "Unity Localization Settings", "Unity Localization 設定");
            Add("生成测试 Prefab 时赋给 InventoryTmpFontEvent 组件的本地化字体资源。" +
                "需同时启用 IS_TMP 才生效。",
                "The localized font asset assigned to the InventoryTmpFontEvent component when generating test prefabs. " +
                "Requires IS_TMP to also be enabled.",
                "テスト用 Prefab の生成時に InventoryTmpFontEvent コンポーネントへ割り当てるローカライズフォントアセット。" +
                "IS_TMP も有効な場合にのみ機能します。");

            // ── 宏开关：通用（对话框 / 状态行）────────────────────────────────────
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

            // ── 页脚 ──────────────────────────────────────────────────────────────
            Add("启动时自动显示", "Show on startup", "起動時に表示");

            // ── 文档 ──────────────────────────────────────────────────────────────
            Add("文档未找到", "Documentation Not Found", "ドキュメントが見つかりません");
            Add("未能找到文档文件：\nPackages/com.ale.inventory/README.md",
                "Could not find the documentation file:\nPackages/com.ale.inventory/README.md",
                "ドキュメントファイルが見つかりませんでした：\nPackages/com.ale.inventory/README.md");
        }
    }
}

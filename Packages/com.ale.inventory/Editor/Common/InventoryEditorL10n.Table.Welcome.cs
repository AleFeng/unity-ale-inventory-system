using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// <see cref="InventoryWelcomeWindow"/> 的英 / 日译表。中文为源语言，故此处只登记英、日两栏；
    /// 未登记的条目在对应语言下自动回退中文。
    /// </summary>
    internal static partial class InventoryEditorL10nTables
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

            // ── 快捷操作 ──────────────────────────────────────────────────────────
            Add("快捷操作",              "Quick Actions",           "クイック操作");
            Add("创建新数据文件",        "Create New Data File",    "新規データファイル作成");
            Add("打开 Inventory Editor", "Open Inventory Editor",   "Inventory Editor を開く");
            Add("打开 Addressable工具窗口", "Open Addressable Tool", "Addressable ツールを開く");
            Add("打开 本地化工具窗口",   "Open Localization Tool",  "ローカライズツールを開く");
            Add("查看文档",              "View Docs",               "ドキュメントを見る");
            Add("打开 Ale Toolkit 设置（语言 / 插件宏）",
                "Open Ale Toolkit Settings (Language / Defines)",
                "Ale Toolkit 設定を開く（言語 / マクロ）");

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

            // ── 文档 ──────────────────────────────────────────────────────────────
            Add("文档未找到", "Documentation Not Found", "ドキュメントが見つかりません");
            Add("未能找到文档文件：\nPackages/com.ale.inventory/README.md",
                "Could not find the documentation file:\nPackages/com.ale.inventory/README.md",
                "ドキュメントファイルが見つかりませんでした：\nPackages/com.ale.inventory/README.md");
        }
    }
}

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// <see cref="InventoryEditorWindow"/> 外壳与三列通用框架基类（<c>Editor/Common/Editor*.cs</c>）的英 / 日译表。
    /// 术语与包内既有英 / 日文档一致：仓库 = Warehouse / 倉庫，蓝图 = Blueprint / ブループリント 等。
    /// </summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterFramework()
        {
            // ── 窗口标题 ──────────────────────────────────────────────────────────
            Add("Inventory Editor", "Inventory Editor", "インベントリエディター");

            // ── 系统页签 ──────────────────────────────────────────────────────────
            Add("道具系统", "Item System",      "アイテムシステム");
            Add("仓库系统", "Warehouse System", "倉庫システム");
            Add("商店系统", "Shop System",      "ショップシステム");
            Add("制作系统", "Crafting System",  "クラフトシステム");
            Add("装备系统", "Equipment System", "装備システム");
            Add("技能系统", "Skill System",     "スキルシステム");

            // ── 实体名词（右列标题 / 删除按钮 / 状态栏；术语同上）──────────────────
            Add("道具",   "Item",            "アイテム");
            Add("仓库",   "Warehouse",       "倉庫");
            Add("商店",   "Shop",            "ショップ");
            Add("蓝图",   "Blueprint",       "ブループリント");
            Add("装备组", "Equipment Group", "装備グループ");
            Add("技能",   "Skill",           "スキル");
            Add("分组标签", "Group Tag",     "グループタグ");

            // ── 三列框架：组合模板 ────────────────────────────────────────────────
            Add("{0} Inspector", "{0} Inspector", "{0} インスペクター");
            Add("删除{0}",       "Delete {0}",    "{0}を削除");
            Add("（无可用{0}模板）",
                "(No {0} templates available)",
                "（利用可能な{0}テンプレートがありません）");

            // ── 工具栏 / 主列表 ───────────────────────────────────────────────────
            Add("数据文件",   "Data File",           "データファイル");
            Add("导出 JSON",  "Export JSON",         "JSON をエクスポート");
            Add("导出二进制", "Export Binary",       "バイナリをエクスポート");
            Add("从模板添加", "Add from Template",   "テンプレートから追加");
            Add("快速添加",   "Quick Add",           "クイック追加");
            Add("全部",       "All",                 "すべて");

            // ── 无数据库占位 ──────────────────────────────────────────────────────
            Add("请创建或选择一个 InventoryDatabase 数据文件",
                "Create or select an InventoryDatabase data file",
                "InventoryDatabase データファイルを作成または選択してください");
            Add("创建新的数据文件", "Create New Data File", "新規データファイル作成");

            // ── Inspector 共用片段 ───────────────────────────────────────────────
            Add("⚠ ID 重复或为空", "⚠ Duplicate or empty ID", "⚠ ID が重複または空です");
            Add("来源模板", "Source Template", "元テンプレート");
            Add("（无）",   "(None)",          "（なし）");
            Add("自定义属性", "Custom Attributes", "カスタム属性");
            Add("名称", "Name",        "名称");
            Add("描述", "Description",  "説明");
            Add("（暂无可用功能标签）",
                "(No function tags available)",
                "（利用可能な機能タグがありません）");

            // ── 分组标签面板 ──────────────────────────────────────────────────────
            Add("(空 ID)", "(Empty ID)", "（空の ID）");
            Add("新分组",  "New Group",  "新規グループ");
            Add("请选择或新建一个分组标签。",
                "Select or create a group tag.",
                "グループタグを選択または新規作成してください。");
            Add("基础信息",   "Basic Info", "基本情報");
            Add("标识颜色",   "Color",      "識別カラー");

            // ── 状态栏（重复 / 空 ID）────────────────────────────────────────────
            Add("(空ID)", "(Empty ID)", "（空 ID）");
            Add("⚠ {0}重复 ID：{1}（导出已禁用）",
                "⚠ Duplicate {0} ID: {1} (export disabled)",
                "⚠ {0} ID が重複：{1}（エクスポート無効）");
            Add("⚠ {0}存在空 ID（导出时将跳过）",
                "⚠ {0} has empty ID (skipped on export)",
                "⚠ {0} に空の ID があります（エクスポート時にスキップ）");

            // ── 创建 / 导出对话框 ─────────────────────────────────────────────────
            Add("创建仓库系统数据文件", "Create Inventory Database", "インベントリデータファイルを作成");
            Add("请选择数据文件保存位置", "Choose where to save the data file", "データファイルの保存先を選択してください");
            Add("导出为 JSON",   "Export as JSON",   "JSON としてエクスポート");
            Add("导出为二进制",  "Export as Binary", "バイナリとしてエクスポート");
            Add("已导出 JSON",   "Exported JSON",    "JSON をエクスポートしました");
            Add("已导出二进制",  "Exported Binary",  "バイナリをエクスポートしました");
            Add("无法导出",      "Cannot Export",    "エクスポートできません");
        }
    }
}

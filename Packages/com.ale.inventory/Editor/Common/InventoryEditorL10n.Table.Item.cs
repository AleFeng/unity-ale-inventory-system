namespace Ale.Inventory.Editor
{
    /// <summary>道具系统面板（<c>Editor/ItemSystem/*.cs</c>）的英 / 日译表。</summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterItem()
        {
            // ── 左列子页签 / 主列表名词 ──────────────────────────────────────────
            Add("枚举类型", "Enum Types",     "列挙型");
            Add("功能标签", "Function Tags",  "機能タグ");
            Add("道具模板", "Item Templates", "アイテムテンプレート");

            // ── 新建默认名 ────────────────────────────────────────────────────────
            Add("新枚举", "New Enum",     "新規列挙型");
            Add("新标签", "New Tag",      "新規タグ");
            Add("新模板", "New Template", "新規テンプレート");
            Add("新项",   "New Item",     "新規項目");

            // ── 枚举类型面板 ──────────────────────────────────────────────────────
            Add("请选择或新建一个枚举类型。",
                "Select or create an enum type.",
                "列挙型を選択または新規作成してください。");
            Add("枚举名称", "Enum Name", "列挙名");
            Add("枚举项属性字段", "Enum Item Attribute Fields", "列挙項目の属性フィールド");
            Add("▸ {0}  的属性值", "▸ {0} — attribute values", "▸ {0} の属性値");
            Add("枚举项（值由系统分配、只读；点击行选中以编辑属性值）",
                "Enum items (values are system-assigned and read-only; click a row to edit its attribute values)",
                "列挙項目（値はシステムが自動採番・読み取り専用。行をクリックして属性値を編集）");

            // ── 功能标签面板 ──────────────────────────────────────────────────────
            Add("请选择或新建一个功能标签。",
                "Select or create a function tag.",
                "機能タグを選択または新規作成してください。");
            Add("标签ID",       "Tag ID",                "タグ ID");
            Add("功能标签属性", "Function Tag Attributes", "機能タグの属性");
            Add("背景图",       "Background Sprite",     "背景画像");
            Add("背景颜色",     "Background Color",      "背景色");
            Add("UI中隐藏",     "Hide in UI",            "UI で非表示");
            Add("道具属性字段", "Item Attribute Fields", "アイテム属性フィールド");
            Add("附加到道具后，会自动添加至道具的「属性字段」列表中",
                "Once attached to an item, they are automatically added to the item's \"attribute fields\" list.",
                "アイテムに付加すると、アイテムの「属性フィールド」一覧に自動追加されます。");

            // ── 道具模板面板 ──────────────────────────────────────────────────────
            Add("请选择或新建一个道具模板。",
                "Select or create an item template.",
                "アイテムテンプレートを選択または新規作成してください。");
            Add("模板名称",     "Template Name",       "テンプレート名");
            Add("默认功能标签", "Default Function Tags", "既定の機能タグ");
            Add("（暂无功能标签，请先在左侧「功能标签」中创建）",
                "(No function tags yet; create them in \"Function Tags\" on the left first)",
                "（機能タグがありません。まず左の「機能タグ」で作成してください）");
            Add("仓库属性",     "Warehouse Attributes", "倉庫属性");
            Add("重量",         "Weight",              "重量");
            Add("堆叠上限",     "Stack Limit",         "スタック上限");
            Add("仓库中隐藏",   "Hide in Warehouse",   "倉庫で非表示");
            Add("属性字段",     "Attribute Fields",    "属性フィールド");

            // ── 道具列表（中列）──────────────────────────────────────────────────
            Add("（无可用模板）", "(No templates available)", "（利用可能なテンプレートがありません）");
            Add("是", "Yes", "はい");
            Add("否", "No",  "いいえ");
            Add("曲线({0}帧)", "Curve ({0} keys)", "カーブ（{0} フレーム）");

            // ── 道具 Inspector ────────────────────────────────────────────────────
            Add("请在中间列表选择一个道具。",
                "Select an item in the middle list.",
                "中央の一覧からアイテムを選択してください。");
            Add("⚠ ID 重复或为空（导出时空 ID 条目将被跳过）",
                "⚠ Duplicate or empty ID (entries with empty ID are skipped on export)",
                "⚠ ID が重複または空です（空 ID のエントリはエクスポート時にスキップされます）");
            Add("{0}  （由模板锁定）", "{0}  (locked by template)", "{0}  （テンプレートでロック）");
            Add("（0 = 无重量）", "(0 = no weight)", "（0 = 重量なし）");
            Add("（0 = 无上限）", "(0 = no limit)",  "（0 = 上限なし）");
            Add("属性", "Attributes", "属性");
            Add("⚠  该道具暂无属性字段。请先在左侧「道具模板」中添加自定义属性字段，" +
                "或为道具关联带属性定义的功能标签。",
                "⚠  This item has no attribute fields yet. Add custom attribute fields in \"Item Templates\" on the left, " +
                "or attach function tags that define attributes.",
                "⚠  このアイテムには属性フィールドがありません。左の「アイテムテンプレート」でカスタム属性フィールドを追加するか、" +
                "属性定義を持つ機能タグを付加してください。");
            Add("模板：{0}", "Template: {0}", "テンプレート：{0}");
            Add("⚠ 存在重复 ID（仅首个来源生效，其余来源已被忽略）：",
                "⚠ Duplicate IDs exist (only the first source applies; the rest are ignored):",
                "⚠ ID の重複があります（最初のソースのみ有効、残りは無視されます）：");
            Add("、", ", ", "、");
            Add("{0}  （模板锁定）", "{0}  (template-locked)", "{0}  （テンプレートロック）");
            Add("其他（来源已删除）", "Others (source deleted)", "その他（ソースが削除済み）");
        }
    }
}

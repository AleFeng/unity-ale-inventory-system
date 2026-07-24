namespace Ale.Inventory.Editor
{
    /// <summary>仓库系统面板（<c>Editor/InventorySystem/*.cs</c>）的英 / 日译表。</summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterInventory()
        {
            // ── 左列子页签 / 主列表名词 ──────────────────────────────────────────
            Add("整理选项", "Sort Options",       "整理オプション");
            Add("数字格式", "Number Format",      "数値フォーマット");
            Add("仓库模板", "Warehouse Template", "倉庫テンプレート");

            // ── 整理选项面板 ──────────────────────────────────────────────────────
            Add("道具ID",   "Item ID",     "アイテム ID");
            Add("功能页签", "Function Tab", "機能タブ");
            Add("列表包含所有可用的排序字段，不可手动增删。",
                "The list contains all available sort fields and cannot be edited manually.",
                "一覧には利用可能なすべてのソートフィールドが含まれ、手動での追加・削除はできません。");
            Add("（暂无排序字段，请先添加道具模板属性或功能标签）",
                "(No sort fields yet; add item-template attributes or function tags first)",
                "（ソートフィールドがありません。まずアイテムテンプレート属性か機能タグを追加してください）");
            Add("（请在左侧选择一个整理选项）",
                "(Select a sort option on the left)",
                "（左側で整理オプションを選択してください）");
            Add("名称（排序下拉显示名；为空时用字段名）",
                "Name (display name in the sort dropdown; falls back to the field name if empty)",
                "名称（ソートドロップダウンの表示名。空欄の場合はフィールド名）");
            Add("忽略ID", "Ignore IDs", "無視 ID");
            Add("排序时跳过这些条目（按道具ID排序 = 道具ID；功能页签 = 标签名；按属性排序 = 属性值）。",
                "Skip these entries when sorting (sort by item ID = item ID; function tab = tag name; sort by attribute = attribute value).",
                "ソート時にこれらのエントリをスキップします（アイテム ID ソート = アイテム ID、機能タブ = タグ名、属性ソート = 属性値）。");
            Add("（未配置）", "(Not configured)", "（未設定）");

            // ── 数字格式面板 ──────────────────────────────────────────────────────
            Add("（未命名）", "(Unnamed)",    "（名称未設定）");
            Add("新格式",     "New Format",   "新規フォーマット");
            Add("请选择或新建一个数字格式配置。",
                "Select or create a number-format config.",
                "数値フォーマット設定を選択または新規作成してください。");
            Add("配置名称", "Config Name", "設定名");
            Add("⚠ 名称为空时无法被引用。",
                "⚠ A config with an empty name cannot be referenced.",
                "⚠ 名称が空の設定は参照できません。");
            Add("⚠ 名称重复，引用将命中第一个同名配置。",
                "⚠ Duplicate name; references will match the first config with this name.",
                "⚠ 名称が重複しています。参照は最初の同名設定に一致します。");
            Add("语言与规则", "Languages & Rules", "言語とルール");

            // ── 仓库模板 / Inspector 共用 ─────────────────────────────────────────
            Add("请选择或新建一个仓库模板。",
                "Select or create a warehouse template.",
                "倉庫テンプレートを選択または新規作成してください。");
            Add("请在中间列表选中一个仓库。",
                "Select a warehouse in the middle list.",
                "中央の一覧から倉庫を選択してください。");
            Add("基础属性",   "Basic Attributes", "基本属性");
            Add("容量上限",   "Capacity Limit",   "容量上限");
            Add("重量上限",   "Weight Limit",     "重量上限");
            Add("放入功能标签", "Put-in Function Tags",   "格納機能タグ");
            Add("取出功能标签", "Take-out Function Tags", "取り出し機能タグ");
            Add("操作功能标签", "Operate Function Tags",  "操作機能タグ");
            Add("过滤设置", "Filter Settings", "フィルタ設定");
            Add("过滤列表（UI 中以标签按钮形式显示）：",
                "Filter list (shown as tag buttons in the UI):",
                "フィルタ一覧（UI ではタグボタンとして表示）：");
            Add("勾选后 UI 过滤页签栏会显示「全部」页签（默认选中、不过滤）；" +
                "取消后不显示「全部」，默认选中第一个过滤标签。",
                "When checked, the UI filter tab bar shows an \"All\" tab (selected by default, no filtering); " +
                "when unchecked, \"All\" is hidden and the first filter tag is selected by default.",
                "チェックすると UI のフィルタタブバーに「すべて」タブが表示されます（既定で選択・フィルタなし）。" +
                "チェックを外すと「すべて」は非表示になり、最初のフィルタタグが既定で選択されます。");
            Add("整理设置",     "Sort Settings",         "整理設定");
            Add("允许拖拽整理", "Allow drag-to-sort",    "ドラッグ整理を許可");
            Add("自动整理",     "Auto sort",             "自動整理");
            Add("UI 配置",      "UI Config",             "UI 設定");
            Add("自定义属性字段", "Custom Attribute Fields", "カスタム属性フィールド");
            Add("⚠  该仓库暂无自定义属性字段。请先在左侧「仓库模板」中添加属性字段，" +
                "再为仓库选择对应模板。",
                "⚠  This warehouse has no custom attribute fields yet. Add attribute fields in \"Warehouse Templates\" on the left, " +
                "then assign the matching template to the warehouse.",
                "⚠  この倉庫にはカスタム属性フィールドがありません。左の「倉庫テンプレート」で属性フィールドを追加し、" +
                "倉庫に対応するテンプレートを割り当ててください。");

            // ── 仓库列表（中列）列头 ─────────────────────────────────────────────
            Add("放入", "Put",     "格納");
            Add("取出", "Take",    "取出");
            Add("操作", "Operate", "操作");
        }
    }
}

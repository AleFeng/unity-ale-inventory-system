namespace Ale.Inventory.Editor
{
    /// <summary>装备系统面板与配置绘制器（<c>Editor/EquipmentSystem/*.cs</c>、<c>EquipmentConfigDrawer</c>）的英 / 日译表。</summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterEquipment()
        {
            // ── 装备组模板 / Inspector ────────────────────────────────────────────
            Add("装备组模板",   "Equipment Group Template",     "装備グループテンプレート");
            Add("新装备组模板", "New Equipment Group Template", "新規装備グループテンプレート");
            Add("请选择或新建一个装备组模板。",
                "Select or create an equipment-group template.",
                "装備グループテンプレートを選択または新規作成してください。");
            Add("请在中间列表选中一个装备组。",
                "Select an equipment group in the middle list.",
                "中央の一覧から装備グループを選択してください。");
            Add("（该装备组暂无自定义属性字段；可在左侧「装备组模板」中添加）",
                "(This equipment group has no custom attribute fields yet; add them in \"Equipment Group Templates\" on the left)",
                "（この装備グループにはカスタム属性フィールドがありません。左の「装備グループテンプレート」で追加できます）");

            // ── 装备组列表列头 ────────────────────────────────────────────────────
            Add("槽组", "Slot Groups", "スロット組");

            // ── 装备配置绘制器 ────────────────────────────────────────────────────
            Add("装备仓库", "Equipment Warehouse", "装備倉庫");
            Add("装备系统 / 装备 UI 可直接交互的仓库；卸下装备时从上到下（Index0 起）找第一个放得下的仓库。",
                "Warehouses the equipment system / equipment UI can interact with directly; when unequipping, the first warehouse (from top, index 0) with room is used.",
                "装備システム／装備 UI が直接操作できる倉庫。装備を外す際は上から（Index0 から）最初に収まる倉庫を探します。");
            Add("可装备道具候选列表按此排序（候选列表有排序栏时玩家可选并升降序，否则以首条为默认排序）：",
                "The equippable-item candidate list is sorted by this (if the candidate list has a sort bar, players can pick and toggle asc/desc; otherwise the first condition is the default sort):",
                "装備可能アイテムの候補一覧はこの順で並べ替えられます（候補一覧にソートバーがある場合はプレイヤーが選択・昇順降順を切り替え可能。ない場合は先頭が既定のソート）：");

            Add("槽位列表",       "Slot List",        "スロットリスト");
            Add("+ 添加槽位列表", "+ Add Slot List",  "+ スロットリストを追加");
            Add("新槽位列表",     "New Slot List",    "新規スロットリスト");
            Add("槽位列表 {0}",   "Slot List {0}",    "スロットリスト {0}");
            Add("详细配置",       "Details",          "詳細設定");
            Add("道具限制（功能标签与枚举约束需全部满足）",
                "Item restrictions (all function tags and enum constraints must be satisfied)",
                "アイテム制限（機能タグと列挙制約をすべて満たす必要があります）");
            Add("（未限制）", "(No restriction)", "（制限なし）");
            Add("（无可添加的功能标签）",
                "(No function tags available to add)",
                "（追加できる機能タグがありません）");
            Add("枚举约束",       "Enum Constraint",         "列挙制約");
            Add("（先选枚举类型）", "(Select an enum type first)", "（先に列挙型を選択）");
            Add("（任意值）",     "(Any value)",             "（任意の値）");
            Add("装备槽",         "Equipment Slot",          "装備スロット");
            Add("+ 添加装备槽",   "+ Add Equipment Slot",    "+ 装備スロットを追加");
            Add("新槽位",         "New Slot",                "新規スロット");
            Add("装备槽 {0}",     "Equipment Slot {0}",      "装備スロット {0}");
            Add("槽位过滤条件（需全部满足）",
                "Slot filter conditions (all must be satisfied)",
                "スロットフィルタ条件（すべて満たす必要があります）");
            Add("属性ID", "Attribute ID", "属性 ID");
            Add("⚠ 未找到属性定义（无法编辑期望值）",
                "⚠ Attribute definition not found (cannot edit expected value)",
                "⚠ 属性定義が見つかりません（期待値を編集できません）");
            Add("期望值", "Expected Value", "期待値");
            Add("装备属性字段", "Equipment Attribute Field", "装備属性フィールド");
            Add("指定道具属性作为装备组总属性加成；拖拽左侧句柄调整顺序。",
                "Specify item attributes as the equipment group's total attribute bonuses; drag the left handle to reorder.",
                "アイテム属性を装備グループの合計属性ボーナスとして指定します。左のハンドルをドラッグして順序を調整します。");
            Add("显示名（可选）", "Display Name (optional)", "表示名（省略可）");
            Add("（清空）", "(Clear)",    "（クリア）");
            Add("(模板)",   "(Template)", "（テンプレート）");
            Add("(标签)",   "(Tag)",      "（タグ）");
            Add("（枚举项名称）", "(Enum item name)", "（列挙項目名）");
            Add("字符串", "String", "文字列");
            Add("文本",   "Text",   "テキスト");
            Add("{0}（已失效）", "{0} (invalid)", "{0}（無効）");
            Add("显示名来源（枚举字段）",
                "Display-name source (enum field)",
                "表示名ソース（列挙フィールド）");
        }
    }
}

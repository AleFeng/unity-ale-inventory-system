namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 共享属性 / 杂项绘制器（<c>AttributeFieldDrawer</c>、<c>AttributeDefinition*Drawer</c>、
    /// <c>SortSettingsDrawer</c>、<c>NumberFormatConfigDrawer</c>、<c>InventoryRefListDrawer</c>）的英 / 日译表。
    /// </summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterDrawers()
        {
            // ── 整理设置绘制器 ────────────────────────────────────────────────────
            Add("整理列表（玩家在 UI 中通过下拉菜单选择排序条件）",
                "Sort list (players pick a sort condition from a dropdown in the UI)",
                "整理リスト（プレイヤーが UI のドロップダウンでソート条件を選択）");
            Add("整理优先级（整理列表条件值相同时，依次对比此列表直至值不同）",
                "Sort priority (when the sort-list condition ties, this list is compared in order until values differ)",
                "整理優先度（整理リストの条件値が同じ場合、この一覧を順に比較して値が異なるまで判定）");
            Add("道具 ID", "Item ID", "アイテム ID");
            Add("升序", "Asc",  "昇順");
            Add("降序", "Desc", "降順");

            // ── 数字格式绘制器 ────────────────────────────────────────────────────
            Add("阈值",   "Threshold", "しきい値");
            Add("除数",   "Divisor",   "除数");
            Add("小数位", "Decimals",  "小数位");
            Add("None", "None", "なし");
            Add("（未命名 {0}）", "(Unnamed {0})", "（名称未設定 {0}）");
            Add("语言 {0}（默认回退）", "Language {0} (default fallback)", "言語 {0}（既定フォールバック）");
            Add("语言 {0}",           "Language {0}",                    "言語 {0}");
            Add("+ 添加语言", "+ Add Language", "+ 言語を追加");
            Add("+ 添加规则", "+ Add Rule",     "+ ルールを追加");
            Add("后缀", "Suffix", "接尾辞");

            // ── 仓库引用列表绘制器 ────────────────────────────────────────────────
            Add("（无可添加的仓库）",
                "(No warehouses available to add)",
                "（追加できる倉庫がありません）");

            // ── 属性定义绘制器 ────────────────────────────────────────────────────
            Add("数组",   "Array",   "配列");
            Add("默认值", "Default Value", "既定値");
            Add("默认",   "Default", "既定");
            Add("{0}（未知类型）", "{0} (unknown type)", "{0}（不明な型）");
            Add("添加字段", "Add Field", "フィールドを追加");

            // ── 属性值绘制器 ──────────────────────────────────────────────────────
            Add("添加", "Add", "追加");
            Add("<未找到枚举类型 \"{0}\">",
                "<Enum type \"{0}\" not found>",
                "<列挙型「{0}」が見つかりません>");
            Add("本地化", "Localized", "ローカライズ");
            Add("复制属性值  Ctrl+C", "Copy Value  Ctrl+C",  "値をコピー  Ctrl+C");
            Add("粘贴属性值  Ctrl+V", "Paste Value  Ctrl+V", "値を貼り付け  Ctrl+V");
        }
    }
}

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 通用绘制器的英 / 日译表：属性定义 / 属性值绘制器（<c>AttributeFieldDrawer</c> /
    /// <c>AttributeDefinition*Drawer</c>）、数字格式绘制器（<c>NumberFormatConfigDrawer</c>）、
    /// 升降序、引用列表绘制器、分组标签（<c>GroupTag</c>）相关的通用文案。以中文原文为键。
    /// </summary>
    public static partial class ToolkitEditorL10n
    {
        static partial void RegisterDrawers()
        {
            // ── 升降序 ────────────────────────────────────────────────────────────
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

            // ── 引用列表绘制器 ────────────────────────────────────────────────────
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

            // ── 分组标签（GroupTag）─────────────────────────────────────────────
            Add("（暂无分组标签；请在左侧「分组标签」中添加）",
                "(No group tags yet; add them in \"Group Tags\" on the left)",
                "（グループタグがありません。左の「グループタグ」で追加してください）");
            Add("主分组标签", "Main Group Tag",       "主グループタグ");
            Add("副分组标签", "Secondary Group Tags", "副グループタグ");
            Add("（未添加）", "(None added)",         "（未追加）");
            Add("（无可添加的分组标签）",
                "(No group tags available to add)",
                "（追加できるグループタグがありません）");
        }
    }
}

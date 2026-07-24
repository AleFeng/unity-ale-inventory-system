namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 技能系统面板与配置绘制器（<c>Editor/SkillSystem/*.cs</c>、<c>SkillConfigDrawer</c>）的英 / 日译表。
    /// 分组标签相关文案（主/副分组标签、（未添加）、（无可添加的分组标签）等）与制作系统共用，已在制作译表登记。
    /// </summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterSkill()
        {
            // ── 技能模板 ──────────────────────────────────────────────────────────
            Add("技能模板",   "Skill Template",     "スキルテンプレート");
            Add("新技能模板", "New Skill Template", "新規スキルテンプレート");
            Add("请选择或新建一个技能模板。",
                "Select or create a skill template.",
                "スキルテンプレートを選択または新規作成してください。");
            Add("技能默认信息（从模板创建时复制）",
                "Skill defaults (copied when creating from the template)",
                "スキル既定情報（テンプレートから作成時にコピー）");

            // ── 技能 Inspector ────────────────────────────────────────────────────
            Add("请在中间列表选中一个技能。",
                "Select a skill in the middle list.",
                "中央の一覧からスキルを選択してください。");
            Add("（该技能暂无自定义属性字段；可在左侧「技能模板」中添加）",
                "(This skill has no custom attribute fields yet; add them in \"Skill Templates\" on the left)",
                "（このスキルにはカスタム属性フィールドがありません。左の「スキルテンプレート」で追加できます）");

            // ── 技能共享配置 ──────────────────────────────────────────────────────
            Add("图标", "Icon", "アイコン");
        }
    }
}

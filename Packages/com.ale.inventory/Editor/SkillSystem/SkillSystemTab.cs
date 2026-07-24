using System.Collections.Generic;
using Ale.Inventory.Runtime;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「技能系统」页签：三列布局，与制作系统页签对称。
    /// 左列 = 子页签（分组标签 / 技能模板）+ 主列表；中列 = 技能列表；右列 = 上下文 Inspector。
    /// 布局与选中互斥逻辑由 <see cref="EditorThreeColumnTab{TEntity}"/> 提供。
    /// </summary>
    public class SkillSystemTab : EditorThreeColumnTab<Skill>
    {
        private readonly SkillGroupTagPanel  _groupTagPanel  = new SkillGroupTagPanel();
        private readonly SkillTemplatePanel  _templatePanel  = new SkillTemplatePanel();
        private readonly SkillListPanel      _listPanel      = new SkillListPanel();
        private readonly SkillInspectorPanel _inspectorPanel = new SkillInspectorPanel();

        private IEditorMasterListPanel[] _leftPanels;

        protected override string[] LeftSubTabs => new[] { Tr("分组标签"), Tr("技能模板") };

        protected override IEditorMasterListPanel[] LeftPanels
            => _leftPanels ??= new IEditorMasterListPanel[] { _groupTagPanel, _templatePanel };

        protected override string EntityNoun => "技能";

        protected override List<Skill> EntityList(InventoryDatabase db) => db.Skills;

        protected override Skill DrawEntityList(IInventoryEditorContext ctx, Skill displaySelected)
            => _listPanel.DrawList(ctx, displaySelected);

        protected override Skill ConsumePendingSelect() => _listPanel.ConsumePendingSelect();

        protected override void DrawEntityInspector(IInventoryEditorContext ctx, Skill entity)
            => _inspectorPanel.DrawInspector(ctx, entity);
    }
}

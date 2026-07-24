using System.Collections.Generic;
using Ale.Inventory.Runtime;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「制作系统」页签：三列布局，与商店系统页签对称。
    /// 左列 = 子页签（分组标签 / 蓝图模板）+ 主列表；中列 = 蓝图列表；右列 = 上下文 Inspector。
    /// 布局与选中互斥逻辑由 <see cref="EditorThreeColumnTab{TEntity}"/> 提供。
    /// </summary>
    public class CraftingSystemTab : EditorThreeColumnTab<CraftingBlueprint>
    {
        private readonly CraftingGroupTagPanel  _groupTagPanel  = new CraftingGroupTagPanel();
        private readonly CraftingTemplatePanel  _templatePanel  = new CraftingTemplatePanel();
        private readonly CraftingListPanel      _listPanel      = new CraftingListPanel();
        private readonly CraftingInspectorPanel _inspectorPanel = new CraftingInspectorPanel();

        private IEditorMasterListPanel[] _leftPanels;

        protected override string[] LeftSubTabs => new[] { Tr("分组标签"), Tr("蓝图模板") };

        protected override IEditorMasterListPanel[] LeftPanels
            => _leftPanels ??= new IEditorMasterListPanel[] { _groupTagPanel, _templatePanel };

        protected override string EntityNoun => "蓝图";

        protected override List<CraftingBlueprint> EntityList(InventoryDatabase db) => db.CraftingBlueprints;

        protected override CraftingBlueprint DrawEntityList(IInventoryEditorContext ctx, CraftingBlueprint displaySelected)
            => _listPanel.DrawList(ctx, displaySelected);

        protected override CraftingBlueprint ConsumePendingSelect() => _listPanel.ConsumePendingSelect();

        protected override void DrawEntityInspector(IInventoryEditorContext ctx, CraftingBlueprint entity)
            => _inspectorPanel.DrawInspector(ctx, entity);
    }
}

using System.Collections.Generic;
using Ale.Inventory.Runtime;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「装备系统」页签：三列布局，与制作 / 商店系统页签对称。
    /// 左列 = 子页签（分组标签 / 装备组模板）+ 主列表；中列 = 装备组列表；右列 = 上下文 Inspector。
    /// 布局与选中互斥逻辑由 <see cref="EditorThreeColumnTab{TEntity}"/> 提供。
    /// </summary>
    public class EquipmentSystemTab : EditorThreeColumnTab<EquipmentGroup>
    {
        private readonly EquipmentGroupTagPanel  _groupTagPanel  = new EquipmentGroupTagPanel();
        private readonly EquipmentTemplatePanel  _templatePanel  = new EquipmentTemplatePanel();
        private readonly EquipmentListPanel      _listPanel      = new EquipmentListPanel();
        private readonly EquipmentInspectorPanel _inspectorPanel = new EquipmentInspectorPanel();

        private IEditorMasterListPanel[] _leftPanels;

        protected override string[] LeftSubTabs => new[] { "分组标签", "装备组模板" };

        protected override IEditorMasterListPanel[] LeftPanels
            => _leftPanels ??= new IEditorMasterListPanel[] { _groupTagPanel, _templatePanel };

        protected override string EntityNoun        => "装备组";
        protected override float  DeleteButtonWidth => 72f;   // 「删除装备组」较长，沿用原有的更宽按钮

        protected override List<EquipmentGroup> EntityList(InventoryDatabase db) => db.EquipmentGroups;

        protected override EquipmentGroup DrawEntityList(IInventoryEditorContext ctx, EquipmentGroup displaySelected)
            => _listPanel.DrawList(ctx, displaySelected);

        protected override EquipmentGroup ConsumePendingSelect() => _listPanel.ConsumePendingSelect();

        protected override void DrawEntityInspector(IInventoryEditorContext ctx, EquipmentGroup entity)
            => _inspectorPanel.DrawInspector(ctx, entity);
    }
}

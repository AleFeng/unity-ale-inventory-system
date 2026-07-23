using System.Collections.Generic;
using Ale.Inventory.Runtime;

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;

    /// <summary>
    /// 「仓库系统」页签：三列布局。
    /// 左列 = 子页签（整理选项 / 数字格式 / 仓库模板）+ 主列表；中列 = 仓库列表；右列 = 上下文 Inspector。
    /// 布局与选中互斥逻辑由 <see cref="EditorThreeColumnTab{TEntity}"/> 提供。
    /// </summary>
    public class InventorySystemTab : EditorThreeColumnTab<Inventory>
    {
        private readonly SortOptionPanel         _sortOptionPanel   = new SortOptionPanel();
        private readonly NumberFormatConfigPanel _numberFormatPanel = new NumberFormatConfigPanel();
        private readonly InventoryTemplatePanel  _templatePanel     = new InventoryTemplatePanel();
        private readonly InventoryListPanel      _listPanel         = new InventoryListPanel();
        private readonly InventoryInspectorPanel _inspectorPanel    = new InventoryInspectorPanel();

        private IEditorMasterListPanel[] _leftPanels;

        protected override string[] LeftSubTabs => new[] { "整理选项", "数字格式", "仓库模板" };

        protected override IEditorMasterListPanel[] LeftPanels
            => _leftPanels ??= new IEditorMasterListPanel[]
                { _sortOptionPanel, _numberFormatPanel, _templatePanel };

        protected override string EntityNoun => "仓库";

        protected override List<Inventory> EntityList(InventoryDatabase db) => db.Inventories;

        protected override Inventory DrawEntityList(IInventoryEditorContext ctx, Inventory displaySelected)
            => _listPanel.DrawList(ctx, displaySelected);

        protected override Inventory ConsumePendingSelect() => _listPanel.ConsumePendingSelect();

        protected override void DrawEntityInspector(IInventoryEditorContext ctx, Inventory entity)
            => _inspectorPanel.DrawInspector(ctx, entity);
    }
}

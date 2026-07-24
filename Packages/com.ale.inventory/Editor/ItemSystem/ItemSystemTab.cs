using System.Collections.Generic;
using Ale.Inventory.Runtime;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「道具系统」页签：三列布局。
    /// 左列 = 子页签（枚举类型 / 功能标签 / 道具模板）+ 主列表；中列 = 道具列表；右列 = 上下文 Inspector。
    ///
    /// 选中互斥规则：左侧选中时清空中列选中，中列选中时清空左侧选中索引。
    /// 这样保证两侧各自都能被「再次点击选中」，不会因引用/索引相同而跳过变化检测。
    /// 布局与该互斥逻辑由 <see cref="EditorThreeColumnTab{TEntity}"/> 提供。
    /// </summary>
    public class ItemSystemTab : EditorThreeColumnTab<Item>
    {
        private readonly EnumTypePanel      _enumPanel          = new EnumTypePanel();
        private readonly FunctionTagPanel   _tagPanel           = new FunctionTagPanel();
        private readonly ItemTemplatePanel  _templatePanel      = new ItemTemplatePanel();
        private readonly ItemListPanel      _itemListPanel      = new ItemListPanel();
        private readonly ItemInspectorPanel _itemInspectorPanel = new ItemInspectorPanel();

        private IEditorMasterListPanel[] _leftPanels;

        protected override string[] LeftSubTabs => new[] { Tr("枚举类型"), Tr("功能标签"), Tr("道具模板") };

        protected override IEditorMasterListPanel[] LeftPanels
            => _leftPanels ??= new IEditorMasterListPanel[] { _enumPanel, _tagPanel, _templatePanel };

        protected override string EntityNoun        => "道具";
        protected override float  DeleteButtonWidth => 68f;

        protected override List<Item> EntityList(InventoryDatabase db) => db.Items;

        protected override Item DrawEntityList(IInventoryEditorContext ctx, Item displaySelected)
            => _itemListPanel.DrawList(ctx, displaySelected);

        protected override Item ConsumePendingSelect() => _itemListPanel.ConsumePendingSelect();

        protected override void DrawEntityInspector(IInventoryEditorContext ctx, Item entity)
            => _itemInspectorPanel.DrawInspector(ctx, entity);
    }
}

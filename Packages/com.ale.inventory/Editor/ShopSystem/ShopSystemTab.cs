using System.Collections.Generic;
using Ale.Inventory.Runtime;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「商店系统」页签：三列布局。
    /// 左列 = 商店模板主列表（只有一个子面板，故不绘制子页签工具栏）；中列 = 商店列表；右列 = 上下文 Inspector。
    /// 布局与选中互斥逻辑由 <see cref="EditorThreeColumnTab{TEntity}"/> 提供。
    /// </summary>
    public class ShopSystemTab : EditorThreeColumnTab<Shop>
    {
        private readonly ShopTemplatePanel  _templatePanel  = new ShopTemplatePanel();
        private readonly ShopListPanel      _listPanel      = new ShopListPanel();
        private readonly ShopInspectorPanel _inspectorPanel = new ShopInspectorPanel();

        private IEditorMasterListPanel[] _leftPanels;

        protected override IEditorMasterListPanel[] LeftPanels
            => _leftPanels ??= new IEditorMasterListPanel[] { _templatePanel };

        protected override string EntityNoun => "商店";

        protected override List<Shop> EntityList(InventoryDatabase db) => db.Shops;

        protected override Shop DrawEntityList(IInventoryEditorContext ctx, Shop displaySelected)
            => _listPanel.DrawList(ctx, displaySelected);

        protected override Shop ConsumePendingSelect() => _listPanel.ConsumePendingSelect();

        protected override void DrawEntityInspector(IInventoryEditorContext ctx, Shop entity)
            => _inspectorPanel.DrawInspector(ctx, entity);
    }
}

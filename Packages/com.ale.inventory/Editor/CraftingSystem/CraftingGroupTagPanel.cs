using System.Collections.Generic;
using Ale.Inventory.Runtime;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 制作-分组标签面板：左侧主列表（分组标签行，可拖拽排序）+ 右侧 Inspector（ID / 名称 / 描述 / 本地化 / 色点）。
    /// 分组标签用于对蓝图分组（蓝图选 1 主分组 + 多副分组），仅承载基础信息。
    /// 绘制逻辑全部来自 <see cref="EditorGroupTagPanel{T}"/>。
    /// </summary>
    public class CraftingGroupTagPanel : EditorGroupTagPanel<CraftingGroupTag>
    {
        protected override List<CraftingGroupTag> GetList(InventoryDatabase db) => db.CraftingGroupTags;
        protected override string                 IdPrefix                     => "group_";
    }
}

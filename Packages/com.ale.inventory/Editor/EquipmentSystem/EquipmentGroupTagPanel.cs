using System.Collections.Generic;
using Ale.Inventory.Runtime;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 装备-分组标签面板：左侧主列表（分组标签行，可拖拽排序）+ 右侧 Inspector（ID / 名称 / 描述 / 本地化 / 色点）。
    /// 分组标签用于对装备组的「装备属性字段」条目分组显示，仅承载基础信息。
    /// 绘制逻辑全部来自 <see cref="EditorGroupTagPanel{T}"/>。
    /// </summary>
    public class EquipmentGroupTagPanel : EditorGroupTagPanel<EquipmentGroupTag>
    {
        protected override List<EquipmentGroupTag> GetList(InventoryDatabase db) => db.EquipmentGroupTags;
        protected override string                  IdPrefix                     => "equip_tag_";
    }
}

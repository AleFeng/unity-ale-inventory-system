using System.Collections.Generic;
using Ale.Inventory.Runtime;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 技能-分组标签面板：左侧主列表（分组标签行，可拖拽排序）+ 右侧 Inspector（ID / 名称 / 描述 / 本地化 / 色点）。
    /// 分组标签用于对技能分组（技能选 1 主分组 + 多副分组），仅承载基础信息。
    /// 绘制逻辑全部来自 <see cref="EditorGroupTagPanel{T}"/>。
    /// </summary>
    public class SkillGroupTagPanel : EditorGroupTagPanel<SkillGroupTag>
    {
        protected override List<SkillGroupTag> GetList(InventoryDatabase db) => db.SkillGroupTags;
        protected override string              IdPrefix                     => "skill_group_";
    }
}

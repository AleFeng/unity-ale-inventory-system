using System.Collections.Generic;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 技能<b>顺序</b>列表（虚拟滚动，单列纵向）。以 <see cref="UiwSkillEntry"/> 为条目，
    /// 在通用顺序虚拟滚动列表 <see cref="UiwInventoryOrderList{TData,TCell}"/> 之上，
    /// 负责「把 <see cref="Skill"/> 显示到技能条目」。技能无堆叠 / 拖拽 / 容量概念，故只需绑定与清空。
    /// </summary>
    public class UiwSkillOrderList : UiwInventoryOrderList<Skill, UiwSkillEntry>
    {
        /// <summary>设置要显示的技能列表（虚拟滚动池化复用条目）。</summary>
        public void SetSkills(IReadOnlyList<Skill> skills) => SetItems(skills);

        /// <summary>清空显示（隐藏所有条目）。</summary>
        public void Clear() => SetItems(null);
        
        /// <summary>
        /// 绑定 技能条目格子。将 <see cref="Skill"/> 显示到 <see cref="UiwSkillEntry"/> 上。
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="skill"></param>
        protected override void BindCell(UiwSkillEntry cell, Skill skill) => cell.SetSkill(skill);
        
        /// <summary>
        /// 清空 技能条目格子。将 <see cref="UiwSkillEntry"/> 清空并隐藏。
        /// </summary>
        /// <param name="cell"></param>
        protected override void ClearCell(UiwSkillEntry cell)
        {
            cell.Clear();
            cell.gameObject.SetActive(false);
        }
    }
}

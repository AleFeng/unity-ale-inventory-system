using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 单个角色的运行时已学技能状态：该角色已学会的技能 ID 列表（保持学习顺序）。
    /// 由 <see cref="SkillRuntimeManager"/> 维护，并作为存档单元（<see cref="SkillRuntimeManager.GetSaveData"/>）。
    /// </summary>
    [Serializable]
    public class RuntimeLearnedSkillState
    {
        /// <summary>角色 ID（由游戏层自行约定，用于区分多角色）。</summary>
        public string characterId;

        /// <summary>已学技能 ID 列表（引用 <see cref="Skill.id"/>，保持学习顺序、去重）。</summary>
        public List<string> skillIds = new List<string>();

        public RuntimeLearnedSkillState() { }

        public RuntimeLearnedSkillState(string characterId)
        {
            this.characterId = characterId;
        }

        /// <summary>深拷贝。</summary>
        public RuntimeLearnedSkillState Clone()
        {
            var clone = new RuntimeLearnedSkillState(characterId);
            clone.skillIds.AddRange(skillIds);
            return clone;
        }
    }
}

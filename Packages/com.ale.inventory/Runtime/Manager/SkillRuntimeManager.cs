using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 技能系统运行时管理器（非 MonoBehaviour 单例，首次访问自动创建）。
    ///
    /// <para>职责：维护「已学技能」可变状态——按 <b>角色 ID → 已学技能 ID 列表</b> 记录（支持多角色），
    /// 提供学习 / 遗忘 / 查询 / 存档接口。技能目录来自已注册数据库（经 <see cref="InventoryDataManager"/> 查询）。</para>
    ///
    /// <para>无状态的三种技能来源（数据库 / 装备 / 仓库）由 <see cref="SkillCollector"/> 采集；
    /// 「角色已学技能」来源（<see cref="ESkillSource.Character"/>）则读取本管理器。
    /// 已学技能变化时触发 <see cref="OnLearnedChanged"/> 供技能 UI 刷新。</para>
    /// </summary>
    public class SkillRuntimeManager
        : InventorySystemSingleton<SkillRuntimeManager>, IInventorySaveable<RuntimeLearnedSkillState>
    {
        /// <summary>角色 ID → 已学技能 ID 列表（保持学习顺序、去重）。按需创建；无已学技能的角色不入字典。</summary>
        private readonly Dictionary<string, List<string>> _learned
            = new Dictionary<string, List<string>>();

        /// <summary>某角色已学技能发生变化时触发。参数为 characterId。供技能 UI 刷新。</summary>
        public event Action<string> OnLearnedChanged;

        protected override void Init()
        {
            // 已学技能初始为空，由游戏层学习 / 存档恢复填充，无需预初始化。
        }

        #region 查询

        /// <summary>某角色是否已学会指定技能。</summary>
        public bool HasLearned(string characterId, string skillId)
        {
            return !string.IsNullOrEmpty(characterId) && !string.IsNullOrEmpty(skillId)
                && _learned.TryGetValue(characterId, out var list) && list.Contains(skillId);
        }

        /// <summary>获取某角色的已学技能 ID 列表（只读；未学任何技能返回空）。</summary>
        public IReadOnlyList<string> GetLearnedSkillIds(string characterId)
        {
            if (!string.IsNullOrEmpty(characterId) && _learned.TryGetValue(characterId, out var list))
                return list;
            return Array.Empty<string>();
        }

        /// <summary>解析某角色的已学技能为技能对象（跳过解析不到的 ID，保持学习顺序）。</summary>
        public List<Skill> GetLearnedSkills(string characterId)
        {
            var result = new List<Skill>();
            var dm     = InventoryDataManager.Instance;
            if (dm == null || string.IsNullOrEmpty(characterId)) return result;
            if (!_learned.TryGetValue(characterId, out var list)) return result;

            foreach (var id in list)
            {
                var skill = dm.GetSkill(id);
                if (skill != null) result.Add(skill);
            }
            return result;
        }

        #endregion

        #region 学习 / 遗忘

        /// <summary>为角色学习一个技能（已学则忽略）。返回是否发生变化。</summary>
        public bool Learn(string characterId, string skillId)
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(skillId)) return false;

            if (!_learned.TryGetValue(characterId, out var list))
                _learned[characterId] = list = new List<string>();
            if (list.Contains(skillId)) return false;

            list.Add(skillId);
            OnLearnedChanged?.Invoke(characterId);
            return true;
        }

        /// <summary>让角色遗忘一个技能。返回是否发生变化。</summary>
        public bool Forget(string characterId, string skillId)
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(skillId)) return false;
            if (!_learned.TryGetValue(characterId, out var list)) return false;
            if (!list.Remove(skillId)) return false;

            OnLearnedChanged?.Invoke(characterId);
            return true;
        }

        /// <summary>清空某角色的全部已学技能。</summary>
        public void ClearLearned(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            if (_learned.Remove(characterId))
                OnLearnedChanged?.Invoke(characterId);
        }

        #endregion

        #region 存档

        /// <inheritdoc cref="IInventorySaveable{TState}.GetSaveData"/>
        public List<RuntimeLearnedSkillState> GetSaveData()
        {
            var result = new List<RuntimeLearnedSkillState>(_learned.Count);
            foreach (var kv in _learned)
            {
                var st = new RuntimeLearnedSkillState(kv.Key);
                st.skillIds.AddRange(kv.Value);
                result.Add(st);
            }
            return result;
        }

        /// <inheritdoc cref="IInventorySaveable{TState}.LoadSaveData"/>
        public void LoadSaveData(List<RuntimeLearnedSkillState> data)
        {
            _learned.Clear();
            if (data == null) return;
            foreach (var st in data)
            {
                if (st == null || string.IsNullOrEmpty(st.characterId)) continue;
                var list = new List<string>();
                foreach (var id in st.skillIds)
                    if (!string.IsNullOrEmpty(id) && !list.Contains(id))
                        list.Add(id);
                _learned[st.characterId] = list;
            }
        }

        /// <inheritdoc cref="IInventorySaveable.ResetAll"/>
        public void ResetAll() => _learned.Clear();

        #endregion
    }
}

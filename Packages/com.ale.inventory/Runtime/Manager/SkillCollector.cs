using System.Collections.Generic;

namespace InventorySystem.Runtime
{
    /// <summary>技能信息来源：决定运行时技能 UI 从何处采集要显示的技能集合。</summary>
    public enum ESkillSource
    {
        /// <summary>从数据库获取全部技能（技能书 / 图鉴等）。</summary>
        InventoryDatabase,

        /// <summary>从某装备组所有装备槽中已装备道具引用的技能（当前角色装备赋予的技能）。</summary>
        Equipment,

        /// <summary>从某仓库中所有道具引用的技能（背包 / 仓库里的魔法卷轴、技能书、技能装备等）。</summary>
        Inventory,

        /// <summary>某角色当前已学会的技能（读取 <see cref="SkillRuntimeManager"/>，按学习顺序）。</summary>
        Character,
    }

    /// <summary>
    /// 技能采集器。按 <see cref="ESkillSource"/> 从三种来源采集要显示的技能集合（去重、保序）。
    /// 无运行时可变状态，故为静态工具类（如后续需要「已学技能 / 冷却」等状态，再升级为 SkillRuntimeManager）。
    ///
    /// <para>装备 / 仓库来源经道具上「技能引用属性字段」（<c>skillRefAttrId</c>，String 类型，支持数组=多技能）
    /// 取技能 ID，再经 <see cref="InventoryDataManager.GetSkill"/> 解析为技能；解析不到的 ID 跳过。</para>
    /// </summary>
    public static class SkillCollector
    {
        /// <summary>
        /// 按来源采集技能集合。
        /// </summary>
        /// <param name="source">技能信息来源。</param>
        /// <param name="configId">来源配置：Equipment=装备组 ID，Inventory=仓库 ID，Character=角色 ID；InventoryDatabase 忽略。</param>
        /// <param name="skillRefAttrId">道具上存放技能 ID 的属性字段 ID（String / String 数组）；仅 Equipment / Inventory 使用。</param>
        public static List<Skill> Collect(ESkillSource source, string configId, string skillRefAttrId)
        {
            switch (source)
            {
                case ESkillSource.Equipment: return CollectFromEquipment(configId, skillRefAttrId);
                case ESkillSource.Inventory: return CollectFromInventory(configId, skillRefAttrId);
                case ESkillSource.Character: return CollectFromCharacter(configId);
                default:                     return CollectFromDatabase();
            }
        }

        /// <summary>数据库来源：返回全部已注册数据库中的技能（按引用去重）。</summary>
        private static List<Skill> CollectFromDatabase()
        {
            var result = new List<Skill>();
            var dm     = InventoryDataManager.Instance;
            if (dm == null) return result;

            var seen = new HashSet<Skill>();
            foreach (var skill in dm.GetAllSkills())
                if (skill != null && seen.Add(skill))
                    result.Add(skill);
            return result;
        }

        /// <summary>装备来源：遍历装备组每个槽位的已装备道具，采集其引用的技能（去重、保序）。</summary>
        private static List<Skill> CollectFromEquipment(string groupId, string skillRefAttrId)
        {
            var result = new List<Skill>();
            var dm     = InventoryDataManager.Instance;
            var eq     = EquipmentRuntimeManager.Instance;
            if (dm == null || eq == null || string.IsNullOrEmpty(groupId)) return result;

            var group = dm.GetEquipmentGroup(groupId);
            if (group == null) return result;

            var seen = new HashSet<Skill>();
            foreach (var slotList in group.slotLists)
            {
                if (slotList?.slots == null) continue;
                foreach (var slot in slotList.slots)
                {
                    if (slot == null) continue;
                    string itemId = eq.GetEquipped(groupId, slot.id);
                    AddItemSkills(dm, itemId, skillRefAttrId, result, seen);
                }
            }
            return result;
        }

        /// <summary>角色来源：读取 <see cref="SkillRuntimeManager"/> 中该角色的已学技能（按学习顺序）。</summary>
        private static List<Skill> CollectFromCharacter(string characterId)
        {
            var mgr = SkillRuntimeManager.Instance;
            if (mgr == null || string.IsNullOrEmpty(characterId)) return new List<Skill>();
            return mgr.GetLearnedSkills(characterId);
        }

        /// <summary>仓库来源：遍历仓库中所有道具，采集其引用的技能（去重、保序）。</summary>
        private static List<Skill> CollectFromInventory(string inventoryId, string skillRefAttrId)
        {
            var result = new List<Skill>();
            var dm     = InventoryDataManager.Instance;
            var inv    = InventoryRuntimeManager.Instance;
            if (dm == null || inv == null || string.IsNullOrEmpty(inventoryId)) return result;

            var seen = new HashSet<Skill>();
            foreach (var slot in inv.GetSlots(inventoryId))
            {
                if (slot == null || string.IsNullOrEmpty(slot.itemId)) continue;
                AddItemSkills(dm, slot.itemId, skillRefAttrId, result, seen);
            }
            return result;
        }

        /// <summary>
        /// 从一个道具的「技能引用属性字段」解析技能 ID（兼容标量与数组），逐个解析为技能并追加到结果（按引用去重）。
        /// 属性字段类型须为 <see cref="EFieldType.String"/>；不存在 / 非 String / 解析不到技能时跳过。
        /// </summary>
        private static void AddItemSkills(InventoryDataManager dm, string itemId, string skillRefAttrId,
            List<Skill> result, HashSet<Skill> seen)
        {
            if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(skillRefAttrId)) return;

            var item = dm.GetItem(itemId);
            if (item == null) return;

            var av = item.GetAttributeValue(skillRefAttrId);
            if (av == null || av.Type != EFieldType.String) return;

            var ids = av.StringArray;
            if (ids == null) return;

            for (int i = 0; i < ids.Count; i++)
            {
                string skillId = ids[i];
                if (string.IsNullOrEmpty(skillId)) continue;
                var skill = dm.GetSkill(skillId);
                if (skill != null && seen.Add(skill))
                    result.Add(skill);
            }
        }
    }
}

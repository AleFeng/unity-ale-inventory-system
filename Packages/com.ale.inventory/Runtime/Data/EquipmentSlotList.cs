using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 槽位列表。一个装备组通常包含多个槽位列表，每个槽位列表包含多个装备槽。
    /// 通过「道具限制」（功能标签 + 枚举类型约束）统一限制本列表内所有槽位可装备的道具，
    /// 各槽位再以自身的过滤条件进一步收窄。
    ///
    /// <para>限制判定语义（运行时）：所列功能标签与枚举约束<b>必须全部满足（AND）</b>，道具才可装入本列表。</para>
    /// </summary>
    [Serializable]
    public class EquipmentSlotList
    {
        /// <summary>唯一标识（运行时/UI 按此 ID 引用槽位列表）。</summary>
        public string id;

        /// <summary>UI 中显示的名称；为空时退回使用 <see cref="id"/>。</summary>
        public string displayName;

        /// <summary>功能说明（可选，用于编辑器提示）。</summary>
        public string description;

        /// <summary>功能标签限制（引用 <see cref="FunctionTag.name"/>；多条之间 AND，全部需具备）。</summary>
        public List<string> requiredTags = new List<string>();

        /// <summary>枚举类型约束（多条之间 AND，全部需满足）。</summary>
        public List<EquipmentEnumConstraint> enumConstraints = new List<EquipmentEnumConstraint>();

        /// <summary>装备槽列表。</summary>
        public List<EquipmentSlot> slots = new List<EquipmentSlot>();

        public EquipmentSlotList()
        {
        }

        public EquipmentSlotList(string id, string displayName = null)
        {
            this.id          = id;
            this.displayName = displayName;
        }

        /// <summary>深拷贝。</summary>
        public EquipmentSlotList Clone()
        {
            var clone = new EquipmentSlotList(id, displayName) { description = description };
            clone.requiredTags = new List<string>(requiredTags);
            foreach (var c in enumConstraints) clone.enumConstraints.Add(c.Clone());
            foreach (var s in slots)           clone.slots.Add(s.Clone());
            return clone;
        }
    }
}

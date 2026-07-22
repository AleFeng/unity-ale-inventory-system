using System;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 装备系统分组标签。用于对装备组的「装备属性字段（属性加成）」条目进行分组，便于在 UI 上分组显示
    /// （如「物品等级」「主属性」「副属性」）。每条装备属性字段条目可指定一个分组标签。
    /// 基础信息（ID / 名称 / 描述 / 列表色点）由 <see cref="GroupTag"/> 承载。
    /// </summary>
    [Serializable]
    public class EquipmentGroupTag : GroupTag
    {
        public EquipmentGroupTag()
        {
        }

        public EquipmentGroupTag(string newId, string newDisplayName = null)
            : base(newId, newDisplayName)
        {
        }

        /// <summary>深拷贝。</summary>
        public EquipmentGroupTag Clone()
        {
            var clone = new EquipmentGroupTag();
            CopyTo(clone);
            return clone;
        }
    }
}

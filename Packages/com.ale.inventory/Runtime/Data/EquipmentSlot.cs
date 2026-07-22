using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 装备槽（配置部分）。一个槽位可装备一个道具；本类仅承载「配置」（稳定 ID + 名称 + 槽级过滤条件），
    /// <b>不存放运行时已装备的道具</b>——运行时已装备数据由后续阶段的 EquipmentRuntimeManager 按槽 ID 维护。
    /// </summary>
    [Serializable]
    public class EquipmentSlot
    {
        /// <summary>槽位稳定标识（运行时按它定位该槽已装备的道具）。</summary>
        public string id;

        /// <summary>UI 中显示的名称（可选）；为空时退回使用 <see cref="id"/>。</summary>
        public string displayName;

        /// <summary>槽级过滤条件（在所属槽位列表限制之上进一步收窄；多条之间 AND）。</summary>
        public List<EquipmentSlotFilter> filters = new List<EquipmentSlotFilter>();

        public EquipmentSlot()
        {
        }

        public EquipmentSlot(string id, string displayName = null)
        {
            this.id          = id;
            this.displayName = displayName;
        }

        /// <summary>深拷贝。</summary>
        public EquipmentSlot Clone()
        {
            var clone = new EquipmentSlot(id, displayName);
            foreach (var f in filters) clone.filters.Add(f.Clone());
            return clone;
        }
    }
}

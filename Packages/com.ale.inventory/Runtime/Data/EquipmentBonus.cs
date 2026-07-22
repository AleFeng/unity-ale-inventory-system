namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 装备组的一项总属性加成结果（由 <see cref="EquipmentRuntimeManager.GetTotalBonuses"/> 计算）：
    /// 对某个属性字段，跨装备组全部已装备道具求和。供 UI 按 <see cref="GroupTag"/> 分组显示。
    ///
    /// <para>记录方式随源属性的 <see cref="AttributeValue.Type"/> 而不同：标量类型汇总为一条；
    /// 数组类型（尤其 <see cref="EFieldType.EnumIntPair"/>）按 Key 拆分为多条——每个枚举 Key 一条，
    /// 其整数值跨全部已装备道具累加进 <see cref="Total"/>。</para>
    /// </summary>
    public class EquipmentBonus
    {
        /// <summary>属性字段 ID。</summary>
        public string AttrId;

        /// <summary>所属分组标签 ID（来自 <see cref="EquipmentAttributeDisplay.groupTag"/>；可为空）。</summary>
        public string GroupTag;

        /// <summary>UI 显示名（属性字段条目的显示名覆盖，为空时回退属性字段 ID）。</summary>
        public string Label;

        /// <summary>跨全部已装备道具的该属性求和值（按 <see cref="AttributeValue.ToComparableNumber"/> 取数）。</summary>
        public double Total;

        /// <summary>
        /// 当本条加成由 <see cref="EFieldType.EnumIntPair"/> 按枚举 Key 拆分而来时，记录其所属枚举类型名称；
        /// 否则为 <c>null</c>。供 UI 进一步读取该枚举项的其它属性（如描述、图标）。
        /// </summary>
        public string EnumTypeRef;

        /// <summary>
        /// 当 <see cref="EnumTypeRef"/> 非空时，记录本条加成对应的枚举不可变值（<see cref="EnumItem.Value"/>）；
        /// 否则为 0（无意义）。
        /// </summary>
        public int EnumValue;
    }
}

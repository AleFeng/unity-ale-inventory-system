using System;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 装备槽级「过滤条件」：对单个道具属性做等值判定的子过滤，用于在槽位列表限制之上进一步收窄
    /// 某个槽位可装备的道具（例如 武器主类型=近战单手、武器次类型=剑）。
    ///
    /// <para>判定语义（运行时）：道具该属性 <see cref="attrId"/> 的值等于 <see cref="value"/> 视为通过；
    /// 同一槽位的多条过滤条件需全部满足（AND）。匹配逻辑在后续运行时阶段实现，本数据仅承载配置。</para>
    /// </summary>
    [Serializable]
    public class EquipmentSlotFilter
    {
        /// <summary>目标属性字段 ID（道具系统中道具模板/功能标签定义的属性字段）。</summary>
        public string attrId;

        /// <summary>期望值。复用标签联合 <see cref="AttributeValue"/>，编辑器据属性定义用 AttributeFieldDrawer 编辑。</summary>
        public AttributeValue value = new AttributeValue();

        public EquipmentSlotFilter()
        {
        }

        public EquipmentSlotFilter(string attrId, AttributeValue value = null)
        {
            this.attrId = attrId;
            this.value  = value ?? new AttributeValue();
        }

        /// <summary>深拷贝。</summary>
        public EquipmentSlotFilter Clone() => new EquipmentSlotFilter(
            attrId, value != null ? value.Clone() : new AttributeValue());
    }
}

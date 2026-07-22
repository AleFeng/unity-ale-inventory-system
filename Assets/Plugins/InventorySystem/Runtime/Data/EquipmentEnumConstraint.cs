using System;
using System.Collections.Generic;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 槽位列表级「枚举类型约束」：从道具系统定义的枚举类型中选一个，再选若干允许的枚举值，
    /// 限制该槽位列表可装备的道具（先选枚举类型，再多选枚举值）。
    ///
    /// <para>判定语义（运行时）：道具需存在某个引用该枚举类型的属性，其值落在 <see cref="allowedValues"/> 内；
    /// 同一槽位列表的多条枚举约束需全部满足（AND）。匹配逻辑在后续运行时阶段实现，本数据仅承载配置。</para>
    /// </summary>
    [Serializable]
    public class EquipmentEnumConstraint
    {
        /// <summary>引用的枚举类型名称（对应 <see cref="EnumType.name"/>）。</summary>
        public string enumTypeRef;

        /// <summary>允许的枚举值集合（对应 <see cref="EnumItem.value"/>）。</summary>
        public List<int> allowedValues = new List<int>();

        public EquipmentEnumConstraint()
        {
        }

        public EquipmentEnumConstraint(string enumTypeRef)
        {
            this.enumTypeRef = enumTypeRef;
        }

        /// <summary>深拷贝。</summary>
        public EquipmentEnumConstraint Clone() => new EquipmentEnumConstraint(enumTypeRef)
        {
            allowedValues = new List<int>(allowedValues),
        };
    }
}

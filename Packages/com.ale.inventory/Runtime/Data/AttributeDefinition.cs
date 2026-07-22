using System;
using UnityEngine.Serialization;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 属性字段定义（模板）。描述一个属性字段的元数据：稳定 ID、类型、是否数组、
    /// 枚举类型引用、以及创建新道具时使用的默认值。功能标签与道具模板各持有一组该定义。
    /// </summary>
    [Serializable]
    public class AttributeDefinition
    {
        /// <summary>
        /// 稳定的字段 ID（支持中文）。在所属容器（标签 / 模板）内唯一，用于与道具上的
        /// <see cref="AttributeEntry.id"/> 关联，同时作为编辑器的显示标签。
        /// </summary>
        [FormerlySerializedAs("key")]
        public string id;

        /// <summary>字段类型。</summary>
        public EFieldType type;

        /// <summary>是否为数组形态。</summary>
        public bool isArray;

        /// <summary>当 <see cref="type"/> 为 <see cref="EFieldType.Enum"/> 时所引用的枚举类型名称。</summary>
        public string enumTypeRef;

        /// <summary>新建道具时用于初始化的默认值。</summary>
        public AttributeValue defaultValue = new AttributeValue();

        public AttributeDefinition()
        {
        }

        public AttributeDefinition(string attrId, EFieldType type, bool isArray = false, string enumTypeRef = null)
        {
            this.id          = attrId;
            this.type        = type;
            this.isArray     = isArray;
            this.enumTypeRef = enumTypeRef;
            defaultValue = new AttributeValue(type, isArray, enumTypeRef);
        }

        /// <summary>根据该定义创建一份默认值的副本，供新道具的 <see cref="AttributeEntry"/> 使用。</summary>
        public AttributeValue CreateValue()
        {
            if (defaultValue == null) return new AttributeValue(type, isArray, enumTypeRef);
            return defaultValue.Clone();
        }

        /// <summary>深拷贝该定义（含默认值）。</summary>
        public AttributeDefinition Clone()
        {
            return new AttributeDefinition
            {
                id          = id,
                type        = type,
                isArray     = isArray,
                enumTypeRef = enumTypeRef,
                defaultValue = defaultValue != null ? defaultValue.Clone() : new AttributeValue(type, isArray, enumTypeRef)
            };
        }
    }
}

using System;
using UnityEngine.Serialization;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 道具上的一条具体属性值。通过 <see cref="id"/> 与某个 <see cref="AttributeDefinition.id"/> 关联，
    /// 由 <see cref="value"/> 承载实际数据。
    /// </summary>
    [Serializable]
    public class AttributeEntry
    {
        /// <summary>对应的属性定义 ID。</summary>
        [FormerlySerializedAs("key")]
        public string id;

        /// <summary>实际属性值。</summary>
        public AttributeValue value = new AttributeValue();

        public AttributeEntry()
        {
        }

        public AttributeEntry(string attrId, AttributeValue value)
        {
            this.id    = attrId;
            this.value = value ?? new AttributeValue();
        }

        /// <summary>深拷贝。</summary>
        public AttributeEntry Clone()
        {
            return new AttributeEntry(id, value != null ? value.Clone() : new AttributeValue());
        }
    }
}

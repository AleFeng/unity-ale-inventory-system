using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 道具模板。定义一组基础属性字段（如 ID/名称/描述/品质/价值/图标/堆叠上限），
    /// 作为创建新道具的蓝本。从模板创建道具时会拷贝其属性字段为默认值。
    /// </summary>
    [Serializable]
    public class ItemTemplate
    {
        /// <summary>模板名称。</summary>
        public string name;

        /// <summary>模板标识颜色（用于列表中的圆形色点，便于快速区分来源）。</summary>
        public Color color = Color.gray;

        /// <summary>模板所定义的基础属性字段。</summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        /// <summary>
        /// 模板默认携带的功能标签列表。从本模板创建的道具会将这些标签的属性字段
        /// 纳入属性集合，且无法在道具层面单独移除（由模板统一管理）。
        /// </summary>
        public List<string> tagRefs = new List<string>();

        /// <summary>道具重量（克/单位，0 = 无重量）。</summary>
        public float weight;

        /// <summary>最大堆叠数量（0 = 无限制，1 = 不可堆叠）。</summary>
        public int stackLimit;

        /// <summary>是否在仓库 UI 的道具仓库中隐藏（使用此模板的道具默认不显示）。</summary>
        public bool hideInInventory;

        public ItemTemplate()
        {
        }

        public ItemTemplate(string name)
        {
            this.name = name;
        }

        public ItemTemplate Clone()
        {
            var clone = new ItemTemplate(name) { color = color, weight = weight, stackLimit = stackLimit, hideInInventory = hideInInventory };
            foreach (var attr in attributes)
                clone.attributes.Add(attr.Clone());
            clone.tagRefs = new List<string>(tagRefs);
            return clone;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 仓库模板。定义一组自定义属性字段（如备注、分区等），作为创建新仓库的蓝本。
    /// 从模板创建仓库时会按字段定义初始化默认值。
    /// </summary>
    [Serializable]
    public class InventoryTemplate
    {
        /// <summary>模板名称。</summary>
        public string name;

        /// <summary>模板标识颜色（用于列表中的圆形色点，便于快速区分来源）。</summary>
        public Color color = Color.gray;

        /// <summary>模板所定义的自定义属性字段。</summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        /// <summary>容量上限。0 表示无限制。</summary>
        public int capacity;

        /// <summary>重量上限。0 表示无限制。</summary>
        public float weightLimit;

        /// <summary>放入功能标签：只允许带有这些标签的道具放入（空列表 = 不限制）。</summary>
        public List<string> allowPutTagRefs     = new List<string>();

        /// <summary>取出功能标签：只允许带有这些标签的道具取出（空列表 = 不限制）。</summary>
        public List<string> allowTakeTagRefs    = new List<string>();

        /// <summary>操作功能标签：限制可对仓库内道具进行的操作类型（空列表 = 不限制）。</summary>
        public List<string> allowOperateTagRefs = new List<string>();

        /// <summary>过滤标签列表（UI 中以标签按钮形式呈现）。</summary>
        public List<string> filterTagRefs = new List<string>();

        /// <summary>是否在过滤页签栏显示"全部"页签（默认创建仓库时复制此值）。</summary>
        public bool showAllFilterTab = true;

        /// <summary>是否启用自动整理。</summary>
        public bool autoSort;

        /// <summary>整理列表（UI 中以下拉菜单形式呈现，玩家可选择排序条件及升降序）。</summary>
        public List<SortPriority> sortPriorities = new List<SortPriority>();

        /// <summary>
        /// 整理优先级。当整理列表所选条件的比较值相同时，
        /// 依次按此列表中的条件对比，直到找到不同值为止。
        /// </summary>
        public List<SortPriority> sortTiebreakers = new List<SortPriority>();

        /// <summary>是否允许拖拽手动调整道具顺序。</summary>
        public bool dragSort;

        /// <summary>引用的数字格式配置名称（对应 InventoryDatabase.NumberFormatConfigs；空 = 不使用）。</summary>
        public string numberFormatRef;

        public InventoryTemplate()
        {
        }

        public InventoryTemplate(string nameArg)
        {
            name = nameArg;
        }

        public InventoryTemplate Clone()
        {
            var clone = new InventoryTemplate(name) { color = color };
            foreach (var attr in attributes)
                clone.attributes.Add(attr.Clone());
            clone.capacity            = capacity;
            clone.weightLimit         = weightLimit;
            clone.allowPutTagRefs     = new List<string>(allowPutTagRefs);
            clone.allowTakeTagRefs    = new List<string>(allowTakeTagRefs);
            clone.allowOperateTagRefs = new List<string>(allowOperateTagRefs);
            clone.filterTagRefs       = new List<string>(filterTagRefs);
            clone.showAllFilterTab     = showAllFilterTab;
            clone.autoSort             = autoSort;
            clone.dragSort             = dragSort;
            clone.numberFormatRef      = numberFormatRef;
            foreach (var sp in sortPriorities)
                clone.sortPriorities.Add(sp.Clone());
            foreach (var sp in sortTiebreakers)
                clone.sortTiebreakers.Add(sp.Clone());
            return clone;
        }
    }
}

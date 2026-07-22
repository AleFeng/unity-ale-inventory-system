using System;
using System.Collections.Generic;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 仓库实例。容纳道具的容器（背包、商店库存、装备栏等）。
    /// 携带唯一 <see cref="id"/>、来源模板引用、三类功能标签限制、整理配置，以及来自模板的自定义属性值。
    /// </summary>
    [Serializable]
    public class Inventory
    {
        /// <summary>唯一标识。</summary>
        public string id;

        /// <summary>显示名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；为空时回退 <see cref="id"/>）。</summary>
        public AttributeValue displayNameText = new AttributeValue(EFieldType.Text);

        /// <summary>描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；正式游戏配置数据，UI 显示为「描述」）。</summary>
        public AttributeValue descriptionText = new AttributeValue(EFieldType.Text);

        /// <summary>来源仓库模板名称（可为空）。</summary>
        public string templateRef;

        /// <summary>容量上限。0 表示无限制。</summary>
        public int capacity;

        /// <summary>重量上限。0 表示无限制。超过上限不阻止放入，但可影响部分操作（如移动速度）。</summary>
        public float weightLimit;

        /// <summary>放入功能标签：只允许带有这些标签的道具放入（空列表 = 不限制）。</summary>
        public List<string> allowPutTagRefs = new List<string>();

        /// <summary>取出功能标签：只允许带有这些标签的道具取出（空列表 = 不限制）。</summary>
        public List<string> allowTakeTagRefs = new List<string>();

        /// <summary>操作功能标签：限制可对仓库内道具进行的操作类型（空列表 = 不限制）。</summary>
        public List<string> allowOperateTagRefs = new List<string>();

        /// <summary>
        /// 过滤标签列表。UI 中将以功能标签按钮的形式呈现。
        /// 玩家可点击切换，只显示带有对应标签的道具。
        /// </summary>
        public List<string> filterTagRefs = new List<string>();

        /// <summary>
        /// 是否在过滤页签栏显示"全部"页签。true = 显示并默认选中（不过滤）；
        /// false = 不显示，默认选中第一个过滤标签（无标签时不过滤）。
        /// </summary>
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

        /// <summary>来自模板的自定义属性值。</summary>
        public List<AttributeEntry> values = new List<AttributeEntry>();

        public Inventory()
        {
        }

        public Inventory(string newId, string newTemplateRef = null)
        {
            id          = newId;
            templateRef = newTemplateRef;
        }

        /// <summary>按属性 ID 查找属性值，未找到返回 null。</summary>
        public AttributeEntry GetEntry(string attrId)
        {
            foreach (var e in values)
                if (e.id == attrId) return e;
            return null;
        }

        /// <summary>
        /// 根据当前模板协调自定义属性值集合：
        /// 为模板中新增的字段追加默认值条目；移除模板中已不存在的字段条目；
        /// 已存在的字段保留现有值。
        /// </summary>
        public void RebuildAttributes(InventoryDatabase db)
        {
            if (!db) return;

            var template = db.GetInventoryTemplate(templateRef);
            var defs     = template != null ? template.attributes : new List<AttributeDefinition>();

            var desiredIds = new HashSet<string>();
            foreach (var def in defs)
                if (!string.IsNullOrEmpty(def.id)) desiredIds.Add(def.id);

            values.RemoveAll(e => !desiredIds.Contains(e.id));

            foreach (var def in defs)
            {
                if (string.IsNullOrEmpty(def.id)) continue;
                var existing = GetEntry(def.id);
                if (existing == null)
                    values.Add(new AttributeEntry(def.id, def.CreateValue()));
                else if (existing.value == null
                         || existing.value.Type    != def.type
                         || existing.value.IsArray != def.isArray)
                    existing.value = def.CreateValue();
            }
        }

        /// <summary>深拷贝。</summary>
        public Inventory Clone()
        {
            var clone = new Inventory(id, templateRef)
            {
                displayNameText      = displayNameText != null ? displayNameText.Clone() : new AttributeValue(EFieldType.Text),
                descriptionText      = descriptionText != null ? descriptionText.Clone() : new AttributeValue(EFieldType.Text),
                capacity             = capacity,
                weightLimit          = weightLimit,
                autoSort             = autoSort,
                dragSort             = dragSort,
                showAllFilterTab     = showAllFilterTab,
                numberFormatRef      = numberFormatRef,
                allowPutTagRefs     = new List<string>(allowPutTagRefs),
                allowTakeTagRefs    = new List<string>(allowTakeTagRefs),
                allowOperateTagRefs = new List<string>(allowOperateTagRefs),
                filterTagRefs       = new List<string>(filterTagRefs),
            };
            foreach (var sp in sortPriorities)
                clone.sortPriorities.Add(sp.Clone());
            foreach (var sp in sortTiebreakers)
                clone.sortTiebreakers.Add(sp.Clone());
            foreach (var e in values)
                clone.values.Add(e.Clone());
            return clone;
        }
    }
}

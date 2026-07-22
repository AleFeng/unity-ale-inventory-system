using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 道具实例。携带一个唯一 <see cref="id"/>、来源模板引用、一组功能标签引用，以及具体属性值列表。
    /// 属性值集合 = 模板定义的字段 ∪ 各已附加功能标签定义的字段，由 <see cref="RebuildAttributes"/> 协调。
    /// </summary>
    [Serializable]
    public class Item : AttributeOwner
    {
        /// <summary>唯一标识（在道具列表中做重复检查）。</summary>
        public string id;

        /// <summary>创建来源的道具模板名称（用于"快速添加"克隆参考）。</summary>
        public string templateRef;

        /// <summary>已附加的功能标签名称列表。</summary>
        public List<string> tagRefs = new List<string>();

        /// <summary>道具的具体属性值。</summary>
        public List<AttributeEntry> values = new List<AttributeEntry>();

        /// <summary>道具重量（用于仓库重量上限计算，0 表示无重量）。</summary>
        public float weight;

        /// <summary>堆叠上限。同一格最多可叠放的数量（0 = 无上限，1 = 不可堆叠，>1 = 具体上限）。</summary>
        public int stackLimit;

        /// <summary>是否在仓库 UI 的道具仓库中隐藏（数据仍然存在，仅不显示）。</summary>
        public bool hideInInventory;

        public Item()
        {
            
        }

        public Item(string id, string templateRef = null)
        {
            this.id = id;
            this.templateRef = templateRef;
        }

        #region 属性字段管理

        // 实现基类 AttributeOwner 的抽象属性，将 values 列表暴露给基类的懒加载字典缓存。
        protected override List<AttributeEntry> AttributeEntries => values;

        /// <summary>
        /// 重建 属性字段
        /// 根据当前模板与已附加的功能标签，协调属性值集合：
        /// 为新增的字段 key 追加默认值条目；移除不再属于任何来源的 key 的条目；
        /// 已存在的 key 保留其现有值。
        /// </summary>
        /// <param name="db"></param>
        public void RebuildAttributes(InventoryDatabase db)
        {
            if (!db) return;

            // 收集期望的字段定义。
            // 顺序规则：① 模板自有字段（最高优先级）；② 功能标签字段（按 db.FunctionTags 列表顺序
            // 合并模板锁定标签与道具自身标签，使属性字段顺序与左侧功能标签面板顺序保持一致）；
            // ③ 不在 FunctionTags 中的遗留标签（保底处理，保持旧有顺序）。
            var desired    = new List<AttributeDefinition>();
            var desiredIds = new HashSet<string>();

            var template = db.GetTemplate(templateRef);

            // ① 模板自有属性
            if (template != null)
                CollectDefinitions(template.attributes, desired, desiredIds);

            // ② 按 db.FunctionTags 顺序收集标签属性（模板锁定标签与道具标签统一排序）
            var processedTagNames = new HashSet<string>();
            foreach (var ft in db.FunctionTags)
            {
                bool inTemplate = template != null && template.tagRefs.Contains(ft.name);
                bool inItem     = tagRefs.Contains(ft.name);
                if (!inTemplate && !inItem) continue;

                var tag = db.GetTag(ft.name);
                if (tag != null)
                    CollectDefinitions(tag.attributes, desired, desiredIds);
                processedTagNames.Add(ft.name);
            }

            // ③ 保底：处理不在 FunctionTags 列表中的模板锁定标签（维持旧有顺序）
            if (template != null)
                foreach (var tTagName in template.tagRefs)
                {
                    if (processedTagNames.Contains(tTagName)) continue;
                    var tTag = db.GetTag(tTagName);
                    if (tTag != null)
                        CollectDefinitions(tTag.attributes, desired, desiredIds);
                }

            // ③ 保底：处理不在 FunctionTags 列表中的道具自身标签（维持旧有顺序）
            foreach (var tagName in tagRefs)
            {
                if (processedTagNames.Contains(tagName)) continue;
                var tag = db.GetTag(tagName);
                if (tag != null)
                    CollectDefinitions(tag.attributes, desired, desiredIds);
            }

            // 移除不再期望的条目。
            values.RemoveAll(e => !desiredIds.Contains(e.id));

            // 追加缺失的条目；同时修正已有条目的类型/数组形态/枚举引用不匹配问题。
            foreach (var def in desired)
            {
                var existing = GetEntry(def.id);
                if (existing == null)
                {
                    values.Add(new AttributeEntry(def.id, def.CreateValue()));
                }
                else
                {
                    // 字段类型、数组形态或枚举类型引用发生变化时，重置为新类型的默认值（保留 id）
                    bool typeMismatch  = existing.value.Type    != def.type;
                    bool arrayMismatch = existing.value.IsArray != def.isArray;
                    bool enumMismatch  = (def.type == EFieldType.Enum || def.type == EFieldType.EnumIntPair)
                                     && existing.value.EnumTypeRef != def.enumTypeRef;
                    if (typeMismatch || arrayMismatch || enumMismatch)
                        existing.value = def.CreateValue();
                }
            }

            // 按模板定义顺序重排 values，确保显示/存储顺序与定义顺序一致。
            for (int i = 0; i < desired.Count; i++)
            {
                string targetId = desired[i].id;
                int cur = -1;
                for (int j = i; j < values.Count; j++)
                {
                    if (values[j].id == targetId) { cur = j; break; }
                }
                if (cur > i)
                {
                    var tmp = values[cur];
                    values.RemoveAt(cur);
                    values.Insert(i, tmp);
                }
            }

            // values 已完整重建，使缓存失效；下次 GetEntry 调用时将从最终状态重建字典。
            InvalidateEntryCache();
        }
        
        /// <summary>
        /// 从源列表中收集字段定义，添加到目标列表和 ID 集合中（仅当 ID 不为空且未重复时）。用于协调属性集合时确定期望的字段定义。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="desired"></param>
        /// <param name="desiredIds"></param>
        private static void CollectDefinitions
        (
            List<AttributeDefinition> source,
            List<AttributeDefinition> desired, 
            HashSet<string> desiredIds
        )
        {
            foreach (var def in source)
            {
                if (string.IsNullOrEmpty(def.id)) continue;
                if (desiredIds.Add(def.id))
                    desired.Add(def);
            }
        }

        #endregion

        #region 功能标签

        /// <summary>添加一个功能标签并重建属性集合（已存在则忽略）。</summary>
        public void AddTag(string tagName, InventoryDatabase db)
        {
            if (string.IsNullOrEmpty(tagName) || tagRefs.Contains(tagName)) return;
            tagRefs.Add(tagName);
            RebuildAttributes(db);
        }

        /// <summary>移除一个功能标签并重建属性集合。</summary>
        public void RemoveTag(string tagName, InventoryDatabase db)
        {
            if (!tagRefs.Remove(tagName)) return;
            RebuildAttributes(db);
        }

        #endregion

        #region Item数据拷贝

        /// <summary>
        /// 拷贝 道具数据
        /// </summary>
        /// <returns></returns>
        public Item Clone()
        {
            var clone = new Item(id, templateRef)
            {
                weight     = weight,
                stackLimit = stackLimit,
                hideInInventory = hideInInventory,
            };
            clone.tagRefs = new List<string>(tagRefs);
            foreach (var e in values)
                clone.values.Add(e.Clone());
            return clone;
        }

        #endregion
    }
}

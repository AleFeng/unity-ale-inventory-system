using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 制作蓝图（配方）。携带唯一 <see cref="id"/>、分组标签、产出/消耗道具列表、制作时间/次数、
    /// 制作仓库、整理与 UI 配置，以及来自模板的自定义属性值。
    /// 蓝图是配置目录；运行时的制作行为（消耗材料 → 产出道具）由 CraftingRuntimeManager 执行。
    /// </summary>
    [Serializable]
    public class CraftingBlueprint : ICraftingConfig
    {
        /// <summary>唯一标识。</summary>
        public string id;

        /// <summary>显示名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；为空时回退 <see cref="id"/>）。</summary>
        public AttributeValue displayText = new AttributeValue(EFieldType.Text);

        /// <summary>蓝图描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；基础信息；UI 详情中的产出描述取主产出道具自身的描述属性）。</summary>
        public AttributeValue descriptionText = new AttributeValue(EFieldType.Text);

        /// <summary>来源蓝图模板名称（可为空）。</summary>
        public string templateRef;

        // ── 分组标签 ──────────────────────────────────────────────────────────────
        /// <summary>主分组标签 ID（单选，引用 <see cref="InventoryDatabase.CraftingGroupTags"/> 的 id）。</summary>
        public string primaryGroupTag;

        /// <summary>副分组标签 ID 列表（可多选）。</summary>
        public List<string> secondaryGroupTags = new List<string>();

        // ── 配方 ──────────────────────────────────────────────────────────────────
        /// <summary>产出道具列表。Index0 为主产出（用于 UI 显示），其余为副产出。</summary>
        public List<CraftingItemAmount> outputs = new List<CraftingItemAmount>();

        /// <summary>消耗道具列表（制作一次所需材料）。</summary>
        public List<CraftingItemAmount> inputs = new List<CraftingItemAmount>();

        // ── 配置（ICraftingConfig）──────────────────────────────────────────────────
        /// <summary>制作一次需要的时间（秒）。</summary>
        public float craftTime;

        /// <summary>连续制作次数上限：1 = 仅一次；-1 = 无限。与材料决定的可制作次数取小。</summary>
        public int maxCraftCount = -1;

        /// <summary>制作仓库 ID 列表（有序，按 Index 优先级作为消耗来源/产出落点）。</summary>
        public List<string> craftInventoryRefs = new List<string>();

        /// <summary>引用的数字格式配置名称（对应 InventoryDatabase.NumberFormatConfigs；空 = 不使用）。</summary>
        public string numberFormatRef;

        /// <summary>UI 上显示的属性字段列表（每条 = Label + 属性字段 ID）。</summary>
        public List<CraftingAttributeDisplay> attributeDisplays = new List<CraftingAttributeDisplay>();

        /// <summary>来自模板的自定义属性值。</summary>
        public List<AttributeEntry> values = new List<AttributeEntry>();

        // ── ICraftingConfig（映射到上述序列化字段，供编辑器共享绘制）────────────────
        float ICraftingConfig.CraftTime { get => craftTime; set => craftTime = value; }
        int ICraftingConfig.MaxCraftCount { get => maxCraftCount; set => maxCraftCount = value; }
        List<string> ICraftingConfig.CraftInventoryRefs => craftInventoryRefs;
        string ICraftingConfig.NumberFormatRef { get => numberFormatRef; set => numberFormatRef = value; }
        List<CraftingAttributeDisplay> ICraftingConfig.AttributeDisplays => attributeDisplays;

        public CraftingBlueprint()
        {
        }

        public CraftingBlueprint(string newId, string newTemplateRef = null)
        {
            id          = newId;
            templateRef = newTemplateRef;
        }

        /// <summary>主产出道具条目（产出列表首项），无产出时返回 null。</summary>
        public CraftingItemAmount PrimaryOutput => outputs.Count > 0 ? outputs[0] : null;

        /// <summary>按属性 ID 查找属性值，未找到返回 null。</summary>
        public AttributeEntry GetEntry(string attrId)
        {
            foreach (var e in values)
                if (e.id == attrId) return e;
            return null;
        }

        /// <summary>
        /// 根据当前模板协调自定义属性值集合：
        /// 为模板新增字段追加默认值条目；移除模板已不存在的字段条目；已存在字段保留现有值。
        /// </summary>
        public void RebuildAttributes(InventoryDatabase db)
        {
            if (!db) return;

            var template = db.GetCraftingBlueprintTemplate(templateRef);
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

            // 制作仓库 / UI 配置（数字格式 + 属性字段显示）为「模板级」配置：蓝图条目不可单独修改，
            // 始终镜像来源模板（编辑器中以只读形式展示，仅可在「蓝图模板」中修改）。模板缺失时保留现有值。
            if (template != null)
            {
                numberFormatRef    = template.numberFormatRef;
                craftInventoryRefs = new List<string>(template.craftInventoryRefs);
                attributeDisplays.Clear();
                foreach (var ad in template.attributeDisplays)
                    attributeDisplays.Add(ad.Clone());
            }
        }

        /// <summary>深拷贝。</summary>
        public CraftingBlueprint Clone()
        {
            var clone = new CraftingBlueprint(id, templateRef)
            {
                displayText           = displayText     != null ? displayText.Clone()     : new AttributeValue(EFieldType.Text),
                descriptionText       = descriptionText != null ? descriptionText.Clone() : new AttributeValue(EFieldType.Text),
                primaryGroupTag       = primaryGroupTag,
                craftTime             = craftTime,
                maxCraftCount         = maxCraftCount,
                numberFormatRef       = numberFormatRef,
                secondaryGroupTags    = new List<string>(secondaryGroupTags),
                craftInventoryRefs    = new List<string>(craftInventoryRefs),
            };
            foreach (var o in outputs)            clone.outputs.Add(o.Clone());
            foreach (var i in inputs)             clone.inputs.Add(i.Clone());
            foreach (var ad in attributeDisplays) clone.attributeDisplays.Add(ad.Clone());
            foreach (var e in values)             clone.values.Add(e.Clone());
            return clone;
        }
    }
}

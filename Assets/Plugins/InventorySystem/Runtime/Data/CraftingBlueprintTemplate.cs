using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 蓝图模板。定义自定义属性字段 + 一整套蓝图可配置项的默认值，作为创建新蓝图的蓝本，
    /// 同时用于分类筛选（如 防具 / 武器 / 饰品 / 食品 / 药物）。
    /// 与 <see cref="CraftingBlueprint"/> 共享 <see cref="ICraftingConfig"/>，使两者配置项一致、编辑器复用同一套绘制。
    /// </summary>
    [Serializable]
    public class CraftingBlueprintTemplate : ICraftingConfig
    {
        /// <summary>模板名称。</summary>
        public string name;

        /// <summary>模板标识颜色（用于列表中的圆形色点，便于快速区分来源）。</summary>
        public Color color = Color.gray;

        /// <summary>模板所定义的自定义属性字段。</summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        // ── 蓝图可配置项（默认值，创建蓝图时复制）────────────────────────────────
        /// <summary>制作一次需要的时间（秒）。</summary>
        public float craftTime;

        /// <summary>连续制作次数上限：1 = 仅一次；-1 = 无限。</summary>
        public int maxCraftCount = -1;

        /// <summary>制作仓库 ID 列表（有序，按 Index 优先级作为消耗来源/产出落点）。</summary>
        public List<string> craftInventoryRefs = new List<string>();

        /// <summary>整理列表（模板级：该模板下所有蓝图在 UI 列表中的排序条件，蓝图自身不再单独配置）。</summary>
        public List<SortPriority> sortPriorities = new List<SortPriority>();

        /// <summary>整理优先级（模板级：整理列表条件值相同时，依次对比此列表直至值不同）。</summary>
        public List<SortPriority> sortTiebreakers = new List<SortPriority>();

        /// <summary>引用的数字格式配置名称（对应 InventoryDatabase.NumberFormatConfigs；空 = 不使用）。</summary>
        public string numberFormatRef;

        /// <summary>UI 上显示的属性字段列表（每条 = Label + 属性字段 ID）。</summary>
        public List<CraftingAttributeDisplay> attributeDisplays = new List<CraftingAttributeDisplay>();

        // ── ICraftingConfig（映射到上述序列化字段，供编辑器共享绘制）────────────────
        float ICraftingConfig.CraftTime { get => craftTime; set => craftTime = value; }
        int ICraftingConfig.MaxCraftCount { get => maxCraftCount; set => maxCraftCount = value; }
        List<string> ICraftingConfig.CraftInventoryRefs => craftInventoryRefs;
        string ICraftingConfig.NumberFormatRef { get => numberFormatRef; set => numberFormatRef = value; }
        List<CraftingAttributeDisplay> ICraftingConfig.AttributeDisplays => attributeDisplays;

        public CraftingBlueprintTemplate()
        {
        }

        public CraftingBlueprintTemplate(string nameArg)
        {
            name = nameArg;
        }

        public CraftingBlueprintTemplate Clone()
        {
            var clone = new CraftingBlueprintTemplate(name)
            {
                color           = color,
                craftTime       = craftTime,
                maxCraftCount   = maxCraftCount,
                numberFormatRef = numberFormatRef,
                craftInventoryRefs = new List<string>(craftInventoryRefs),
            };
            foreach (var attr in attributes)        clone.attributes.Add(attr.Clone());
            foreach (var sp in sortPriorities)      clone.sortPriorities.Add(sp.Clone());
            foreach (var sp in sortTiebreakers)     clone.sortTiebreakers.Add(sp.Clone());
            foreach (var ad in attributeDisplays)   clone.attributeDisplays.Add(ad.Clone());
            return clone;
        }
    }
}

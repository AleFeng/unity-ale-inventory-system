using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 蓝图可配置项的共享契约。由蓝图实例 <see cref="CraftingBlueprint"/> 与
    /// 蓝图模板 <see cref="CraftingBlueprintTemplate"/> 共同实现，使两者配置项一致，
    /// 编辑器得以复用同一套绘制（<c>CraftingConfigDrawer</c>）。
    ///
    /// <para>说明：产出/消耗道具列表与分组标签为蓝图实例独有（配方相关），不在此共享接口中。</para>
    /// </summary>
    public interface ICraftingConfig
    {
        /// <summary>制作一次需要的时间（秒）。</summary>
        float CraftTime { get; set; }

        /// <summary>连续制作次数上限：1 = 仅一次；-1 = 无限。与材料决定的可制作次数取小。</summary>
        int MaxCraftCount { get; set; }

        /// <summary>制作仓库 ID 列表（有序）：按 Index 优先级作为消耗道具来源与产出道具落点。</summary>
        List<string> CraftInventoryRefs { get; }

        /// <summary>引用的数字格式配置名称（对应 InventoryDatabase.NumberFormatConfigs；空 = 不使用）。</summary>
        string NumberFormatRef { get; set; }

        /// <summary>UI 上显示的属性字段列表（每条 = Label + 属性字段 ID）。</summary>
        List<CraftingAttributeDisplay> AttributeDisplays { get; }
    }
}

using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 装备组可配置项的共享契约。由装备组实例 <see cref="EquipmentGroup"/> 与
    /// 装备组模板 <see cref="EquipmentGroupTemplate"/> 共同实现，使两者配置项一致，
    /// 编辑器得以复用同一套绘制（<c>EquipmentConfigDrawer</c>）。
    ///
    /// <para>说明：身份（ID / 名称 / 描述 / 本地化 / 来源模板）为装备组实例独有；
    /// 自定义属性「值」亦为实例独有（由模板的属性「定义」驱动），均不在此共享接口中。</para>
    /// </summary>
    public interface IEquipmentConfig
    {
        /// <summary>装备仓库（装备系统 / 装备 UI 可直接交互的玩家仓库 ID 列表；卸下时从 Index0 起找第一个放得下的仓库）。</summary>
        List<string> EquipmentInventoryRefs { get; }

        /// <summary>槽位列表（每个槽位列表含多个装备槽与道具限制）。</summary>
        List<EquipmentSlotList> SlotLists { get; }

        /// <summary>装备属性字段列表（指定哪些道具属性作为装备组的总属性加成）。</summary>
        List<EquipmentAttributeDisplay> AttributeDisplays { get; }

        /// <summary>整理排序 - 排序条件（应用于可装备道具候选列表 UiwEquipmentCandidateList 的显示排序）。</summary>
        List<SortPriority> SortPriorities { get; }

        /// <summary>整理排序 - 整理优先级（排序条件值相同时，依次按此列表比较直至值不同）。</summary>
        List<SortPriority> SortTiebreakers { get; }
    }
}

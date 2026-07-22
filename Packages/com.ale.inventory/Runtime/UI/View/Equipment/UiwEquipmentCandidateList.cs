using System.Collections.Generic;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>装备候选条目：候选道具 + 其取出来源仓库 + 跨全部装备仓库的合计持有量。</summary>
    public struct EquipmentCandidateEntry
    {
        public string SourceInv;   // 该候选道具的取出来源仓库（首个持有该道具的装备仓库）
        public string ItemId;      // 道具 ID
        public int    Count;       // 跨全部装备仓库的合计持有量

        public EquipmentCandidateEntry(string sourceInv, string itemId, int count)
        {
            this.SourceInv = sourceInv;
            this.ItemId    = itemId;
            this.Count     = count;
        }
    }

    /// <summary>
    /// 可装备道具列表显示组件（虚拟滚动网格）。从装备组的「装备仓库」列表（可多个）取出全部道具，
    /// 按当前槽位列表的道具限制筛选，显示为候选格子；每个候选记录其真实来源仓库。
    ///
    /// <para>虚拟滚动与网格布局（纵向 / 横向、自动跨轴数量）由基类 <see cref="UiwInventoryGridList{TData,TCell}"/>
    /// 提供；本类只负责筛选候选与「把候选显示到 <see cref="UiwInventoryItemCell"/> 格子」。</para>
    ///
    /// <para>候选格子复用仓库格子 <see cref="UiwInventoryItemCell"/>（显示）+ <see cref="GridCellDragHandler"/>
    /// （未接入网格整理列表——本类不设 <c>ItemGridList</c>——故驱动「拖到装备槽装备」），与背包格子共用同一套交互：
    /// <b>右键</b>由 <see cref="UiwInventoryItemCell"/> 广播、<see cref="UiwEquipmentView"/> 订阅统一快速装备；
    /// <b>左键拖拽</b>到 <see cref="UiwEquipmentSlot"/> 装备。</para>
    /// </summary>
    public class UiwEquipmentCandidateList : UiwInventoryGridList<EquipmentCandidateEntry, UiwInventoryItemCell>
    {
        private IReadOnlyList<string> _sourceInventoryIds;
        private EquipmentSlotList      _slotListDef;

        /// <summary>
        /// 绑定来源仓库列表（装备组的「装备仓库」）与当前槽位列表定义，并（可选）配置候选列表的显示排序，然后刷新。
        /// 排序条件来自装备组模板（<see cref="EquipmentGroupTemplate.sortPriorities"/> / sortTiebreakers），由 <see cref="UiwEquipmentSelectPanel"/> 解析后传入；
        /// 排序栏引用（可选）配置在本列表组件上，未配置则以首条为默认排序（降序）。
        /// </summary>
        public void Bind(IReadOnlyList<string> sourceInventoryIds, EquipmentSlotList slotListDef,
            IReadOnlyList<SortPriority> sortPriorities = null, IReadOnlyList<SortPriority> sortTiebreakers = null,
            InventoryDatabase sortDb = null)
        {
            _sourceInventoryIds = sourceInventoryIds;
            _slotListDef        = slotListDef;
            // 显示排序：排序键取候选道具 ID，复用 CompareSlots 按道具属性比较（仅显示排序，不写运行时）。
            ConfigureSort(e => e.ItemId, sortDb, sortPriorities, sortTiebreakers);
            Refresh();
        }

        /// <summary>重新筛选并刷新候选道具显示（经内建 排序 管线）。</summary>
        public void Refresh() => SetSourceItems(BuildCandidates());

        // ── 格子绑定 ──────────────────────────────────────────────────────────────

        // 复用仓库格子的 SetSlot：来源仓库 → InventoryId，道具 → ItemId（slotId 对候选显示无意义，置空）。
        protected override void BindCell(UiwInventoryItemCell cell, EquipmentCandidateEntry entry)
            => cell.SetSlot(entry.SourceInv, new RuntimeItemSlot(null, entry.ItemId, entry.Count));

        protected override void ClearCell(UiwInventoryItemCell cell) => cell.ClearAndHide();

        // ── 候选筛选 ──────────────────────────────────────────────────────────────

        private List<EquipmentCandidateEntry> BuildCandidates()
        {
            var result = new List<EquipmentCandidateEntry>();
            var invMgr = InventoryRuntimeManager.Instance;
            var eqMgr  = EquipmentRuntimeManager.Instance;
            if (!invMgr || eqMgr == null || _slotListDef == null || _sourceInventoryIds == null)
                return result;

            // 跨全部「装备仓库」汇总各道具持有量（保持首次出现顺序，并记录首个持有该道具的来源仓库）。
            var counts = new Dictionary<string, int>();
            var source = new Dictionary<string, string>();
            var order  = new List<string>();
            foreach (var invId in _sourceInventoryIds)
            {
                if (string.IsNullOrEmpty(invId)) continue;
                foreach (var slot in invMgr.GetSlots(invId))
                {
                    if (string.IsNullOrEmpty(slot.itemId)) continue;
                    if (!counts.ContainsKey(slot.itemId))
                    {
                        counts[slot.itemId] = 0;
                        source[slot.itemId] = invId;   // 首个持有该道具的仓库作为装备取出来源
                        order.Add(slot.itemId);
                    }
                    counts[slot.itemId] += slot.count;
                }
            }

            // 仅保留满足当前槽位列表道具限制的道具。
            foreach (var id in order)
                if (eqMgr.ItemMatchesSlotList(_slotListDef, id))
                    result.Add(new EquipmentCandidateEntry(source[id], id, counts[id]));

            return result;
        }
    }
}

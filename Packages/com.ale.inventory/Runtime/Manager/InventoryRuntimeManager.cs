using System;
using System.Collections.Generic;
using UnityEngine;
using Ale.Toolkit.Runtime;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 仓库系统运行时管理器（MonoBehaviour 单例）。
    /// 在 Scene 中挂载此组件并在 Inspector 拖入 <see cref="InventoryDatabase"/> 文件；
    /// 游戏启动时自动注册数据库并初始化所有仓库的运行时状态。
    ///
    /// <para>职责：</para>
    /// <list type="bullet">
    ///   <item>将数据库注册到 <see cref="InventoryDataManager"/>（定义查询层）</item>
    ///   <item>维护每个 <see cref="Inventory"/> 的 <see cref="RuntimeInventoryState"/>（运行时格子列表）</item>
    ///   <item>提供 TryAddItem / TryRemoveItem / SortInventory 等运行时操作接口</item>
    ///   <item>提供 GetSaveData / LoadSaveData 存档接口供游戏层 SaveManager 调用</item>
    /// </list>
    ///
    /// <para><b>本类按职责拆为多个分部文件</b>：本文件为核心（字段 / 初始化 / 数据获取 / 道具管理 / 整理排序入口 / 存档），
    /// 另有 <c>.Time.cs</c>（时间获取器）、<c>.UiHost.cs</c>（覆盖式 UI 与悬停弹窗宿主）、
    /// <c>.TestSeed.cs</c>（测试道具填充，仅编辑器 / 开发版构建）。
    /// 与实例状态无关的排序实现已独立为 <see cref="InventorySortService"/>。</para>
    /// </summary>
    public partial class InventoryRuntimeManager
        : ToolkitMonoSingleton<InventoryRuntimeManager>, ISaveable<RuntimeInventoryState>
    {
        [Header("数据库（可挂载多个，运行时合并查询）")]
        [SerializeField] private InventoryDatabase[] databases;
        
        /// <summary>inventoryId → 运行时状态。</summary>
        private readonly Dictionary<string, RuntimeInventoryState> _inventoryStates
            = new Dictionary<string, RuntimeInventoryState>();

        // ── 事件 ──────────────────────────────────────────────────────────────────

        /// <summary>仓库内容发生变化时触发。参数为 inventoryId。</summary>
        public event Action<string> OnInventoryChanged;

        // ── 初始化 ────────────────────────────────────────────────────────────────

        protected override void Init()
        {
            if (databases == null) return;

            // 注册数据库到定义查询层
            foreach (var db in databases)
            {
                if (db)
                    InventoryDataManager.Instance.Register(db);
            }

            // 为每个已定义的 Inventory 初始化空运行时状态（已有状态的仓库跳过，避免覆盖）
            BuildEmptyStates(skipExisting: true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 测试功能：向测试仓库自动注入道具（见 InventoryRuntimeManager.TestSeed.cs，发布构建中不参与编译）。
            TestFunction();
#endif
        }

        /// <summary>
        /// 为所有已注册数据库中的每个 <see cref="Inventory"/> 建立空运行时状态
        /// （固定容量仓库预分配带 slotId 的空槽，支持拖拽定位）。
        /// </summary>
        /// <param name="skipExisting">
        /// true = 已存在状态的仓库跳过（<see cref="Init"/> 用，不覆盖已加载 / 已填充的状态）；
        /// false = 无条件重建（<see cref="ResetAll"/> 用，调用方已先 <c>Clear()</c>）。
        /// </param>
        private void BuildEmptyStates(bool skipExisting)
        {
            if (databases == null) return;
            foreach (var db in databases)
            {
                if (!db) continue;
                foreach (var inv in db.Inventories)
                {
                    if (string.IsNullOrEmpty(inv.id)) continue;
                    if (skipExisting && _inventoryStates.ContainsKey(inv.id)) continue;

                    var state = new RuntimeInventoryState(inv.id);
                    // 固定容量仓库预分配空槽，保证每个位置都有 slotId（支持拖拽定位）
                    if (inv.capacity > 0)
                        for (int i = 0; i < inv.capacity; i++)
                            state.itemSlots.Add(new RuntimeItemSlot(Guid.NewGuid().ToString(), null, 0));
                    _inventoryStates[inv.id] = state;
                }
            }
        }

        
        #region 数据获取
        
        // ── 定义查询：一律转调 InventoryDataManager 的 O(1) 索引 ────────────────────
        // 此前这三个方法各自遍历本组件的 databases 数组、再在每个库里线性扫仓库列表；
        // 而 GetCapacity 在 TryAddItem / TryRemoveItem / TryRemoveItemById / GetFreeSpaceFor /
        // StackOrSwapSlots 上都是必经之路，等于每次增删都付两层嵌套线性扫描。
        // InventoryDataManager 已对全部已注册数据库建有惰性 O(1) 索引（Init 中已注册本组件的 databases），
        // 查询范围因此从「本组件的 databases」扩为「全部已注册数据库」——与商店 / 装备两个运行时管理器一致。

        /// <summary>查找 仓库定义（跨全部已注册数据库，先注册者优先）。未找到返回 null。</summary>
        private static Inventory FindInventoryDef(string inventoryId)
            => InventoryDataManager.Instance.GetInventory(inventoryId);

        /// <summary>查找 包含指定仓库定义的数据库（跨全部已注册数据库，先注册者优先）。未找到返回 null。</summary>
        private static InventoryDatabase FindDatabaseForInventory(string inventoryId)
            => InventoryDataManager.Instance.FindDatabaseForInventory(inventoryId);

        /// <summary>获取 仓库容量。返回 0 表示无限容量。</summary>
        private static int GetCapacity(string inventoryId)
            => FindInventoryDef(inventoryId)?.capacity ?? 0;


        // 仓库不存在时返回的共享空列表：避免每次未命中都分配一个新 List
        // （UI 刷新可能高频调用）。调用方<b>不得</b>写入 GetSlots 的返回值。
        private static readonly List<RuntimeItemSlot> EmptySlots = new List<RuntimeItemSlot>();

        /// <summary>
        /// 获取指定仓库的所有格子列表。仓库不存在时返回一个共享的空列表。
        /// <para><b>返回值仅供读取</b>：命中时返回的是运行时状态的实时引用，
        /// 未命中时返回的是全局共享的空列表——两种情况下写入都会造成意外后果。
        /// 需要修改内容请走 <see cref="TryAddItem"/> / <see cref="TryRemoveItem"/> /
        /// <see cref="SetSlotContent"/> 等接口；需要自行排序 / 过滤请先拷贝一份。</para>
        /// </summary>
        public List<RuntimeItemSlot> GetSlots(string inventoryId)
        {
            if (_inventoryStates.TryGetValue(inventoryId, out var state))
                return state.itemSlots;
            return EmptySlots;
        }
        
        /// <summary>计算指定仓库当前所有道具的总重量（道具 weight × 数量 累加）。</summary>
        public float GetTotalWeight(string inventoryId)
        {
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return 0f;

            // 每槽经 InventoryDataManager 的 O(1) 索引取道具（原为「先线性定位数据库、再逐槽在库内线性找道具」，
            // 整体 O(槽数 × 道具数)）；同时不再要求道具与仓库定义在同一个数据库里。
            var dm    = InventoryDataManager.Instance;
            float total = 0f;
            foreach (var slot in state.itemSlots)
            {
                if (string.IsNullOrEmpty(slot.itemId)) continue;
                var item = dm.GetItem(slot.itemId);
                if (item != null) total += item.weight * slot.count;
            }
            return total;
        }

        /// <summary>获取指定仓库的重量上限（0 = 无限制）。</summary>
        public float GetWeightLimit(string inventoryId)
        {
            var inv = FindInventoryDef(inventoryId);
            return inv?.weightLimit ?? 0f;
        }

        /// <summary>
        /// 将属性值 转换为 数值，用于比较排序。按 <see cref="AttributeValue.Type"/> 取值：
        /// Int/Bool/Enum/Float 取标量数值；Vector2/3/4、Color、VectorInt2/3/4 取模长（magnitude）；
        /// StringIntPair 取其整数值；其余类型返回 0。具体规则见 <see cref="AttributeValue.ToComparableNumber"/>。
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static double GetAttrNumeric(AttributeEntry entry)
            => entry?.value?.ToComparableNumber() ?? 0.0;

        #endregion
        
        #region 道具管理
        
        /// <summary>仓库中是否拥有足够数量的指定道具。</summary>
        public bool HasItem(string inventoryId, string itemId, int count = 1)
            => GetTotalCount(inventoryId, itemId) >= count;

        /// <summary>
        /// 向仓库添加道具。自动合并可堆叠的已有格，满了再开新格。
        /// 返回 false 当仓库容量已满且无法放入任何数量。
        /// </summary>
        public bool TryAddItem(string inventoryId, string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(inventoryId) || string.IsNullOrEmpty(itemId) || count <= 0)
                return false;

            if (!_inventoryStates.TryGetValue(inventoryId, out var state))
                return false;

            var itemDef  = InventoryDataManager.Instance.GetItem(itemId);
            int stackMax = itemDef?.stackLimit ?? 0;    // 0 = 无限制
            int capMax   = GetCapacity(inventoryId);    // 0 = 无限制

            int remaining = count;

            // 1. 先尝试填满已有的同道具格（可堆叠时）
            if (stackMax != 1)
            {
                foreach (var slot in state.itemSlots)
                {
                    if (slot.itemId != itemId) continue;
                    if (stackMax > 0 && slot.count >= stackMax) continue;

                    int canAdd = stackMax > 0 ? stackMax - slot.count : remaining;
                    int add    = Mathf.Min(canAdd, remaining);
                    slot.count += add;
                    remaining     -= add;

                    if (remaining <= 0) break;
                }
            }

            // 2. 剩余数量 填入空槽（预分配模式）或开新格（无限容量模式）。
            // 预分配模式用一个「只进不退」的游标扫空槽：本轮填过的槽不会再变空，
            // 游标之前也不会重新出现空槽，故整个 while 合计只扫一遍列表。
            // （原写法每轮都 state.itemSlots.Find(...) 从头扫，是 O(n²)，且每轮分配一个闭包委托。）
            int emptyCursor = 0;
            while (remaining > 0)
            {
                int take = stackMax > 0 ? Mathf.Min(stackMax, remaining) : remaining;
                if (capMax > 0)
                {
                    // 预分配模式：推进到第一个空槽（itemId 为空）
                    while (emptyCursor < state.itemSlots.Count
                           && !string.IsNullOrEmpty(state.itemSlots[emptyCursor].itemId))
                        emptyCursor++;
                    if (emptyCursor >= state.itemSlots.Count) break; // 无空槽 = 仓库已满

                    var emptySlot = state.itemSlots[emptyCursor];
                    emptySlot.itemId = itemId;
                    emptySlot.count  = take;
                }
                else
                {
                    // 无限容量：动态新增槽位
                    state.itemSlots.Add(new RuntimeItemSlot(Guid.NewGuid().ToString(), itemId, take));
                }
                remaining -= take;
            }

            bool added = remaining < count;
            if (added)
                OnInventoryChanged?.Invoke(inventoryId);

            return remaining == 0;
        }
        
        /// <summary>按 slotId 精确移除。count 大于当前格数量时视为全部移除该格。返回是否成功。</summary>
        public bool TryRemoveItem(string inventoryId, string slotId, int count = 1)
        {
            // 与 TryAddItem 一致的入口守卫。缺了它，count 为负时 remove 也为负、
            // slot.count -= remove 反而会**增加**道具数量。
            if (count <= 0) return false;
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return false;

            int capMax = GetCapacity(inventoryId);
            for (int i = 0; i < state.itemSlots.Count; i++)
            {
                var slot = state.itemSlots[i];
                if (slot.slotId != slotId) continue;

                int remove = Mathf.Min(count, slot.count);
                slot.count -= remove;

                if (slot.count <= 0)
                {
                    if (capMax > 0) { slot.itemId = null; slot.count = 0; } // 预分配：清空槽保留位置
                    else             state.itemSlots.RemoveAt(i);            // 无限容量：移除槽位
                }

                OnInventoryChanged?.Invoke(inventoryId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 按 itemId 搜索并扣减。从第一个匹配格开始扣减，不足时跨格累减。
        /// 返回 false 当仓库中该道具数量不足。
        /// </summary>
        public bool TryRemoveItemById(string inventoryId, string itemId, int count = 1)
        {
            // 与 TryAddItem 一致的入口守卫。缺了它，count <= 0 时循环体一次都不执行，
            // 却仍会广播 OnInventoryChanged 并返回 true（谎报「移除成功」）。
            if (count <= 0) return false;
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return false;

            if (GetTotalCount(inventoryId, itemId) < count)
                return false;

            int capMax    = GetCapacity(inventoryId);
            int remaining = count;
            for (int i = state.itemSlots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = state.itemSlots[i];
                if (slot.itemId != itemId) continue;

                int remove = Mathf.Min(remaining, slot.count);
                slot.count -= remove;
                remaining  -= remove;

                if (slot.count <= 0)
                {
                    if (capMax > 0) { slot.itemId = null; slot.count = 0; }
                    else             state.itemSlots.RemoveAt(i);
                }
            }

            OnInventoryChanged?.Invoke(inventoryId);
            return true;
        }
        
        /// <summary>统计指定道具在仓库中的总数量。</summary>
        public int GetTotalCount(string inventoryId, string itemId)
        {
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return 0;
            int total = 0;
            foreach (var slot in state.itemSlots)
                if (slot.itemId == itemId) total += slot.count;
            return total;
        }

        /// <summary>
        /// 计算指定仓库还能再容纳多少个指定道具（考虑容量与堆叠上限）。
        /// 无限容量、或容量内仍可无限堆叠时返回 <see cref="int.MaxValue"/>。
        /// 供商店等上层在交易前做「能否放下」校验，避免实际写入再回滚。
        /// </summary>
        public int GetFreeSpaceFor(string inventoryId, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return 0;

            var itemDef  = InventoryDataManager.Instance.GetItem(itemId);
            int stackMax = itemDef?.stackLimit ?? 0;    // 0 = 无限堆叠
            int capMax   = GetCapacity(inventoryId);     // 0 = 无限容量

            // 无限容量：总能开新格放入
            if (capMax <= 0) return int.MaxValue;

            long free = 0;

            // 1. 已有同道具格的剩余堆叠空间（可堆叠时）
            if (stackMax != 1)
            {
                foreach (var slot in state.itemSlots)
                {
                    if (slot.itemId != itemId) continue;
                    if (stackMax <= 0) return int.MaxValue; // 已有该道具格且无限堆叠 → 可放无限
                    free += stackMax - slot.count;
                }
            }

            // 2. 空槽容量
            int emptyCount = 0;
            foreach (var slot in state.itemSlots)
                if (string.IsNullOrEmpty(slot.itemId)) emptyCount++;

            if (emptyCount > 0)
            {
                if (stackMax <= 0) return int.MaxValue;  // 有空槽且无限堆叠 → 可放无限
                free += (long)emptyCount * stackMax;
            }

            return free > int.MaxValue ? int.MaxValue : (int)free;
        }

        /// <summary>
        /// 交换 两个格子的 道具数据（保留 slotId 不变）。
        /// slotIdA 或 slotIdB 不存在时不操作。用于网格拖拽换位（拖到有道具的格子）。
        /// </summary>
        public void SwapSlotContents(string inventoryId, string slotIdA, string slotIdB)
        {
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return;
            var slotA = state.GetSlot(slotIdA);
            var slotB = state.GetSlot(slotIdB);
            if (slotA == null || slotB == null) return;

            (slotA.itemId, slotB.itemId) = (slotB.itemId, slotA.itemId);
            (slotA.count,  slotB.count)  = (slotB.count,  slotA.count);

            OnInventoryChanged?.Invoke(inventoryId);
        }

        /// <summary>
        /// 按 slotId 获取指定仓库的格子（不存在返回 null）。返回实时引用，仅供只读查询（如读取该格道具 / 数量）。
        /// </summary>
        public RuntimeItemSlot GetSlot(string inventoryId, string slotId)
        {
            if (_inventoryStates.TryGetValue(inventoryId, out var state))
                return state.GetSlot(slotId);
            return null;
        }

        /// <summary>
        /// 直接设置指定格子的内容（<paramref name="itemId"/> 为空或 <paramref name="count"/> ≤ 0 表示清空该格）。
        /// 精确落位到指定 slot，供装备系统卸下 / 交换时使用（调用方自负容量 / 堆叠合理性）。
        /// 槽位不存在时返回 false。成功触发 <see cref="OnInventoryChanged"/>。
        /// </summary>
        public bool SetSlotContent(string inventoryId, string slotId, string itemId, int count)
        {
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return false;
            var slot = state.GetSlot(slotId);
            if (slot == null) return false;

            if (string.IsNullOrEmpty(itemId) || count <= 0) { slot.itemId = null; slot.count = 0; }
            else                                            { slot.itemId = itemId; slot.count = count; }

            OnInventoryChanged?.Invoke(inventoryId);
            return true;
        }

        /// <summary>
        /// 拖拽整理落点：把源格子拖到目标格子。
        /// 目标与源为<b>同一道具</b>且可堆叠、目标未满时，优先把源尽量堆叠进目标（能叠多少叠多少，源余量保留在原格）；
        /// 目标堆叠已满、或道具不同 / 不可堆叠 / 目标为空时，退回为<b>交换</b>两格内容（等同 <see cref="SwapSlotContents"/>）。
        /// slotId 不存在或两者相同时不操作。成功触发 <see cref="OnInventoryChanged"/>。
        /// </summary>
        public void StackOrSwapSlots(string inventoryId, string srcSlotId, string targetSlotId)
        {
            if (string.IsNullOrEmpty(srcSlotId) || srcSlotId == targetSlotId) return;
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return;
            var src    = state.GetSlot(srcSlotId);
            var target = state.GetSlot(targetSlotId);
            if (src == null || target == null) return;

            // 同一道具（且非空）→ 先尝试堆叠。
            if (!string.IsNullOrEmpty(src.itemId) && src.itemId == target.itemId)
            {
                int stackMax = InventoryDataManager.Instance.GetItem(src.itemId)?.stackLimit ?? 0;   // 0 = 无限堆叠
                int free     = stackMax > 0 ? stackMax - target.count : src.count;                    // 目标可再接收数量
                if (free > 0)
                {
                    int move      = Mathf.Min(free, src.count);
                    target.count += move;
                    src.count    -= move;
                    if (src.count <= 0)
                    {
                        if (GetCapacity(inventoryId) > 0) { src.itemId = null; src.count = 0; }  // 预分配：清空槽保留位置
                        else                               state.itemSlots.Remove(src);          // 无限容量：移除槽位
                    }
                    OnInventoryChanged?.Invoke(inventoryId);
                    return;   // 堆叠成功（含部分堆叠），不再交换
                }
                // free == 0（目标已满堆叠 / 不可堆叠）→ 落到下方交换
            }

            // 交换两格内容（不同道具 / 目标为空 / 目标堆叠已满，保留 slotId 不变）。
            (src.itemId, target.itemId) = (target.itemId, src.itemId);
            (src.count,  target.count)  = (target.count,  src.count);
            OnInventoryChanged?.Invoke(inventoryId);
        }

        #endregion

        #region 整理排序

        /// <summary>
        /// 按仓库定义的 <see cref="Inventory.sortPriorities"/> + <see cref="Inventory.sortTiebreakers"/>
        /// 对格子列表进行原地排序，并触发 <see cref="OnInventoryChanged"/>。
        /// </summary>
        public void SortInventory(string inventoryId)
        {
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return;

            var inv = FindInventoryDef(inventoryId);
            if (inv == null) return;

            var db = FindDatabaseForInventory(inventoryId);
            if (!db) return;

            // 合并主优先级和次优先级（主在前）
            var allPriorities = new List<SortPriority>(inv.sortPriorities);
            allPriorities.AddRange(inv.sortTiebreakers);
            if (allPriorities.Count == 0) return;

            InventorySortService.SortSlots(state.itemSlots, allPriorities, db);
            OnInventoryChanged?.Invoke(inventoryId);
        }

        /// <summary>
        /// 按指定优先级列表对 <see cref="RuntimeInventoryState.itemSlots"/> 进行原地排序并触发事件。
        /// 由 UI 层在用户手动点击排序/整理按钮时调用，使排序结果写入运行时状态（可存档）。
        /// 空槽（itemId == null）始终排在末尾。
        /// </summary>
        public void SortInventory(string inventoryId, List<SortPriority> priorities)
        {
            if (!_inventoryStates.TryGetValue(inventoryId, out var state)) return;
            var db = FindDatabaseForInventory(inventoryId);
            if (!db || priorities == null || priorities.Count == 0) return;

            InventorySortService.SortSlots(state.itemSlots, priorities, db);
            OnInventoryChanged?.Invoke(inventoryId);
        }

        #endregion

        #region 存档

        /// <inheritdoc cref="ISaveable{TState}.GetSaveData"/>
        public List<RuntimeInventoryState> GetSaveData()
        {
            var result = new List<RuntimeInventoryState>(_inventoryStates.Count);
            foreach (var kvp in _inventoryStates)
            {
                var clone = new RuntimeInventoryState(kvp.Key);
                foreach (var slot in kvp.Value.itemSlots)
                    clone.itemSlots.Add(slot.Clone());
                result.Add(clone);
            }
            return result;
        }

        /// <summary>
        /// 从存档数据恢复运行时状态（在 Init() 之后调用）。契约见 <see cref="ISaveable{TState}"/>。
        /// <para>本实现的覆盖分三步：清空 → 重建所有仓库的空骨架（固定容量仓库恢复预分配空槽）→ 叠加存档中的槽位。
        /// 未在存档中的仓库因此回到初始空态，而非残留上一局的内容。</para>
        /// </summary>
        public void LoadSaveData(List<RuntimeInventoryState> data)
        {
            // 覆盖当前内存状态：清空 → 重建所有仓库空骨架 → 叠加存档。
            _inventoryStates.Clear();
            BuildEmptyStates(skipExisting: false);

            if (data == null) return;

            foreach (var saved in data)
            {
                if (string.IsNullOrEmpty(saved.inventoryId)) continue;

                var state = new RuntimeInventoryState(saved.inventoryId);
                foreach (var slot in saved.itemSlots)
                    state.itemSlots.Add(slot.Clone());

                _inventoryStates[saved.inventoryId] = state;

                // 补足空槽至容量（兼容旧存档迁移）
                var invDef = FindInventoryDef(saved.inventoryId);
                if (invDef != null && invDef.capacity > 0)
                    for (int i = state.itemSlots.Count; i < invDef.capacity; i++)
                        state.itemSlots.Add(new RuntimeItemSlot(Guid.NewGuid().ToString(), null, 0));
            }
        }

        /// <summary>
        /// 清空全部仓库运行时状态并重建为初始空状态，如开始新游戏。契约见 <see cref="ISaveable"/>。
        /// <para>区别于其余三个管理器的裸 <c>Clear()</c>：仓库有「按容量预分配空槽」的概念，
        /// 清空后须重建空骨架，否则固定容量仓库会变成 0 格。</para>
        /// </summary>
        public void ResetAll()
        {
            _inventoryStates.Clear();
            BuildEmptyStates(skipExisting: false);
        }

        #endregion
    }
}

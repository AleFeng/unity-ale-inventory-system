using System;
using System.Collections.Generic;
using UnityEngine;

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
    /// </summary>
    public class InventoryRuntimeManager : InventorySystemMonoBehaviourSingleton<InventoryRuntimeManager>
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

            // 为每个已定义的 Inventory 初始化空运行时状态
            foreach (var db in databases)
            {
                if (!db) continue;
                foreach (var inv in db.Inventories)
                {
                    if (!string.IsNullOrEmpty(inv.id) && !_inventoryStates.ContainsKey(inv.id))
                    {
                        var state = new RuntimeInventoryState(inv.id);
                        // 固定容量仓库预分配空槽，保证每个位置都有 slotId（支持拖拽定位）
                        if (inv.capacity > 0)
                            for (int i = 0; i < inv.capacity; i++)
                                state.itemSlots.Add(new RuntimeItemSlot(Guid.NewGuid().ToString(), null, 0));
                        _inventoryStates[inv.id] = state;
                    }
                }
            }
            
            // 测试功能。测试用 道具列表自动注入等功能。
            TestFunction();
        }

        #region 时间

        // 各时钟类型的当前时间获取器（由游戏层在 GameInstance 中注册，将耦合集中到 GameInstance）。
        // 未注册的类型一律回退系统本地时间，从而无需为「本地时间」单独注册。
        private readonly Dictionary<ShopTimeType, Func<DateTime>> _timeGetters
            = new Dictionary<ShopTimeType, Func<DateTime>>();

        /// <summary>
        /// 注册某种时钟类型的当前时间获取器，使 <see cref="GetNow"/> 能返回游戏层的
        /// 「游戏时间 / 服务器时间」。通常在 GameInstance 中统一对接，例如：
        /// <code>InventoryRuntimeManager.Instance.RegisterTimeGetter(ShopTimeType.服务器时间, () =&gt; timeMgr.ServerNow);</code>
        /// 重复注册同一类型将覆盖之前的获取器；传入 null 则注销该类型。
        /// 「本地时间」无需注册——未注册时即回退系统本地时间。
        /// </summary>
        public void RegisterTimeGetter(ShopTimeType type, Func<DateTime> getter)
        {
            if (getter == null)
            {
                _timeGetters.Remove(type);
                return;
            }
            if (_timeGetters.ContainsKey(type))
                Debug.LogWarning($"[InventoryRuntimeManager] 时间获取器 '{type}' 已注册，将覆盖之前的注册。");
            _timeGetters[type] = getter;
        }

        /// <summary>注销某种时钟类型的时间获取器（之后该类型回退系统本地时间）。</summary>
        public void UnregisterTimeGetter(ShopTimeType type) => _timeGetters.Remove(type);

        /// <summary>
        /// 获取指定时钟类型的当前时间。优先走已注册的获取器；
        /// 未注册 / 获取器返回默认值时回退系统本地时间（<see cref="System.DateTime.Now"/>）。
        /// 商店刷新等周期性逻辑统一经此取时。
        /// </summary>
        public DateTime GetNow(ShopTimeType type)
        {
            if (_timeGetters.TryGetValue(type, out var getter) && getter != null)
            {
                var now = getter.Invoke();
                if (now != default) return now;
            }
            return DateTime.Now;
        }

        #endregion
        
        #region 数据获取
        
        /// <summary>
        /// 查找 仓库数据。优先从第一个数据库中查询到的定义返回。
        /// </summary>
        /// <param name="inventoryId"></param>
        /// <returns></returns>
        private Inventory FindInventoryDef(string inventoryId)
        {
            if (databases == null) return null;
            foreach (var db in databases)
            {
                if (!db) continue;
                var inv = db.GetInventory(inventoryId);
                if (inv != null) return inv;
            }
            return null;
        }
        
        /// <summary>
        /// 查找 包含指定仓库定义的数据库。
        /// 优先返回第一个包含该定义的数据库（通常是唯一的）。
        /// 返回 null 表示未找到。注意返回的数据库可能不包含该定义（但至少包含一个同 ID 定义）。如果需要确保定义存在，请先调用 FindInventoryDef()。
        /// </summary>
        /// <param name="inventoryId"></param>
        /// <returns></returns>
        private InventoryDatabase FindDatabaseForInventory(string inventoryId)
        {
            if (databases == null) return null;
            foreach (var db in databases)
            {
                if (!db) continue;
                if (db.GetInventory(inventoryId) != null) return db;
            }
            return null;
        }
        
        /// <summary>
        /// 获取 仓库容量。
        /// 返回 0 表示无限容量。优先从第一个数据库中查询到的定义返回。
        /// </summary>
        /// <param name="inventoryId"></param>
        /// <returns></returns>
        private int GetCapacity(string inventoryId)
        {
            var inv = FindInventoryDef(inventoryId);
            return inv?.capacity ?? 0;
        }
        
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
            var db = FindDatabaseForInventory(inventoryId);
            if (!db) return 0f;
            float total = 0f;
            foreach (var slot in state.itemSlots)
            {
                if (string.IsNullOrEmpty(slot.itemId)) continue;
                var item = db.GetItem(slot.itemId);
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
            var slotA = state.itemSlots.Find(itemSlot => itemSlot.slotId == slotIdA);
            var slotB = state.itemSlots.Find(itemSlot => itemSlot.slotId == slotIdB);
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
            if (string.IsNullOrEmpty(slotId)) return null;
            if (_inventoryStates.TryGetValue(inventoryId, out var state))
                return state.itemSlots.Find(s => s.slotId == slotId);
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
            var slot = state.itemSlots.Find(s => s.slotId == slotId);
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
            var src    = state.itemSlots.Find(s => s.slotId == srcSlotId);
            var target = state.itemSlots.Find(s => s.slotId == targetSlotId);
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
        /// 对格子列表进行原地排序。
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

            // 整次排序共用一份字段查表（见 SortLookup），比较器内不再逐次线性扫描。
            var lookup = new SortLookup(db);
            state.itemSlots.Sort((a, b) =>
            {
                bool aE = string.IsNullOrEmpty(a.itemId), bE = string.IsNullOrEmpty(b.itemId);
                if (aE && bE) return 0;
                if (aE)       return 1;  // 空槽排末尾
                if (bE)       return -1;
                return CompareSlots(a, b, allPriorities, lookup);
            });
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

            // 整次排序共用一份字段查表（见 SortLookup）。
            var lookup = new SortLookup(db);
            state.itemSlots.Sort((a, b) =>
            {
                bool aE = string.IsNullOrEmpty(a.itemId), bE = string.IsNullOrEmpty(b.itemId);
                if (aE && bE) return 0;
                if (aE)       return 1;
                if (bE)       return -1;
                return CompareSlots(a, b, priorities, lookup);
            });
            OnInventoryChanged?.Invoke(inventoryId);
        }

        /// <summary>
        /// 对任意 slot 列表按指定优先级排序（不触发事件，不写运行时状态）。
        /// 供 UI 层 autoSort 显示排序使用。空槽排末尾。
        /// </summary>
        public static void SortSlots(List<RuntimeItemSlot> slots, List<SortPriority> priorities,
            InventoryDatabase db)
        {
            if (priorities == null || priorities.Count == 0 || slots == null || slots.Count <= 1 || !db) return;

            // 整次排序共用一份字段查表（见 SortLookup）。
            var lookup = new SortLookup(db);
            slots.Sort((a, b) =>
            {
                bool aE = string.IsNullOrEmpty(a.itemId), bE = string.IsNullOrEmpty(b.itemId);
                if (aE && bE) return 0;
                if (aE)       return 1;
                if (bE)       return -1;
                return CompareSlots(a, b, priorities, lookup);
            });
        }

        /// <summary>
        /// 一次排序过程内复用的字段查表。把原先在<b>每次两两比较</b>里重复做的线性扫描
        /// —— 整理选项忽略列表（扫 <c>db.SortOptions</c>）、属性字段定义（扫全部模板 × 属性）、
        /// 道具模板与枚举类型、功能标签序号（扫 <c>db.FunctionTags</c>）—— 预先算成字典，
        /// 使比较器内的查找降到 O(1)。
        ///
        /// <para>只在单次排序期间存活、随即丢弃，因此不存在「数据改动后缓存过期」的问题。</para>
        /// </summary>
        internal sealed class SortLookup
        {
            private readonly InventoryDatabase _db;
            private readonly Dictionary<string, IReadOnlyList<string>> _ignoreIds
                = new Dictionary<string, IReadOnlyList<string>>();
            private readonly Dictionary<string, AttributeDefinition> _attrDefs
                = new Dictionary<string, AttributeDefinition>();
            private readonly Dictionary<string, ItemTemplate> _templates
                = new Dictionary<string, ItemTemplate>();
            private readonly Dictionary<string, EnumType> _enumTypes
                = new Dictionary<string, EnumType>();
            // 惰性：仅当排序用到 "__tagOrder__" 字段时才构建。
            private Dictionary<string, int> _tagOrder;

            internal SortLookup(InventoryDatabase db) => _db = db;

            /// <summary>取该排序字段的忽略 ID 列表（未配置返回 null）。</summary>
            internal IReadOnlyList<string> IgnoreIds(string field)
            {
                if (_ignoreIds.TryGetValue(field, out var ids)) return ids;
                ids = _db ? _db.GetSortOption(field)?.EffectiveIgnoreIds : null;
                _ignoreIds[field] = ids;
                return ids;
            }

            /// <summary>取属性字段定义（模板与功能标签中先到先得；未找到返回 null）。</summary>
            internal AttributeDefinition AttrDef(string attrId)
            {
                if (_attrDefs.TryGetValue(attrId, out var def)) return def;
                def = FindAttrDef(attrId, _db);
                _attrDefs[attrId] = def;
                return def;
            }

            /// <summary>取道具模板（未找到返回 null）。</summary>
            internal ItemTemplate Template(string templateName)
            {
                if (string.IsNullOrEmpty(templateName)) return null;
                if (_templates.TryGetValue(templateName, out var t)) return t;
                t = _db ? _db.GetTemplate(templateName) : null;
                _templates[templateName] = t;
                return t;
            }

            /// <summary>取枚举类型（未找到返回 null）。</summary>
            internal EnumType EnumTypeOf(string enumName)
            {
                if (string.IsNullOrEmpty(enumName)) return null;
                if (_enumTypes.TryGetValue(enumName, out var e)) return e;
                e = _db ? _db.GetEnumType(enumName) : null;
                _enumTypes[enumName] = e;
                return e;
            }

            /// <summary>功能标签名 → 在 <c>db.FunctionTags</c> 中的序号（越小优先级越高；未定义返回 int.MaxValue）。</summary>
            internal int TagOrder(string tagName)
            {
                if (_tagOrder == null)
                {
                    _tagOrder = new Dictionary<string, int>();
                    if (_db)
                        for (int i = 0; i < _db.FunctionTags.Count; i++)
                        {
                            string n = _db.FunctionTags[i]?.name;
                            if (!string.IsNullOrEmpty(n) && !_tagOrder.ContainsKey(n))
                                _tagOrder[n] = i;
                        }
                }
                return !string.IsNullOrEmpty(tagName) && _tagOrder.TryGetValue(tagName, out int idx)
                    ? idx : int.MaxValue;
            }
        }

        /// <summary>
        /// 按道具 ID 对任意列表做<b>显示排序</b>（原地排序，不触发事件、不写运行时状态）。
        /// 供 UI 层把「商品 / 蓝图 / 候选装备」等条目按其道具属性排序时使用。
        ///
        /// <para>相比在比较器里自行 <c>new RuntimeItemSlot(...)</c> 再调 <see cref="CompareSlots(RuntimeItemSlot,RuntimeItemSlot,List{SortPriority},InventoryDatabase)"/>，
        /// 本方法整次排序只建<b>一份</b>字段查表、只用<b>两个</b>复用的临时槽位——
        /// 省掉每次比较的两次对象分配与多次线性扫描。</para>
        /// </summary>
        /// <param name="list">待原地排序的列表。</param>
        /// <param name="itemIdSelector">从元素取出用于属性比较的道具 ID。</param>
        /// <param name="priorities">排序优先级（按顺序比较，取首个非零结果）。</param>
        /// <param name="db">属性定义与整理选项的来源数据库。</param>
        public static void SortByItemId<T>(List<T> list, Func<T, string> itemIdSelector,
            List<SortPriority> priorities, InventoryDatabase db)
        {
            if (list == null || list.Count <= 1 || itemIdSelector == null
                || priorities == null || priorities.Count == 0 || !db) return;

            var lookup = new SortLookup(db);
            // 两个复用的临时槽位：比较器只读 itemId，List.Sort 为单线程同步执行，复用安全。
            var sa = new RuntimeItemSlot(null, null, 0);
            var sb = new RuntimeItemSlot(null, null, 0);
            list.Sort((x, y) =>
            {
                sa.itemId = itemIdSelector(x);
                sb.itemId = itemIdSelector(y);
                return CompareSlots(sa, sb, priorities, lookup);
            });
        }

        /// <summary>
        /// 比较两个 道具槽位。
        /// 优先级列表按顺序尝试比较，直到找到第一个非零结果返回；如果所有优先级都相等则返回 0。
        ///
        /// <para><b>批量排序请改用 <see cref="SortSlots"/> / <see cref="SortByItemId{T}"/></b>：
        /// 它们整次排序只构建一份字段查表，而本重载<b>每次调用</b>都会新建一份。</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="priorities"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static int CompareSlots(RuntimeItemSlot a, RuntimeItemSlot b,
            List<SortPriority> priorities, InventoryDatabase db)
            => CompareSlots(a, b, priorities, new SortLookup(db));

        /// <summary>比较两个道具槽位（复用调用方预建的字段查表）。</summary>
        internal static int CompareSlots(RuntimeItemSlot a, RuntimeItemSlot b,
            List<SortPriority> priorities, SortLookup lookup)
        {
            foreach (var sp in priorities)
            {
                int cmp = CompareByField(a, b, sp.field, sp.ascending, lookup);
                if (cmp != 0) return cmp;
            }
            return 0;
        }

        /// <summary>
        /// 根据指定字段比较两个道具槽位。
        /// 支持特殊字段 "__id__"（按 itemId 字典序）和 "__tagOrder__"（按第一个标签在数据库定义的顺序）。
        /// 支持数值和字符串类型，字符串按长度比较。
        /// </summary>
        /// <param name="slotA"></param>
        /// <param name="slotB"></param>
        /// <param name="field"></param>
        /// <param name="ascending"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static int CompareByField(RuntimeItemSlot slotA, RuntimeItemSlot slotB,
            string field, bool ascending, InventoryDatabase db)
            => CompareByField(slotA, slotB, field, ascending, new SortLookup(db));

        /// <summary>按指定字段比较两个道具槽位（复用调用方预建的字段查表）。</summary>
        internal static int CompareByField(RuntimeItemSlot slotA, RuntimeItemSlot slotB,
            string field, bool ascending, SortLookup lookup)
        {
            int sign = ascending ? 1 : -1;

            // 中文别名 → 内部特殊键
            if (field == "道具ID")  field = "__id__";
            if (field == "功能标签") field = "__tagOrder__";

            // 读取该字段对应整理选项的忽略列表（内置 ignoreIds，兼容未迁移旧数据）
            IReadOnlyList<string> ignoreIds = lookup.IgnoreIds(field);

            if (field == "__id__")
            {
                bool aIgn = ignoreIds != null && ContainsStr(ignoreIds, slotA.itemId);
                bool bIgn = ignoreIds != null && ContainsStr(ignoreIds, slotB.itemId);
                if (aIgn != bIgn) return aIgn ? 1 : -1;
                if (aIgn) return 0;
                int c = string.Compare(slotA.itemId ?? "", slotB.itemId ?? "",
                    StringComparison.Ordinal);
                return c * sign;
            }

            if (field == "__tagOrder__")
            {
                int oa = GetTagOrder(slotA.itemId, lookup, ignoreIds);
                int ob = GetTagOrder(slotB.itemId, lookup, ignoreIds);
                if (oa == int.MaxValue && ob == int.MaxValue) return 0;
                if (oa == int.MaxValue) return 1;
                if (ob == int.MaxValue) return -1;
                return oa.CompareTo(ob) * sign;
            }

            var itemA  = InventoryDataManager.Instance.GetItem(slotA.itemId);
            var itemB  = InventoryDataManager.Instance.GetItem(slotB.itemId);
            var entryA = itemA?.GetEntry(field);
            var entryB = itemB?.GetEntry(field);

            bool aIgnored = IsIgnoredByField(entryA, field, ignoreIds, lookup);
            bool bIgnored = IsIgnoredByField(entryB, field, ignoreIds, lookup);
            if (aIgnored != bIgnored) return aIgnored ? 1 : -1;
            if (aIgnored) return 0;

            if (entryA?.value?.Type == EFieldType.String
             || entryB?.value?.Type == EFieldType.String)
            {
                string sa = entryA?.value?.AsString ?? string.Empty;
                string sb = entryB?.value?.AsString ?? string.Empty;
                int lenCmp = sa.Length.CompareTo(sb.Length);
                if (lenCmp != 0) return lenCmp * sign;
                return string.Compare(sa, sb, StringComparison.Ordinal) * sign;
            }

            double ka = GetAttrNumeric(entryA);
            double kb = GetAttrNumeric(entryB);
            return ka.CompareTo(kb) * sign;
        }
        
        /// <summary>
        /// 判断属性值是否在整理选项的忽略列表中。
        /// 对于枚举类型会将数值转换为名称进行匹配。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="field"></param>
        /// <param name="ignoreIds"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static bool IsIgnoredByField(AttributeEntry entry, string field,
            IReadOnlyList<string> ignoreIds, InventoryDatabase db)
            => IsIgnoredByField(entry, field, ignoreIds, new SortLookup(db));

        /// <summary>判断属性值是否在忽略列表中（复用调用方预建的字段查表）。</summary>
        internal static bool IsIgnoredByField(AttributeEntry entry, string field,
            IReadOnlyList<string> ignoreIds, SortLookup lookup)
        {
            if (ignoreIds == null || ignoreIds.Count == 0 || entry?.value == null) return false;
            var v = entry.value;
            switch (v.Type)
            {
                case EFieldType.String:
                    return ContainsStr(ignoreIds, v.AsString);
                case EFieldType.Enum:
                {
                    // 枚举存的是 EnumItem.value（自增、永不回收的不可变值），不是 items 的下标——
                    // 删过枚举项后二者会错位，必须按值查找（与 AttributeValue.EnumValueName 一致）。
                    // 枚举类型引用优先取属性定义，取不到时回退属性值自身持久化的 EnumTypeRef。
                    var    def      = lookup.AttrDef(field);
                    string enumRef  = !string.IsNullOrEmpty(def?.enumTypeRef) ? def.enumTypeRef : v.EnumTypeRef;
                    var    enumType = lookup.EnumTypeOf(enumRef);
                    string name     = enumType?.GetItemByValue(v.AsEnumValue)?.name ?? v.AsEnumValue.ToString();
                    return ContainsStr(ignoreIds, name);
                }
                case EFieldType.Int:
                    return ContainsStr(ignoreIds, v.AsInt.ToString());
                case EFieldType.Bool:
                    return ContainsStr(ignoreIds, v.AsInt != 0 ? "true" : "false");
                case EFieldType.Float:
                    return ContainsStr(ignoreIds, v.AsFloat.ToString("G"));
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 查找属性定义。
        /// 优先从第一个数据库中查询到的定义返回。
        /// </summary>
        /// <param name="attrId"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static AttributeDefinition FindAttrDef(string attrId, InventoryDatabase db)
        {
            if (!db) return null;
            foreach (var tmpl in db.ItemTemplates)
                foreach (var def in tmpl.attributes)
                    if (def.id == attrId) return def;
            foreach (var tag in db.FunctionTags)
                foreach (var def in tag.attributes)
                    if (def.id == attrId) return def;
            return null;
        }
        
        /// <summary>
        /// 判断 字符串列表是否包含指定值。
        /// 用于整理选项的忽略列表匹配。
        /// </summary>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ContainsStr(IReadOnlyList<string> list, string value)
        {
            foreach (var s in list)
                if (s == value) return true;
            return false;
        }
        
        /// <summary>
        /// 获取 道具的 功能标签序号。
        /// 返回该道具的第一个标签在数据库定义的 FunctionTags 列表中的索引（越小优先级越高）。
        /// 如果没有标签或所有标签都不在列表中，则返回 int.MaxValue。
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="db"></param>
        /// <param name="ignoreIds"></param>
        /// <returns></returns>
        public static int GetTagOrder(string itemId, InventoryDatabase db,
            IReadOnlyList<string> ignoreIds)
            => GetTagOrder(itemId, new SortLookup(db), ignoreIds);

        /// <summary>取道具的功能标签序号（复用调用方预建的字段查表）。</summary>
        internal static int GetTagOrder(string itemId, SortLookup lookup,
            IReadOnlyList<string> ignoreIds)
        {
            var item = InventoryDataManager.Instance.GetItem(itemId);
            if (item == null) return int.MaxValue;

            // 道具自身标签优先，再回退到模板继承标签（与编辑器展示行为一致）
            foreach (string tag in item.tagRefs)
            {
                if (ignoreIds != null && ContainsStr(ignoreIds, tag)) continue;
                int order = lookup.TagOrder(tag);
                if (order != int.MaxValue) return order;
            }

            // 模板只在道具自身标签全部落空时才需要，故延后解析。
            var tmpl = lookup.Template(item.templateRef);
            if (tmpl != null)
                foreach (string tag in tmpl.tagRefs)
                {
                    if (ignoreIds != null && ContainsStr(ignoreIds, tag)) continue;
                    int order = lookup.TagOrder(tag);
                    if (order != int.MaxValue) return order;
                }

            return int.MaxValue;
        }
        
        #endregion

        #region UI设置

        [Header("UI设置")]
        [Tooltip("弹窗、幽灵图标、下拉窗等覆盖式UI的根节点。为空则运行时自动查找场景中首个 Canvas。")]
        [SerializeField] private Transform coverUiRoot;

        [Tooltip("是否将覆盖式UI（弹窗 / 幽灵图标等）强制设置到下方指定的 Layer。\n" +
                 "当使用独立 UI 摄像机、且其 Culling Mask 仅渲染 UI 层时开启：弹窗 / 幽灵等会分配独立 Canvas，" +
                 "其 Layer 可能与父级不一致，需在实例化后重新指定，UI 摄像机方可渲染。")]
        [SerializeField] private bool applyCoverUiLayer;

        [Layer]
        [Tooltip("覆盖式UI 强制设置到的 Layer（如 UI）。仅当上方开关开启时生效。")]
        [SerializeField] private int coverUiLayer;

        /// <summary>
        /// 设置 覆盖UI根节点
        /// </summary>
        /// <param name="parent"></param>
        public void SetCoverUiRoot(Transform parent)
        {
            coverUiRoot = parent;
        }

        /// <summary>
        /// 设置 覆盖式UI 强制 Layer（同时开启强制开关）。layer 会被约束到 0~31。
        /// </summary>
        public void SetCoverUiLayer(int layer)
        {
            coverUiLayer      = Mathf.Clamp(layer, 0, 31);
            applyCoverUiLayer = true;
        }

        /// <summary>
        /// 按层名设置 覆盖式UI 强制 Layer（同时开启强制开关）。层名不存在则记警告且不改动。
        /// </summary>
        public void SetCoverUiLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[InventoryRuntimeManager] Layer 名称 \"{layerName}\" 不存在，未设置 覆盖式UI Layer。");
                return;
            }
            SetCoverUiLayer(layer);
        }

        /// <summary>关闭「覆盖式UI 强制 Layer」（此后不再改动覆盖式UI的 Layer）。</summary>
        public void DisableCoverUiLayer() => applyCoverUiLayer = false;

        /// <summary>
        /// 将指定 覆盖式UI 对象（及其所有子级）递归设置到配置的 Layer。
        /// 弹窗、幽灵图标等在实例化 / 创建后统一调用；未开启强制开关时为无操作。
        /// 递归是必要的：带独立 Canvas 的子级也须落到目标 Layer，UI 摄像机（仅渲染该 Layer）方可渲染。
        /// </summary>
        public void ApplyCoverUiLayer(GameObject go)
        {
            if (!go || !applyCoverUiLayer) return;
            SetLayerRecursively(go, coverUiLayer);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        #region 道具悬停弹窗

        [Header("道具悬停弹窗")]
        [Tooltip("道具悬停详情弹窗预制体：运行时由本管理器全局实例化一次。其根节点需实现 IItemTooltip（如 UI 层 UiwItemTooltip）。可空。")]
        [SerializeField] private GameObject itemTooltipPrefab;
        
        private IItemTooltip _itemTooltip;
        private bool         _itemTooltipResolved;

        /// <summary>
        /// 全局道具悬停弹窗（首次访问时按 <see cref="itemTooltipPrefab"/> 懒实例化一次）。
        /// 未配置预制体时为 null。UI 层经本管理器统一调用，将全局共用功能集中于此管理。
        /// </summary>
        public IItemTooltip ItemTooltip => EnsureItemTooltip();

        private IItemTooltip EnsureItemTooltip()
        {
            if (_itemTooltipResolved) return _itemTooltip;
            _itemTooltipResolved = true;

            if (!itemTooltipPrefab) return _itemTooltip = null;

            var parent = coverUiRoot ? coverUiRoot : FindCanvasTransform();
            var go     = parent ? Instantiate(itemTooltipPrefab, parent) : Instantiate(itemTooltipPrefab);
            go.transform.SetAsLastSibling();   // 置于父级最上层渲染
            ApplyCoverUiLayer(go);             // 覆盖式UI：按需强制到指定 Layer（如 UI）
            _itemTooltip = go.GetComponent<IItemTooltip>();
            if (_itemTooltip == null)
                Debug.LogWarning("[InventoryRuntimeManager] itemTooltipPrefab 根节点未实现 IItemTooltip（如 UiwItemTooltip），悬停弹窗不可用。");
            return _itemTooltip;
        }

        private static Transform FindCanvasTransform()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            return canvas ? canvas.transform : null;
        }

        /// <summary>在光标处（屏幕坐标）显示指定道具的悬停详情弹窗（全局统一入口）。count 为持有数量（显示在数量文本）。</summary>
        public void ShowItemTooltip(string itemId, int count, Vector2 screenPos)
            => EnsureItemTooltip()?.Show(itemId, count, screenPos);

        /// <summary>隐藏（原位淡出）道具悬停弹窗。未实例化时为无操作。</summary>
        public void HideItemTooltip()
        {
            if (_itemTooltipResolved) _itemTooltip?.Hide();
        }

        #endregion

        #region 技能悬停弹窗

        [Header("技能悬停弹窗")]
        [Tooltip("技能悬停详情弹窗预制体：运行时由本管理器全局实例化一次。其根节点需实现 ISkillTooltip（如 UI 层 UiwSkillTooltip）。可空。父节点复用上方「弹窗实例的父节点」。")]
        [SerializeField] private GameObject skillTooltipPrefab;

        private ISkillTooltip _skillTooltip;
        private bool          _skillTooltipResolved;

        /// <summary>
        /// 全局技能悬停弹窗（首次访问时按 <see cref="skillTooltipPrefab"/> 懒实例化一次）。
        /// 未配置预制体时为 null。UI 层经本管理器统一调用，将全局共用功能集中于此管理。
        /// </summary>
        public ISkillTooltip SkillTooltip => EnsureSkillTooltip();

        private ISkillTooltip EnsureSkillTooltip()
        {
            if (_skillTooltipResolved) return _skillTooltip;
            _skillTooltipResolved = true;

            if (!skillTooltipPrefab) return _skillTooltip = null;

            var parent = coverUiRoot ? coverUiRoot : FindCanvasTransform();
            var go     = parent ? Instantiate(skillTooltipPrefab, parent) : Instantiate(skillTooltipPrefab);
            go.transform.SetAsLastSibling();   // 置于父级最上层渲染
            ApplyCoverUiLayer(go);             // 覆盖式UI：按需强制到指定 Layer（如 UI）
            _skillTooltip = go.GetComponent<ISkillTooltip>();
            if (_skillTooltip == null)
                Debug.LogWarning("[InventoryRuntimeManager] skillTooltipPrefab 根节点未实现 ISkillTooltip（如 UiwSkillTooltip），技能悬停弹窗不可用。");
            return _skillTooltip;
        }

        /// <summary>在光标处（屏幕坐标）显示指定技能的悬停详情弹窗（全局统一入口）。</summary>
        public void ShowSkillTooltip(Skill skill, Vector2 screenPos)
            => EnsureSkillTooltip()?.Show(skill, screenPos);

        /// <summary>隐藏（原位淡出）技能悬停弹窗。未实例化时为无操作。</summary>
        public void HideSkillTooltip()
        {
            if (_skillTooltipResolved) _skillTooltip?.Hide();
        }

        #endregion

        #endregion
        
        #region 存档

        /// <summary>获取全部仓库运行时状态的深拷贝（由游戏层 SaveManager 序列化）。</summary>
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
        /// 从存档数据恢复运行时状态（在 Init() 之后调用）。
        /// 存档中存在的仓库 ID 覆盖当前状态；数据库中有但存档中没有的仓库保持空状态。
        /// </summary>
        public void LoadSaveData(List<RuntimeInventoryState> data)
        {
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

        #endregion
        
        #region 测试功能
        /// <summary>
        /// 测试道具条目。
        /// </summary>
        [Serializable]
        public class TestItemEntry
        {
            [Tooltip("道具 ID，须与 InventoryDatabase 中的道具 ID 完全一致。")]
            public string itemId;
            [Tooltip("填入的数量（最小 1）。")]
            [Min(1)] public int count = 1;
        }
        
        [Header("测试功能")]
        [Tooltip("自动向 测试仓库填入道具。")]
        [SerializeField] private bool autoPopulateOnStart = true;
        [Tooltip("测试目标仓库ID（需与数据库中的 Inventory.id 一致）。")]
        [SerializeField] private string testInventoryId = "玩家背包";
        [Tooltip("测试目标仓库 自动填入的道具列表。")]
        [SerializeField] private TestItemEntry[] testItems;

        [Tooltip("额外把所有数据库（Databases）里配置的道具，各添加若干个到测试仓库。" +
                 "已在上面「测试道具列表」中配置的道具会跳过（保留其指定数量，不重复添加）。")]
        [SerializeField] private bool addAllConfiguredItems;
        [Tooltip("「添加所有配置表道具」时，每种道具添加的数量（最小 1）。")]
        [Min(1)] [SerializeField] private int addAllItemCount = 1;

        /// <summary>
        /// 编辑器测试：向 <see cref="testInventoryId"/> 填入 <see cref="testItems"/>。
        /// 由 <see cref="Init"/> 在 Awake 时机调用；仅填充数据，界面由各视图自行打开。
        /// </summary>
        private void TestFunction()
        {
            AddTestItems(); // 先添加测试道具列表
            AddAllConfiguredItems(); // 再添加所有配置表道具（跳过已在测试列表的道具）
        }
        
        /// <summary>
        /// 测试功能：添加 测试用道具列表
        /// </summary>
        private void AddTestItems()
        {
            if (!autoPopulateOnStart || testItems == null) return;

            if (string.IsNullOrEmpty(testInventoryId))
            {
                Debug.LogWarning("[InventoryTest] testInventoryId 为空，跳过。");
                return;
            }

            foreach (var entry in testItems)
            {
                if (string.IsNullOrEmpty(entry.itemId)) continue;
                if (!TryAddItem(testInventoryId, entry.itemId, Mathf.Max(1, entry.count)))
                    Debug.LogWarning($"[InventoryTest] 添加失败：{entry.itemId} × {entry.count}" +
                                     "（仓库已满、道具 ID 不存在或参数无效）");
            }
        }
        
        /// <summary>
        /// 测试功能：把所有数据库（<see cref="databases"/>）中配置的道具，各按 <see cref="addAllItemCount"/>
        /// 的数量添加到 <see cref="testInventoryId"/>。已在 <see cref="testItems"/> 中配置的道具会跳过
        /// （保留其指定数量，不重复添加）；同一道具 ID 跨库仅添加一次。由 <see cref="Init"/> 调用。
        /// </summary>
        private void AddAllConfiguredItems()
        {
            if (!autoPopulateOnStart || !addAllConfiguredItems || databases == null) return;

            if (string.IsNullOrEmpty(testInventoryId))
            {
                Debug.LogWarning("[InventoryTest] testInventoryId 为空，跳过「添加所有配置表道具」。");
                return;
            }

            // 预置「已在 testItems 中配置的道具 ID」；添加时 HashSet 同时承担 跨库去重
            // （skip.Add 返回 false = 已在 testItems 或已处理过 → 跳过）。
            var skip = new HashSet<string>();
            if (testItems != null)
                foreach (var e in testItems)
                    if (e != null && !string.IsNullOrEmpty(e.itemId)) skip.Add(e.itemId);

            int count = Mathf.Max(1, addAllItemCount);
            foreach (var db in databases)
            {
                if (!db) continue;
                foreach (var item in db.Items)
                {
                    if (item == null || string.IsNullOrEmpty(item.id)) continue;
                    if (!skip.Add(item.id)) continue;   // 已在 testItems 或已处理过 → 跳过
                    if (!TryAddItem(testInventoryId, item.id, count))
                        Debug.LogWarning($"[InventoryTest] 添加失败：{item.id} × {count}" +
                                         "（仓库已满、道具 ID 不存在或参数无效）");
                }
            }
        }

        #endregion
    }
}

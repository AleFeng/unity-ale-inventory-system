using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 商店系统运行时管理器（非 MonoBehaviour 单例，首次访问自动创建）。
    ///
    /// <para>职责：</para>
    /// <list type="bullet">
    ///   <item>价格解析：从命中道具的 <see cref="Shop.priceAttrSource"/>（StringIntPair 货币→价）× 倍率求单价（可多货币）</item>
    ///   <item>货币 / 持有量统计：跨 <see cref="Shop.tradeInventoryRefs"/> 汇总</item>
    ///   <item>刷新：按 <see cref="ShopRefreshSchedule"/> 周期性重置「可交易次数」（商品级覆盖组级）</item>
    ///   <item>交易：购买（售卖店）/ 回收（回收店），自动按 可交易次数 / 货币 / 仓库容量 下调成交次数</item>
    ///   <item>存档：<see cref="GetSaveData"/> / <see cref="LoadSaveData"/> 持久化每玩家交易进度</item>
    /// </list>
    ///
    /// <para>道具数据经 <see cref="InventoryDataManager"/> 查询；仓库读写与时间一律经
    /// <see cref="InventoryRuntimeManager"/>。等价交换类型本期为占位，交易接口返回
    /// <see cref="ShopTradeFailReason.NotSupported"/>。</para>
    /// </summary>
    public class ShopRuntimeManager : InventorySystemSingleton<ShopRuntimeManager>
    {
        /// <summary>shopId → 每玩家运行时状态（交易进度）。按需创建。</summary>
        private readonly Dictionary<string, ShopRuntimeState> _shopStates
            = new Dictionary<string, ShopRuntimeState>();

        /// <summary>商店交易进度发生变化时触发。参数为 shopId。供 UI 刷新。</summary>
        public event Action<string> OnShopChanged;

        protected override void Init()
        {
            // 商品目录来自已注册数据库（由 InventoryRuntimeManager / InventoryDataManager 提供），
            // 此处无需预初始化；每玩家进度按需创建。
        }

        #region 查询：价格 / 货币 / 持有量 / 容量

        /// <summary>
        /// 计算单次交易的价格（货币ID → 金额）。
        /// 读取命中道具上 <see cref="Shop.priceAttrSource"/> 指向的 StringIntPair 属性，
        /// 各货币对的价格乘以 <see cref="ShopCommodity.priceMultiplier"/> 后汇总。
        /// 无价格来源 / 无该属性时返回空字典（视为免费 / 无收益）。
        /// </summary>
        public Dictionary<string, int> GetUnitPrice(Shop shop, ShopCommodity commodity)
        {
            var result = new Dictionary<string, int>();
            if (shop == null || commodity == null || string.IsNullOrEmpty(commodity.itemId)) return result;
            if (string.IsNullOrEmpty(shop.priceAttrSource)) return result;

            var item = InventoryDataManager.Instance.GetItem(commodity.itemId);
            var av   = item?.GetAttributeValue(shop.priceAttrSource);
            if (av == null || av.Type != EFieldType.StringIntPair) return result;

            int n = av.Count;
            for (int i = 0; i < n; i++)
            {
                var (currency, price) = av.GetStringIntPair(i);
                if (string.IsNullOrEmpty(currency)) continue;
                int p = ApplyMultiplier(price, commodity.priceMultiplier);
                result.TryGetValue(currency, out var existing);
                result[currency] = existing + p;
            }
            return result;
        }

        /// <summary>计算 <paramref name="times"/> 次交易的总价（货币ID → 金额）。</summary>
        public Dictionary<string, int> GetTotalPrice(Shop shop, ShopCommodity commodity, int times)
        {
            var unit = GetUnitPrice(shop, commodity);
            if (times <= 1) return unit;
            var result = new Dictionary<string, int>(unit.Count);
            foreach (var kv in unit)
                result[kv.Key] = kv.Value * times;
            return result;
        }

        /// <summary>
        /// 统计玩家在该商店交易仓库中持有的指定道具总数量。
        /// 同样适用于统计某种货币的持有量（货币即 id 等于货币ID 的道具）。
        /// </summary>
        public int GetOwnedCount(Shop shop, string itemId)
        {
            var invMgr = InventoryRuntimeManager.Instance;
            if (invMgr == null || shop == null || string.IsNullOrEmpty(itemId)) return 0;
            long total = 0;
            foreach (var refId in shop.tradeInventoryRefs)
                total += invMgr.GetTotalCount(refId, itemId);
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        /// <summary>
        /// 计算该商店交易仓库还能再容纳多少个指定道具（跨所有仓库汇总）。
        /// 任一仓库可无限容纳时返回 <see cref="int.MaxValue"/>。
        /// </summary>
        public int GetFreeSpace(Shop shop, string itemId)
        {
            var invMgr = InventoryRuntimeManager.Instance;
            if (invMgr == null || shop == null || string.IsNullOrEmpty(itemId)) return 0;
            long total = 0;
            foreach (var refId in shop.tradeInventoryRefs)
            {
                int f = invMgr.GetFreeSpaceFor(refId, itemId);
                if (f == int.MaxValue) return int.MaxValue;
                total += f;
            }
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        #endregion

        #region 查询：可交易次数（含刷新）

        /// <summary>
        /// 获取该商品在「当前刷新周期内」的剩余可交易次数（先按刷新计划结算一次）。
        /// 无限次数（tradeLimit &lt; 0）返回 <see cref="int.MaxValue"/>。
        /// </summary>
        public int GetRemainingTrades(Shop shop, ShopCommodity commodity)
        {
            if (shop == null || commodity == null) return 0;
            if (!Locate(shop, commodity, out var group, out var gi, out var ci))
                return commodity.tradeLimit < 0 ? int.MaxValue : Mathf.Max(0, commodity.tradeLimit);
            return RemainingTradesCore(shop, group, gi, commodity, ci);
        }

        /// <summary>获取该商品当前可购买的最大次数（综合 可交易次数 / 货币 / 仓库容量）。</summary>
        public int GetMaxPurchasable(Shop shop, ShopCommodity commodity)
        {
            if (shop == null || commodity == null || shop.shopType != ShopType.Sell) return 0;
            if (!Locate(shop, commodity, out var group, out var gi, out var ci)) return 0;

            int max = RemainingTradesCore(shop, group, gi, commodity, ci);
            if (max <= 0) return 0;

            foreach (var kv in GetUnitPrice(shop, commodity))
            {
                if (kv.Value <= 0) continue;
                max = Mathf.Min(max, GetOwnedCount(shop, kv.Key) / kv.Value);
            }

            int per = Mathf.Max(1, commodity.count);
            max = Mathf.Min(max, GetFreeSpace(shop, commodity.itemId) / per);
            return Mathf.Max(0, max);
        }

        /// <summary>获取该商品当前可回收的最大次数（综合 可交易次数 / 持有量）。</summary>
        public int GetMaxRecyclable(Shop shop, ShopCommodity commodity)
        {
            if (shop == null || commodity == null || shop.shopType != ShopType.Recycle) return 0;
            if (!Locate(shop, commodity, out var group, out var gi, out var ci)) return 0;

            int max = RemainingTradesCore(shop, group, gi, commodity, ci);
            if (max <= 0) return 0;

            int per = Mathf.Max(1, commodity.count);
            max = Mathf.Min(max, GetOwnedCount(shop, commodity.itemId) / per);
            return Mathf.Max(0, max);
        }

        #endregion

        #region 交易：购买（售卖店）

        /// <summary>
        /// 在售卖店购买指定商品 <paramref name="times"/> 次。
        /// 成交次数自动按 可交易次数 / 货币 / 仓库容量 下调（至少 1 次方成交）。
        /// </summary>
        public ShopTradeResult Purchase(string shopId, ShopCommodity commodity, int times)
        {
            var shop = ResolveShop(shopId);
            if (shop == null) return ShopTradeResult.Fail(ShopTradeFailReason.ShopNotFound);
            if (commodity == null || !Locate(shop, commodity, out var group, out var gi, out var ci))
                return ShopTradeResult.Fail(ShopTradeFailReason.CommodityInvalid);
            return PurchaseInternal(shop, group, gi, commodity, ci, times);
        }

        private ShopTradeResult PurchaseInternal(Shop shop, ShopCommodityGroup group, int gi,
            ShopCommodity commodity, int ci, int times)
        {
            if (shop.shopType != ShopType.Sell)
                return ShopTradeResult.Fail(ShopTradeFailReason.NotSupported);
            if (InventoryRuntimeManager.Instance == null)
                return ShopTradeResult.Fail(ShopTradeFailReason.NotSupported);
            if (InventoryDataManager.Instance.GetItem(commodity.itemId) == null)
                return ShopTradeResult.Fail(ShopTradeFailReason.CommodityInvalid);

            if (times <= 0) times = 1;

            // 1. 可交易次数
            int remaining = RemainingTradesCore(shop, group, gi, commodity, ci);
            if (remaining <= 0) return ShopTradeResult.Fail(ShopTradeFailReason.TradeLimitReached);
            times = Mathf.Min(times, remaining);

            // 2. 货币是否充足（按各货币可负担次数取最小）
            var unit = GetUnitPrice(shop, commodity);
            foreach (var kv in unit)
            {
                if (kv.Value <= 0) continue;
                times = Mathf.Min(times, GetOwnedCount(shop, kv.Key) / kv.Value);
            }
            if (times <= 0) return ShopTradeResult.Fail(ShopTradeFailReason.NotEnoughCurrency);

            // 3. 仓库能否容纳购入道具
            int per = Mathf.Max(1, commodity.count);
            times = Mathf.Min(times, GetFreeSpace(shop, commodity.itemId) / per);
            if (times <= 0) return ShopTradeResult.Fail(ShopTradeFailReason.InventoryFull);

            // 4. 落实：扣货币 → 加道具 → 累加次数
            var delta = new Dictionary<string, int>();
            foreach (var kv in unit)
            {
                int cost = kv.Value * times;
                if (cost <= 0) continue;
                RemoveAcross(shop, kv.Key, cost);
                delta[kv.Key] = cost;
            }
            AddAcross(shop, commodity.itemId, per * times);
            CommitTradeCount(shop, group, gi, commodity, ci, times);

            OnShopChanged?.Invoke(shop.id);
            return new ShopTradeResult
            {
                Success       = true,
                AppliedTimes  = times,
                Reason        = ShopTradeFailReason.None,
                CurrencyDelta = delta,
            };
        }

        #endregion

        #region 交易：回收（回收店）

        /// <summary>在回收店回收指定（已配置的）商品 <paramref name="times"/> 次。</summary>
        public ShopTradeResult Recycle(string shopId, ShopCommodity commodity, int times)
        {
            var shop = ResolveShop(shopId);
            if (shop == null) return ShopTradeResult.Fail(ShopTradeFailReason.ShopNotFound);
            if (commodity == null || !Locate(shop, commodity, out var group, out var gi, out var ci))
                return ShopTradeResult.Fail(ShopTradeFailReason.CommodityInvalid);
            return RecycleInternal(shop, group, gi, commodity, ci, times);
        }

        /// <summary>
        /// 在回收店按道具 ID 回收 <paramref name="times"/> 次（用于「未配置商品组」的默认回收：
        /// 倍率 1、不限次数、不刷新）。价格仍取自该道具的 <see cref="Shop.priceAttrSource"/> 属性。
        /// </summary>
        public ShopTradeResult RecycleItem(string shopId, string itemId, int times)
        {
            var shop = ResolveShop(shopId);
            if (shop == null) return ShopTradeResult.Fail(ShopTradeFailReason.ShopNotFound);
            if (string.IsNullOrEmpty(itemId))
                return ShopTradeResult.Fail(ShopTradeFailReason.CommodityInvalid);
            var tmp = new ShopCommodity { itemId = itemId, count = 1, priceMultiplier = 1f, tradeLimit = -1 };
            return RecycleInternal(shop, null, -1, tmp, -1, times);
        }

        private ShopTradeResult RecycleInternal(Shop shop, ShopCommodityGroup group, int gi,
            ShopCommodity commodity, int ci, int times)
        {
            if (shop.shopType != ShopType.Recycle)
                return ShopTradeResult.Fail(ShopTradeFailReason.NotSupported);
            if (InventoryRuntimeManager.Instance == null)
                return ShopTradeResult.Fail(ShopTradeFailReason.NotSupported);
            if (InventoryDataManager.Instance.GetItem(commodity.itemId) == null)
                return ShopTradeResult.Fail(ShopTradeFailReason.CommodityInvalid);

            if (times <= 0) times = 1;

            // 1. 可交易次数
            int remaining = RemainingTradesCore(shop, group, gi, commodity, ci);
            if (remaining <= 0) return ShopTradeResult.Fail(ShopTradeFailReason.TradeLimitReached);
            times = Mathf.Min(times, remaining);

            // 2. 持有量是否足够
            int per = Mathf.Max(1, commodity.count);
            times = Mathf.Min(times, GetOwnedCount(shop, commodity.itemId) / per);
            if (times <= 0) return ShopTradeResult.Fail(ShopTradeFailReason.ItemNotOwned);

            // 3. 落实：移除道具（先腾出空间）→ 发放货币 → 累加次数
            var unit = GetUnitPrice(shop, commodity);
            RemoveAcross(shop, commodity.itemId, per * times);

            var delta = new Dictionary<string, int>();
            foreach (var kv in unit)
            {
                int pay = kv.Value * times;
                if (pay <= 0) continue;
                // 假设货币道具为无限堆叠（常见情形）；若货币有堆叠上限且超出，超出部分按容量丢弃。
                AddAcross(shop, kv.Key, pay);
                delta[kv.Key] = pay;
            }
            CommitTradeCount(shop, group, gi, commodity, ci, times);

            OnShopChanged?.Invoke(shop.id);
            return new ShopTradeResult
            {
                Success       = true,
                AppliedTimes  = times,
                Reason        = ShopTradeFailReason.None,
                CurrencyDelta = delta,
            };
        }

        #endregion

        #region 刷新结算

        /// <summary>取商品的生效刷新计划：覆盖时用商品自身，否则用组级。</summary>
        private static ShopRefreshSchedule EffectiveSchedule(ShopCommodityGroup group, ShopCommodity commodity)
        {
            if (commodity != null && commodity.overrideRefresh && commodity.refresh != null)
                return commodity.refresh;
            return group?.refresh;
        }

        /// <summary>结算刷新并返回剩余可交易次数（核心，已知组与索引）。</summary>
        private int RemainingTradesCore(Shop shop, ShopCommodityGroup group, int gi,
            ShopCommodity commodity, int ci)
        {
            if (commodity.tradeLimit < 0) return int.MaxValue;   // 无限次数

            var prog     = GetState(shop.id).GetOrAdd(GroupKey(group, gi), CommodityKey(commodity, ci));
            var schedule = EffectiveSchedule(group, commodity);
            ApplyRefresh(schedule, prog);

            int rem = commodity.tradeLimit - prog.tradedCount;
            return rem < 0 ? 0 : rem;
        }

        /// <summary>累加已交易次数（仅当存在次数上限时记账）。</summary>
        private void CommitTradeCount(Shop shop, ShopCommodityGroup group, int gi,
            ShopCommodity commodity, int ci, int times)
        {
            if (commodity.tradeLimit < 0) return;
            var prog = GetState(shop.id).GetOrAdd(GroupKey(group, gi), CommodityKey(commodity, ci));
            prog.tradedCount += times;
        }

        /// <summary>
        /// 若自 <see cref="ShopCommodityProgress.lastRefreshTicks"/> 起已跨过最近一次刷新边界，
        /// 则重置已交易次数并记录刷新时间。「不刷新」直接跳过。
        /// </summary>
        private void ApplyRefresh(ShopRefreshSchedule schedule, ShopCommodityProgress prog)
        {
            if (schedule == null || schedule.refreshType == ShopRefreshType.不刷新) return;

            var now      = ResolveNow(schedule);
            var boundary = MostRecentBoundary(schedule, now);
            // 与 now 同一参考系的上次刷新时间（0 = 尚未初始化 → DateTime.MinValue）。
            var last     = new DateTime(prog.lastRefreshTicks);

            if (last < boundary)
            {
                prog.tradedCount      = 0;
                prog.lastRefreshTicks = now.Ticks;
            }
        }

        /// <summary>按刷新计划取当前时间（经时间类型选时钟，再按可选时区换算）。</summary>
        private DateTime ResolveNow(ShopRefreshSchedule schedule)
        {
            var mgr = InventoryRuntimeManager.Instance;
            var now = mgr != null ? mgr.GetNow(schedule.timeType) : DateTime.Now;

            if (!string.IsNullOrEmpty(schedule.timeZoneId))
            {
                try
                {
                    var tz  = TimeZoneInfo.FindSystemTimeZoneById(schedule.timeZoneId);
                    // 先归一到 UTC（Local/Unspecified 按设备本地时区解释），再换算到目标时区的墙钟时间。
                    var utc = now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime();
                    now = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
                }
                catch
                {
                    // 时区 ID 无效则沿用原时钟，不阻断刷新。
                }
            }
            return now;
        }

        /// <summary>
        /// 计算 ≤ <paramref name="now"/> 的最近一次刷新边界时间点（与 <paramref name="now"/> 同参考系）。
        /// </summary>
        private static DateTime MostRecentBoundary(ShopRefreshSchedule s, DateTime now)
        {
            int h = Mathf.Clamp(s.hour, 0, 23);
            int m = Mathf.Clamp(s.minute, 0, 59);

            switch (s.refreshType)
            {
                case ShopRefreshType.每日:
                {
                    var today = new DateTime(now.Year, now.Month, now.Day, h, m, 0, now.Kind);
                    return now >= today ? today : today.AddDays(-1);
                }
                case ShopRefreshType.每周:
                {
                    int target = Mathf.Clamp(s.dayOfWeek, 0, 6);            // 0 = 周日
                    var c      = new DateTime(now.Year, now.Month, now.Day, h, m, 0, now.Kind);
                    int diff   = ((int)c.DayOfWeek - target + 7) % 7;        // 距最近一次目标星期的天数
                    c = c.AddDays(-diff);
                    if (c > now) c = c.AddDays(-7);
                    return c;
                }
                case ShopRefreshType.每月:
                {
                    int day = Mathf.Clamp(s.dayOfMonth, 1, 31);
                    var c   = MakeMonthDate(now.Year, now.Month, day, h, m, now.Kind);
                    if (c > now)
                    {
                        var prev = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                        c = MakeMonthDate(prev.Year, prev.Month, day, h, m, now.Kind);
                    }
                    return c;
                }
                default:
                    return now;
            }
        }

        /// <summary>构造某年某月的「几号 时:分」时间点，号数超过当月天数时取当月最后一天。</summary>
        private static DateTime MakeMonthDate(int year, int month, int day, int h, int m, DateTimeKind kind)
        {
            int dim = DateTime.DaysInMonth(year, month);
            int d   = Mathf.Clamp(day, 1, dim);
            return new DateTime(year, month, d, h, m, 0, kind);
        }

        #endregion

        #region 存档

        /// <summary>获取全部商店运行时进度的深拷贝（由游戏层 SaveManager 序列化）。</summary>
        public List<ShopRuntimeState> GetSaveData()
        {
            var result = new List<ShopRuntimeState>(_shopStates.Count);
            foreach (var kvp in _shopStates)
                result.Add(kvp.Value.Clone());
            return result;
        }

        /// <summary>从存档数据恢复商店运行时进度（覆盖当前内存状态）。</summary>
        public void LoadSaveData(List<ShopRuntimeState> data)
        {
            _shopStates.Clear();
            if (data == null) return;
            foreach (var s in data)
            {
                if (s == null || string.IsNullOrEmpty(s.shopId)) continue;
                _shopStates[s.shopId] = s.Clone();
            }
        }

        /// <summary>清空全部商店进度（如开始新游戏）。</summary>
        public void ResetAll() => _shopStates.Clear();

        #endregion

        #region 内部辅助

        /// <summary>跨商店交易仓库移除指定道具（按持有顺序分摊）。调用前应已确认总量充足。</summary>
        private static void RemoveAcross(Shop shop, string itemId, int amount)
        {
            var invMgr = InventoryRuntimeManager.Instance;
            if (invMgr == null) return;
            int remaining = amount;
            foreach (var refId in shop.tradeInventoryRefs)
            {
                if (remaining <= 0) break;
                int owned = invMgr.GetTotalCount(refId, itemId);
                int take  = Mathf.Min(owned, remaining);
                if (take > 0)
                {
                    invMgr.TryRemoveItemById(refId, itemId, take);
                    remaining -= take;
                }
            }
        }

        /// <summary>跨商店交易仓库添加指定道具（按剩余容量分摊）。调用前应已确认总容量充足。</summary>
        private static void AddAcross(Shop shop, string itemId, int count)
        {
            var invMgr = InventoryRuntimeManager.Instance;
            if (invMgr == null) return;
            int remaining = count;
            foreach (var refId in shop.tradeInventoryRefs)
            {
                if (remaining <= 0) break;
                int free = invMgr.GetFreeSpaceFor(refId, itemId);
                int add  = Mathf.Min(free, remaining);
                if (add > 0)
                {
                    invMgr.TryAddItem(refId, itemId, add);
                    remaining -= add;
                }
            }
        }

        /// <summary>在商店商品组中定位某商品（引用相等），返回所属组与索引。</summary>
        private static bool Locate(Shop shop, ShopCommodity commodity,
            out ShopCommodityGroup group, out int gi, out int ci)
        {
            group = null; gi = -1; ci = -1;
            if (shop == null || commodity == null) return false;
            for (int g = 0; g < shop.groups.Count; g++)
            {
                int idx = shop.groups[g].commodities.IndexOf(commodity);
                if (idx >= 0)
                {
                    group = shop.groups[g]; gi = g; ci = idx;
                    return true;
                }
            }
            return false;
        }

        private ShopRuntimeState GetState(string shopId)
        {
            if (!_shopStates.TryGetValue(shopId, out var st))
            {
                st = new ShopRuntimeState(shopId);
                _shopStates[shopId] = st;
            }
            return st;
        }

        private static Shop ResolveShop(string shopId)
            => string.IsNullOrEmpty(shopId) ? null : InventoryDataManager.Instance.GetShop(shopId);

        /// <summary>组的稳定键：组名优先，空名回退为 <c>#组索引</c>，无组（默认回收）为 <c>__default__</c>。</summary>
        private static string GroupKey(ShopCommodityGroup group, int gi)
        {
            if (group == null) return "__default__";
            return string.IsNullOrEmpty(group.name) ? $"#{gi}" : group.name;
        }

        /// <summary>商品的稳定键：<c>组内索引:道具ID</c>。</summary>
        private static string CommodityKey(ShopCommodity commodity, int ci) => $"{ci}:{commodity?.itemId}";

        /// <summary>价格倍率换算（四舍五入，下限 0）。倍率为 1 时原样返回。</summary>
        private static int ApplyMultiplier(int basePrice, float multiplier)
        {
            if (Mathf.Approximately(multiplier, 1f)) return basePrice;
            return Mathf.Max(0, Mathf.RoundToInt(basePrice * multiplier));
        }

        #endregion
    }
}

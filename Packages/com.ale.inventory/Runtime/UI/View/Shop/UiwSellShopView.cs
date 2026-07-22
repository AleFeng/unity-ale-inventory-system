using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 售卖店视图：玩家用货币购买商店商品。
    /// 结算前按 货币 / 背包容量 预校验并自动下调次数（有下调则提示、需再次点击）。
    /// </summary>
    public class UiwSellShopView : UiwShopViewBase
    {
        protected override ShopType ExpectedType => ShopType.Sell;

        /// <summary>售卖：某货币的合计消耗超出持有量时，总价变红。</summary>
        protected override bool IsOverBudget(string currencyId, int amount)
        {
            var shopMgr = ShopRuntimeManager.Instance;
            return shopMgr != null && amount > shopMgr.GetOwnedCount(Shop, currencyId);
        }

        /// <summary>
        /// 售卖结算：先预校验，若有下调则刷新并提示；否则按当前次数逐行购买。
        /// </summary>
        protected override void Settle()
        {
            // 预校验：以运行预算（货币 / 容量）模拟，按需逐行下调；若有下调则刷新并等待再次点击。
            if (PrevalidateSell())
            {
                RefreshAllCells();
                RecomputeTotals();
                ShowHint(adjustHint);
                return;
            }

            Settling = true;
            var shopMgr = ShopRuntimeManager.Instance;
            foreach (var e in Entries)
            {
                if (e.commodity == null || e.times <= 0) continue;
                shopMgr.Purchase(ShopId, e.commodity, e.times);
            }
            Settling = false;

            AfterSettle();
        }

        /// <summary>
        /// 售卖 预校验。
        /// 逐行按两个阶段独立下调购买次数：① 货币上限 → ② 背包容量上限；任一行被下调即返回 true，否则返回 false。
        /// </summary>
        /// <remarks>
        /// 例：单次购买获得 3 个、单价 10 金币（即单次价 30 金币），购买 90 次。
        ///   阶段① 货币：持有 2000 金币 → 2000/30 = 66 次（先下调到货币能支撑的次数）。
        ///   阶段② 容量：背包剩余可容纳 29 个 → 29/3 = 9 次（再下调到容量能容纳的次数）。
        /// 多行共用同一货币时，budget 按各行「最终成交次数」扣减（而非阶段①的货币上限），
        /// 因此被容量卡住的行不会过度占用后续行的货币预算。
        /// </remarks>
        /// <returns>是否有任意行被下调。</returns>
        private bool PrevalidateSell()
        {
            bool reduced = false;
            var shopMgr  = ShopRuntimeManager.Instance;
            if (shopMgr == null) return false;

            var budget = new Dictionary<string, int>();
            foreach (var e in Entries)
            {
                if (e.commodity == null || e.times <= 0) continue;
                var unitPrice = shopMgr.GetUnitPrice(Shop, e.commodity);

                // ── 阶段①：货币上限 ── 取各货币「剩余预算 / 单次价」的最小值，下调到货币能支撑的次数。
                int currencyCap = e.times;
                foreach (var kv in unitPrice)
                {
                    if (kv.Value <= 0) continue;
                    if (!budget.TryGetValue(kv.Key, out var b))
                    {
                        b = shopMgr.GetOwnedCount(Shop, kv.Key);
                        budget[kv.Key] = b;
                    }
                    currencyCap = Mathf.Min(currencyCap, b / kv.Value);   // 剩余货币 / 单次价 = 可负担次数
                }
                currencyCap = Mathf.Max(0, currencyCap);

                // ── 阶段②：背包容量上限 ── 在阶段①结果之上，再下调到剩余容量能容纳的次数。
                int per         = Mathf.Max(1, e.commodity.count);        // 单次购买获得的道具数量
                int freeSpace   = shopMgr.GetFreeSpace(Shop, e.commodity.itemId);
                int feasible    = Mathf.Min(currencyCap, freeSpace / per); // 剩余容量 / 单次数量 = 可容纳次数
                feasible        = Mathf.Max(0, feasible);

                if (feasible < e.times)
                {
                    reduced = true;
                    e.times = feasible;   // 数据模型下调（可见格随后由 RefreshAllCells 同步）
                }

                // 预算按「最终成交次数」扣减：被容量卡住的部分不计入货币消耗。
                foreach (var kv in unitPrice)
                    if (kv.Value > 0 && budget.ContainsKey(kv.Key))
                        budget[kv.Key] -= kv.Value * feasible;
            }
            return reduced;
        }
    }
}

using System.Collections.Generic;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 回收店视图：玩家出售背包道具给商店换取货币。
    /// 未配置任何商品组时，自动基于交易仓库中现有的不同道具合成可回收商品列表。
    /// </summary>
    public class UiwRecycleShopView : UiwShopViewBase
    {
        protected override ShopType ExpectedType => ShopType.Recycle;

        /// <summary>回收店总价文本使用「总收益」前缀。</summary>
        protected override string TotalPrefix => recycleTotalPrefix;

        /// <summary>无配置商品时，把交易仓库中现有的不同道具合成为可回收商品。</summary>
        protected override void BuildExtraCommodities()
        {
            SyntheticCommodities.Clear();
            if (!ShopHasNoCommodities()) return;
            if (!InventoryRuntimeManager.Instance) return;

            var seen = new HashSet<string>();
            foreach (var invId in Shop.tradeInventoryRefs)
            {
                foreach (var slot in InventoryRuntimeManager.Instance.GetSlots(invId))
                {
                    if (string.IsNullOrEmpty(slot.itemId) || !seen.Add(slot.itemId)) continue;
                    if (!MatchesTradeTags(slot.itemId)) continue;   // 交易功能标签限制：不含勾选标签的道具不收集
                    SyntheticCommodities.Add(new ShopCommodity
                    {
                        itemId          = slot.itemId,
                        count           = 1,
                        priceMultiplier = 1f,
                        tradeLimit      = -1,
                    });
                }
            }
        }

        /// <summary>无配置商品组时，提供合成的可回收商品列表（标记 synthetic = true）；否则走默认按组逻辑。</summary>
        protected override bool TryProvideCommodities(
            ShopCommodityGroup activeGroup, List<KeyValuePair<ShopCommodity, bool>> result)
        {
            if (!ShopHasNoCommodities()) return false;
            foreach (var sc in SyntheticCommodities)
                result.Add(new KeyValuePair<ShopCommodity, bool>(sc, true));
            return true;
        }

        /// <summary>
        /// 回收结算：按当前次数逐行交易。合成商品走 RecycleItem，配置商品走 Recycle。
        /// </summary>
        protected override void Settle()
        {
            Settling = true;
            var shopMgr = ShopRuntimeManager.Instance;
            foreach (var e in Entries)
            {
                if (e.commodity == null || e.times <= 0) continue;
                if (e.synthetic) shopMgr.RecycleItem(ShopId, e.commodity.itemId, e.times);
                else             shopMgr.Recycle(ShopId, e.commodity, e.times);
            }
            Settling = false;

            AfterSettle();
        }

        /// <summary>
        /// 回收店按「交易功能标签」限制可交易道具：只有道具功能标签含其中任一标签才显示在商品列表中。
        /// 同时作用于合成商品与配置商品组。
        /// </summary>
        protected override bool IsCommodityTradeable(ShopCommodity commodity)
            => commodity != null && MatchesTradeTags(commodity.itemId);

        /// <summary>道具是否满足本店「交易功能标签」限制（空列表 = 不限制）。</summary>
        private bool MatchesTradeTags(string itemId)
        {
            var funcTags = Shop.tradeTagRefs;
            if (funcTags == null || funcTags.Count == 0) return true;   // 空 = 不限制

            var dm = InventoryDataManager.Instance;
            if (dm == null || string.IsNullOrEmpty(itemId)) return false;
            foreach (var funcTag in funcTags)
                if (dm.ItemHasTag(itemId, funcTag)) return true;
            return false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 商店商品条目数据模型。虚拟滚动下，选中<b>交易次数</b>必须存放于数据模型而非格子——
    /// 否则离屏商品的次数会随格子回收丢失，导致购物车总价 / 结算出错。单价与最大可交易次数不缓存，
    /// 需要时由 <see cref="ShopRuntimeManager"/> 按商店 + 商品实时计算。
    /// </summary>
    public class ShopCommodityEntry
    {
        /// <summary>本条目对应的商品配置。</summary>
        public readonly ShopCommodity commodity;
        /// <summary>是否为合成商品（回收店无配置商品时基于交易仓库合成，结算走 RecycleItem）。</summary>
        public readonly bool synthetic;
        /// <summary>当前选中的交易次数（数据模型持有，虚拟滚动下不随格子回收丢失）。</summary>
        public int times;

        public ShopCommodityEntry(ShopCommodity commodity, bool synthetic)
        {
            this.commodity = commodity;
            this.synthetic = synthetic;
        }
    }

    /// <summary>某商品当前最大可交易次数的计算（供格子设置计数器上限、视图钳制数据模型次数共用）。</summary>
    internal static class ShopTradeMath
    {
        /// <summary>
        /// 售卖：受「剩余可交易次数」与「每单上限」约束；回收：再受「持有量 / 单次数量」约束。
        /// </summary>
        public static int MaxTimes(ShopRuntimeManager shopMgr, Shop shop, ShopCommodity commodity,
            ShopType mode, int maxQuantityPerOrder)
        {
            if (shopMgr == null || shop == null || commodity == null) return 0;
            int per             = Mathf.Max(1, commodity.count);
            int remainingTrades = shopMgr.GetRemainingTrades(shop, commodity);
            if (mode == ShopType.Recycle)
            {
                int owned = shopMgr.GetOwnedCount(shop, commodity.itemId);
                return Mathf.Max(0, Mathf.Min(remainingTrades, owned / per));
            }
            int m = remainingTrades == int.MaxValue ? maxQuantityPerOrder : remainingTrades;
            return Mathf.Max(0, Mathf.Min(m, maxQuantityPerOrder));
        }
    }

    /// <summary>
    /// 商店商品列表（虚拟滚动，单列纵向）。以 <see cref="UiwShopItemDetail"/> 为条目，在通用顺序虚拟滚动列表
    /// <see cref="UiwInventoryOrderList{TData,TCell}"/> 之上，负责「把 <see cref="ShopCommodityEntry"/> 显示到商品格子」。
    /// 选中次数存于 <see cref="ShopCommodityEntry.times"/>（数据模型），格子仅显示 / 回写当前可见项的次数；
    /// 由 <see cref="UiwShopViewBase"/> 通过 <see cref="SetContext"/> + <see cref="SetCommodities"/> 驱动。
    /// </summary>
    public class UiwShopCommodityList : UiwInventoryOrderList<ShopCommodityEntry, UiwShopItemDetail>
    {
        private UiwShopViewBase    _owner;
        private Shop               _shop;
        private ShopType           _mode;
        private NumberFormatLocale _numberFormat;

        /// <summary>设置绑定上下文（所属视图 / 商店 / 类型 / 数字格式）。应在 <see cref="SetCommodities"/> 之前调用。</summary>
        public void SetContext(UiwShopViewBase owner, Shop shop, ShopType mode, NumberFormatLocale numberFormat)
        {
            _owner        = owner;
            _shop         = shop;
            _mode         = mode;
            _numberFormat = numberFormat;
        }

        /// <summary>设置商品条目数据（全集）并经内建 过滤 / 排序 管线刷新显示。</summary>
        public void SetCommodities(IReadOnlyList<ShopCommodityEntry> entries) => SetSourceItems(entries);

        /// <summary>刷新当前可见格子的显示（重算剩余 / 持有 / 单价，并按各自 entry.times 钳制次数）。</summary>
        public void RefreshVisibleCells()
        {
            foreach (var cell in Instances)
                if (cell && cell.gameObject.activeSelf && cell.Commodity != null)
                    cell.RefreshDisplay();
        }

        protected override void BindCell(UiwShopItemDetail cell, ShopCommodityEntry entry)
        {
            cell.numberFormat = _numberFormat;
            cell.Bind(_owner, _shop, entry, _mode);
        }

        protected override void ClearCell(UiwShopItemDetail cell) => cell.SetEmpty();
    }
}

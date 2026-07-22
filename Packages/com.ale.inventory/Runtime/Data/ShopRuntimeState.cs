using System;
using System.Collections.Generic;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 单个商店的运行时状态（每玩家）。仅保存交易进度——商品目录本身是配置数据。
    /// 由 <see cref="ShopRuntimeManager"/> 维护并随存档持久化。
    /// </summary>
    [Serializable]
    public class ShopRuntimeState
    {
        /// <summary>所属商店 ID。</summary>
        public string shopId;

        /// <summary>各商品交易进度。</summary>
        public List<ShopCommodityProgress> progresses = new List<ShopCommodityProgress>();

        public ShopRuntimeState()
        {
        }

        public ShopRuntimeState(string shopId)
        {
            this.shopId = shopId;
        }

        /// <summary>按 (组键, 商品键) 查找进度条目，未找到返回 null。</summary>
        public ShopCommodityProgress Find(string groupKey, string commodityKey)
        {
            foreach (var p in progresses)
                if (p.groupKey == groupKey && p.commodityKey == commodityKey)
                    return p;
            return null;
        }

        /// <summary>按 (组键, 商品键) 查找进度条目，不存在则创建并加入。</summary>
        public ShopCommodityProgress GetOrAdd(string groupKey, string commodityKey)
        {
            var p = Find(groupKey, commodityKey);
            if (p == null)
            {
                p = new ShopCommodityProgress(groupKey, commodityKey);
                progresses.Add(p);
            }
            return p;
        }

        public ShopRuntimeState Clone()
        {
            var clone = new ShopRuntimeState(shopId);
            foreach (var p in progresses)
                clone.progresses.Add(p.Clone());
            return clone;
        }
    }
}

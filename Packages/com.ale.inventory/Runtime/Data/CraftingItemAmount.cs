using System;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 制作配方中的一个「道具 + 数量」条目。用于蓝图的产出道具列表与消耗道具列表。
    /// 关联道具系统中的道具 ID，获取其道具数据（图标 / 名称 / 描述等）。
    /// </summary>
    [Serializable]
    public class CraftingItemAmount
    {
        /// <summary>关联的道具 ID（道具系统）。</summary>
        public string itemId;

        /// <summary>制作一次涉及的该道具数量（产出 = 获得数量；消耗 = 需求数量）。</summary>
        public int count = 1;

        public CraftingItemAmount()
        {
        }

        public CraftingItemAmount(string itemId, int count = 1)
        {
            this.itemId = itemId;
            this.count  = count;
        }

        /// <summary>深拷贝。</summary>
        public CraftingItemAmount Clone() => new CraftingItemAmount(itemId, count);
    }
}

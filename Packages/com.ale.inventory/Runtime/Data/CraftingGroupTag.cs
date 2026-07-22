using System;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 制作系统分组标签。用于对蓝图进行分组，便于 UI 中筛选与查找
    /// （每个蓝图可指定 1 个主分组标签 + 多个副分组标签）。
    /// 基础信息（ID / 名称 / 描述 / 列表色点）由 <see cref="GroupTag"/> 承载。
    /// </summary>
    [Serializable]
    public class CraftingGroupTag : GroupTag
    {
        public CraftingGroupTag()
        {
        }

        public CraftingGroupTag(string newId, string newDisplayName = null)
            : base(newId, newDisplayName)
        {
        }

        /// <summary>深拷贝。</summary>
        public CraftingGroupTag Clone()
        {
            var clone = new CraftingGroupTag();
            CopyTo(clone);
            return clone;
        }
    }
}

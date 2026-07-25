using System.Collections.Generic;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 标签序号查表：把一组 <see cref="Tag"/>（按其在列表中的位置定义优先级）解析为「名称 → 序号」，
    /// 并据此求某元素的标签序号——其首个「未被忽略且已定义」标签的序号（越小优先级越高）。
    ///
    /// <para>名称→序号字典惰性构建并缓存，适合在单次排序内复用（每次两两比较降到 O(1)）。
    /// 每个实例持有一次查表快照，随即丢弃，不存在缓存过期问题。</para>
    /// </summary>
    public sealed class TagOrderMap
    {
        private readonly IReadOnlyList<Tag> _tags;
        private Dictionary<string, int>     _index;   // 惰性构建

        /// <param name="tags">定义标签顺序的列表（元素在列表中的下标即其序号）。可为 null。</param>
        public TagOrderMap(IReadOnlyList<Tag> tags) => _tags = tags;

        /// <summary>标签名 → 序号；未定义 / 空名返回 <see cref="int.MaxValue"/>。</summary>
        public int IndexOf(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return int.MaxValue;
            _index ??= Build();
            return _index.TryGetValue(tagName, out int i) ? i : int.MaxValue;
        }

        /// <summary>
        /// 求元素的标签序号：按给定顺序遍历标签名，返回首个「不在忽略列表 且 已定义」标签的序号；
        /// 全部落空返回 <see cref="int.MaxValue"/>。<paramref name="tagNames"/> 惰性遍历、命中即止。
        /// </summary>
        public int OrderOf(IEnumerable<string> tagNames, IReadOnlyList<string> ignoreIds)
        {
            if (tagNames == null) return int.MaxValue;
            foreach (string tag in tagNames)
            {
                if (ignoreIds != null && AttributeSortService.Contains(ignoreIds, tag)) continue;
                int order = IndexOf(tag);
                if (order != int.MaxValue) return order;
            }
            return int.MaxValue;
        }

        /// <summary>
        /// 比较两个标签序号：缺失（<see cref="int.MaxValue"/>）者恒排末尾（不受升降序影响），其余按序号升 / 降序。
        /// </summary>
        public static int Compare(int orderA, int orderB, bool ascending)
        {
            if (orderA == int.MaxValue && orderB == int.MaxValue) return 0;
            if (orderA == int.MaxValue) return 1;
            if (orderB == int.MaxValue) return -1;
            return orderA.CompareTo(orderB) * (ascending ? 1 : -1);
        }

        private Dictionary<string, int> Build()
        {
            var d = new Dictionary<string, int>();
            if (_tags != null)
                for (int i = 0; i < _tags.Count; i++)
                {
                    string n = _tags[i]?.name;
                    if (!string.IsNullOrEmpty(n) && !d.ContainsKey(n))
                        d[n] = i;
                }
            return d;
        }
    }
}

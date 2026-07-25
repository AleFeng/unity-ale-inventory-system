using System.Collections.Generic;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 在 <see cref="SortContextBase{TData}"/> 之上增加对 <see cref="SortFieldKeys.TagOrder"/>（标签序号）排序的
    /// 内建支持：宿主只需提供「标签顺序列表」<see cref="TagOrderList"/> 与「取元素的有序标签名」<see cref="TagsOf"/>，
    /// 即可白得「按首个有效标签的序号排序」（缺失 / 全被忽略者恒排末尾）。
    ///
    /// <para>标签序号查表经 <see cref="TagOrderMap"/> 在本上下文内缓存复用。<see cref="SortContextBase{TData}"/>
    /// 保持标签无关——只想要属性 / 主键排序的宿主直接继承它即可，本类是可选的标签排序层。</para>
    ///
    /// <para>若宿主还有其它领域特殊字段，覆写 <see cref="SortContextBase{TData}.TryCompareDomainSpecial"/> 时
    /// 记得对未命中者转调 <c>base</c>（本类即在其中处理 <see cref="SortFieldKeys.TagOrder"/>）。</para>
    /// </summary>
    /// <typeparam name="TData">列表数据元素类型。</typeparam>
    public abstract class TagSortContextBase<TData> : SortContextBase<TData>
    {
        private TagOrderMap _tagMap;

        /// <summary>本上下文缓存复用的标签序号查表（据 <see cref="TagOrderList"/> 惰性构建）。</summary>
        protected TagOrderMap TagMap => _tagMap ??= new TagOrderMap(TagOrderList);

        /// <summary>定义标签顺序的列表（元素下标即序号）；null / 空表示不支持标签排序。</summary>
        protected abstract IReadOnlyList<Tag> TagOrderList { get; }

        /// <summary>取元素用于标签排序的有序标签名（如「自身标签 → 模板继承标签」）。惰性遍历、命中即止。</summary>
        protected abstract IEnumerable<string> TagsOf(TData data);

        /// <summary>元素的标签序号：其首个「未被忽略且已定义」标签的序号；无则 <see cref="int.MaxValue"/>。</summary>
        public int TagOrderOf(TData data, IReadOnlyList<string> ignoreIds)
            => TagMap.OrderOf(TagsOf(data), ignoreIds);

        /// <summary>内建处理 <see cref="SortFieldKeys.TagOrder"/>；其余字段转调 <c>base</c>。</summary>
        protected override bool TryCompareDomainSpecial(TData a, TData b, string field, bool ascending, out int result)
        {
            if (field != SortFieldKeys.TagOrder)
                return base.TryCompareDomainSpecial(a, b, field, ascending, out result);

            var ignoreIds = OptionOf(SortFieldKeys.TagOrder)?.EffectiveIgnoreIds;
            int oa = TagOrderOf(a, ignoreIds);
            int ob = TagOrderOf(b, ignoreIds);
            result = TagOrderMap.Compare(oa, ob, ascending);
            return true;
        }
    }
}

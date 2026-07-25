namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 排序引擎 <see cref="AttributeSortService"/> 向宿主索取的全部领域信息。宿主插件针对自己的数据类型
    /// <typeparamref name="TData"/> 实现一次，即可复用整套「按属性字段比较」的通用排序算法。
    ///
    /// <para>通用算法只理解「属性载体 + 字段定义 + 忽略列表」三样通用概念；一切领域专有的排序键
    /// （如按主键、按分组序号及其别名）通过 <see cref="TryCompareSpecial"/> 交回宿主处理。</para>
    /// </summary>
    /// <typeparam name="TData">列表数据元素类型（如 <c>RuntimeItemSlot</c> / 商品条目 / 蓝图）。</typeparam>
    public interface ISortContext<TData>
    {
        /// <summary>取该条数据用于比较的属性载体（如道具定义）；无则 null。</summary>
        AttributeOwner OwnerOf(TData data);

        /// <summary>取排序字段的属性定义（决定按枚举 / 数值 / 字符串比较，并提供枚举类型引用）；无则 null。</summary>
        AttributeDefinition FindDefinition(string field);

        /// <summary>取该字段的整理选项（忽略列表等）；无则 null。</summary>
        SortOption OptionOf(string field);

        /// <summary>
        /// 处理宿主自定义的特殊排序字段（如「按主键」「按分组序号」及其中文别名）。命中则把最终比较结果
        /// （已按 <paramref name="ascending"/> 定向、且「被忽略 / 缺失项恒排末尾」）写入 <paramref name="result"/>
        /// 并返回 true；返回 false 则交由通用属性比较处理。
        /// </summary>
        bool TryCompareSpecial(TData a, TData b, string field, bool ascending, out int result);
    }
}

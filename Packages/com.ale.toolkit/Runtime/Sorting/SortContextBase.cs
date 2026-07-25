using System;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// <see cref="ISortContext{TData}"/> 的通用基类：把与领域无关的部分（<see cref="SortFieldKeys.Id"/>
    /// 主键排序、字段别名归一化、特殊字段分发）收口于此，宿主子类只需补全领域绑定
    /// （属性载体 / 字段定义 / 整理选项）与领域专属特殊字段（如 <see cref="SortFieldKeys.TagOrder"/>）。
    ///
    /// <para><b>主键排序开箱即用</b>：<see cref="IdOf"/> 默认取 <c>TData</c> 实现的 <see cref="ISortId.SortId"/>；
    /// 若元素主键随上下文变化（如同一槽位类型在不同列表按不同键排序），子类改写 <see cref="IdOf"/> 按上下文返回即可。</para>
    /// </summary>
    /// <typeparam name="TData">列表数据元素类型。</typeparam>
    public abstract class SortContextBase<TData> : ISortContext<TData>
    {
        // ── 领域绑定（子类实现）──────────────────────────────────────────────────
        public abstract AttributeOwner OwnerOf(TData data);
        public abstract AttributeDefinition FindDefinition(string field);
        public abstract SortOption OptionOf(string field);

        // ── 可选改写点 ───────────────────────────────────────────────────────────

        /// <summary>取元素的排序主键。默认取 <see cref="ISortId.SortId"/>；子类可按上下文改写。</summary>
        protected virtual string IdOf(TData data) => data is ISortId s ? s.SortId : null;

        /// <summary>把宿主别名归一化为保留键（如「道具ID」→ <see cref="SortFieldKeys.Id"/>）。默认原样返回。</summary>
        protected virtual string NormalizeField(string field) => field;

        /// <summary>
        /// 处理领域专属的特殊排序字段（如 <see cref="SortFieldKeys.TagOrder"/>）。命中则把已定向、
        /// 已把「被忽略 / 缺失恒排末尾」的结果写入 <paramref name="result"/> 返回 true；否则返回 false 交通用属性比较。
        /// 传入的 <paramref name="field"/> 已经过 <see cref="NormalizeField"/> 归一化。默认不处理任何字段。
        /// </summary>
        protected virtual bool TryCompareDomainSpecial(TData a, TData b, string field, bool ascending, out int result)
        {
            result = 0;
            return false;
        }

        // ── ISortContext 分发 ─────────────────────────────────────────────────────

        /// <summary>先归一化字段名 → 通用主键（<see cref="SortFieldKeys.Id"/>）→ 领域特殊字段。</summary>
        public bool TryCompareSpecial(TData a, TData b, string field, bool ascending, out int result)
        {
            field = NormalizeField(field);
            if (field == SortFieldKeys.Id)
            {
                result = CompareById(a, b, ascending);
                return true;
            }
            return TryCompareDomainSpecial(a, b, field, ascending, out result);
        }

        /// <summary>
        /// 按主键字典序比较：整理选项忽略列表命中者恒排末尾（不受升降序影响），其余按序数比较。
        /// </summary>
        protected int CompareById(TData a, TData b, bool ascending)
        {
            int sign = ascending ? 1 : -1;
            var ignoreIds = OptionOf(SortFieldKeys.Id)?.EffectiveIgnoreIds;
            string ida = IdOf(a);
            string idb = IdOf(b);

            bool aIgn = ignoreIds != null && AttributeSortService.Contains(ignoreIds, ida);
            bool bIgn = ignoreIds != null && AttributeSortService.Contains(ignoreIds, idb);
            if (aIgn != bIgn) return aIgn ? 1 : -1;
            if (aIgn) return 0;

            return string.Compare(ida ?? string.Empty, idb ?? string.Empty, StringComparison.Ordinal) * sign;
        }
    }
}

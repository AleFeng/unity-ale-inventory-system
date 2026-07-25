using System;
using System.Collections.Generic;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 通用属性排序引擎（静态）。承载与领域无关的排序算法核心：逐条 <see cref="SortPriority"/> 取可比较值 →
    /// 比较 → 相等则落到下一条。领域信息（属性载体、字段定义、忽略列表、特殊字段）全部经
    /// <see cref="ISortContext{TData}"/> 由宿主提供，因此同一套算法可被任意「按属性字段排序」的列表复用。
    ///
    /// <para>比较规则：① 特殊字段交由 <see cref="ISortContext{TData}.TryCompareSpecial"/>；
    /// ② 属性字段先按整理选项忽略列表判定（被忽略项恒排末尾，不受升降序影响）；
    /// ③ 任一端为字符串按「长度优先、再按序数」比较；④ 其余按 <see cref="AttributeValue.ToComparableNumber"/> 数值比较。</para>
    /// </summary>
    public static class AttributeSortService
    {
        /// <summary>按优先级列表对 <paramref name="list"/> 原地排序。空 / 单元素 / 无优先级 / 无上下文时为空操作。</summary>
        public static void Sort<TData>(List<TData> list,
            IReadOnlyList<SortPriority> priorities, ISortContext<TData> ctx)
        {
            if (list == null || list.Count <= 1 || priorities == null || priorities.Count == 0 || ctx == null) return;
            list.Sort((a, b) => Compare(a, b, priorities, ctx));
        }

        /// <summary>逐条优先级比较两条数据，取首个非零结果；全部相等返回 0。</summary>
        public static int Compare<TData>(TData a, TData b,
            IReadOnlyList<SortPriority> priorities, ISortContext<TData> ctx)
        {
            if (priorities == null || ctx == null) return 0;
            foreach (var sp in priorities)
            {
                int cmp = CompareByField(a, b, sp.field, sp.ascending, ctx);
                if (cmp != 0) return cmp;
            }
            return 0;
        }

        /// <summary>
        /// 按指定字段比较两条数据。特殊字段交由宿主 <see cref="ISortContext{TData}.TryCompareSpecial"/>；
        /// 否则取两者的属性条目，按忽略列表（恒排末尾）→ 字符串（长度优先）→ 数值 依次比较。
        /// </summary>
        public static int CompareByField<TData>(TData a, TData b, string field, bool ascending, ISortContext<TData> ctx)
        {
            // 领域特殊字段（如主键 / 分组序号 及其别名）由宿主处理，返回值已定向、已把「被忽略 / 缺失」排末尾。
            if (ctx.TryCompareSpecial(a, b, field, ascending, out int special)) return special;

            int sign = ascending ? 1 : -1;
            var ignoreIds = ctx.OptionOf(field)?.EffectiveIgnoreIds;
            var entryA = ctx.OwnerOf(a)?.GetEntry(field);
            var entryB = ctx.OwnerOf(b)?.GetEntry(field);

            // 被忽略项恒排末尾（不受升降序影响，与特殊字段一致）。
            bool aIgnored = IsIgnored(entryA, field, ignoreIds, ctx.FindDefinition);
            bool bIgnored = IsIgnored(entryB, field, ignoreIds, ctx.FindDefinition);
            if (aIgnored != bIgnored) return aIgnored ? 1 : -1;
            if (aIgnored) return 0;

            // 字符串（任一端为 String）：长度优先，再按序数比较。
            if (entryA?.value?.Type == EFieldType.String
             || entryB?.value?.Type == EFieldType.String)
            {
                string sa = entryA?.value?.AsString ?? string.Empty;
                string sb = entryB?.value?.AsString ?? string.Empty;
                int lenCmp = sa.Length.CompareTo(sb.Length);
                if (lenCmp != 0) return lenCmp * sign;
                return string.Compare(sa, sb, StringComparison.Ordinal) * sign;
            }

            // 其余：按可比较数值。
            double ka = entryA?.value?.ToComparableNumber() ?? 0.0;
            double kb = entryB?.value?.ToComparableNumber() ?? 0.0;
            return ka.CompareTo(kb) * sign;
        }

        /// <summary>
        /// 判断属性条目是否落在整理选项的忽略列表中。枚举按「值 → 名称」匹配：枚举类型引用优先取
        /// <paramref name="defOf"/> 提供的属性定义，取不到时回退属性值自身持久化的引用，经
        /// <see cref="EnumTypeResolver"/> 解析（未注入时退化为整数字符串匹配）。
        /// </summary>
        public static bool IsIgnored(AttributeEntry entry, string field,
            IReadOnlyList<string> ignoreIds, Func<string, AttributeDefinition> defOf)
        {
            if (ignoreIds == null || ignoreIds.Count == 0 || entry?.value == null) return false;
            var v = entry.value;
            switch (v.Type)
            {
                case EFieldType.String:
                    return Contains(ignoreIds, v.AsString);
                case EFieldType.Enum:
                {
                    var    def      = defOf?.Invoke(field);
                    string enumRef  = !string.IsNullOrEmpty(def?.enumTypeRef) ? def.enumTypeRef : v.EnumTypeRef;
                    var    enumType = EnumTypeResolver.Get(enumRef);
                    string name     = enumType?.GetItemByValue(v.AsEnumValue)?.name ?? v.AsEnumValue.ToString();
                    return Contains(ignoreIds, name);
                }
                case EFieldType.Int:
                    return Contains(ignoreIds, v.AsInt.ToString());
                case EFieldType.Bool:
                    return Contains(ignoreIds, v.AsInt != 0 ? "true" : "false");
                case EFieldType.Float:
                    return Contains(ignoreIds, v.AsFloat.ToString("G"));
                default:
                    return false;
            }
        }

        /// <summary>字符串列表是否包含指定值（序数相等）。</summary>
        public static bool Contains(IReadOnlyList<string> list, string value)
        {
            if (list == null) return false;
            foreach (var s in list)
                if (s == value) return true;
            return false;
        }
    }
}

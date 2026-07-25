namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 通用排序字段的保留键。<see cref="SortPriority.field"/> / <see cref="SortOption.field"/> 取这些值时，
    /// 表示按对应的<b>特殊排序键</b>（而非某个 <see cref="AttributeDefinition.id"/>）排序。
    ///
    /// <para>宿主可为这些键提供本地化别名（如「道具ID」→ <see cref="Id"/>），在其
    /// <see cref="SortContextBase{TData}.NormalizeField"/> 中归一化后交由通用逻辑处理。</para>
    /// </summary>
    public static class SortFieldKeys
    {
        /// <summary>按宿主主键（如道具 ID）字典序排序。由 <see cref="SortContextBase{TData}"/> 通用处理。</summary>
        public const string Id = "__id__";

        /// <summary>按标签序号排序（元素首个未被忽略标签在标签列表中的位置）。由宿主实现处理。</summary>
        public const string TagOrder = "__tagOrder__";
    }
}

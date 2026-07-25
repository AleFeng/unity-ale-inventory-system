namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 可选：列表元素自带「用于排序的主键」。当 <c>TData</c> 实现本接口时，
    /// <see cref="SortContextBase{TData}"/> 默认即可对 <see cref="SortFieldKeys.Id"/> 排序，
    /// 无需宿主再显式提供取键委托。
    ///
    /// <para>元素的排序主键与其自身身份不一定相同——例如背包槽位的排序主键是它承载的<b>道具 ID</b>，
    /// 而非槽位自身。若同一元素类型在不同场景需按不同键排序，改为在
    /// <see cref="SortContextBase{TData}.IdOf"/> 中按上下文返回，比固定实现本接口更灵活。</para>
    /// </summary>
    public interface ISortId
    {
        /// <summary>该元素用于 <see cref="SortFieldKeys.Id"/> 比较的主键；无则返回 null / 空串。</summary>
        string SortId { get; }
    }
}

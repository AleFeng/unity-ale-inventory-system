using System.Collections.Generic;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 枚举类型集合的只读来源。宿主的数据库资产实现本接口，供编辑器绘制器（如枚举类型下拉候选列表）
    /// 与其它按名取 <see cref="EnumType"/> 的场合取用，而无需耦合具体数据库类型。
    /// </summary>
    public interface IEnumTypeSource
    {
        /// <summary>全部枚举类型（只读）。</summary>
        IReadOnlyList<EnumType> EnumTypes { get; }

        /// <summary>按名取枚举类型；未找到返回 null。</summary>
        EnumType GetEnumType(string name);
    }
}

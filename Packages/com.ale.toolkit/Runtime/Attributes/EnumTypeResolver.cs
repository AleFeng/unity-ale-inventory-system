using System;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 枚举类型的解析钩子。<see cref="AttributeValue"/> 在把枚举整数值转成显示名时，需要按
    /// 「枚举类型名」找到对应的 <see cref="EnumType"/> —— 而枚举类型集合归宿主插件持有
    /// （通常在其数据库资产上）。故本类只提供一个可注入的委托，由宿主在启动时安装。
    ///
    /// <para><b>未注入时</b>枚举值退化为整数字符串显示（不报错）。宿主应在运行时与编辑器**两条**
    /// 路径都安装：<c>[RuntimeInitializeOnLoadMethod]</c>（Play）+ <c>[InitializeOnLoad]</c>（编辑器），
    /// 后者不可省，否则编辑器内的枚举属性字段会显示整数而非枚举项名。</para>
    /// </summary>
    public static class EnumTypeResolver
    {
        /// <summary>由宿主插件注入：按枚举类型名取 <see cref="EnumType"/>。未注入时返回 null。</summary>
        public static Func<string, EnumType> Resolve;

        /// <summary>按枚举类型名解析 <see cref="EnumType"/>；未注入或名称为空时返回 null。</summary>
        public static EnumType Get(string name)
            => (Resolve != null && !string.IsNullOrEmpty(name)) ? Resolve(name) : null;
    }
}

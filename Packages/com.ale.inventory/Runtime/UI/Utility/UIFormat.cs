using System.Collections.Generic;
using System.Text;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// UI 数值 / 价格文本格式化（静态）。把「按 <see cref="NumberFormatLocale"/> 格式化数值」
    /// 与「拼接多货币价格串」两段此前在道具格、商店视图、商店商品详情里各写一遍的逻辑收口于此。
    /// </summary>
    public static class UIFormat
    {
        // 拼接复用缓冲：价格串很短且都在主线程同步拼接，可安全共用，避免每次调用新建 StringBuilder。
        private static readonly StringBuilder Builder = new StringBuilder();

        /// <summary>多货币价格串中，各货币之间的分隔符。</summary>
        public const string PriceSeparator = "  ";

        /// <summary>按 <paramref name="locale"/> 规则格式化数值；locale 为空时退回 <c>ToString()</c>。</summary>
        public static string Number(NumberFormatLocale locale, long value)
            => locale != null ? locale.Format(value) : value.ToString();

        /// <summary>
        /// 构造「金额 货币ID」串（多货币以 <see cref="PriceSeparator"/> 分隔），金额按
        /// <paramref name="locale"/> 格式化并乘以 <paramref name="multiplier"/>。
        /// <paramref name="price"/> 为空时返回空串。
        /// </summary>
        public static string PriceString(NumberFormatLocale locale, IReadOnlyDictionary<string, int> price,
            int multiplier = 1)
        {
            if (price == null || price.Count == 0) return string.Empty;

            Builder.Clear();
            foreach (var kv in price)
            {
                if (Builder.Length > 0) Builder.Append(PriceSeparator);
                Builder.Append(Number(locale, (long)kv.Value * multiplier)).Append(' ').Append(kv.Key);
            }
            return Builder.ToString();
        }
    }
}

using Ale.Toolkit.Runtime;
using Ale.Toolkit.Runtime.UI;
using System.Collections.Generic;
using System.Text;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 商店的价格文本：「金额 货币名」，金额按数字格式，货币写货币道具的显示名称（货币也是道具）。
    /// 名称属性为空、或货币道具没有该属性时回退为货币道具 ID（与 <see cref="UIFormat.PriceString"/> 一致）。
    /// </summary>
    public static class ShopPriceText
    {
        private static readonly StringBuilder Builder = new StringBuilder();

        /// <summary>多货币以 <see cref="UIFormat.PriceSeparator"/> 分隔；<paramref name="price"/> 为空时返回空串。</summary>
        public static string Format(NumberFormatLocale locale, IReadOnlyDictionary<string, int> price,
            string nameAttrId, int multiplier = 1)
        {
            if (price == null || price.Count == 0) return string.Empty;

            Builder.Clear();
            foreach (var kv in price)
            {
                if (Builder.Length > 0) Builder.Append(UIFormat.PriceSeparator);
                Builder.Append(UIFormat.Number(locale, (long)kv.Value * multiplier)).Append(' ').Append(CurrencyName(kv.Key, nameAttrId));
            }
            return Builder.ToString();
        }

        /// <summary>货币道具的显示名称；取不到时回退为道具 ID。</summary>
        public static string CurrencyName(string currencyId, string nameAttrId)
        {
            if (string.IsNullOrEmpty(nameAttrId)) return currencyId;
            var name = InventoryDataManager.Instance?.GetItem(currencyId)?.GetEntry(nameAttrId)?.value?.AsString;
            return string.IsNullOrEmpty(name) ? currencyId : name;
        }
    }
}

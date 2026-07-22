using System;
using System.Collections.Generic;

namespace InventorySystem.Runtime
{
    /// <summary>单条数字格式化规则。当数值 ≥ <see cref="threshold"/> 时适用。规则列表应按 threshold 从大到小排列。</summary>
    [Serializable]
    public class NumberFormatRule
    {
        /// <summary>触发此规则的最小数值（含）。</summary>
        public long threshold;

        /// <summary>除数，将原始数值缩放后显示（如 1000 → 显示 1K）。</summary>
        public double divisor = 1.0;

        /// <summary>后缀文字（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用），如 "K"、"M"、"亿"。</summary>
        public AttributeValue suffixText = new AttributeValue(EFieldType.Text);

        /// <summary>小数位数。0 = 取整。</summary>
        public int decimalPlaces;

        /// <summary>解析后缀显示文本：本地化优先、回退纯文本（见 <see cref="AttributeValue.ResolveText"/>）。</summary>
        public string ResolveSuffix() => suffixText != null ? suffixText.ResolveText() : string.Empty;

        public NumberFormatRule Clone() => new NumberFormatRule
        {
            threshold       = threshold,
            divisor         = divisor,
            suffixText      = suffixText != null ? suffixText.Clone() : new AttributeValue(EFieldType.Text),
            decimalPlaces   = decimalPlaces,
        };
    }

    /// <summary>某种语言下的全套格式化规则。</summary>
    [Serializable]
    public class NumberFormatLocale
    {
        /// <summary>语言代码，如 "zh-CN"、"en-US"。空字符串视为默认回退语言。</summary>
        public string languageCode;

        /// <summary>格式化规则列表，按 threshold 从大到小排列；无规则命中时直接显示原始数值。</summary>
        public List<NumberFormatRule> rules = new List<NumberFormatRule>();

        /// <summary>将数值按本语言的规则格式化。无命中规则时返回原始数值字符串。</summary>
        public string Format(long value) => Format(value, null);

        /// <summary>
        /// 将数值按本语言的规则格式化。
        /// <paramref name="suffixResolver"/> 用于自定义后缀解析（如本地化）：传入命中的规则，返回最终后缀文本；
        /// 为 null 或返回 null 时退回 <see cref="NumberFormatRule.ResolveSuffix"/>（Text：本地化优先、回退纯文本）。
        /// </summary>
        public string Format(long value, Func<NumberFormatRule, string> suffixResolver)
        {
            if (rules == null || rules.Count == 0) return value.ToString();
            foreach (var rule in rules)
            {
                if (value >= rule.threshold)
                {
                    double scaled = value / rule.divisor;
                    string numStr = rule.decimalPlaces > 0
                        ? scaled.ToString("F" + rule.decimalPlaces)
                        : ((long)Math.Round(scaled)).ToString();
                    string suffix = suffixResolver?.Invoke(rule) ?? rule.ResolveSuffix();
                    return numStr + (suffix ?? string.Empty);
                }
            }
            return value.ToString();
        }

        public NumberFormatLocale Clone()
        {
            var c = new NumberFormatLocale { languageCode = languageCode };
            foreach (var r in rules) c.rules.Add(r.Clone());
            return c;
        }
    }

    /// <summary>
    /// 数字显示格式配置（内联可序列化）。
    /// 支持多语言、多阈值分段格式化，例如：1500 → "1.5K"、10000000 → "1000万"。
    /// </summary>
    [Serializable]
    public class NumberFormatConfig
    {
        /// <summary>配置名称（中心列表中唯一标识，供仓库/模板按名引用）。</summary>
        public string name;

        /// <summary>各语言的格式化规则。至少提供一个 languageCode 为空字符串的默认语言。</summary>
        public List<NumberFormatLocale> locales = new List<NumberFormatLocale>();

        /// <summary>
        /// 将数值格式化为本地化字符串。
        /// </summary>
        /// <param name="value">原始数值。</param>
        /// <param name="langCode">当前语言代码（如 "zh-CN"）。未匹配时自动回退到空字符串语言。</param>
        public string Format(long value, string langCode) => Format(value, langCode, null);

        /// <summary>
        /// 将数值格式化为本地化字符串。
        /// <paramref name="suffixResolver"/> 用于自定义后缀解析（如本地化）：传入命中的规则，返回最终后缀文本；
        /// 为 null 或返回 null 时退回 <see cref="NumberFormatRule.ResolveSuffix"/>（Text：本地化优先、回退纯文本）。
        /// </summary>
        public string Format(long value, string langCode, Func<NumberFormatRule, string> suffixResolver)
        {
            var rules = FindRules(langCode);
            if (rules == null || rules.Count == 0) return value.ToString();

            foreach (var rule in rules)
            {
                if (value >= rule.threshold)
                {
                    double scaled = value / rule.divisor;
                    string numStr = rule.decimalPlaces > 0
                        ? scaled.ToString("F" + rule.decimalPlaces)
                        : ((long)Math.Round(scaled)).ToString();
                    string suffix = suffixResolver?.Invoke(rule) ?? rule.ResolveSuffix();
                    return numStr + (suffix ?? string.Empty);
                }
            }

            return value.ToString();
        }

        private List<NumberFormatRule> FindRules(string langCode)
        {
            if (locales == null) return null;
            foreach (var locale in locales)
                if (locale.languageCode == langCode) return locale.rules;
            foreach (var locale in locales)
                if (string.IsNullOrEmpty(locale.languageCode)) return locale.rules;
            return null;
        }

        public NumberFormatConfig Clone()
        {
            var c = new NumberFormatConfig { name = name };
            foreach (var l in locales) c.locales.Add(l.Clone());
            return c;
        }
    }
}

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 技能显示文本解析（名称 / 描述 / 自定义属性字段），供技能条目与技能 Tooltip 共用。
    /// 名称 / 描述优先取本地化（IS_LOCALIZATION 且引用非空），否则取原始字符串；自定义字段兼容 String / Text / 其它类型。
    /// </summary>
    public static class UiwSkillText
    {
        /// <summary>解析技能显示名：displayText（本地化优先、回退纯文本）→（可选）技能 ID。</summary>
        public static string ResolveName(Skill skill, bool fallbackToId = true)
        {
            if (skill == null) return string.Empty;
            string s = skill.displayText != null ? skill.displayText.ResolveText() : null;
            if (!string.IsNullOrEmpty(s)) return s;
            return fallbackToId ? (skill.id ?? string.Empty) : string.Empty;
        }

        /// <summary>解析技能描述：descriptionText（本地化优先、回退纯文本）→ 空。</summary>
        public static string ResolveDescription(Skill skill)
        {
            if (skill == null) return string.Empty;
            string s = skill.descriptionText != null ? skill.descriptionText.ResolveText() : null;
            return string.IsNullOrEmpty(s) ? string.Empty : s;
        }

        /// <summary>
        /// 解析技能某自定义属性字段的显示值：String 取字符串、Text 取本地化文本（取不到回退纯文本）、其它类型取通用显示串。
        /// 无该属性 / 值为空时返回空串。
        /// </summary>
        public static string ResolveCustomField(Skill skill, string key)
        {
            if (skill == null || string.IsNullOrEmpty(key)) return string.Empty;
            var av = skill.GetAttributeValue(key);
            if (av == null) return string.Empty;

            if (av.Type == EFieldType.String)
            {
                string s = av.AsString;
                if (!string.IsNullOrEmpty(s)) return s;
            }
            if (av.Type == EFieldType.Text)
            {
#if IS_LOCALIZATION
                var (tableRef, entryKey) = av.GetLocalizedStringRef(0);
                string loc = ResolveLocalized(tableRef, entryKey);
                if (!string.IsNullOrEmpty(loc)) return loc;
#endif
                string plain = av.GetTextValue(0);
                if (!string.IsNullOrEmpty(plain)) return plain;
            }
            // 其它类型（Int / Float / Enum / Vector / StringIntPair / EnumIntPair…）用通用显示串。
            return av.ToDisplayString();
        }

#if IS_LOCALIZATION
        /// <summary>
        /// 解析 <see cref="EFieldType.Text"/> 的本地化引用（表 + 条目）当前语言文本。
        /// 供自定义字段用；固定字段（名称 / 描述）已改用 <see cref="AttributeValue.ResolveText"/>。
        /// </summary>
        private static string ResolveLocalized(string tableRef, string entryKey)
        {
            if (string.IsNullOrEmpty(tableRef) && string.IsNullOrEmpty(entryKey)) return null;
            var ls = new UnityEngine.Localization.LocalizedString(tableRef, entryKey);
            return ls.GetLocalizedString();
        }
#endif
    }

    /// <summary>
    /// 技能「位阶」枚举解析辅助：从技能的位阶枚举属性字段解析出对应枚举项，
    /// 供条目背景框（枚举项「背景框」Sprite）与 Tooltip（枚举项「名称」）共用。镜像道具 UI 的品质背景解析链。
    /// </summary>
    public static class SkillRankUtil
    {
        /// <summary>
        /// 解析技能「位阶」枚举属性对应的枚举项。<paramref name="rankAttrId"/> 为技能上的枚举属性字段 ID；
        /// 解析链：该属性的枚举值 + 枚举类型引用 → 枚举类型 → 按值取枚举项。无 / 解析不到返回 null。
        /// </summary>
        public static EnumItem Resolve(Skill skill, string rankAttrId)
        {
            if (skill == null || string.IsNullOrEmpty(rankAttrId)) return null;
            var av = skill.GetEntry(rankAttrId)?.value;
            if (av == null) return null;

            var enumType = InventoryDataManager.Instance != null
                ? InventoryDataManager.Instance.GetEnumType(av.EnumTypeRef) : null;
            return enumType != null ? enumType.GetItemByValue(av.AsEnumValue) : null;
        }
    }
}

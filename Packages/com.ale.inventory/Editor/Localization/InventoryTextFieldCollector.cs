#if IS_LOCALIZATION
using System.Collections.Generic;
using Ale.Inventory.Runtime;

namespace Ale.Inventory.Editor
{
    /// <summary>本地化工具收集到的一个 <see cref="EFieldType.Text"/> 位置：属性值 + 元素索引 + 语义 Key 路径。</summary>
    internal readonly struct TextFieldRef
    {
        public readonly AttributeValue Value;
        public readonly int            Element;
        public readonly string         KeyPath;

        public TextFieldRef(AttributeValue value, int element, string keyPath)
        {
            Value   = value;
            Element = element;
            KeyPath = keyPath;
        }
    }

    /// <summary>
    /// 结构化遍历 <see cref="InventoryDatabase"/>，收集库内<b>所有</b> <see cref="EFieldType.Text"/> 字段
    /// （固定字段 displayText/descriptionText/suffixText/… + 各属性值列表里的 Text 条目），
    /// 为每处产出一个带**语义中文 Key 路径**的 <see cref="TextFieldRef"/>，供本地化工具生成唯一 Key。
    ///
    /// <para>Key 命名：<c>道具系统-{类别}-{实例id}-{字段}[-{元素索引}]</c>；只收集<b>有纯文本内容</b>的元素
    /// （空字段无需本地化）；同名 Key 追加 <c>#n</c> 去重；排除 <see cref="AttributeDefinition.defaultValue"/>（模板默认）。</para>
    /// </summary>
    internal static class InventoryTextFieldCollector
    {
        private const string Root = "道具系统";

        #region 采集入口

        public static List<TextFieldRef> Collect(InventoryDatabase db)
        {
            var list = new List<TextFieldRef>();
            var seen = new HashSet<string>();
            if (!db) return list;

            // 道具条目：名称/描述等一律为属性 Text
            foreach (var it in db.Items)
                if (it != null) AddEntries(list, seen, it.values, $"{Root}-道具条目-{Id(it.id)}");

            // 枚举类型 → 枚举项：属性 Text
            foreach (var et in db.EnumTypes)
            {
                if (et?.items == null) continue;
                foreach (var ei in et.items)
                    if (ei != null)
                        AddEntries(list, seen, ei.attributeValues, $"{Root}-枚举类型-{Id(et.name)}-{Id(ei.name)}");
            }

            // 功能标签：固定 名称/描述
            foreach (var ft in db.FunctionTags)
            {
                if (ft == null) continue;
                string p = $"{Root}-功能标签-{Id(ft.name)}";
                AddText(list, seen, ft.displayNameText, $"{p}-名称");
                AddText(list, seen, ft.descriptionText, $"{p}-描述");
            }

            // 技能：固定 名称/描述 + 属性
            foreach (var s in db.Skills)
            {
                if (s == null) continue;
                string p = $"{Root}-技能-{Id(s.id)}";
                AddText(list, seen, s.displayText,     $"{p}-名称");
                AddText(list, seen, s.descriptionText, $"{p}-描述");
                AddEntries(list, seen, s.values, p);
            }

            // 技能模板：固定 名称/描述
            foreach (var t in db.SkillTemplates)
            {
                if (t == null) continue;
                string p = $"{Root}-技能模板-{Id(t.name)}";
                AddText(list, seen, t.displayText,     $"{p}-名称");
                AddText(list, seen, t.descriptionText, $"{p}-描述");
            }

            // 商店：固定 名称/描述 + 属性
            foreach (var s in db.Shops)
            {
                if (s == null) continue;
                string p = $"{Root}-商店-{Id(s.id)}";
                AddText(list, seen, s.displayNameText, $"{p}-名称");
                AddText(list, seen, s.descriptionText, $"{p}-描述");
                AddEntries(list, seen, s.values, p);
            }

            // 制作蓝图：固定 名称/描述 + 属性
            foreach (var bp in db.CraftingBlueprints)
            {
                if (bp == null) continue;
                string p = $"{Root}-制作蓝图-{Id(bp.id)}";
                AddText(list, seen, bp.displayText,     $"{p}-名称");
                AddText(list, seen, bp.descriptionText, $"{p}-描述");
                AddEntries(list, seen, bp.values, p);
            }

            // 装备组：固定 名称/描述 + 属性
            foreach (var g in db.EquipmentGroups)
            {
                if (g == null) continue;
                string p = $"{Root}-装备组-{Id(g.id)}";
                AddText(list, seen, g.displayNameText, $"{p}-名称");
                AddText(list, seen, g.descriptionText, $"{p}-描述");
                AddEntries(list, seen, g.values, p);
            }

            // 仓库：固定 名称/描述 + 属性
            foreach (var inv in db.Inventories)
            {
                if (inv == null) continue;
                string p = $"{Root}-仓库-{Id(inv.id)}";
                AddText(list, seen, inv.displayNameText, $"{p}-名称");
                AddText(list, seen, inv.descriptionText, $"{p}-描述");
                AddEntries(list, seen, inv.values, p);
            }

            // 数字格式 → 语言 → 规则：后缀
            foreach (var cfg in db.NumberFormatConfigs)
            {
                if (cfg?.locales == null) continue;
                foreach (var loc in cfg.locales)
                {
                    if (loc?.rules == null) continue;
                    string lang = string.IsNullOrEmpty(loc.languageCode) ? "默认" : loc.languageCode;
                    for (int i = 0; i < loc.rules.Count; i++)
                    {
                        var rule = loc.rules[i];
                        if (rule != null)
                            AddText(list, seen, rule.suffixText, $"{Root}-数字格式-{Id(cfg.name)}-{lang}-规则{i}-后缀");
                    }
                }
            }

            // 整理选项：固定 名称 + 属性
            foreach (var so in db.SortOptions)
            {
                if (so == null) continue;
                string p = $"{Root}-整理选项-{Id(so.field)}";
                AddText(list, seen, so.displayName, $"{p}-名称");
                AddEntries(list, seen, so.attributeValues, p);
            }

            // 分组标签（技能/制作/装备）：固定 名称/描述
            AddGroupTags(list, seen, db.SkillGroupTags,     "技能分组标签");
            AddGroupTags(list, seen, db.CraftingGroupTags,  "制作分组标签");
            AddGroupTags(list, seen, db.EquipmentGroupTags, "装备分组标签");

            return list;
        }

        #endregion

        #region 收集辅助

        private static void AddGroupTags(List<TextFieldRef> list, HashSet<string> seen,
            IEnumerable<GroupTag> tags, string category)
        {
            if (tags == null) return;
            foreach (var t in tags)
            {
                if (t == null) continue;
                string p = $"{Root}-{category}-{Id(t.id)}";
                AddText(list, seen, t.displayName, $"{p}-名称");
                AddText(list, seen, t.description, $"{p}-描述");
            }
        }

        /// <summary>收集属性值列表里所有 Text 条目（keyPath 末段 = 属性 id）。</summary>
        private static void AddEntries(List<TextFieldRef> list, HashSet<string> seen,
            IEnumerable<AttributeEntry> values, string keyBase)
        {
            if (values == null) return;
            foreach (var entry in values)
            {
                if (entry?.value == null || entry.value.Type != EFieldType.Text) continue;
                AddText(list, seen, entry.value, $"{keyBase}-{Id(entry.id)}");
            }
        }

        /// <summary>收集一个 Text 属性值（标量 1 个 / 数组逐元素）。</summary>
        private static void AddText(List<TextFieldRef> list, HashSet<string> seen, AttributeValue av, string keyBase)
        {
            if (av == null || av.Type != EFieldType.Text) return;
            if (!av.IsArray)
            {
                Add(list, seen, av, 0, keyBase);
            }
            else
            {
                for (int e = 0; e < av.Count; e++)
                    Add(list, seen, av, e, $"{keyBase}-{e}");
            }
        }

        private static void Add(List<TextFieldRef> list, HashSet<string> seen, AttributeValue av, int element, string keyPath)
        {
            // 仅收集有纯文本内容的元素（空字段无需本地化）
            if (string.IsNullOrEmpty(av.GetTextValue(element))) return;
            list.Add(new TextFieldRef(av, element, Unique(seen, keyPath)));
        }

        /// <summary>保证 Key 唯一：撞名追加 <c>#2</c>、<c>#3</c>…</summary>
        private static string Unique(HashSet<string> seen, string key)
        {
            if (seen.Add(key)) return key;
            int n = 2;
            while (!seen.Add($"{key}#{n}")) n++;
            return $"{key}#{n}";
        }

        private static string Id(string s) => string.IsNullOrEmpty(s) ? "(空)" : s;
        #endregion

    }
}
#endif

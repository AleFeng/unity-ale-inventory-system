#if ATK_LOCALIZATION
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Runtime;
using Ale.Toolkit.Editor;
using static Ale.Toolkit.Editor.TextFieldCollector;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 结构化遍历 <see cref="InventoryDatabase"/>，收集库内<b>所有</b> <see cref="EFieldType.Text"/> 字段
    /// （固定字段 displayText/descriptionText/suffixText/… + 各属性值列表里的 Text 条目），
    /// 为每处产出一个带**语义中文 Key 路径**的 <see cref="TextFieldRef"/>，供本地化工具生成唯一 Key。
    ///
    /// <para>Key 命名：<c>道具系统-{类别}-{实例id}-{字段}[-{元素索引}]</c>；只收集<b>有纯文本内容</b>的元素；
    /// 同名 Key 追加 <c>#n</c> 去重。与领域无关的收集与去重机制经 <see cref="TextFieldCollector"/>；本类只负责
    /// 「遍历哪些对象、拼什么语义 Key 路径」。</para>
    /// </summary>
    internal static class InventoryTextFieldCollector
    {
        private const string Root = "道具系统";

        public static List<TextFieldRef> Collect(InventoryDatabase db)
        {
            var c = new TextFieldCollector();
            if (!db) return c.Result;

            // 道具条目：名称/描述等一律为属性 Text
            foreach (var it in db.Items)
                if (it != null) c.AddEntries(it.values, $"{Root}-道具条目-{Id(it.id)}");

            // 枚举类型 → 枚举项：属性 Text
            foreach (var et in db.EnumTypes)
            {
                if (et?.items == null) continue;
                foreach (var ei in et.items)
                    if (ei != null)
                        c.AddEntries(ei.attributeValues, $"{Root}-枚举类型-{Id(et.name)}-{Id(ei.name)}");
            }

            // 功能标签：固定 名称/描述
            foreach (var ft in db.FunctionTags)
            {
                if (ft == null) continue;
                string p = $"{Root}-功能标签-{Id(ft.name)}";
                c.AddText(ft.displayNameText, $"{p}-名称");
                c.AddText(ft.descriptionText, $"{p}-描述");
            }

            // 技能：固定 名称/描述 + 属性
            foreach (var s in db.Skills)
            {
                if (s == null) continue;
                string p = $"{Root}-技能-{Id(s.id)}";
                c.AddText(s.displayText,     $"{p}-名称");
                c.AddText(s.descriptionText, $"{p}-描述");
                c.AddEntries(s.values, p);
            }

            // 技能模板：固定 名称/描述
            foreach (var t in db.SkillTemplates)
            {
                if (t == null) continue;
                string p = $"{Root}-技能模板-{Id(t.name)}";
                c.AddText(t.displayText,     $"{p}-名称");
                c.AddText(t.descriptionText, $"{p}-描述");
            }

            // 商店：固定 名称/描述 + 属性
            foreach (var s in db.Shops)
            {
                if (s == null) continue;
                string p = $"{Root}-商店-{Id(s.id)}";
                c.AddText(s.displayNameText, $"{p}-名称");
                c.AddText(s.descriptionText, $"{p}-描述");
                c.AddEntries(s.values, p);
            }

            // 制作蓝图：固定 名称/描述 + 属性
            foreach (var bp in db.CraftingBlueprints)
            {
                if (bp == null) continue;
                string p = $"{Root}-制作蓝图-{Id(bp.id)}";
                c.AddText(bp.displayText,     $"{p}-名称");
                c.AddText(bp.descriptionText, $"{p}-描述");
                c.AddEntries(bp.values, p);
            }

            // 装备组：固定 名称/描述 + 属性
            foreach (var g in db.EquipmentGroups)
            {
                if (g == null) continue;
                string p = $"{Root}-装备组-{Id(g.id)}";
                c.AddText(g.displayNameText, $"{p}-名称");
                c.AddText(g.descriptionText, $"{p}-描述");
                c.AddEntries(g.values, p);
            }

            // 仓库：固定 名称/描述 + 属性
            foreach (var inv in db.Inventories)
            {
                if (inv == null) continue;
                string p = $"{Root}-仓库-{Id(inv.id)}";
                c.AddText(inv.displayNameText, $"{p}-名称");
                c.AddText(inv.descriptionText, $"{p}-描述");
                c.AddEntries(inv.values, p);
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
                            c.AddText(rule.suffixText, $"{Root}-数字格式-{Id(cfg.name)}-{lang}-规则{i}-后缀");
                    }
                }
            }

            // 整理选项：固定 名称 + 属性
            foreach (var so in db.SortOptions)
            {
                if (so == null) continue;
                string p = $"{Root}-整理选项-{Id(so.field)}";
                c.AddText(so.displayName, $"{p}-名称");
                c.AddEntries(so.attributeValues, p);
            }

            // 分组标签（技能/制作/装备）：固定 名称/描述
            AddGroupTags(c, db.SkillGroupTags,     "技能分组标签");
            AddGroupTags(c, db.CraftingGroupTags,  "制作分组标签");
            AddGroupTags(c, db.EquipmentGroupTags, "装备分组标签");

            return c.Result;
        }

        private static void AddGroupTags(TextFieldCollector c, IEnumerable<GroupTag> tags, string category)
        {
            if (tags == null) return;
            foreach (var t in tags)
            {
                if (t == null) continue;
                string p = $"{Root}-{category}-{Id(t.id)}";
                c.AddText(t.displayName, $"{p}-名称");
                c.AddText(t.description, $"{p}-描述");
            }
        }
    }
}
#endif

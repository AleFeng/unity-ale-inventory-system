using InventorySystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// <see cref="NumberFormatConfig"/> 的 IMGUI 绘制辅助类。
    /// 提供语言列表与规则列表的内联编辑 UI，供 InventoryInspectorPanel / InventoryTemplatePanel 共用。
    /// </summary>
    public static class NumberFormatConfigDrawer
    {
        private static readonly GUIContent[] RuleHeaders =
        {
            new GUIContent("阈值"),
            new GUIContent("除数"),
            new GUIContent("小数位"),
        };

        /// <summary>
        /// 绘制「数字格式」引用下拉框：选项为 None + 数据库中所有数字格式配置名称。
        /// 选择后通过 <paramref name="setRef"/> 回写引用名称（None → 空字符串）。
        /// </summary>
        public static void DrawRefPopup(IInventoryEditorContext ctx, string label,
            string current, System.Action<string> setRef)
        {
            var configs = ctx.Database.NumberFormatConfigs;

            // 选项 0 = None，其后为各配置名称
            var displays = new string[configs.Count + 1];
            displays[0]  = "None";
            int curIdx   = 0;
            for (int i = 0; i < configs.Count; i++)
            {
                string name  = string.IsNullOrEmpty(configs[i].name) ? $"（未命名 {i}）" : configs[i].name;
                displays[i + 1] = name;
                if (!string.IsNullOrEmpty(current) && configs[i].name == current)
                    curIdx = i + 1;
            }

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup(label, curIdx, displays);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改数字格式引用");
                setRef(picked <= 0 ? string.Empty : configs[picked - 1].name);
                ctx.MarkDirty();
            }
        }

        /// <summary>绘制 <see cref="NumberFormatConfig"/> 的完整 Inspector UI。</summary>
        public static void Draw(IInventoryEditorContext ctx, NumberFormatConfig config)
        {
            if (config == null) return;

            int pendingDeleteLocale = -1;

            for (int li = 0; li < config.locales.Count; li++)
            {
                var locale = config.locales[li];

                // ── 语言行 ──────────────────────────────────────────────
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                string langLabel = string.IsNullOrEmpty(locale.languageCode)
                    ? $"语言 {li}（默认回退）" : $"语言 {li}";
                string newLang = EditorGUILayout.TextField(langLabel,
                    locale.languageCode ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改语言代码");
                    locale.languageCode = newLang;
                    ctx.MarkDirty();
                }
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    pendingDeleteLocale = li;
                EditorGUILayout.EndHorizontal();

                // ── 规则列表 ─────────────────────────────────────────────
                DrawRules(ctx, locale);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (pendingDeleteLocale >= 0)
            {
                ctx.RecordUndo("删除语言");
                config.locales.RemoveAt(pendingDeleteLocale);
                ctx.MarkDirty();
            }

            if (GUILayout.Button("+ 添加语言", EditorStyles.miniButton))
            {
                ctx.RecordUndo("添加语言");
                config.locales.Add(new NumberFormatLocale());
                ctx.MarkDirty();
            }
        }

        private static void DrawRules(IInventoryEditorContext ctx, NumberFormatLocale locale)
        {
            // ── 列标题 ───────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f + 4f);
            foreach (var h in RuleHeaders)
                EditorGUILayout.LabelField(h, EditorStyles.miniLabel, GUILayout.Width(70));
            GUILayout.Space(26f);
            EditorGUILayout.EndHorizontal();

            int pendingDeleteRule = -1;

            for (int ri = 0; ri < locale.rules.Count; ri++)
            {
                var rule = locale.rules[ri];

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 15f + 4f);

                EditorGUI.BeginChangeCheck();
                long   newThreshold    = EditorGUILayout.LongField(rule.threshold,    GUILayout.Width(70));
                double newDivisor      = EditorGUILayout.DoubleField(rule.divisor,    GUILayout.Width(70));
                int    newDecimalPlaces = EditorGUILayout.IntField(rule.decimalPlaces, GUILayout.Width(70));
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改格式规则");
                    rule.threshold     = newThreshold;
                    rule.divisor       = newDivisor;
                    rule.decimalPlaces = newDecimalPlaces;
                    ctx.MarkDirty();
                }

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    pendingDeleteRule = ri;

                EditorGUILayout.EndHorizontal();

                // 后缀：Text（纯文本 fallback + 原生可搜索本地化选择器），独立整行绘制
                AttributeFieldDrawer.Draw(ctx, "后缀", rule.suffixText, null);
            }

            if (pendingDeleteRule >= 0)
            {
                ctx.RecordUndo("删除格式规则");
                locale.rules.RemoveAt(pendingDeleteRule);
                ctx.MarkDirty();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f + 4f);
            if (GUILayout.Button("+ 添加规则", EditorStyles.miniButton))
            {
                ctx.RecordUndo("添加格式规则");
                locale.rules.Add(new NumberFormatRule());
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

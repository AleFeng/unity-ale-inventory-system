using Ale.Toolkit.Runtime;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 数字格式配置面板（泛型基类）：
    ///   左侧 — 配置主列表（名称 + 语言数，可拖拽排序、增删）
    ///   右侧 Inspector —
    ///     1. 配置名称（供宿主库按名引用）
    ///     2. 语言 / 规则编辑（复用 <see cref="NumberFormatConfigDrawer"/>）
    ///
    /// <para>子类仅需绑定 <see cref="EditorMasterListPanel{TDb,T}.GetList"/>（即宿主库的数字格式配置列表）。</para>
    /// </summary>
    /// <typeparam name="TDb">宿主数据库类型。</typeparam>
    public abstract class EditorNumberFormatConfigPanel<TDb> : EditorMasterListPanel<TDb, NumberFormatConfig>
        where TDb : ScriptableObject
    {
        #region 主列表配置

        protected override string Noun => "数字格式";

        protected override string RowLabel(NumberFormatConfig c)
        {
            string name = string.IsNullOrEmpty(c.name) ? Tr("（未命名）") : c.name;
            return $"{name}  ({c.locales.Count})";
        }

        /// <summary>新建配置：名称去重，并默认带一个空 languageCode 的回退语言。</summary>
        protected override NumberFormatConfig CreateNew(TDb db, List<NumberFormatConfig> list)
        {
            var cfg = new NumberFormatConfig { name = GenerateUniqueName(list, Tr("新格式")) };
            cfg.locales.Add(new NumberFormatLocale());
            return cfg;
        }

        #endregion

        // ── Inspector ─────────────────────────────────────────────────────────────

        public override void DrawInspector(IEditorDbContext<TDb> ctx, NumberFormatConfig config)
        {
            if (config == null)
            {
                EditorGUILayout.LabelField(Tr("请选择或新建一个数字格式配置。"));
                return;
            }

            // ── 1. 配置名称 ───────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField(Tr("配置名称"), config.name);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改数字格式名称");
                config.name = newName;
                ctx.MarkDirty();
            }

            // 名称为空 / 重复 时提示（引用按名称匹配，需保持唯一）
            var configs = GetList(ctx.Database);
            if (string.IsNullOrEmpty(config.name))
                EditorGUILayout.LabelField(Tr("⚠ 名称为空时无法被引用。"), ToolkitEditorStyles.StatusError);
            else if (CountByName(configs, config.name) > 1)
                EditorGUILayout.LabelField(Tr("⚠ 名称重复，引用将命中第一个同名配置。"),
                    ToolkitEditorStyles.StatusError);

            EditorGUILayout.Space(6);

            // ── 2. 语言 / 规则 ────────────────────────────────────────────────
            EditorGUILayout.LabelField(Tr("语言与规则"), ToolkitEditorStyles.Header);
            NumberFormatConfigDrawer.Draw(ctx, config);
        }

        // ── 辅助 ────────────────────────────────────────────────────────────────

        private static int CountByName(List<NumberFormatConfig> list, string name)
        {
            int n = 0;
            foreach (var c in list)
                if (c.name == name) n++;
            return n;
        }

        private static string GenerateUniqueName(List<NumberFormatConfig> list, string baseName)
        {
            if (CountByName(list, baseName) == 0) return baseName;
            for (int i = 2; ; i++)
            {
                string candidate = $"{baseName} {i}";
                if (CountByName(list, candidate) == 0) return candidate;
            }
        }
    }
}

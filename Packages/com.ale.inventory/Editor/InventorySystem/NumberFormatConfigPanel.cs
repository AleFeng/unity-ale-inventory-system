using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 数字格式配置面板：
    ///   左侧 — 配置主列表（名称 + 语言数，可拖拽排序、增删）
    ///   右侧 Inspector —
    ///     1. 配置名称（供仓库/模板按名引用）
    ///     2. 语言 / 规则编辑（复用 <see cref="NumberFormatConfigDrawer"/>）
    /// </summary>
    public class NumberFormatConfigPanel
    {
        private ReorderableList          _masterList;
        private List<NumberFormatConfig> _boundMasterList;
        private int                      _selectedIndex      = -1;
        private int                      _pendingDeleteIndex = -1;
        private IInventoryEditorContext  _masterCtx;

        // ── 主列表 ────────────────────────────────────────────────────────────────

        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            var db   = ctx.Database;
            var list = db.NumberFormatConfigs;
            _masterCtx = ctx;

            if (_masterList == null || !ReferenceEquals(_boundMasterList, list))
            {
                _selectedIndex = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                BuildMasterList(list);
            }
            else
            {
                int clamped = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                if (_selectedIndex != clamped)
                {
                    _selectedIndex    = clamped;
                    _masterList.index = clamped;
                }
            }

            // ── 标题栏 + 添加按钮 ──────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("数字格式", InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加数字格式");
                var cfg = new NumberFormatConfig { name = GenerateUniqueName(list, "新格式") };
                // 默认带一个空 languageCode 的回退语言
                cfg.locales.Add(new NumberFormatLocale());
                list.Add(cfg);
                ctx.MarkDirty();
                _selectedIndex    = list.Count - 1;
                _masterList.index = _selectedIndex;
            }
            EditorGUILayout.EndHorizontal();

            _masterList.DoLayoutList();

            // ── 延迟删除 ──────────────────────────────────────────────────────
            if (_pendingDeleteIndex >= 0)
            {
                int di = _pendingDeleteIndex;
                _pendingDeleteIndex = -1;
                if (di < list.Count)
                {
                    ctx.RecordUndo("删除数字格式");
                    list.RemoveAt(di);
                    ctx.MarkDirty();
                    _selectedIndex    = Mathf.Clamp(_selectedIndex, -1, list.Count - 1);
                    _masterList.index = _selectedIndex;
                }
            }

            return _selectedIndex;
        }

        private void BuildMasterList(List<NumberFormatConfig> list)
        {
            _boundMasterList = list;
            _masterList = new ReorderableList(list, typeof(NumberFormatConfig),
                draggable: true, displayHeader: false,
                displayAddButton: false, displayRemoveButton: false);

            _masterList.elementHeight = 22f;
            _masterList.index         = _selectedIndex;

            _masterList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (active)
                    InventoryEditorStyles.DrawRowBackground(rect, InventoryEditorStyles.SelectedColor);
            };

            _masterList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= list.Count) return;
                var c    = list[index];
                float cy = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;

                var delRect   = new Rect(rect.xMax - 22, cy, 20f, EditorGUIUtility.singleLineHeight);
                var labelRect = new Rect(rect.x, cy,
                    rect.xMax - 22 - rect.x - 4, EditorGUIUtility.singleLineHeight);
                string name = string.IsNullOrEmpty(c.name) ? "（未命名）" : c.name;
                GUI.Label(labelRect, $"{name}  ({c.locales.Count})");

                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    _pendingDeleteIndex = index;
            };

            _masterList.onSelectCallback = rl => _selectedIndex = rl.index;

            _masterList.onReorderCallback = _ =>
            {
                _masterCtx.RecordUndo("调整数字格式顺序");
                _masterCtx.MarkDirty();
            };
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        public void DrawInspector(IInventoryEditorContext ctx, NumberFormatConfig config)
        {
            if (config == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个数字格式配置。");
                return;
            }

            // ── 1. 配置名称 ───────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("配置名称", config.name);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改数字格式名称");
                config.name = newName;
                ctx.MarkDirty();
            }

            // 名称为空 / 重复 时提示（引用按名称匹配，需保持唯一）
            if (string.IsNullOrEmpty(config.name))
                EditorGUILayout.LabelField("⚠ 名称为空时无法被引用。", InventoryEditorStyles.StatusError);
            else if (CountByName(ctx.Database.NumberFormatConfigs, config.name) > 1)
                EditorGUILayout.LabelField("⚠ 名称重复，引用将命中第一个同名配置。",
                    InventoryEditorStyles.StatusError);

            EditorGUILayout.Space(6);

            // ── 2. 语言 / 规则 ────────────────────────────────────────────────
            EditorGUILayout.LabelField("语言与规则", InventoryEditorStyles.Header);
            NumberFormatConfigDrawer.Draw(ctx, config);
        }

        /// <summary>数据库切换、外部重置或 Undo/Redo 时调用，清空缓存。</summary>
        public void Invalidate()
        {
            _masterList         = null;
            _boundMasterList    = null;
            _selectedIndex      = -1;
            _pendingDeleteIndex = -1;
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

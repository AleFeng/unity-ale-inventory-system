using System.Collections.Generic;
using InventorySystem.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 整理选项面板。
    ///   左侧 — 从仓库模板自动生成的整理选项列表（只读，不可手动增删）。
    ///   右侧 Inspector — 选中整理选项的两个内置字段：
    ///     1. 名称（<see cref="EFieldType.Text"/>：排序下拉显示名，纯文本 fallback + 可选本地化引用）
    ///     2. 忽略ID（排序时跳过的条目 ID 列表，可拖拽重排、手动输入）
    /// </summary>
    public class SortOptionPanel
    {
        // ── 字段名 → 显示名 ──────────────────────────────────────────────────────────
        private static string GetFieldDisplayName(string field)
        {
            if (field == "__id__")       return "道具ID";
            if (field == "__tagOrder__") return "功能页签";
            return field;
        }

        // ── 主列表 ReorderableList 状态 ────────────────────────────────────────────
        private ReorderableList         _masterList;
        private List<SortOption>        _boundList;
        private int                     _selectedIndex = -1;
        private IInventoryEditorContext _masterCtx;

        // ── 忽略ID 列表的拖拽重排状态 ──────────────────────────────────────────────
        private readonly EditorReorderableDrag _ignoreDrag = new EditorReorderableDrag("SortOptionIgnoreDrag");

        // 样式缓存
        private GUIStyle _itemHeaderStyle;
        private GUIStyle ItemHeaderStyle => _itemHeaderStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            normal   = { textColor = new Color(0.55f, 0.85f, 1.0f) },
            fontSize = 11
        };

        // ── 主列表 ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 绘制主列表（左侧面板），返回当前选中索引。
        /// 每次调用时自动执行 <see cref="InventoryDatabase.RebuildSortOptions"/>，
        /// 保证列表与模板数据保持同步。
        /// </summary>
        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            var db   = ctx.Database;
            _masterCtx = ctx;

            // 每帧同步（幂等）：从模板重建整理选项列表，仅在列表内容有变化时标脏
            int prevCount = db.SortOptions.Count;
            db.RebuildSortOptions();
            if (db.SortOptions.Count != prevCount)
                ctx.MarkDirty();

            var list = db.SortOptions;

            if (_masterList == null || !ReferenceEquals(_boundList, list))
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

            // ── 标题栏（只读，无添加按钮）─────────────────────────────────────
            EditorGUILayout.LabelField("整理选项", InventoryEditorStyles.Header);
            EditorGUILayout.HelpBox("列表包含所有可用的排序字段，不可手动增删。", MessageType.None);

            if (list.Count == 0)
            {
                EditorGUILayout.LabelField("（暂无排序字段，请先添加道具模板属性或功能标签）",
                    InventoryEditorStyles.Placeholder);
            }
            else
            {
                _masterList.DoLayoutList();
            }

            return _selectedIndex;
        }

        private void BuildMasterList(List<SortOption> list)
        {
            _boundList  = list;
            _masterList = new ReorderableList(list, typeof(SortOption),
                draggable: false, displayHeader: false,
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
                float cy = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;
                GUI.Label(new Rect(rect.x + 4, cy, rect.width - 4, EditorGUIUtility.singleLineHeight),
                    GetFieldDisplayName(list[index].field));
            };

            _masterList.onSelectCallback = rl => _selectedIndex = rl.index;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        /// <summary>绘制右侧 Inspector（选中整理选项的内置字段：名称 + 忽略ID）。</summary>
        public void DrawInspector(IInventoryEditorContext ctx, SortOption selected)
        {
            if (selected == null)
            {
                EditorGUILayout.LabelField("（请在左侧选择一个整理选项）", InventoryEditorStyles.Placeholder);
                return;
            }

            EditorGUILayout.LabelField($"▸ {GetFieldDisplayName(selected.field)}", ItemHeaderStyle);
            EditorGUILayout.Space(4);

            // 1. 名称（Text：纯文本 fallback + 可选本地化引用），复用统一属性绘制器。
            selected.NormalizeDisplayName();
            EditorGUILayout.LabelField("名称（排序下拉显示名；为空时用字段名）", EditorStyles.miniLabel);
            AttributeFieldDrawer.Draw(ctx, "名称", selected.displayName, null);

            EditorGUILayout.Space(6);

            // 2. 忽略ID
            DrawIgnoreIds(ctx, selected);
        }

        // ── 忽略ID：可拖拽重排 + 手动输入的单行列表 ────────────────────────────────

        private void DrawIgnoreIds(IInventoryEditorContext ctx, SortOption so)
        {
            if (so.ignoreIds == null) so.ignoreIds = new List<string>();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("忽略ID", InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加忽略ID");
                so.ignoreIds.Add(string.Empty);
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                "排序时跳过这些条目（按道具ID排序 = 道具ID；功能页签 = 标签名；按属性排序 = 属性值）。",
                EditorStyles.miniLabel);

            if (so.ignoreIds.Count == 0)
            {
                EditorGUILayout.LabelField("（未配置）", EditorStyles.miniLabel);
                return;
            }

            _ignoreDrag.BeginFrame();
            int removeIndex = -1;
            for (int i = 0; i < so.ignoreIds.Count; i++)
            {
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                _ignoreDrag.RecordRow(i, rowRect);

                GUILayout.Space(EditorReorderableDrag.HandleWidth);
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));

                EditorGUI.BeginChangeCheck();
                string v = EditorGUILayout.TextField(so.ignoreIds[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改忽略ID");
                    so.ignoreIds[i] = v;
                    ctx.MarkDirty();
                }

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();

                var handleRect = new Rect(rowRect.x,
                    rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                    EditorReorderableDrag.HandleWidth, EditorGUIUtility.singleLineHeight);
                _ignoreDrag.DrawHandle(handleRect, i);
            }
            _ignoreDrag.EndFrame(ctx, so.ignoreIds, "调整忽略ID顺序");

            if (removeIndex >= 0)
            {
                ctx.RecordUndo("移除忽略ID");
                so.ignoreIds.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }

        /// <summary>数据库切换或 Undo/Redo 时调用，清空所有缓存列表。</summary>
        public void Invalidate()
        {
            _masterList    = null;
            _boundList     = null;
            _selectedIndex = -1;
        }
    }
}

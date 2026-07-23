using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库模板面板：左侧主列表（模板行，可拖拽排序）+ 右侧 Inspector（名称、颜色、容量、标签设置、整理设置、自定义属性字段）。
    /// </summary>
    public class InventoryTemplatePanel
    {
        // ── 主列表 ReorderableList 状态 ────────────────────────────────────────────
        private ReorderableList           _masterList;
        private List<InventoryTemplate>   _boundMasterList;
        private int                       _masterSelectedIndex = -1;
        private int                       _pendingDeleteIndex  = -1;
        private IInventoryEditorContext   _masterCtx;

        // 整理列表 / 整理优先级由 SortSettingsDrawer 统一绘制并自持状态，本类不再缓存。

        // ── 属性字段定义列表绘制器（实例持有，保持拖拽排序缓存）──────────────────────
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        // ── 主列表 ────────────────────────────────────────────────────────────────

        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            var db   = ctx.Database;
            var list = db.InventoryTemplates;
            _masterCtx = ctx;

            if (_masterList == null || !ReferenceEquals(_boundMasterList, list))
            {
                _masterSelectedIndex = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                BuildMasterList(list);
            }
            else
            {
                int clamped = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                if (_masterSelectedIndex != clamped)
                {
                    _masterSelectedIndex = clamped;
                    _masterList.index    = clamped;
                }
            }

            // ── 标题栏 + 添加按钮 ──────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("仓库模板", InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加仓库模板");
                list.Add(new InventoryTemplate("新模板"));
                ctx.MarkDirty();
                _masterSelectedIndex = list.Count - 1;
                _masterList.index    = _masterSelectedIndex;
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
                    ctx.RecordUndo("删除仓库模板");
                    list.RemoveAt(di);
                    ctx.MarkDirty();
                    _masterSelectedIndex = Mathf.Clamp(_masterSelectedIndex, -1, list.Count - 1);
                    _masterList.index    = _masterSelectedIndex;
                }
            }

            return _masterSelectedIndex;
        }

        private void BuildMasterList(List<InventoryTemplate> list)
        {
            _boundMasterList = list;
            _masterList = new ReorderableList(list, typeof(InventoryTemplate),
                draggable: true, displayHeader: false,
                displayAddButton: false, displayRemoveButton: false);

            _masterList.elementHeight = 22f;
            _masterList.index         = _masterSelectedIndex;

            _masterList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (active)
                    InventoryEditorStyles.DrawRowBackground(rect, InventoryEditorStyles.SelectedColor);
            };

            _masterList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= list.Count) return;
                var t    = list[index];
                float cy = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;

                // 圆形色点
                var dotRect = new Rect(rect.x, cy, 14f, EditorGUIUtility.singleLineHeight);
                InventoryEditorStyles.DrawColorDot(dotRect, t.color);

                var delRect   = new Rect(rect.xMax - 22, cy, 20f, EditorGUIUtility.singleLineHeight);
                var labelRect = new Rect(dotRect.xMax + 3, cy,
                    rect.xMax - 22 - dotRect.xMax - 7, EditorGUIUtility.singleLineHeight);
                GUI.Label(labelRect, t.name);

                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    _pendingDeleteIndex = index;
            };

            _masterList.onSelectCallback = rl => _masterSelectedIndex = rl.index;

            _masterList.onReorderCallback = _ =>
            {
                _masterCtx.RecordUndo("调整仓库模板顺序");
                _masterCtx.MarkDirty();
            };
        }

        /// <summary>数据库切换或外部重置时调用，清空主列表缓存。</summary>
        public void Invalidate()
        {
            _masterList          = null;
            _boundMasterList     = null;
            _masterSelectedIndex = -1;
            _pendingDeleteIndex  = -1;
            _attrDefsDrawer.Invalidate();
        }

        // ── Inspector ────────────────────────────────────────────────────────────

        public void DrawInspector(IInventoryEditorContext ctx, InventoryTemplate template)
        {
            if (template == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个仓库模板。");
                return;
            }

            EditorGUI.BeginChangeCheck();
            string newName  = EditorGUILayout.TextField("模板名称", template.name);
            Color  newColor = EditorGUILayout.ColorField("标识颜色", template.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改仓库模板基本信息");
                template.name  = newName;
                template.color = newColor;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 容量上限 / 重量上限 ──────────────────────────────────────────
            EditorGUILayout.LabelField("基础属性", InventoryEditorStyles.Header);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newCap = EditorGUILayout.IntField("容量上限", template.capacity);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改模板容量上限");
                template.capacity = Mathf.Max(0, newCap);
                ctx.MarkDirty();
            }
            EditorGUILayout.LabelField("（0 = 无上限）", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            float newWt = EditorGUILayout.FloatField("重量上限", template.weightLimit);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改模板重量上限");
                template.weightLimit = Mathf.Max(0f, newWt);
                ctx.MarkDirty();
            }
            EditorGUILayout.LabelField("（0 = 无上限）", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            DrawTagRefList(ctx, template, template.allowPutTagRefs,     "放入功能标签");
            EditorGUILayout.Space(4);
            DrawTagRefList(ctx, template, template.allowTakeTagRefs,    "取出功能标签");
            EditorGUILayout.Space(4);
            DrawTagRefList(ctx, template, template.allowOperateTagRefs, "操作功能标签");

            EditorGUILayout.Space(6);

            // ── 过滤设置 ─────────────────────────────────────────────────────
            EditorGUILayout.LabelField("过滤设置", InventoryEditorStyles.Header);
            EditorGUILayout.LabelField("过滤列表（UI 中以标签按钮形式显示）：",
                EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            bool newShowAll = EditorGUILayout.ToggleLeft(
                new GUIContent("全部", "勾选后 UI 过滤页签栏会显示「全部」页签（默认选中、不过滤）；" +
                                       "取消后不显示「全部」，默认选中第一个过滤标签。"),
                template.showAllFilterTab);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改显示全部页签");
                template.showAllFilterTab = newShowAll;
                ctx.MarkDirty();
            }

            DrawFilterTagList(ctx, template, template.filterTagRefs);

            EditorGUILayout.Space(6);

            // ── 整理设置 ─────────────────────────────────────────────────────
            EditorGUILayout.LabelField("整理设置", InventoryEditorStyles.Header);

            EditorGUI.BeginChangeCheck();
            bool newDragSort = EditorGUILayout.Toggle("允许拖拽整理", template.dragSort);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改模板拖拽整理");
                template.dragSort = newDragSort;
                ctx.MarkDirty();
            }
            
            EditorGUI.BeginChangeCheck();
            bool newAutoSort = EditorGUILayout.Toggle("自动整理", template.autoSort);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改模板自动整理");
                template.autoSort = newAutoSort;
                ctx.MarkDirty();
            }

            SortSettingsDrawer.Draw(ctx, template.sortPriorities, template.sortTiebreakers, undoPrefix: "模板");

            EditorGUILayout.Space(6);

            // ── UI 配置 ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("UI 配置", InventoryEditorStyles.Header);
            NumberFormatConfigDrawer.DrawRefPopup(ctx, "数字格式",
                template.numberFormatRef, v => template.numberFormatRef = v);

            EditorGUILayout.Space(6);

            _attrDefsDrawer.Draw(ctx, template.attributes, "自定义属性字段");
        }

        // ── 过滤标签勾选 ──────────────────────────────────────────────────────────

        private static void DrawFilterTagList(IInventoryEditorContext ctx,
            InventoryTemplate template, List<string> filterTagRefs)
        {
            var db = ctx.Database;
            if (db.FunctionTags.Count == 0)
            {
                EditorGUILayout.LabelField("（暂无可用功能标签）", EditorStyles.miniLabel);
                return;
            }
            foreach (var tag in db.FunctionTags)
            {
                bool has = filterTagRefs.Contains(tag.name);
                bool now = EditorGUILayout.ToggleLeft(tag.name, has);
                if (now != has)
                {
                    ctx.RecordUndo(now ? "模板添加过滤标签" : "模板移除过滤标签");
                    if (now) filterTagRefs.Add(tag.name);
                    else     filterTagRefs.Remove(tag.name);
                    ctx.MarkDirty();
                }
            }
        }

        // ── 功能标签勾选 ──────────────────────────────────────────────────────────

        private static void DrawTagRefList(IInventoryEditorContext ctx,
            InventoryTemplate template, List<string> tagRefs, string labelText)
        {
            EditorGUILayout.LabelField(labelText, InventoryEditorStyles.Header);

            var db = ctx.Database;
            if (db.FunctionTags.Count == 0)
            {
                EditorGUILayout.LabelField("（暂无可用功能标签）", EditorStyles.miniLabel);
                return;
            }

            foreach (var tag in db.FunctionTags)
            {
                bool has = tagRefs.Contains(tag.name);
                bool now = EditorGUILayout.ToggleLeft(tag.name, has);
                if (now != has)
                {
                    ctx.RecordUndo(now ? $"模板添加{labelText}" : $"模板移除{labelText}");
                    if (now) tagRefs.Add(tag.name);
                    else     tagRefs.Remove(tag.name);
                    ctx.MarkDirty();
                }
            }
        }
    }
}

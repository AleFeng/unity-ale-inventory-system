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

        // ── 整理列表 ReorderableList 状态 ─────────────────────────────────────────
        private ReorderableList         _sortList;
        private ReorderableList         _sortTiebreakerList;
        private InventoryTemplate       _boundTemplate;
        private IInventoryEditorContext _ctx;

        // ── 属性字段定义列表绘制器（实例持有，保持拖拽排序缓存）──────────────────────
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();
        private string[]                _fieldDisplays;
        private string[]                _fieldValues;

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

        /// <summary>数据库切换或外部重置时调用，清空主列表与整理列表缓存。</summary>
        public void Invalidate()
        {
            _masterList          = null;
            _boundMasterList     = null;
            _masterSelectedIndex = -1;
            _pendingDeleteIndex  = -1;
            _sortList            = null;
            _sortTiebreakerList  = null;
            _boundTemplate       = null;
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

            DrawSortPriorities(ctx, template);
            DrawSortTiebreakers(ctx, template);

            EditorGUILayout.Space(6);

            // ── UI 配置 ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("UI 配置", InventoryEditorStyles.Header);
            NumberFormatConfigDrawer.DrawRefPopup(ctx, "数字格式",
                template.numberFormatRef, v => template.numberFormatRef = v);

            EditorGUILayout.Space(6);

            _attrDefsDrawer.Draw(ctx, template.attributes, "自定义属性字段");
        }

        // ── 整理列表 ──────────────────────────────────────────────────────────────

        private void DrawSortPriorities(IInventoryEditorContext ctx, InventoryTemplate template)
        {
            _ctx = ctx;
            BuildFieldOptions(ctx.Database, out _fieldDisplays, out _fieldValues);

            if (_boundTemplate != template)
            {
                BuildSortList(template);           // 先 Build，会设置 _boundTemplate
                BuildSortTiebreakerList(template); // 共用同一个绑定对象
            }

            _sortList?.DoLayoutList();
        }

        private void DrawSortTiebreakers(IInventoryEditorContext ctx, InventoryTemplate template)
        {
            if (_sortTiebreakerList == null)
                BuildSortTiebreakerList(template);

            _sortTiebreakerList?.DoLayoutList();
        }

        private void BuildSortList(InventoryTemplate template)
        {
            _boundTemplate = template;
            _sortList = new ReorderableList(
                template.sortPriorities, typeof(SortPriority),
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _sortList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "整理列表（玩家在 UI 中通过下拉菜单选择排序条件）");

            _sortList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= template.sortPriorities.Count) return;
                var sp = template.sortPriorities[index];
                rect.y += 2;

                float ascW    = 58f;
                var fieldRect = new Rect(rect.x, rect.y, rect.width - ascW - 4,
                    EditorGUIUtility.singleLineHeight);
                var ascRect   = new Rect(rect.xMax - ascW, rect.y, ascW,
                    EditorGUIUtility.singleLineHeight);

                var displays = _fieldDisplays ?? new[] { "道具 ID" };
                var values   = _fieldValues   ?? new[] { "__id__" };
                int curIdx   = FindOptionIndex(values, sp.field);

                EditorGUI.BeginChangeCheck();
                int  picked = EditorGUI.Popup(fieldRect, curIdx, displays);
                bool newAsc = EditorGUI.Toggle(ascRect,
                    new GUIContent(sp.ascending ? "升序" : "降序"), sp.ascending);
                if (EditorGUI.EndChangeCheck())
                {
                    _ctx.RecordUndo("修改模板整理列表");
                    sp.field     = values[picked];
                    sp.ascending = newAsc;
                    _ctx.MarkDirty();
                }
            };

            _sortList.onAddCallback = _ =>
            {
                _ctx.RecordUndo("添加模板整理列表项");
                template.sortPriorities.Add(new SortPriority("__id__"));
                _ctx.MarkDirty();
            };

            _sortList.onRemoveCallback = list =>
            {
                _ctx.RecordUndo("删除模板整理列表项");
                template.sortPriorities.RemoveAt(list.index);
                _ctx.MarkDirty();
            };

            _sortList.onReorderCallback = _ =>
            {
                _ctx.RecordUndo("调整模板整理列表顺序");
                _ctx.MarkDirty();
            };
        }

        private void BuildSortTiebreakerList(InventoryTemplate template)
        {
            _sortTiebreakerList = new ReorderableList(
                template.sortTiebreakers, typeof(SortPriority),
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _sortTiebreakerList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "整理优先级（整理列表条件值相同时，依次对比此列表直至值不同）");

            _sortTiebreakerList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= template.sortTiebreakers.Count) return;
                var sp  = template.sortTiebreakers[index];
                rect.y += 2;

                float ascW    = 58f;
                var fieldRect = new Rect(rect.x, rect.y, rect.width - ascW - 4,
                    EditorGUIUtility.singleLineHeight);
                var ascRect   = new Rect(rect.xMax - ascW, rect.y, ascW,
                    EditorGUIUtility.singleLineHeight);

                var displays = _fieldDisplays ?? new[] { "道具 ID" };
                var values   = _fieldValues   ?? new[] { "__id__" };
                int curIdx   = FindOptionIndex(values, sp.field);

                EditorGUI.BeginChangeCheck();
                int  picked = EditorGUI.Popup(fieldRect, curIdx, displays);
                bool newAsc = EditorGUI.Toggle(ascRect,
                    new GUIContent(sp.ascending ? "升序" : "降序"), sp.ascending);
                if (EditorGUI.EndChangeCheck())
                {
                    _ctx.RecordUndo("修改模板整理优先级");
                    sp.field     = values[picked];
                    sp.ascending = newAsc;
                    _ctx.MarkDirty();
                }
            };

            _sortTiebreakerList.onAddCallback = _ =>
            {
                _ctx.RecordUndo("添加模板整理优先级项");
                template.sortTiebreakers.Add(new SortPriority("__id__"));
                _ctx.MarkDirty();
            };

            _sortTiebreakerList.onRemoveCallback = list =>
            {
                _ctx.RecordUndo("删除模板整理优先级项");
                template.sortTiebreakers.RemoveAt(list.index);
                _ctx.MarkDirty();
            };

            _sortTiebreakerList.onReorderCallback = _ =>
            {
                _ctx.RecordUndo("调整模板整理优先级顺序");
                _ctx.MarkDirty();
            };
        }

        /// <summary>
        /// 构建整理条件选项。<br/>
        /// displays 为下拉菜单显示文本（含分组前缀），values 为对应存储值：<br/>
        ///   "__id__"       = 道具 ID；<br/>
        ///   属性 ID 字符串 = 按该属性值排序；<br/>
        ///   "__tagOrder__" = 按道具在功能标签列表中的顺序排序。
        /// </summary>
        private static void BuildFieldOptions(InventoryDatabase db,
            out string[] displays, out string[] values)
        {
            var dList = new List<string> { "道具 ID" };
            var vList = new List<string> { "__id__" };
            var seen  = new HashSet<string>();

            foreach (var tmpl in db.ItemTemplates)
                foreach (var def in tmpl.attributes)
                    if (!string.IsNullOrEmpty(def.id) && seen.Add(def.id))
                    { dList.Add("属性/" + def.id); vList.Add(def.id); }

            foreach (var tag in db.FunctionTags)
                foreach (var def in tag.attributes)
                    if (!string.IsNullOrEmpty(def.id) && seen.Add(def.id))
                    { dList.Add("属性/" + def.id); vList.Add(def.id); }

            // 单一「功能标签」选项：运行时按功能标签列表的全局顺序对道具排序
            if (db.FunctionTags.Count > 0)
            { dList.Add("功能标签"); vList.Add("__tagOrder__"); }

            displays = dList.ToArray();
            values   = vList.ToArray();
        }

        private static int FindOptionIndex(string[] values, string field)
        {
            if (string.IsNullOrEmpty(field) || field == "__id__") return 0;
            for (int i = 1; i < values.Length; i++)
                if (values[i] == field) return i;
            return 0;
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

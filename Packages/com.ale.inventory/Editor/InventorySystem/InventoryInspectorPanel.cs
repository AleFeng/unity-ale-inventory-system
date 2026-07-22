using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;
    /// <summary>
    /// 仓库 Inspector（右侧列）：编辑 ID（重复检查高亮）、来源模板、容量、
    /// 三类功能标签限制、整理设置（含 ReorderableList 优先级）、自定义属性值。
    /// </summary>
    public class InventoryInspectorPanel
    {
        private GUIStyle _warnStyle;
        private GUIStyle WarnStyle => _warnStyle ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            { normal = { textColor = new Color(0.95f, 0.8f, 0.2f) } };

        private ReorderableList         _sortList;
        private ReorderableList         _tiebreakerList;
        private Inventory               _boundInventory;
        private IInventoryEditorContext _ctx;
        private string[]                _fieldDisplays;   // Popup 显示文本
        private string[]                _fieldValues;     // 对应存储值

        public void DrawInspector(IInventoryEditorContext ctx, Inventory inventory)
        {
            if (inventory == null)
            {
                EditorGUILayout.LabelField("请在中间列表选中一个仓库。");
                return;
            }

            _ctx = ctx;
            var db = ctx.Database;

            // ── 1. 基础属性 ───────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("基础属性", InventoryEditorStyles.Header);

            bool isDup = ctx.InventoryDuplicateIds.Contains(
                string.IsNullOrWhiteSpace(inventory.id) ? string.Empty : inventory.id);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID");
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField(
                inventory.id, isDup ? InventoryEditorStyles.RedField : EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改仓库 ID");
                inventory.id = newId;
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            if (isDup)
                EditorGUILayout.LabelField("⚠ ID 重复或为空", InventoryEditorStyles.StatusError);

            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器；名称为空时 UI 退回使用 ID）
            AttributeFieldDrawer.Draw(ctx, "名称", inventory.displayNameText, null);
            AttributeFieldDrawer.Draw(ctx, "描述", inventory.descriptionText, null);

            // 来源模板（只读，创建后不可更改）
            using (new EditorGUI.DisabledScope(true))
            {
                string tmplDisplay = string.IsNullOrEmpty(inventory.templateRef)
                    ? "（无）" : inventory.templateRef;
                EditorGUILayout.TextField("来源模板", tmplDisplay);
            }

            // 容量
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newCap = EditorGUILayout.IntField("容量上限", inventory.capacity);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改仓库容量");
                inventory.capacity = Mathf.Max(0, newCap);
                ctx.MarkDirty();
            }
            EditorGUILayout.LabelField("（0 = 无上限）", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            // 重量上限
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            float newWeightLimit = EditorGUILayout.FloatField("重量上限", inventory.weightLimit);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改仓库重量上限");
                inventory.weightLimit = Mathf.Max(0f, newWeightLimit);
                ctx.MarkDirty();
            }
            EditorGUILayout.LabelField("（0 = 无上限）", EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ── 2. 三类功能标签 ───────────────────────────────────────────────────────
            DrawTagRefList(ctx, inventory.allowPutTagRefs,     "放入功能标签");
            EditorGUILayout.Space(4);
            DrawTagRefList(ctx, inventory.allowTakeTagRefs,    "取出功能标签");
            EditorGUILayout.Space(4);
            DrawTagRefList(ctx, inventory.allowOperateTagRefs, "操作功能标签");

            EditorGUILayout.Space(6);

            // ── 3. 过滤设置 ───────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("过滤设置", InventoryEditorStyles.Header);
            EditorGUILayout.LabelField("过滤列表（UI 中以标签按钮形式显示）：",
                EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            bool newShowAll = EditorGUILayout.ToggleLeft(
                new GUIContent("全部", "勾选后 UI 过滤页签栏会显示「全部」页签（默认选中、不过滤）；" +
                                       "取消后不显示「全部」，默认选中第一个过滤标签。"),
                inventory.showAllFilterTab);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改显示全部页签");
                inventory.showAllFilterTab = newShowAll;
                ctx.MarkDirty();
            }

            DrawFilterTagList(ctx, inventory.filterTagRefs);

            EditorGUILayout.Space(6);

            // ── 4. 整理设置 ───────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("整理设置", InventoryEditorStyles.Header);

            EditorGUI.BeginChangeCheck();
            bool newDragSort = EditorGUILayout.Toggle("允许拖拽整理", inventory.dragSort);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改拖拽整理");
                inventory.dragSort = newDragSort;
                ctx.MarkDirty();
            }
            
            EditorGUI.BeginChangeCheck();
            bool newAutoSort = EditorGUILayout.Toggle("自动整理", inventory.autoSort);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改自动整理");
                inventory.autoSort = newAutoSort;
                ctx.MarkDirty();
            }

            DrawSortPriorities(ctx, inventory, db);
            DrawSortTiebreakers(ctx, inventory);

            EditorGUILayout.Space(6);

            // ── 5. UI 配置 ────────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("UI 配置", InventoryEditorStyles.Header);
            NumberFormatConfigDrawer.DrawRefPopup(ctx, "数字格式",
                inventory.numberFormatRef, v => inventory.numberFormatRef = v);

            EditorGUILayout.Space(6);

            // ── 6. 自定义属性值（来自模板）────────────────────────────────────────────
            EditorGUILayout.LabelField("自定义属性", InventoryEditorStyles.Header);

            if (inventory.values.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "⚠  该仓库暂无自定义属性字段。请先在左侧「仓库模板」中添加属性字段，" +
                    "再为仓库选择对应模板。", WarnStyle);
            }
            else
            {
                var template = db.GetInventoryTemplate(inventory.templateRef);
                foreach (var entry in inventory.values)
                {
                    AttributeDefinition def = null;
                    if (template != null)
                        foreach (var d in template.attributes)
                            if (d.id == entry.id) { def = d; break; }

                    var enumType = def != null && def.type == EFieldType.Enum
                        ? db.GetEnumType(def.enumTypeRef) : null;
                    AttributeFieldDrawer.Draw(ctx, entry.id, entry.value, enumType);
                }
            }
        }

        private static void DrawFilterTagList(IInventoryEditorContext ctx, List<string> filterTagRefs)
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
                    ctx.RecordUndo(now ? "添加过滤标签" : "移除过滤标签");
                    if (now) filterTagRefs.Add(tag.name);
                    else     filterTagRefs.Remove(tag.name);
                    ctx.MarkDirty();
                }
            }
        }

        private static void DrawTagRefList(IInventoryEditorContext ctx,
            List<string> tagRefs, string labelText)
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
                    ctx.RecordUndo(now ? $"添加{labelText}" : $"移除{labelText}");
                    if (now) tagRefs.Add(tag.name);
                    else     tagRefs.Remove(tag.name);
                    ctx.MarkDirty();
                }
            }
        }

        private void DrawSortPriorities(IInventoryEditorContext ctx,
            Inventory inventory, InventoryDatabase db)
        {
            _ctx = ctx;
            BuildFieldOptions(db, out _fieldDisplays, out _fieldValues);

            if (_boundInventory != inventory)
            {
                BuildSortList(inventory);       // 先 Build，会设置 _boundInventory
                BuildTiebreakerList(inventory); // 共用同一个绑定对象
            }

            _sortList?.DoLayoutList();
        }

        private void DrawSortTiebreakers(IInventoryEditorContext ctx, Inventory inventory)
        {
            // _fieldDisplays / _fieldValues 已由 DrawSortPriorities 填充，直接复用
            if (_tiebreakerList == null)
                BuildTiebreakerList(inventory);

            _tiebreakerList?.DoLayoutList();
        }

        private void BuildSortList(Inventory inventory)
        {
            _boundInventory = inventory;
            _sortList = new ReorderableList(
                inventory.sortPriorities, typeof(SortPriority),
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _sortList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "整理列表（玩家在 UI 中通过下拉菜单选择排序条件）");

            _sortList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= inventory.sortPriorities.Count) return;
                var sp  = inventory.sortPriorities[index];
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
                    _ctx.RecordUndo("修改整理列表");
                    sp.field     = values[picked];
                    sp.ascending = newAsc;
                    _ctx.MarkDirty();
                }
            };

            _sortList.onAddCallback = _ =>
            {
                _ctx.RecordUndo("添加整理列表项");
                inventory.sortPriorities.Add(new SortPriority("__id__"));
                _ctx.MarkDirty();
            };

            _sortList.onRemoveCallback = list =>
            {
                _ctx.RecordUndo("删除整理列表项");
                inventory.sortPriorities.RemoveAt(list.index);
                _ctx.MarkDirty();
            };

            _sortList.onReorderCallback = _ =>
            {
                _ctx.RecordUndo("调整整理列表顺序");
                _ctx.MarkDirty();
            };
        }

        private void BuildTiebreakerList(Inventory inventory)
        {
            _tiebreakerList = new ReorderableList(
                inventory.sortTiebreakers, typeof(SortPriority),
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _tiebreakerList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "整理优先级（整理列表条件值相同时，依次对比此列表直至值不同）");

            _tiebreakerList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= inventory.sortTiebreakers.Count) return;
                var sp  = inventory.sortTiebreakers[index];
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
                    _ctx.RecordUndo("修改整理优先级");
                    sp.field     = values[picked];
                    sp.ascending = newAsc;
                    _ctx.MarkDirty();
                }
            };

            _tiebreakerList.onAddCallback = _ =>
            {
                _ctx.RecordUndo("添加整理优先级项");
                inventory.sortTiebreakers.Add(new SortPriority("__id__"));
                _ctx.MarkDirty();
            };

            _tiebreakerList.onRemoveCallback = list =>
            {
                _ctx.RecordUndo("删除整理优先级项");
                inventory.sortTiebreakers.RemoveAt(list.index);
                _ctx.MarkDirty();
            };

            _tiebreakerList.onReorderCallback = _ =>
            {
                _ctx.RecordUndo("调整整理优先级顺序");
                _ctx.MarkDirty();
            };
        }

        /// <summary>
        /// 构建整理条件选项。<br/>
        /// displays 为下拉菜单显示文本（含分组前缀），values 为对应存储值：<br/>
        ///   "__id__"        = 道具 ID；<br/>
        ///   属性 ID 字符串  = 按该属性值排序；<br/>
        ///   "__tagOrder__"  = 按道具在功能标签列表中的顺序排序。
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
    }
}

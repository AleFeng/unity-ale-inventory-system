using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;
    /// <summary>
    /// 仓库 Inspector（右侧列）：编辑 ID（重复检查高亮）、来源模板、容量、
    /// 三类功能标签限制、整理设置（委托 <see cref="SortSettingsDrawer"/>）、自定义属性值。
    /// </summary>
    public class InventoryInspectorPanel
    {
        private GUIStyle _warnStyle;
        private GUIStyle WarnStyle => _warnStyle ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            { normal = { textColor = new Color(0.95f, 0.8f, 0.2f) } };

        public void DrawInspector(IInventoryEditorContext ctx, Inventory inventory)
        {
            if (inventory == null)
            {
                EditorGUILayout.LabelField("请在中间列表选中一个仓库。");
                return;
            }

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

            SortSettingsDrawer.Draw(ctx, inventory.sortPriorities, inventory.sortTiebreakers);

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

    }
}

using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 蓝图 Inspector（右侧列）：身份（ID / 名称 / 本地化 / 描述 / 来源模板）、分组标签（主 + 副）、
    /// 产出道具列表、消耗道具列表、共享可配置项（<see cref="CraftingConfigDrawer"/>）、自定义属性值。
    /// 仿 <see cref="ShopInspectorPanel"/>。
    /// </summary>
    public class CraftingInspectorPanel
    {
        private GUIStyle _mainStyle;
        private GUIStyle MainStyle => _mainStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            { normal = { textColor = new Color(0.30f, 0.85f, 0.35f) } };

        // 副分组标签列表的拖拽重排（每帧仅绘制一份蓝图 Inspector，单实例安全）。
        private readonly EditorReorderableDrag _secondaryDrag
            = new EditorReorderableDrag("CraftingSecondaryGroupDrag");

        // 产出 / 消耗道具列表各自的拖拽重排（两列表同帧绘制，需各用一个实例）。
        private readonly EditorReorderableDrag _outputDrag
            = new EditorReorderableDrag("CraftingOutputsDrag");
        private readonly EditorReorderableDrag _inputDrag
            = new EditorReorderableDrag("CraftingInputsDrag");

        public void DrawInspector(IInventoryEditorContext ctx, CraftingBlueprint bp)
        {
            if (bp == null)
            {
                EditorGUILayout.LabelField("请在中间列表选中一个蓝图。");
                return;
            }

            DrawBasic(ctx, bp);
            EditorGUILayout.Space(6);
            DrawGroupTags(ctx, bp);
            EditorGUILayout.Space(6);
            DrawItemAmountList(ctx, bp, bp.outputs, "产出道具列表", isOutput: true);
            EditorGUILayout.Space(6);
            DrawItemAmountList(ctx, bp, bp.inputs, "消耗道具列表", isOutput: false);
            EditorGUILayout.Space(6);
            CraftingConfigDrawer.DrawBlueprintConfig(ctx, bp);
            EditorGUILayout.Space(6);
            DrawCustomAttributes(ctx, bp);
        }

        // ── 身份 ──────────────────────────────────────────────────────────────────

        private static void DrawBasic(IInventoryEditorContext ctx, CraftingBlueprint bp)
        {
            EditorGUILayout.LabelField("基础属性", InventoryEditorStyles.Header);

            bool isDup = ctx.CraftingDuplicateIds.Contains(
                string.IsNullOrWhiteSpace(bp.id) ? string.Empty : bp.id);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID");
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField(
                bp.id, isDup ? InventoryEditorStyles.RedField : EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改蓝图 ID");
                bp.id = newId;
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            if (isDup)
                EditorGUILayout.LabelField("⚠ ID 重复或为空", InventoryEditorStyles.StatusError);

            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器）
            AttributeFieldDrawer.Draw(ctx, "名称", bp.displayText, null);
            AttributeFieldDrawer.Draw(ctx, "描述", bp.descriptionText, null);

            using (new EditorGUI.DisabledScope(true))
            {
                string tmplDisplay = string.IsNullOrEmpty(bp.templateRef) ? "（无）" : bp.templateRef;
                EditorGUILayout.TextField("来源模板", tmplDisplay);
            }
        }

        // ── 分组标签 ──────────────────────────────────────────────────────────────

        private void DrawGroupTags(IInventoryEditorContext ctx, CraftingBlueprint bp)
        {
            var db = ctx.Database;
            EditorGUILayout.LabelField("分组标签", InventoryEditorStyles.Header);

            if (db.CraftingGroupTags.Count == 0)
            {
                EditorGUILayout.LabelField("（暂无分组标签；请在左侧「分组标签」中添加）", EditorStyles.miniLabel);
                return;
            }

            // 主分组（单选 Popup；index 0 = 无）
            var ids      = new List<string>();
            var displays = new List<string> { "（无）" };
            foreach (var g in db.CraftingGroupTags)
            {
                ids.Add(g.id);
                displays.Add(g.PlainName());
            }
            int curIdx = 0;
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] == bp.primaryGroupTag) { curIdx = i + 1; break; }

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup("主分组标签", curIdx, displays.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改主分组标签");
                bp.primaryGroupTag = picked <= 0 ? string.Empty : ids[picked - 1];
                ctx.MarkDirty();
            }

            // 副分组（列表 + 「+」下拉添加，已添加的不在下拉中显示）
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("副分组标签", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(24)))
                ShowAddSecondaryGroupMenu(ctx, bp);
            EditorGUILayout.EndHorizontal();

            var secs = bp.secondaryGroupTags;
            if (secs.Count == 0)
            {
                EditorGUILayout.LabelField("（未添加）", EditorStyles.miniLabel);
                return;
            }

            _secondaryDrag.BeginFrame();
            int removeIndex = -1;
            for (int i = 0; i < secs.Count; i++)
            {
                var g = db.GetCraftingGroupTag(secs[i]);
                bool exists = g != null;
                string label = exists ? g.PlainName() : secs[i] + "（已删除）";

                // 左侧预留句柄列，句柄按整行 Rect 垂直居中绘制，与右侧单行内容对齐。
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                _secondaryDrag.RecordRow(i, rowRect);
                GUILayout.Space(EditorReorderableDrag.HandleWidth);
                EditorGUILayout.LabelField(label,
                    exists ? EditorStyles.label : InventoryEditorStyles.StatusError);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();

                var handleRect = new Rect(rowRect.x,
                    rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                    EditorReorderableDrag.HandleWidth, EditorGUIUtility.singleLineHeight);
                _secondaryDrag.DrawHandle(handleRect, i);
            }
            _secondaryDrag.EndFrame(ctx, secs, "调整副分组标签顺序");

            if (removeIndex >= 0)
            {
                ctx.RecordUndo("移除副分组标签");
                secs.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }

        private static void ShowAddSecondaryGroupMenu(IInventoryEditorContext ctx, CraftingBlueprint bp)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            bool any = false;
            foreach (var g in db.CraftingGroupTags)
            {
                if (string.IsNullOrEmpty(g.id) || bp.secondaryGroupTags.Contains(g.id)) continue;
                any = true;
                string id    = g.id;
                string label = g.PlainName();
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    ctx.RecordUndo("添加副分组标签");
                    bp.secondaryGroupTags.Add(id);
                    ctx.MarkDirty();
                    ctx.Repaint();
                });
            }
            if (!any)
                menu.AddDisabledItem(new GUIContent("（无可添加的分组标签）"));
            menu.ShowAsContext();
        }

        // ── 产出 / 消耗道具列表 ──────────────────────────────────────────────────────

        private void DrawItemAmountList(IInventoryEditorContext ctx, CraftingBlueprint bp,
            List<CraftingItemAmount> list, string header, bool isOutput)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, InventoryEditorStyles.Header);
            if (GUILayout.Button("+ 添加", EditorStyles.miniButton, GUILayout.Width(64)))
            {
                ctx.RecordUndo("添加" + header);
                list.Add(new CraftingItemAmount());
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            if (isOutput)
                EditorGUILayout.LabelField("第 1 项为主产出（用于 UI 显示），其余为副产出；拖拽左侧句柄调整顺序。",
                    EditorStyles.miniLabel);

            var drag = isOutput ? _outputDrag : _inputDrag;
            drag.BeginFrame();

            int removeIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var amount = list[i];

                // 左侧拖拽句柄列 + 右侧条目内容（多行块，与商品组拖拽一致）
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                drag.RecordRow(i, rowRect);
                drag.DrawHandleColumn(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (isOutput && i == 0)
                    EditorGUILayout.LabelField("★ 主产出", MainStyle);

                // 道具ID 行：输入框 + 「选择」下拉（同一字段的快捷设置）+ 删除
                bool invalid = !string.IsNullOrEmpty(amount.itemId) && ctx.Database.GetItem(amount.itemId) == null;
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                string newItemId = EditorGUILayout.DelayedTextField(
                    new GUIContent("道具ID", "直接输入道具 ID，回车确认；右侧「选择」可从道具列表快捷选择，写入此处。"),
                    amount.itemId ?? string.Empty,
                    invalid ? InventoryEditorStyles.RedField : EditorStyles.textField);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改道具ID");
                    amount.itemId = newItemId;
                    ctx.MarkDirty();
                }
                Rect dropRect = GUILayoutUtility.GetRect(new GUIContent("选择"), EditorStyles.popup, GUILayout.Width(56));
                if (EditorGUI.DropdownButton(dropRect,
                        new GUIContent("选择", "从道具列表快捷选择，结果写入左侧道具ID。"),
                        FocusType.Keyboard, EditorStyles.popup))
                    ShowItemMenu(ctx, amount, dropRect);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();

                if (invalid)
                    EditorGUILayout.LabelField("⚠ 无效道具 ID（导出将被阻止）", InventoryEditorStyles.StatusError);

                // 数量
                EditorGUI.BeginChangeCheck();
                int newCount = EditorGUILayout.IntField(
                    new GUIContent("数量", isOutput ? "制作一次获得的该道具数量。" : "制作一次需要的该道具数量。"),
                    amount.count);
                if (EditorGUI.EndChangeCheck())
                {
                    ctx.RecordUndo("修改道具数量");
                    amount.count = Mathf.Max(1, newCount);
                    ctx.MarkDirty();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            drag.EndFrame(ctx, list, "调整" + header + "顺序");

            if (removeIndex >= 0)
            {
                ctx.RecordUndo("删除" + header + "条目");
                list.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }

        /// <summary>弹出按「道具模板」分组的道具选择菜单（仿 ShopConfigDrawer.ShowItemMenu）。</summary>
        private static void ShowItemMenu(IInventoryEditorContext ctx, CraftingItemAmount amount, Rect rect)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("（未选择）"), string.IsNullOrEmpty(amount.itemId), () =>
            {
                ctx.RecordUndo("修改道具");
                amount.itemId = string.Empty;
                ctx.MarkDirty();
                ctx.Repaint();
            });
            menu.AddSeparator(string.Empty);

            foreach (var item in db.Items)
            {
                if (string.IsNullOrEmpty(item.id)) continue;
                string group   = string.IsNullOrEmpty(item.templateRef) ? "（无模板）" : item.templateRef;
                string path    = group + "/" + item.id;
                string capture = item.id;
                menu.AddItem(new GUIContent(path), amount.itemId == item.id, () =>
                {
                    ctx.RecordUndo("修改道具");
                    amount.itemId = capture;
                    ctx.MarkDirty();
                    ctx.Repaint();
                });
            }

            menu.DropDown(rect);
        }

        // ── 自定义属性 ──────────────────────────────────────────────────────────────

        private static void DrawCustomAttributes(IInventoryEditorContext ctx, CraftingBlueprint bp)
        {
            var db = ctx.Database;

            EditorGUILayout.LabelField("自定义属性", InventoryEditorStyles.Header);

            if (bp.values.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "（该蓝图暂无自定义属性字段；可在左侧「蓝图模板」中添加）", EditorStyles.miniLabel);
                return;
            }

            var template = db.GetCraftingBlueprintTemplate(bp.templateRef);
            foreach (var entry in bp.values)
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
}

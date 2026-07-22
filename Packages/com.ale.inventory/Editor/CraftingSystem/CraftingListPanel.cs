using System;
using System.Collections.Generic;
using InventorySystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 蓝图列表面板（中间列）：蓝图模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 蓝图行列表。
    /// 每行显示：拖拽句柄、主分组色点、蓝图 ID（粗体，重复红色高亮）、名称、主分组名、产出数、删除按钮。
    /// 左侧拖拽句柄（≡）支持长按拖动调整顺序。仿 <see cref="ShopListPanel"/>。
    /// </summary>
    public class CraftingListPanel
    {
        private const float KeyRowH     = 13f;  // 列名行高
        private const float ValRowH     = 22f;  // 值行高
        private const float DragHandleW = 16f;
        private const float DotW        = 14f;
        private const float IdColW      = 90f;
        private const float NameColW    = 96f;
        private const float DescColW    = 120f;
        private const float GroupColW   = 72f;
        private const float OutColW     = 48f;
        private const float DelBtnW     = 20f;
        private const float Pad         = 4f;

        private Vector2 _scroll;
        private string  _search         = string.Empty;
        private string  _templateFilter = null; // null = "全部"

        private readonly EditorReorderableDrag _drag = new EditorReorderableDrag("CraftingListDrag");

        private GUIStyle _keyStyle;
        private GUIStyle _idStyle;
        private GUIStyle _subStyle;
        private GUIStyle KeyStyle  => _keyStyle  ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.6f, 0.85f, 1.0f) }, wordWrap = false };
        private GUIStyle IdStyle  => _idStyle  ??= new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 11, wordWrap = false, clipping = TextClipping.Clip };
        private GUIStyle SubStyle => _subStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.62f, 0.62f, 0.62f) }, wordWrap = false, clipping = TextClipping.Clip };

        private static CraftingBlueprint _pendingSelect;

        /// <summary>绘制列表，返回当前选中的蓝图引用。</summary>
        public CraftingBlueprint DrawList(IInventoryEditorContext ctx, CraftingBlueprint selectedBlueprint)
        {
            var db = ctx.Database;

            if (_templateFilter != null && db.GetCraftingBlueprintTemplate(_templateFilter) == null)
                _templateFilter = null;

            _templateFilter = EditorFilterTabs.Draw(_templateFilter, db.CraftingBlueprintTemplates, t => t.name);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("从模板添加", EditorStyles.toolbarDropDown, GUILayout.Width(84)))
                ShowAddFromTemplateMenu(ctx);
            using (new EditorGUI.DisabledScope(db.CraftingBlueprints.Count == 0))
            {
                if (GUILayout.Button("快速添加", EditorStyles.toolbarButton, GUILayout.Width(72)))
                    selectedBlueprint = QuickAdd(ctx);
            }
            EditorGUILayout.EndHorizontal();

            _drag.BeginFrame();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int deleteIndex = -1;
            var visible = new List<CraftingBlueprint>();   // 本帧可见（已过滤）条目，供键盘上下键导航

            for (int i = 0; i < db.CraftingBlueprints.Count; i++)
            {
                var bp = db.CraftingBlueprints[i];

                if (_templateFilter != null && bp.templateRef != _templateFilter) continue;
                if (!MatchesSearch(bp, _search)) continue;

                visible.Add(bp);

                bool isDup    = ctx.CraftingDuplicateIds.Contains(
                    string.IsNullOrWhiteSpace(bp.id) ? string.Empty : bp.id);
                bool selected = (bp == selectedBlueprint);

                Rect keyRow   = EditorGUILayout.GetControlRect(false, KeyRowH);
                Rect valRow   = EditorGUILayout.GetControlRect(false, ValRowH);
                Rect fullRect = Rect.MinMaxRect(keyRow.xMin, keyRow.yMin, valRow.xMax, valRow.yMax);

                _drag.RecordRow(i, fullRect);

                if (selected)
                    InventoryEditorStyles.DrawRowBackground(fullRect, InventoryEditorStyles.SelectedColor);
                if (isDup)
                    InventoryEditorStyles.DrawRowBackground(fullRect,
                        new Color(InventoryEditorStyles.ErrorColor.r,
                                  InventoryEditorStyles.ErrorColor.g,
                                  InventoryEditorStyles.ErrorColor.b, 0.25f));
                if (_drag.IsDragSource(i))
                    InventoryEditorStyles.DrawRowBackground(fullRect, EditorReorderableDrag.DragSourceTint);

                var delRect = new Rect(fullRect.xMax - DelBtnW, valRow.y + 2, DelBtnW - 2, ValRowH - 4);
                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    deleteIndex = i;

                var dragRect = new Rect(fullRect.xMin, fullRect.yMin, DragHandleW - 2, fullRect.height);
                _drag.DrawHandle(dragRect, i);

                float cx = fullRect.x + DragHandleW;
                float vy = valRow.y + (ValRowH - EditorGUIUtility.singleLineHeight) * 0.5f;
                float vh = EditorGUIUtility.singleLineHeight;

                var tmplObj  = db.GetCraftingBlueprintTemplate(bp.templateRef);
                var groupObj = db.GetCraftingGroupTag(bp.primaryGroupTag);
                Color dotClr = tmplObj != null ? tmplObj.color : Color.gray;
                InventoryEditorStyles.DrawColorDot(
                    new Rect(cx, fullRect.y + (fullRect.height - DotW) * 0.5f, DotW, DotW), dotClr);
                cx += DotW + Pad;

                // ── 上行：列名表头 ──────────────────────────────────────────────────
                {
                    float kx = fullRect.x + DragHandleW + DotW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,    KeyRowH - 2), "ID",    KeyStyle); kx += IdColW    + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, NameColW,  KeyRowH - 2), "名称",  KeyStyle); kx += NameColW  + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, DescColW,  KeyRowH - 2), "描述",  KeyStyle); kx += DescColW  + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, GroupColW, KeyRowH - 2), "主分组", KeyStyle); kx += GroupColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, OutColW,   KeyRowH - 2), "产出",  KeyStyle);
                }

                // ── 下行：值 ────────────────────────────────────────────────────────
                GUI.Label(new Rect(cx, vy, IdColW, vh),
                    string.IsNullOrWhiteSpace(bp.id) ? "(空 ID)" : bp.id, IdStyle);
                cx += IdColW + Pad;

                string bpName = bp.displayText != null ? bp.displayText.GetTextValue(0) : null;
                GUI.Label(new Rect(cx, vy, NameColW, vh),
                    string.IsNullOrEmpty(bpName) ? "—" : bpName, SubStyle);
                cx += NameColW + Pad;

                string bpDesc = bp.descriptionText != null ? bp.descriptionText.GetTextValue(0) : null;
                GUI.Label(new Rect(cx, vy, DescColW, vh),
                    string.IsNullOrEmpty(bpDesc) ? "—" : bpDesc, SubStyle);
                cx += DescColW + Pad;

                string groupName = groupObj != null ? groupObj.PlainName() : "—";
                GUI.Label(new Rect(cx, vy, GroupColW, vh), groupName, SubStyle);
                cx += GroupColW + Pad;

                GUI.Label(new Rect(cx, vy, OutColW, vh), bp.outputs.Count.ToString(), SubStyle);

                if (Event.current.type == EventType.MouseDown
                    && fullRect.Contains(Event.current.mousePosition)
                    && !delRect.Contains(Event.current.mousePosition)
                    && !dragRect.Contains(Event.current.mousePosition))
                {
                    selectedBlueprint = bp;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            _drag.EndFrame(ctx, db.CraftingBlueprints, "调整蓝图顺序", DragHandleW, DelBtnW);

            EditorGUILayout.EndScrollView();
            float viewportHeight = GUILayoutUtility.GetLastRect().height;

            if (deleteIndex >= 0 && deleteIndex < db.CraftingBlueprints.Count)
            {
                var toDelete = db.CraftingBlueprints[deleteIndex];
                if (toDelete == selectedBlueprint) selectedBlueprint = null;
                ctx.RecordUndo("删除蓝图");
                db.CraftingBlueprints.RemoveAt(deleteIndex);
                ctx.MarkDirty();
            }

            // 键盘 上/下 方向键：在可见条目间切换选中，并在越界时自动滚动一行
            float rowPitch = KeyRowH + ValRowH + 2f * EditorGUIUtility.standardVerticalSpacing;
            if (EditorListKeyboardNav.HandleUpDown(visible, selectedBlueprint, out var navBp,
                    ref _scroll, rowPitch, viewportHeight))
            {
                selectedBlueprint = navBp;
                GUI.FocusControl(null);
                ctx.Repaint();
            }

            return selectedBlueprint;
        }

        /// <summary>取出并清空待选中蓝图（由 CraftingSystemTab 在每帧 Layout 前调用）。</summary>
        public CraftingBlueprint ConsumePendingSelect()
        {
            var bp = _pendingSelect;
            _pendingSelect = null;
            return bp;
        }

        private void ShowAddFromTemplateMenu(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            if (db.CraftingBlueprintTemplates.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("（无可用蓝图模板）"));
            }
            else
            {
                foreach (var template in db.CraftingBlueprintTemplates)
                {
                    string name = template.name;
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        var bp = AddFromTemplate(ctx, name);
                        _pendingSelect = bp;
                        ctx.Repaint();
                    });
                }
            }
            menu.ShowAsContext();
        }

        private static CraftingBlueprint AddFromTemplate(IInventoryEditorContext ctx, string templateName)
        {
            var db = ctx.Database;
            ctx.RecordUndo("从模板添加蓝图");
            var bp   = new CraftingBlueprint(GenerateBlueprintId(db), templateName);
            var tmpl = db.GetCraftingBlueprintTemplate(templateName);
            if (tmpl != null)
            {
                // 制作参数（时间 / 连续次数）为蓝图级初始值；制作仓库与 UI 配置为模板级配置，
                // 由 RebuildAttributes 从模板镜像同步，无需在此复制。
                bp.craftTime     = tmpl.craftTime;
                bp.maxCraftCount = tmpl.maxCraftCount;
            }
            bp.RebuildAttributes(db);
            db.CraftingBlueprints.Add(bp);
            ctx.MarkDirty();
            return bp;
        }

        private CraftingBlueprint QuickAdd(IInventoryEditorContext ctx)
        {
            var db = ctx.Database;
            ctx.RecordUndo("快速添加蓝图");
            var bp = db.CraftingBlueprints[db.CraftingBlueprints.Count - 1].Clone();
            bp.id  = GenerateBlueprintId(db);
            db.CraftingBlueprints.Add(bp);
            ctx.MarkDirty();
            return bp;
        }

        private static string GenerateBlueprintId(InventoryDatabase db)
        {
            int n = db.CraftingBlueprints.Count + 1;
            string id;
            do { id = "blueprint_" + n; n++; }
            while (db.GetCraftingBlueprint(id) != null);
            return id;
        }

        private static bool MatchesSearch(CraftingBlueprint bp, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(bp.id) &&
                bp.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string name = bp.displayText != null ? bp.displayText.GetTextValue(0) : null;
            if (!string.IsNullOrEmpty(name) &&
                name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}

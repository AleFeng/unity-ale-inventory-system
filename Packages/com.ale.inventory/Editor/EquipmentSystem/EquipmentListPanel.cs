using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 装备组列表面板（中间列）：装备组模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 装备组行列表。
    /// 每行显示：拖拽句柄、模板色点、装备组 ID（粗体，重复红色高亮）、名称、槽位列表数、删除按钮。
    /// 左侧拖拽句柄（≡）支持长按拖动调整顺序。仿 <see cref="CraftingListPanel"/>。
    /// </summary>
    public class EquipmentListPanel
    {
        private const float KeyRowH     = 13f;  // 列名行高
        private const float ValRowH     = 22f;  // 值行高
        private const float DragHandleW = 16f;
        private const float DotW        = 14f;
        private const float IdColW      = 96f;
        private const float NameColW    = 110f;
        private const float DescColW    = 120f;
        private const float SlotColW    = 56f;
        private const float DelBtnW     = 20f;
        private const float Pad         = 4f;

        private Vector2 _scroll;
        private string  _search         = string.Empty;
        private string  _templateFilter = null; // null = "全部"

        private readonly EditorReorderableDrag _drag = new EditorReorderableDrag("EquipmentListDrag");

        private GUIStyle _keyStyle;
        private GUIStyle _idStyle;
        private GUIStyle _subStyle;
        private GUIStyle KeyStyle  => _keyStyle  ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.6f, 0.85f, 1.0f) }, wordWrap = false };
        private GUIStyle IdStyle  => _idStyle  ??= new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 11, wordWrap = false, clipping = TextClipping.Clip };
        private GUIStyle SubStyle => _subStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.62f, 0.62f, 0.62f) }, wordWrap = false, clipping = TextClipping.Clip };

        private static EquipmentGroup _pendingSelect;

        /// <summary>绘制列表，返回当前选中的装备组引用。</summary>
        public EquipmentGroup DrawList(IInventoryEditorContext ctx, EquipmentGroup selectedGroup)
        {
            var db = ctx.Database;

            if (_templateFilter != null && db.GetEquipmentGroupTemplate(_templateFilter) == null)
                _templateFilter = null;

            _templateFilter = EditorFilterTabs.Draw(_templateFilter, db.EquipmentGroupTemplates, t => t.name);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("从模板添加", EditorStyles.toolbarDropDown, GUILayout.Width(84)))
                ShowAddFromTemplateMenu(ctx);
            using (new EditorGUI.DisabledScope(db.EquipmentGroups.Count == 0))
            {
                if (GUILayout.Button("快速添加", EditorStyles.toolbarButton, GUILayout.Width(72)))
                    selectedGroup = QuickAdd(ctx);
            }
            EditorGUILayout.EndHorizontal();

            _drag.BeginFrame();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int deleteIndex = -1;
            var visible = new List<EquipmentGroup>();   // 本帧可见（已过滤）条目，供键盘上下键导航

            for (int i = 0; i < db.EquipmentGroups.Count; i++)
            {
                var g = db.EquipmentGroups[i];

                if (_templateFilter != null && g.templateRef != _templateFilter) continue;
                if (!MatchesSearch(g, _search)) continue;

                visible.Add(g);

                bool isDup    = ctx.EquipmentDuplicateIds.Contains(
                    string.IsNullOrWhiteSpace(g.id) ? string.Empty : g.id);
                bool selected = (g == selectedGroup);

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

                var tmplObj  = db.GetEquipmentGroupTemplate(g.templateRef);
                Color dotClr = tmplObj != null ? tmplObj.color : Color.gray;
                InventoryEditorStyles.DrawColorDot(
                    new Rect(cx, fullRect.y + (fullRect.height - DotW) * 0.5f, DotW, DotW), dotClr);
                cx += DotW + Pad;

                float vy = valRow.y + (ValRowH - EditorGUIUtility.singleLineHeight) * 0.5f;
                float vh = EditorGUIUtility.singleLineHeight;

                // ── 上行：列名表头 ──────────────────────────────────────────────────
                {
                    float kx = fullRect.x + DragHandleW + DotW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,   KeyRowH - 2), "ID",   KeyStyle); kx += IdColW   + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, NameColW, KeyRowH - 2), "名称", KeyStyle); kx += NameColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, DescColW, KeyRowH - 2), "描述", KeyStyle); kx += DescColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, SlotColW, KeyRowH - 2), "槽组", KeyStyle);
                }

                // ── 下行：值 ────────────────────────────────────────────────────────
                GUI.Label(new Rect(cx, vy, IdColW, vh),
                    string.IsNullOrWhiteSpace(g.id) ? "(空 ID)" : g.id, IdStyle);
                cx += IdColW + Pad;

                string gName = g.displayNameText != null ? g.displayNameText.GetTextValue(0) : null;
                GUI.Label(new Rect(cx, vy, NameColW, vh),
                    string.IsNullOrEmpty(gName) ? "—" : gName, SubStyle);
                cx += NameColW + Pad;

                string gDesc = g.descriptionText != null ? g.descriptionText.GetTextValue(0) : null;
                GUI.Label(new Rect(cx, vy, DescColW, vh),
                    string.IsNullOrEmpty(gDesc) ? "—" : gDesc, SubStyle);
                cx += DescColW + Pad;

                GUI.Label(new Rect(cx, vy, SlotColW, vh), g.slotLists.Count.ToString(), SubStyle);

                if (Event.current.type == EventType.MouseDown
                    && fullRect.Contains(Event.current.mousePosition)
                    && !delRect.Contains(Event.current.mousePosition)
                    && !dragRect.Contains(Event.current.mousePosition))
                {
                    selectedGroup = g;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            _drag.EndFrame(ctx, db.EquipmentGroups, "调整装备组顺序", DragHandleW, DelBtnW);

            EditorGUILayout.EndScrollView();
            float viewportHeight = GUILayoutUtility.GetLastRect().height;

            if (deleteIndex >= 0 && deleteIndex < db.EquipmentGroups.Count)
            {
                var toDelete = db.EquipmentGroups[deleteIndex];
                if (toDelete == selectedGroup) selectedGroup = null;
                ctx.RecordUndo("删除装备组");
                db.EquipmentGroups.RemoveAt(deleteIndex);
                ctx.MarkDirty();
            }

            // 键盘 上/下 方向键：在可见条目间切换选中，并在越界时自动滚动一行
            float rowPitch = KeyRowH + ValRowH + 2f * EditorGUIUtility.standardVerticalSpacing;
            if (EditorListKeyboardNav.HandleUpDown(visible, selectedGroup, out var navGroup,
                    ref _scroll, rowPitch, viewportHeight))
            {
                selectedGroup = navGroup;
                GUI.FocusControl(null);
                ctx.Repaint();
            }

            return selectedGroup;
        }

        /// <summary>取出并清空待选中装备组（由 EquipmentSystemTab 在每帧 Layout 前调用）。</summary>
        public EquipmentGroup ConsumePendingSelect()
        {
            var g = _pendingSelect;
            _pendingSelect = null;
            return g;
        }

        private void ShowAddFromTemplateMenu(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            if (db.EquipmentGroupTemplates.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("（无可用装备组模板）"));
            }
            else
            {
                foreach (var template in db.EquipmentGroupTemplates)
                {
                    string name = template.name;
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        var g = AddFromTemplate(ctx, name);
                        _pendingSelect = g;
                        ctx.Repaint();
                    });
                }
            }
            menu.ShowAsContext();
        }

        private static EquipmentGroup AddFromTemplate(IInventoryEditorContext ctx, string templateName)
        {
            var db = ctx.Database;
            ctx.RecordUndo("从模板添加装备组");
            var g    = new EquipmentGroup(GenerateGroupId(db), templateName);
            var tmpl = db.GetEquipmentGroupTemplate(templateName);
            if (tmpl != null)
            {
                // 从模板深拷贝全部可配置项（装备仓库 + 槽位列表 + 装备属性字段列表）作为初始数据；之后装备组可独立编辑。
                g.equipmentInventoryRefs = new List<string>(tmpl.equipmentInventoryRefs);
                foreach (var sl in tmpl.slotLists)         g.slotLists.Add(sl.Clone());
                foreach (var ad in tmpl.attributeDisplays) g.attributeDisplays.Add(ad.Clone());
                foreach (var sp in tmpl.sortPriorities)    g.sortPriorities.Add(sp.Clone());
                foreach (var sp in tmpl.sortTiebreakers)   g.sortTiebreakers.Add(sp.Clone());
            }
            // 自定义属性值按模板属性字段定义协调默认值。
            g.RebuildAttributes(db);
            db.EquipmentGroups.Add(g);
            ctx.MarkDirty();
            return g;
        }

        private EquipmentGroup QuickAdd(IInventoryEditorContext ctx)
        {
            var db = ctx.Database;
            ctx.RecordUndo("快速添加装备组");
            var g = db.EquipmentGroups[db.EquipmentGroups.Count - 1].Clone();
            g.id  = GenerateGroupId(db);
            db.EquipmentGroups.Add(g);
            ctx.MarkDirty();
            return g;
        }

        private static string GenerateGroupId(InventoryDatabase db)
        {
            int n = db.EquipmentGroups.Count + 1;
            string id;
            do { id = "equipment_" + n; n++; }
            while (db.GetEquipmentGroup(id) != null);
            return id;
        }

        private static bool MatchesSearch(EquipmentGroup g, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(g.id) &&
                g.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string gName = g.displayNameText != null ? g.displayNameText.GetTextValue(0) : null;
            if (!string.IsNullOrEmpty(gName) &&
                gName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}

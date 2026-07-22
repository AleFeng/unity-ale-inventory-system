using InventorySystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 仓库列表面板（中间列）：仓库模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 仓库行列表。
    /// 每行显示：仓库 ID（粗体）、名称、描述、来源模板名（灰色）、容量。
    /// 左侧拖拽句柄（≡）支持长按拖动来调整条目顺序。
    /// </summary>
    public class InventoryListPanel
    {
        private const float KeyRowH     = 13f;
        private const float ValRowH     = 22f;
        private const float DragHandleW = 16f;  // 拖拽句柄宽度
        private const float DotW        = 14f;
        private const float TmplColW    = 80f;
        private const float IdColW      = 76f;
        private const float NameColW    = 96f;
        private const float DescColW    = 120f;
        private const float CapColW     = 48f;
        private const float WtColW      = 48f;
        private const float TagColW     = 64f;
        private const float DelBtnW     = 20f;
        private const float Pad         = 4f;

        private Vector2 _scroll;
        private string  _search         = string.Empty;
        private string  _templateFilter = null; // null = "全部"

        // ── 拖拽重排（复用统一工具类）────────────────────────────────────────────────
        private readonly EditorReorderableDrag _drag = new EditorReorderableDrag("InventoryListDrag");

        // ── 样式缓存 ────────────────────────────────────────────────────────────────
        private GUIStyle _keyStyle;
        private GUIStyle _idStyle;
        private GUIStyle _subStyle;
        private GUIStyle _tmplStyle;

        private GUIStyle KeyStyle  => _keyStyle  ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.6f, 0.85f, 1.0f) }, wordWrap = false };
        private GUIStyle IdStyle   => _idStyle   ??= new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 11, wordWrap = false, clipping = TextClipping.Clip };
        private GUIStyle SubStyle  => _subStyle  ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }, wordWrap = false, clipping = TextClipping.Clip };
        private GUIStyle TmplStyle => _tmplStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal    = { textColor = new Color(0.65f, 0.65f, 0.65f) },
            wordWrap  = false,
            clipping  = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
        };

        private static Inventory _pendingSelect;

        /// <summary>绘制列表，返回当前选中的 Inventory 引用。</summary>
        public Inventory DrawList(IInventoryEditorContext ctx, Inventory selectedInventory)
        {
            var db = ctx.Database;

            // 校验过滤标签（模板被删除或改名时自动重置为"全部"）
            if (_templateFilter != null && db.GetInventoryTemplate(_templateFilter) == null)
                _templateFilter = null;

            _templateFilter = EditorFilterTabs.Draw(_templateFilter, db.InventoryTemplates, t => t.name);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("从模板添加", EditorStyles.toolbarDropDown, GUILayout.Width(84)))
                ShowAddFromTemplateMenu(ctx);
            using (new EditorGUI.DisabledScope(db.Inventories.Count == 0))
            {
                if (GUILayout.Button("快速添加", EditorStyles.toolbarButton, GUILayout.Width(72)))
                    selectedInventory = QuickAdd(ctx);
            }
            EditorGUILayout.EndHorizontal();

            _drag.BeginFrame();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int deleteIndex = -1;
            var visible = new System.Collections.Generic.List<Inventory>();   // 本帧可见（已过滤）条目，供键盘上下键导航

            for (int i = 0; i < db.Inventories.Count; i++)
            {
                var inv = db.Inventories[i];

                // 仓库模板过滤
                if (_templateFilter != null && inv.templateRef != _templateFilter) continue;
                if (!MatchesSearch(inv, _search)) continue;

                visible.Add(inv);

                bool isDup    = ctx.InventoryDuplicateIds.Contains(
                    string.IsNullOrWhiteSpace(inv.id) ? string.Empty : inv.id);
                bool selected = (inv == selectedInventory);

                // ── 分配两行 ──────────────────────────────────────────────────────
                Rect keyRow   = EditorGUILayout.GetControlRect(false, KeyRowH);
                Rect valRow   = EditorGUILayout.GetControlRect(false, ValRowH);
                Rect fullRect = Rect.MinMaxRect(keyRow.xMin, keyRow.yMin, valRow.xMax, valRow.yMax);

                _drag.RecordRow(i, fullRect);

                // ── 行背景 ──────────────────────────────────────────────────────────
                if (selected)
                    InventoryEditorStyles.DrawRowBackground(fullRect, InventoryEditorStyles.SelectedColor);
                if (isDup)
                    InventoryEditorStyles.DrawRowBackground(fullRect,
                        new Color(InventoryEditorStyles.ErrorColor.r,
                                  InventoryEditorStyles.ErrorColor.g,
                                  InventoryEditorStyles.ErrorColor.b, 0.25f));
                if (_drag.IsDragSource(i))
                    InventoryEditorStyles.DrawRowBackground(fullRect, EditorReorderableDrag.DragSourceTint);

                // ── 删除按钮 ──────────────────────────────────────────────────────
                var delRect = new Rect(fullRect.xMax - DelBtnW, valRow.y + 2, DelBtnW - 2, ValRowH - 4);
                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    deleteIndex = i;

                // ── 拖拽句柄 ──────────────────────────────────────────────────────
                var dragRect = new Rect(fullRect.xMin, fullRect.yMin, DragHandleW - 2, fullRect.height);
                _drag.DrawHandle(dragRect, i);

                // ── 上行：列名 ────────────────────────────────────────────────────
                {
                    float kx = fullRect.x + DragHandleW + DotW + Pad + TmplColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,  KeyRowH - 2), "ID",     KeyStyle); kx += IdColW  + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, NameColW, KeyRowH - 2), "名称",   KeyStyle); kx += NameColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, DescColW, KeyRowH - 2), "描述",   KeyStyle); kx += DescColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, CapColW, KeyRowH - 2), "容量上限", KeyStyle); kx += CapColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, WtColW,  KeyRowH - 2), "重量上限", KeyStyle); kx += WtColW  + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, TagColW, KeyRowH - 2), "放入",    KeyStyle); kx += TagColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, TagColW, KeyRowH - 2), "取出",    KeyStyle); kx += TagColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, TagColW, KeyRowH - 2), "操作",    KeyStyle);
                }

                // ── 下行：● 色点 + 模板名 + ID + 容量 + 重量 + 标签 ─────────────────
                float cx = fullRect.x + DragHandleW;

                var tmplObj  = db.GetInventoryTemplate(inv.templateRef);
                Color dotClr = tmplObj != null ? tmplObj.color : Color.gray;
                InventoryEditorStyles.DrawColorDot(
                    new Rect(cx, fullRect.y + (fullRect.height - DotW) * 0.5f, DotW, DotW), dotClr);
                cx += DotW + Pad;

                string tmplText = string.IsNullOrEmpty(inv.templateRef) ? "—" : inv.templateRef;
                GUI.Label(new Rect(cx, fullRect.y, TmplColW, fullRect.height), tmplText, TmplStyle);
                cx += TmplColW + Pad;

                float vy = valRow.y + 3;
                float vh = ValRowH - 6;

                GUI.Label(new Rect(cx, vy, IdColW, vh),
                    string.IsNullOrWhiteSpace(inv.id) ? "(空 ID)" : inv.id, IdStyle);
                cx += IdColW + Pad;

                string invName = inv.displayNameText != null ? inv.displayNameText.GetTextValue(0) : null;
                GUI.Label(new Rect(cx, vy, NameColW, vh),
                    string.IsNullOrEmpty(invName) ? "—" : invName, SubStyle);
                cx += NameColW + Pad;

                string invDesc = inv.descriptionText != null ? inv.descriptionText.GetTextValue(0) : null;
                GUI.Label(new Rect(cx, vy, DescColW, vh),
                    string.IsNullOrEmpty(invDesc) ? "—" : invDesc, SubStyle);
                cx += DescColW + Pad;

                GUI.Label(new Rect(cx, vy, CapColW, vh),
                    inv.capacity <= 0 ? "∞" : inv.capacity.ToString(), SubStyle);
                cx += CapColW + Pad;

                GUI.Label(new Rect(cx, vy, WtColW, vh),
                    inv.weightLimit <= 0f ? "∞" : inv.weightLimit.ToString("F1"), SubStyle);
                cx += WtColW + Pad;

                GUI.Label(new Rect(cx, vy, TagColW, vh), TagsToString(inv.allowPutTagRefs),     SubStyle); cx += TagColW + Pad;
                GUI.Label(new Rect(cx, vy, TagColW, vh), TagsToString(inv.allowTakeTagRefs),    SubStyle); cx += TagColW + Pad;
                GUI.Label(new Rect(cx, vy, TagColW, vh), TagsToString(inv.allowOperateTagRefs), SubStyle);

                // ── 行点击选中（排除句柄和删除按钮）──────────────────────────────────
                if (Event.current.type == EventType.MouseDown
                    && fullRect.Contains(Event.current.mousePosition)
                    && !delRect.Contains(Event.current.mousePosition)
                    && !dragRect.Contains(Event.current.mousePosition))
                {
                    selectedInventory = inv;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            // ── 拖拽落点处理 + 插入指示线（复用统一工具类，坐标系与行 Rect 一致）──────
            _drag.EndFrame(ctx, db.Inventories, "调整仓库顺序", DragHandleW, DelBtnW);

            EditorGUILayout.EndScrollView();
            float viewportHeight = GUILayoutUtility.GetLastRect().height;

            if (deleteIndex >= 0 && deleteIndex < db.Inventories.Count)
            {
                var toDelete = db.Inventories[deleteIndex];
                if (toDelete == selectedInventory) selectedInventory = null;
                ctx.RecordUndo("删除仓库");
                db.Inventories.RemoveAt(deleteIndex);
                ctx.MarkDirty();
            }

            // 键盘 上/下 方向键：在可见条目间切换选中，并在越界时自动滚动一行
            float rowPitch = KeyRowH + ValRowH + 2f * EditorGUIUtility.standardVerticalSpacing;
            if (EditorListKeyboardNav.HandleUpDown(visible, selectedInventory, out var navInv,
                    ref _scroll, rowPitch, viewportHeight))
            {
                selectedInventory = navInv;
                GUI.FocusControl(null);
                ctx.Repaint();
            }

            return selectedInventory;
        }

        /// <summary>取出并清空待选中仓库（由 InventorySystemTab 在每帧 Layout 前调用）。</summary>
        public Inventory ConsumePendingSelect()
        {
            var inv = _pendingSelect;
            _pendingSelect = null;
            return inv;
        }

        private void ShowAddFromTemplateMenu(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            if (db.InventoryTemplates.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("（无可用仓库模板）"));
            }
            else
            {
                foreach (var template in db.InventoryTemplates)
                {
                    string name = template.name;
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        var inv = AddFromTemplate(ctx, name);
                        _pendingSelect = inv;
                        ctx.Repaint();
                    });
                }
            }
            menu.ShowAsContext();
        }

        private static Inventory AddFromTemplate(IInventoryEditorContext ctx, string templateName)
        {
            var db   = ctx.Database;
            ctx.RecordUndo("从模板添加仓库");
            var inv  = new Inventory(GenerateInventoryId(db), templateName);
            var tmpl = db.GetInventoryTemplate(templateName);
            if (tmpl != null)
            {
                inv.capacity            = tmpl.capacity;
                inv.weightLimit         = tmpl.weightLimit;
                inv.allowPutTagRefs     = new System.Collections.Generic.List<string>(tmpl.allowPutTagRefs);
                inv.allowTakeTagRefs    = new System.Collections.Generic.List<string>(tmpl.allowTakeTagRefs);
                inv.allowOperateTagRefs = new System.Collections.Generic.List<string>(tmpl.allowOperateTagRefs);
                inv.filterTagRefs       = new System.Collections.Generic.List<string>(tmpl.filterTagRefs);
                inv.showAllFilterTab    = tmpl.showAllFilterTab;
                inv.autoSort            = tmpl.autoSort;
                inv.dragSort            = tmpl.dragSort;
                inv.numberFormatRef     = tmpl.numberFormatRef;
                foreach (var sp in tmpl.sortPriorities)
                    inv.sortPriorities.Add(sp.Clone());
                foreach (var sp in tmpl.sortTiebreakers)
                    inv.sortTiebreakers.Add(sp.Clone());
            }
            inv.RebuildAttributes(db);
            db.Inventories.Add(inv);
            ctx.MarkDirty();
            return inv;
        }

        private Inventory QuickAdd(IInventoryEditorContext ctx)
        {
            var db  = ctx.Database;
            ctx.RecordUndo("快速添加仓库");
            var inv = db.Inventories[db.Inventories.Count - 1].Clone();
            inv.id  = GenerateInventoryId(db);
            db.Inventories.Add(inv);
            ctx.MarkDirty();
            return inv;
        }

        private static string GenerateInventoryId(InventoryDatabase db)
        {
            int n = db.Inventories.Count + 1;
            string id;
            do { id = "inv_" + n; n++; }
            while (db.GetInventory(id) != null);
            return id;
        }

        private static bool MatchesSearch(Inventory inv, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(inv.id) &&
                inv.id.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string invName = inv.displayNameText != null ? inv.displayNameText.GetTextValue(0) : null;
            if (!string.IsNullOrEmpty(invName) &&
                invName.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(inv.templateRef) &&
                inv.templateRef.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static string TagsToString(System.Collections.Generic.List<string> tags)
        {
            if (tags == null || tags.Count == 0) return "—";
            return string.Join("/", tags);
        }
    }
}

using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 商店列表面板（中间列）：商店模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 商店行列表。
    /// 每行显示：拖拽句柄、模板色点、商店 ID（粗体，重复红色高亮）、名称、类型、商品组数、删除按钮。
    /// 左侧拖拽句柄（≡）支持长按拖动来调整条目顺序。
    /// </summary>
    public class ShopListPanel
    {
        private const float KeyRowH     = 13f;  // 列名行高
        private const float ValRowH     = 22f;  // 值行高
        private const float DragHandleW = 16f;  // 拖拽句柄宽度
        private const float DotW        = 14f;
        private const float IdColW      = 90f;
        private const float NameColW    = 96f;
        private const float DescColW    = 120f;
        private const float TypeColW    = 64f;
        private const float GrpColW     = 48f;
        private const float DelBtnW     = 20f;
        private const float Pad         = 4f;

        private Vector2 _scroll;
        private string  _search         = string.Empty;
        private string  _templateFilter; // null = "全部"

        // ── 拖拽重排（复用统一工具类）────────────────────────────────────────────────
        private readonly EditorReorderableDrag _drag = new EditorReorderableDrag("ShopListDrag");

        private GUIStyle _keyStyle;
        private GUIStyle _idStyle;
        private GUIStyle _subStyle;
        private GUIStyle KeyStyle  => _keyStyle  ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.6f, 0.85f, 1.0f) }, wordWrap = false };
        private GUIStyle IdStyle  => _idStyle  ??= new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 11, wordWrap = false, clipping = TextClipping.Clip };
        private GUIStyle SubStyle => _subStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.62f, 0.62f, 0.62f) }, wordWrap = false, clipping = TextClipping.Clip };

        private static Shop _pendingSelect;

        /// <summary>绘制列表，返回当前选中的 Shop 引用。</summary>
        public Shop DrawList(IInventoryEditorContext ctx, Shop selectedShop)
        {
            var db = ctx.Database;

            // 校验过滤标签（模板被删除或改名时自动重置为"全部"）
            if (_templateFilter != null && db.GetShopTemplate(_templateFilter) == null)
                _templateFilter = null;

            _templateFilter = EditorFilterTabs.Draw(_templateFilter, db.ShopTemplates, t => t.name);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("从模板添加", EditorStyles.toolbarDropDown, GUILayout.Width(84)))
                ShowAddFromTemplateMenu(ctx);
            using (new EditorGUI.DisabledScope(db.Shops.Count == 0))
            {
                if (GUILayout.Button("快速添加", EditorStyles.toolbarButton, GUILayout.Width(72)))
                    selectedShop = QuickAdd(ctx);
            }
            EditorGUILayout.EndHorizontal();

            _drag.BeginFrame();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int deleteIndex = -1;
            var visible = new List<Shop>();   // 本帧可见（已过滤）条目，供键盘上下键导航

            for (int i = 0; i < db.Shops.Count; i++)
            {
                var shop = db.Shops[i];

                // 商店模板过滤
                if (_templateFilter != null && shop.templateRef != _templateFilter) continue;
                if (!MatchesSearch(shop, _search)) continue;

                visible.Add(shop);

                bool isDup    = ctx.DuplicateIdsOf(EInventoryEntityKind.Shop).Contains(
                    string.IsNullOrWhiteSpace(shop.id) ? string.Empty : shop.id);
                bool selected = (shop == selectedShop);

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

                // ── 拖拽句柄 ──────────────────────────────────────────────────────
                var dragRect = new Rect(fullRect.xMin, fullRect.yMin, DragHandleW - 2, fullRect.height);
                _drag.DrawHandle(dragRect, i);

                float cx = fullRect.x + DragHandleW;
                float vy = valRow.y + (ValRowH - EditorGUIUtility.singleLineHeight) * 0.5f;
                float vh = EditorGUIUtility.singleLineHeight;

                var tmplObj  = db.GetShopTemplate(shop.templateRef);
                Color dotClr = tmplObj != null ? tmplObj.color : Color.gray;
                InventoryEditorStyles.DrawColorDot(
                    new Rect(cx, fullRect.y + (fullRect.height - DotW) * 0.5f, DotW, DotW), dotClr);
                cx += DotW + Pad;

                // ── 上行：列名表头 ──────────────────────────────────────────────────
                {
                    float kx = fullRect.x + DragHandleW + DotW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,   KeyRowH - 2), "ID",     KeyStyle); kx += IdColW   + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, NameColW, KeyRowH - 2), "名称",   KeyStyle); kx += NameColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, DescColW, KeyRowH - 2), "描述",   KeyStyle); kx += DescColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, TypeColW, KeyRowH - 2), "类型",   KeyStyle); kx += TypeColW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, GrpColW,  KeyRowH - 2), "商品组", KeyStyle);
                }

                // ── 下行：值 ────────────────────────────────────────────────────────
                GUI.Label(new Rect(cx, vy, IdColW, vh),
                    string.IsNullOrWhiteSpace(shop.id) ? "(空 ID)" : shop.id, IdStyle);
                cx += IdColW + Pad;

                string shopName = shop.displayNameText != null ? shop.displayNameText.GetTextValue() : null;
                GUI.Label(new Rect(cx, vy, NameColW, vh),
                    string.IsNullOrEmpty(shopName) ? "—" : shopName, SubStyle);
                cx += NameColW + Pad;

                string shopDesc = shop.descriptionText != null ? shop.descriptionText.GetTextValue() : null;
                GUI.Label(new Rect(cx, vy, DescColW, vh),
                    string.IsNullOrEmpty(shopDesc) ? "—" : shopDesc, SubStyle);
                cx += DescColW + Pad;

                GUI.Label(new Rect(cx, vy, TypeColW, vh), shop.shopType.ToString(), SubStyle);
                cx += TypeColW + Pad;

                GUI.Label(new Rect(cx, vy, GrpColW, vh), shop.groups.Count.ToString(), SubStyle);

                if (Event.current.type == EventType.MouseDown
                    && fullRect.Contains(Event.current.mousePosition)
                    && !delRect.Contains(Event.current.mousePosition)
                    && !dragRect.Contains(Event.current.mousePosition))
                {
                    selectedShop = shop;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            // ── 拖拽落点处理 + 插入指示线（复用统一工具类，坐标系与行 Rect 一致）──────
            _drag.EndFrame(ctx, db.Shops, "调整商店顺序", DragHandleW, DelBtnW);

            EditorGUILayout.EndScrollView();
            float viewportHeight = GUILayoutUtility.GetLastRect().height;

            if (deleteIndex >= 0 && deleteIndex < db.Shops.Count)
            {
                var toDelete = db.Shops[deleteIndex];
                if (toDelete == selectedShop) selectedShop = null;
                ctx.RecordUndo("删除商店");
                db.Shops.RemoveAt(deleteIndex);
                ctx.MarkDirty();
            }

            // 键盘 上/下 方向键：在可见条目间切换选中，并在越界时自动滚动一行
            float rowPitch = KeyRowH + ValRowH + 2f * EditorGUIUtility.standardVerticalSpacing;
            if (EditorListKeyboardNav.HandleUpDown(visible, selectedShop, out var navShop,
                    ref _scroll, rowPitch, viewportHeight))
            {
                selectedShop = navShop;
                GUI.FocusControl(null);
                ctx.Repaint();
            }

            return selectedShop;
        }

        /// <summary>取出并清空待选中商店（由 ShopSystemTab 在每帧 Layout 前调用）。</summary>
        public Shop ConsumePendingSelect()
        {
            var shop = _pendingSelect;
            _pendingSelect = null;
            return shop;
        }

        private void ShowAddFromTemplateMenu(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            if (db.ShopTemplates.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("（无可用商店模板）"));
            }
            else
            {
                foreach (var template in db.ShopTemplates)
                {
                    string name = template.name;
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        var shop = AddFromTemplate(ctx, name);
                        _pendingSelect = shop;
                        ctx.Repaint();
                    });
                }
            }
            menu.ShowAsContext();
        }

        private static Shop AddFromTemplate(IInventoryEditorContext ctx, string templateName)
        {
            var db = ctx.Database;
            ctx.RecordUndo("从模板添加商店");
            var shop = new Shop(GenerateShopId(db), templateName);
            var tmpl = db.GetShopTemplate(templateName);
            if (tmpl != null)
            {
                shop.shopType           = tmpl.shopType;
                shop.numberFormatRef    = tmpl.numberFormatRef;
                shop.priceAttrSource    = tmpl.priceAttrSource;
                shop.tradeInventoryRefs = new List<string>(tmpl.tradeInventoryRefs);
                shop.tradeTagRefs       = new List<string>(tmpl.tradeTagRefs);
                shop.filterTagRefs      = new List<string>(tmpl.filterTagRefs);
                shop.showAllFilterTab   = tmpl.showAllFilterTab;
                foreach (var sp in tmpl.sortPriorities)
                    shop.sortPriorities.Add(sp.Clone());
                foreach (var sp in tmpl.sortTiebreakers)
                    shop.sortTiebreakers.Add(sp.Clone());
                foreach (var g in tmpl.groups)
                    shop.groups.Add(g.Clone());
            }
            shop.RebuildAttributes(db);
            db.Shops.Add(shop);
            ctx.MarkDirty();
            return shop;
        }

        private Shop QuickAdd(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            ctx.RecordUndo("快速添加商店");
            var shop = db.Shops[db.Shops.Count - 1].Clone();
            shop.id  = GenerateShopId(db);
            db.Shops.Add(shop);
            ctx.MarkDirty();
            return shop;
        }

        private static string GenerateShopId(InventoryDatabase db)
        {
            int n = db.Shops.Count + 1;
            string id;
            do { id = "shop_" + n; n++; }
            while (db.GetShop(id) != null);
            return id;
        }

        private static bool MatchesSearch(Shop shop, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(shop.id) &&
                shop.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string name = shop.displayNameText != null ? shop.displayNameText.GetTextValue() : null;
            if (!string.IsNullOrEmpty(name) &&
                name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}

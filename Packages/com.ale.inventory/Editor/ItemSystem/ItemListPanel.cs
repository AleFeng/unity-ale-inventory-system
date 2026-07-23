using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 道具列表面板（中间列）：模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」+ 道具行列表。
    ///
    /// 每个道具条目占用两行（有属性时）：
    ///   上行（细）— 属性 Key 名称（蓝色小字）
    ///   下行（主）— 道具 ID（加粗）+ 各属性值 + 右侧删除按钮
    ///
    /// 左侧拖拽句柄（≡）支持长按拖动来调整条目顺序。
    /// </summary>
    public class ItemListPanel
    {
        // ── 布局常量 ────────────────────────────────────────────────────────────────
        private const float KeyRowH     = 13f;  // 属性名行高
        private const float ValRowH     = 22f;  // 属性值行高
        private const float DragHandleW = 16f;  // 拖拽句柄宽度
        private const float DotW        = 14f;  // 色点宽度
        private const float TmplW       = 72f;  // 模板名列宽
        private const float IdColW      = 80f;  // ID 列固定宽度
        private const float DelBtnW     = 20f;  // 删除按钮宽度
        private const float ColPad      = 4f;   // 列间距
        private const float MinColW     = 36f;
        private const float MaxColW     = 120f;

        private Vector2 _scroll;
        private string  _search         = string.Empty;
        private string  _templateFilter = null; // null = "全部"

        // ── 拖拽重排（复用统一工具类）────────────────────────────────────────────────
        private readonly EditorReorderableDrag _drag = new EditorReorderableDrag("ItemListDrag");

        // ── 样式缓存 ────────────────────────────────────────────────────────────────
        private GUIStyle _keyStyle;
        private GUIStyle _idStyle;
        private GUIStyle _valStyle;
        private GUIStyle _tmplStyle;

        private GUIStyle KeyStyle  => _keyStyle  ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.6f, 0.85f, 1.0f) }, wordWrap = false };
        private GUIStyle IdStyle   => _idStyle   ??= new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 11, wordWrap = false, clipping = TextClipping.Clip };
        private GUIStyle ValStyle  => _valStyle  ??= new GUIStyle(EditorStyles.miniLabel)
            { wordWrap = false, clipping = TextClipping.Clip };
        private GUIStyle TmplStyle => _tmplStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal    = { textColor = new Color(0.65f, 0.65f, 0.65f) },
            wordWrap  = false,
            clipping  = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
        };

        // ── 主绘制方法 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 绘制列表，返回当前选中的 Item 引用。
        /// 若被选中的道具被删除，返回 null。
        /// </summary>
        public Item DrawList(IInventoryEditorContext ctx, Item selectedItem)
        {
            var db = ctx.Database;

            // 校验过滤标签（模板被删除或改名时自动重置为"全部"）
            if (_templateFilter != null && db.GetTemplate(_templateFilter) == null)
                _templateFilter = null;

            _templateFilter = EditorFilterTabs.Draw(_templateFilter, db.ItemTemplates, t => t.name);

            // 搜索栏 + 添加按钮
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("从模板添加", EditorStyles.toolbarDropDown, GUILayout.Width(84)))
                ShowAddFromTemplateMenu(ctx);
            using (new EditorGUI.DisabledScope(db.Items.Count == 0))
            {
                if (GUILayout.Button("快速添加", EditorStyles.toolbarButton, GUILayout.Width(72)))
                    selectedItem = QuickAdd(ctx);
            }
            EditorGUILayout.EndHorizontal();

            _drag.BeginFrame();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int deleteIndex = -1;
            var visible = new List<Item>();   // 本帧可见（已过滤）条目，按显示顺序，供键盘上下键导航

            for (int i = 0; i < db.Items.Count; i++)
            {
                var item = db.Items[i];

                // 模板过滤
                if (_templateFilter != null && item.templateRef != _templateFilter) continue;
                if (!ItemQueryUtil.Matches(db, item, _search)) continue;

                visible.Add(item);

                bool isDup    = ctx.DuplicateIdsOf(EInventoryEntityKind.Item).Contains(
                    string.IsNullOrWhiteSpace(item.id) ? string.Empty : item.id);
                bool selected = (item == selectedItem);
                bool hasAttrs = item.values.Count > 0;

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
                if (_drag.IsDragSource(i)) // 拖拽中的源行轻微高亮
                    InventoryEditorStyles.DrawRowBackground(fullRect, EditorReorderableDrag.DragSourceTint);

                // ── 删除按钮 ──────────────────────────────────────────────────────
                var delRect = new Rect(fullRect.xMax - DelBtnW, valRow.y + 2, DelBtnW - 2, ValRowH - 4);
                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    deleteIndex = i;

                // ── 拖拽句柄 ──────────────────────────────────────────────────────
                var dragRect = new Rect(fullRect.xMin, fullRect.yMin, DragHandleW - 2, fullRect.height);
                _drag.DrawHandle(dragRect, i);

                // ── 预计算可见属性列 ────────────────────────────────────────────────
                float fixedPrefix  = DragHandleW + DotW + ColPad + TmplW + ColPad + IdColW + ColPad;
                float contentRight = fullRect.xMax - DelBtnW - ColPad;
                var cols = new List<(AttributeEntry entry, EnumType enumType, float x, float colW)>();

                if (hasAttrs)
                {
                    var itemDefMap = ItemQueryUtil.BuildDefMap(db, item);
                    float cx = fullRect.x + fixedPrefix;
                    foreach (var entry in item.values)
                    {
                        float cw = CalcColWidth(entry.id);
                        if (cx + cw > contentRight) break;
                        itemDefMap.TryGetValue(entry.id, out var def);
                        var et = (def?.type == EFieldType.Enum || def?.type == EFieldType.EnumIntPair)
                            ? db.GetEnumType(def.enumTypeRef) : null;
                        cols.Add((entry, et, cx, cw));
                        cx += cw + ColPad;
                    }
                }

                // ── 上行：列名 ──────────────────────────────────────────────────────
                {
                    float idKeyX = fullRect.x + DragHandleW + DotW + ColPad + TmplW + ColPad;
                    GUI.Label(new Rect(idKeyX, keyRow.y + 1, IdColW, KeyRowH - 2), "ID", KeyStyle);
                    foreach (var (entry, _, x, cw) in cols)
                        GUI.Label(new Rect(x, keyRow.y + 1, cw, KeyRowH - 2), entry.id, KeyStyle);
                }

                // ── 下行：● 色点 + 模板名 + ID + 属性值 ──────────────────────────────
                float drawX = fullRect.x + DragHandleW;

                var tmplObj  = db.GetTemplate(item.templateRef);
                Color dotClr = tmplObj != null ? tmplObj.color : Color.gray;
                InventoryEditorStyles.DrawColorDot(
                    new Rect(drawX, fullRect.y + (fullRect.height - DotW) * 0.5f, DotW, DotW), dotClr);
                drawX += DotW + ColPad;

                string tmplText = string.IsNullOrEmpty(item.templateRef) ? "—" : item.templateRef;
                GUI.Label(new Rect(drawX, fullRect.y, TmplW, fullRect.height), tmplText, TmplStyle);
                drawX += TmplW + ColPad;

                string idText = string.IsNullOrWhiteSpace(item.id) ? "(空 ID)" : item.id;
                GUI.Label(new Rect(drawX, valRow.y + 3, IdColW, ValRowH - 6), idText, IdStyle);

                foreach (var (entry, et, x, cw) in cols)
                {
                    string raw     = GetValueString(entry.value, et);
                    string display = TruncateText(raw, cw, ValStyle);
                    GUI.Label(new Rect(x, valRow.y + 3, cw, ValRowH - 6), display, ValStyle);
                }

                // ── 行点击选中（排除句柄和删除按钮）──────────────────────────────────
                if (Event.current.type == EventType.MouseDown
                    && fullRect.Contains(Event.current.mousePosition)
                    && !delRect.Contains(Event.current.mousePosition)
                    && !dragRect.Contains(Event.current.mousePosition))
                {
                    selectedItem = item;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            // ── 拖拽落点处理 + 插入指示线（复用统一工具类，坐标系与行 Rect 一致）──────
            _drag.EndFrame(ctx, db.Items, "调整道具顺序", DragHandleW, DelBtnW);

            EditorGUILayout.EndScrollView();
            float viewportHeight = GUILayoutUtility.GetLastRect().height;

            // 删除在滚动区外处理，防止中途 break 导致 IMGUI 控件数不一致
            if (deleteIndex >= 0 && deleteIndex < db.Items.Count)
            {
                var toDelete = db.Items[deleteIndex];
                if (toDelete == selectedItem) selectedItem = null;
                ctx.RecordUndo("删除道具");
                db.Items.RemoveAt(deleteIndex);
                ctx.MarkDirty();
            }

            // 键盘 上/下 方向键：在可见条目间切换选中，并在越界时自动滚动一行
            float rowPitch = KeyRowH + ValRowH + 2f * EditorGUIUtility.standardVerticalSpacing;
            if (EditorListKeyboardNav.HandleUpDown(visible, selectedItem, out var navItem,
                    ref _scroll, rowPitch, viewportHeight))
            {
                selectedItem = navItem;
                GUI.FocusControl(null);
                ctx.Repaint();
            }

            return selectedItem;
        }

        // ── 列宽计算 ────────────────────────────────────────────────────────────────

        private static float CalcColWidth(string key)
        {
            if (string.IsNullOrEmpty(key)) return MinColW;
            return Mathf.Clamp(key.Length * 12f, MinColW, MaxColW);
        }

        // ── 属性值 → 字符串 ─────────────────────────────────────────────────────────

        private static string GetValueString(AttributeValue val, EnumType enumType)
        {
            if (val == null || val.Count == 0) return "—";
            if (val.IsArray) return $"[{val.Count}]";

            switch (val.Type)
            {
                case EFieldType.Int:    return val.AsInt.ToString();
                case EFieldType.Float:  return val.AsFloat.ToString("F2");
                case EFieldType.String: return val.AsString;
                case EFieldType.Bool:   return val.AsBool ? "是" : "否";
                case EFieldType.Enum:
                    return enumType != null
                        ? enumType.GetDisplayName(val.AsEnumValue)
                        : val.AsEnumValue.ToString();
                case EFieldType.Color:
                    return "#" + ColorUtility.ToHtmlStringRGB(val.AsColor);
                case EFieldType.Vector2:
                    var v2 = val.AsVector2; return $"({v2.x:F1},{v2.y:F1})";
                case EFieldType.Vector3:
                    var v3 = val.AsVector3; return $"({v3.x:F1},{v3.y:F1},{v3.z:F1})";
                case EFieldType.Vector4:
                    var v4 = val.AsVector4; return $"({v4.x:F1},{v4.y:F1},{v4.z:F1},{v4.w:F1})";
                case EFieldType.Sprite:
                    return val.AsSprite != null ? val.AsSprite.name : "—";
                case EFieldType.Prefab:
                    return val.AsPrefab != null ? val.AsPrefab.name : "—";
                case EFieldType.Texture:
                    return val.AsTexture != null ? val.AsTexture.name : "—";
                case EFieldType.Material:
                    return val.AsMaterial != null ? val.AsMaterial.name : "—";
                case EFieldType.AudioClip:
                    return val.AsAudioClip != null ? val.AsAudioClip.name : "—";
                case EFieldType.AnimationClip:
                    return val.AsAnimationClip != null ? val.AsAnimationClip.name : "—";
                case EFieldType.PhysicsMaterial:
                case EFieldType.PhysicsMaterial2D:
                    return val.AsObject != null ? val.AsObject.name : "—";
                case EFieldType.AnimationCurve:
                {
                    var c = val.GetAnimationCurve(0);
                    return c != null && c.length > 0 ? $"曲线({c.length}帧)" : "—";
                }
                case EFieldType.Text:
                {
                    // 优先纯文本 fallback，其次本地化条目键 / 表名。
                    string plain = val.GetTextValue(0);
                    if (!string.IsNullOrEmpty(plain)) return plain;
                    var (t, e) = val.GetLocalizedStringRef(0);
                    if (!string.IsNullOrEmpty(e)) return e;
                    return string.IsNullOrEmpty(t) ? "—" : t;
                }
                case EFieldType.StringIntPair:
                {
                    var (k, v) = val.GetStringIntPair(0);
                    return string.IsNullOrEmpty(k) ? v.ToString() : $"{k}:{v}";
                }
                case EFieldType.EnumIntPair:
                {
                    var (ev, v) = val.GetEnumIntPair(0);
                    string name = enumType != null ? enumType.GetDisplayName(ev) : ev.ToString();
                    return $"{name}:{v}";
                }
                default: return "—";
            }
        }

        // ── 文本截断 ────────────────────────────────────────────────────────────────

        private static string TruncateText(string text, float width, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (style.CalcSize(new GUIContent(text)).x <= width) return text;

            float suffW = style.CalcSize(new GUIContent("…")).x;
            float avail = width - suffW;
            if (avail <= 0) return "…";

            int lo = 0, hi = text.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (style.CalcSize(new GUIContent(text.Substring(0, mid))).x <= avail)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo > 0 ? text.Substring(0, lo) + "…" : "…";
        }

        // ── 添加道具 ────────────────────────────────────────────────────────────────

        private void ShowAddFromTemplateMenu(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            if (db.ItemTemplates.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("（无可用模板）"));
            }
            else
            {
                foreach (var template in db.ItemTemplates)
                {
                    string name = template.name;
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        var item = AddFromTemplate(ctx, name);
                        SelectAfterMenu(ctx, item);
                    });
                }
            }
            menu.ShowAsContext();
        }

        private static Item _pendingSelectItem;

        private void SelectAfterMenu(IInventoryEditorContext ctx, Item item)
        {
            _pendingSelectItem = item;
            ctx.Repaint();
        }

        /// <summary>取出并清空待选中道具（由 ItemSystemTab 在每帧 Layout 前调用）。</summary>
        public Item ConsumePendingSelect()
        {
            var item = _pendingSelectItem;
            _pendingSelectItem = null;
            return item;
        }

        private Item AddFromTemplate(IInventoryEditorContext ctx, string templateName)
        {
            var db  = ctx.Database;
            ctx.RecordUndo("从模板添加道具");
            string id = GenerateItemId(db);
            var item  = new Item(id, templateName);
            // 复制模板的基础字段（重量 / 堆叠上限 / 隐藏）；自定义属性字段值由 RebuildAttributes 按定义默认值填充。
            var tmpl  = db.GetTemplate(templateName);
            if (tmpl != null)
            {
                item.weight          = tmpl.weight;
                item.stackLimit      = tmpl.stackLimit;
                item.hideInInventory = tmpl.hideInInventory;
            }
            item.RebuildAttributes(db);
            db.Items.Add(item);
            ctx.MarkDirty();
            return item;
        }

        private Item QuickAdd(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            ctx.RecordUndo("快速添加道具");
            var item = db.Items[db.Items.Count - 1].Clone();
            item.id  = GenerateItemId(db);
            db.Items.Add(item);
            ctx.MarkDirty();
            return item;
        }

        private static string GenerateItemId(InventoryDatabase db)
        {
            int n = db.Items.Count + 1;
            string id;
            do { id = "item_" + n; n++; }
            while (db.GetItem(id) != null);
            return id;
        }
    }
}

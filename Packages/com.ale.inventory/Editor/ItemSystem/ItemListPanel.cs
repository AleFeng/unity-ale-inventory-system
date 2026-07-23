using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 道具列表面板（中间列）：道具模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 道具行列表。
    /// 每行显示：拖拽句柄、模板色点、模板名、道具 ID（粗体，重复红色高亮）、**按放得下的宽度动态排布的属性列**、删除按钮。
    /// 骨架来自 <see cref="EditorEntityListPanel{TEntity,TTemplate}"/>；本面板独有的是动态属性列布局。
    /// </summary>
    public class ItemListPanel : EditorEntityListPanel<Item, ItemTemplate>
    {
        private const float TmplW   = 72f;  // 模板名列宽
        private const float IdColW  = 80f;  // ID 列固定宽度
        private const float MinColW = 36f;
        private const float MaxColW = 120f;

        private GUIStyle _valStyle;
        private GUIStyle _tmplStyle;
        private GUIStyle ValStyle  => _valStyle  ??= new GUIStyle(EditorStyles.miniLabel)
            { wordWrap = false, clipping = TextClipping.Clip };
        private GUIStyle TmplStyle => _tmplStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal    = { textColor = new Color(0.65f, 0.65f, 0.65f) },
            wordWrap  = false,
            clipping  = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
        };

        public ItemListPanel() : base("ItemListDrag") { }

        #region 列表配置

        protected override List<Item>         Entities(InventoryDatabase db)  => db.Items;
        protected override List<ItemTemplate> Templates(InventoryDatabase db) => db.ItemTemplates;
        protected override string TemplateName(ItemTemplate t) => t.name;
        protected override string TemplateRefOf(Item e)        => e.templateRef;
        protected override string IdOf(Item e)                 => e.id;
        protected override EInventoryEntityKind Kind           => EInventoryEntityKind.Item;
        protected override string Noun                         => "道具";
        protected override string NoTemplateHint               => "（无可用模板）";

        protected override Color RowDotColor(InventoryDatabase db, Item e)
        {
            var tmpl = db.GetTemplate(e.templateRef);
            return tmpl != null ? tmpl.color : Color.gray;
        }

        /// <summary>道具的搜索走 <see cref="ItemQueryUtil"/>（除 ID / 名称外还能按属性值匹配）。</summary>
        protected override bool Matches(InventoryDatabase db, Item item, string term)
            => ItemQueryUtil.Matches(db, item, term);

        #endregion

        #region 行列布局（动态属性列）

        protected override void DrawRowColumns(InventoryDatabase db, Item item,
            Rect keyRow, float cx, float contentRight, float vy, float vh)
        {
            // 模板名列：跨整行高度垂直居中
            float rowH = KeyRowH + ValRowH + EditorGUIUtility.standardVerticalSpacing;
            string tmplText = string.IsNullOrEmpty(item.templateRef) ? "—" : item.templateRef;
            GUI.Label(new Rect(cx, keyRow.y, TmplW, rowH), tmplText, TmplStyle);
            cx += TmplW + Pad;

            // ID 列
            GUI.Label(new Rect(cx, keyRow.y + 1, IdColW, KeyRowH - 2), "ID", KeyStyle);
            GUI.Label(new Rect(cx, vy, IdColW, vh),
                string.IsNullOrWhiteSpace(item.id) ? "(空 ID)" : item.id, IdStyle);
            cx += IdColW + Pad;

            // ── 动态属性列：逐个按列宽排布，放不下即停 ──────────────────────────
            if (item.values.Count == 0) return;

            var itemDefMap = ItemQueryUtil.BuildDefMap(db, item);
            foreach (var entry in item.values)
            {
                float cw = CalcColWidth(entry.id);
                if (cx + cw > contentRight) break;

                itemDefMap.TryGetValue(entry.id, out var def);
                var et = (def?.type == EFieldType.Enum || def?.type == EFieldType.EnumIntPair)
                    ? db.GetEnumType(def.enumTypeRef) : null;

                GUI.Label(new Rect(cx, keyRow.y + 1, cw, KeyRowH - 2), entry.id, KeyStyle);

                string raw     = GetValueString(entry.value, et);
                string display = TruncateText(raw, cw, ValStyle);
                GUI.Label(new Rect(cx, vy, cw, vh), display, ValStyle);

                cx += cw + Pad;
            }
        }

        private static float CalcColWidth(string key)
            => string.IsNullOrEmpty(key) ? MinColW : Mathf.Clamp(key.Length * 12f, MinColW, MaxColW);

        #endregion

        #region 新增

        protected override Item AddFromTemplate(IInventoryEditorContext ctx, string templateName)
        {
            var db  = ctx.Database;
            ctx.RecordUndo("从模板添加道具");
            var item = new Item(GenerateItemId(db), templateName);
            // 复制模板的基础字段（重量 / 堆叠上限 / 隐藏）；自定义属性字段值由 RebuildAttributes 按定义默认值填充。
            var tmpl = db.GetTemplate(templateName);
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

        protected override Item QuickAdd(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            ctx.RecordUndo("快速添加道具");
            var item = db.Items[db.Items.Count - 1].Clone();
            item.id  = GenerateItemId(db);
            db.Items.Add(item);
            ctx.MarkDirty();
            return item;
        }

        private string GenerateItemId(InventoryDatabase db)
            => GenerateId(db, "item_", id => db.GetItem(id) != null);

        #endregion

        #region 属性值 → 字符串 / 文本截断
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
        #endregion
    }
}

using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;

    /// <summary>
    /// 仓库列表面板（中间列）：仓库模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 仓库行列表。
    /// 每行显示：拖拽句柄、模板色点、模板名、仓库 ID（粗体，重复红色高亮）、名称、描述、
    /// 容量上限、重量上限、三类功能标签、删除按钮。
    /// 骨架来自 <see cref="EditorEntityListPanel{TEntity,TTemplate}"/>。
    /// </summary>
    public class InventoryListPanel : EditorEntityListPanel<Inventory, InventoryTemplate>
    {
        private const float TmplColW = 80f;
        private const float IdColW   = 76f;
        private const float NameColW = 96f;
        private const float DescColW = 120f;
        private const float CapColW  = 48f;
        private const float WtColW   = 48f;
        private const float TagColW  = 64f;

        private GUIStyle _tmplStyle;
        private GUIStyle TmplStyle => _tmplStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal    = { textColor = new Color(0.65f, 0.65f, 0.65f) },
            wordWrap  = false,
            clipping  = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
        };

        public InventoryListPanel() : base("InventoryListDrag") { }

        #region 列表配置

        protected override List<Inventory>         Entities(InventoryDatabase db)  => db.Inventories;
        protected override List<InventoryTemplate> Templates(InventoryDatabase db) => db.InventoryTemplates;
        protected override string TemplateName(InventoryTemplate t) => t.name;
        protected override string TemplateRefOf(Inventory e)        => e.templateRef;
        protected override string IdOf(Inventory e)                 => e.id;
        protected override EInventoryEntityKind Kind                => EInventoryEntityKind.Inventory;
        protected override string Noun                              => "仓库";

        protected override Color RowDotColor(InventoryDatabase db, Inventory e)
        {
            var tmpl = db.GetInventoryTemplate(e.templateRef);
            return tmpl != null ? tmpl.color : Color.gray;
        }

        /// <summary>仓库的搜索额外匹配「来源模板名」（其余五个系统只搜 ID 与名称）。</summary>
        protected override bool Matches(InventoryDatabase db, Inventory inv, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(inv.id) &&
                inv.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string invName = inv.displayNameText != null ? inv.displayNameText.GetTextValue(0) : null;
            if (!string.IsNullOrEmpty(invName) &&
                invName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return !string.IsNullOrEmpty(inv.templateRef) &&
                   inv.templateRef.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region 行列布局

        protected override void DrawRowColumns(InventoryDatabase db, Inventory inv,
            Rect keyRow, float cx, float contentRight, float vy, float vh)
        {
            // 模板名列：跨整行高度垂直居中（本面板独有的一列，位于色点之后、ID 之前）
            float rowTop = keyRow.y;
            float rowH   = KeyRowH + ValRowH + EditorGUIUtility.standardVerticalSpacing;
            string tmplText = string.IsNullOrEmpty(inv.templateRef) ? "—" : inv.templateRef;
            GUI.Label(new Rect(cx, rowTop, TmplColW, rowH), tmplText, TmplStyle);
            cx += TmplColW + Pad;

            // ── 上行：列名表头 ──────────────────────────────────────────────────
            float kx = cx;
            GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,   KeyRowH - 2), "ID",          KeyStyle); kx += IdColW   + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, NameColW, KeyRowH - 2), Tr("名称"),   KeyStyle); kx += NameColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, DescColW, KeyRowH - 2), Tr("描述"),   KeyStyle); kx += DescColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, CapColW,  KeyRowH - 2), Tr("容量上限"), KeyStyle); kx += CapColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, WtColW,   KeyRowH - 2), Tr("重量上限"), KeyStyle); kx += WtColW  + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, TagColW,  KeyRowH - 2), Tr("放入"),   KeyStyle); kx += TagColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, TagColW,  KeyRowH - 2), Tr("取出"),   KeyStyle); kx += TagColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, TagColW,  KeyRowH - 2), Tr("操作"),   KeyStyle);

            // ── 下行：值 ────────────────────────────────────────────────────────
            GUI.Label(new Rect(cx, vy, IdColW, vh),
                string.IsNullOrWhiteSpace(inv.id) ? Tr("(空 ID)") : inv.id, IdStyle);
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
        }

        private static string TagsToString(List<string> tags)
            => tags == null || tags.Count == 0 ? "—" : string.Join("/", tags);

        #endregion

        #region 新增

        protected override Inventory AddFromTemplate(IInventoryEditorContext ctx, string templateName)
        {
            var db   = ctx.Database;
            ctx.RecordUndo("从模板添加仓库");
            var inv  = new Inventory(GenerateInventoryId(db), templateName);
            var tmpl = db.GetInventoryTemplate(templateName);
            if (tmpl != null)
            {
                inv.capacity            = tmpl.capacity;
                inv.weightLimit         = tmpl.weightLimit;
                inv.allowPutTagRefs     = new List<string>(tmpl.allowPutTagRefs);
                inv.allowTakeTagRefs    = new List<string>(tmpl.allowTakeTagRefs);
                inv.allowOperateTagRefs = new List<string>(tmpl.allowOperateTagRefs);
                inv.filterTagRefs       = new List<string>(tmpl.filterTagRefs);
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

        protected override Inventory QuickAdd(IInventoryEditorContext ctx)
        {
            var db  = ctx.Database;
            ctx.RecordUndo("快速添加仓库");
            var inv = db.Inventories[db.Inventories.Count - 1].Clone();
            inv.id  = GenerateInventoryId(db);
            db.Inventories.Add(inv);
            ctx.MarkDirty();
            return inv;
        }

        private string GenerateInventoryId(InventoryDatabase db)
            => GenerateId(db, "inv_", id => db.GetInventory(id) != null);

        #endregion
    }
}

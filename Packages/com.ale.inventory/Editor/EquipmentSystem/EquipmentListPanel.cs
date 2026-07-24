using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 装备组列表面板（中间列）：装备组模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 装备组行列表。
    /// 每行显示：拖拽句柄、模板色点、装备组 ID（粗体，重复红色高亮）、名称、描述、槽组数、删除按钮。
    /// 骨架来自 <see cref="EditorEntityListPanel{TEntity,TTemplate}"/>。
    /// </summary>
    public class EquipmentListPanel : EditorEntityListPanel<EquipmentGroup, EquipmentGroupTemplate>
    {
        private const float IdColW   = 96f;
        private const float NameColW = 110f;
        private const float DescColW = 120f;
        private const float SlotColW = 56f;

        public EquipmentListPanel() : base("EquipmentListDrag") { }

        #region 列表配置

        protected override List<EquipmentGroup>         Entities(InventoryDatabase db)  => db.EquipmentGroups;
        protected override List<EquipmentGroupTemplate> Templates(InventoryDatabase db) => db.EquipmentGroupTemplates;
        protected override string TemplateName(EquipmentGroupTemplate t) => t.name;
        protected override string TemplateRefOf(EquipmentGroup e)        => e.templateRef;
        protected override string IdOf(EquipmentGroup e)                 => e.id;
        protected override EInventoryEntityKind Kind                     => EInventoryEntityKind.Equipment;
        protected override string Noun                                   => "装备组";

        protected override Color RowDotColor(InventoryDatabase db, EquipmentGroup e)
        {
            var tmpl = db.GetEquipmentGroupTemplate(e.templateRef);
            return tmpl != null ? tmpl.color : Color.gray;
        }

        protected override bool Matches(InventoryDatabase db, EquipmentGroup g, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(g.id) &&
                g.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string gName = g.displayNameText != null ? g.displayNameText.GetTextValue(0) : null;
            return !string.IsNullOrEmpty(gName) &&
                   gName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region 行列布局

        protected override void DrawRowColumns(InventoryDatabase db, EquipmentGroup g,
            Rect keyRow, float cx, float contentRight, float vy, float vh)
        {
            // ── 上行：列名表头 ──────────────────────────────────────────────────
            float kx = cx;
            GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,   KeyRowH - 2), "ID",       KeyStyle); kx += IdColW   + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, NameColW, KeyRowH - 2), Tr("名称"), KeyStyle); kx += NameColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, DescColW, KeyRowH - 2), Tr("描述"), KeyStyle); kx += DescColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, SlotColW, KeyRowH - 2), Tr("槽组"), KeyStyle);

            // ── 下行：值 ────────────────────────────────────────────────────────
            GUI.Label(new Rect(cx, vy, IdColW, vh),
                string.IsNullOrWhiteSpace(g.id) ? Tr("(空 ID)") : g.id, IdStyle);
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
        }

        #endregion

        #region 新增

        protected override EquipmentGroup AddFromTemplate(IInventoryEditorContext ctx, string templateName)
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

        protected override EquipmentGroup QuickAdd(IInventoryEditorContext ctx)
        {
            var db = ctx.Database;
            ctx.RecordUndo("快速添加装备组");
            var g = db.EquipmentGroups[db.EquipmentGroups.Count - 1].Clone();
            g.id  = GenerateGroupId(db);
            db.EquipmentGroups.Add(g);
            ctx.MarkDirty();
            return g;
        }

        private string GenerateGroupId(InventoryDatabase db)
            => GenerateId(db, "equipment_", id => db.GetEquipmentGroup(id) != null);

        #endregion
    }
}

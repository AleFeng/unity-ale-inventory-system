using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 蓝图列表面板（中间列）：蓝图模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 蓝图行列表。
    /// 每行显示：拖拽句柄、模板色点、蓝图 ID（粗体，重复红色高亮）、名称、描述、主分组名、产出数、删除按钮。
    /// 骨架来自 <see cref="EditorEntityListPanel{TEntity,TTemplate}"/>。
    /// </summary>
    public class CraftingListPanel : EditorEntityListPanel<CraftingBlueprint, CraftingBlueprintTemplate>
    {
        private const float IdColW    = 90f;
        private const float NameColW  = 96f;
        private const float DescColW  = 120f;
        private const float GroupColW = 72f;
        private const float OutColW   = 48f;

        public CraftingListPanel() : base("CraftingListDrag") { }

        #region 列表配置

        protected override List<CraftingBlueprint>         Entities(InventoryDatabase db)  => db.CraftingBlueprints;
        protected override List<CraftingBlueprintTemplate> Templates(InventoryDatabase db) => db.CraftingBlueprintTemplates;
        protected override string TemplateName(CraftingBlueprintTemplate t) => t.name;
        protected override string TemplateRefOf(CraftingBlueprint e)        => e.templateRef;
        protected override string IdOf(CraftingBlueprint e)                 => e.id;
        protected override EInventoryEntityKind Kind                        => EInventoryEntityKind.Crafting;
        protected override string Noun                                      => "蓝图";

        protected override Color RowDotColor(InventoryDatabase db, CraftingBlueprint e)
        {
            var tmpl = db.GetCraftingBlueprintTemplate(e.templateRef);
            return tmpl != null ? tmpl.color : Color.gray;
        }

        protected override bool Matches(InventoryDatabase db, CraftingBlueprint bp, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(bp.id) &&
                bp.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string name = bp.displayText != null ? bp.displayText.GetTextValue(0) : null;
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region 行列布局

        protected override void DrawRowColumns(InventoryDatabase db, CraftingBlueprint bp,
            Rect keyRow, float cx, float contentRight, float vy, float vh)
        {
            // ── 上行：列名表头 ──────────────────────────────────────────────────
            float kx = cx;
            GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,    KeyRowH - 2), "ID",         KeyStyle); kx += IdColW    + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, NameColW,  KeyRowH - 2), Tr("名称"),   KeyStyle); kx += NameColW  + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, DescColW,  KeyRowH - 2), Tr("描述"),   KeyStyle); kx += DescColW  + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, GroupColW, KeyRowH - 2), Tr("主分组"), KeyStyle); kx += GroupColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, OutColW,   KeyRowH - 2), Tr("产出"),   KeyStyle);

            // ── 下行：值 ────────────────────────────────────────────────────────
            GUI.Label(new Rect(cx, vy, IdColW, vh),
                string.IsNullOrWhiteSpace(bp.id) ? Tr("(空 ID)") : bp.id, IdStyle);
            cx += IdColW + Pad;

            string bpName = bp.displayText != null ? bp.displayText.GetTextValue(0) : null;
            GUI.Label(new Rect(cx, vy, NameColW, vh),
                string.IsNullOrEmpty(bpName) ? "—" : bpName, SubStyle);
            cx += NameColW + Pad;

            string bpDesc = bp.descriptionText != null ? bp.descriptionText.GetTextValue(0) : null;
            GUI.Label(new Rect(cx, vy, DescColW, vh),
                string.IsNullOrEmpty(bpDesc) ? "—" : bpDesc, SubStyle);
            cx += DescColW + Pad;

            var    groupObj  = db.GetCraftingGroupTag(bp.primaryGroupTag);
            string groupName = groupObj != null ? groupObj.PlainName() : "—";
            GUI.Label(new Rect(cx, vy, GroupColW, vh), groupName, SubStyle);
            cx += GroupColW + Pad;

            GUI.Label(new Rect(cx, vy, OutColW, vh), bp.outputs.Count.ToString(), SubStyle);
        }

        #endregion

        #region 新增

        protected override CraftingBlueprint AddFromTemplate(IInventoryEditorContext ctx, string templateName)
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

        protected override CraftingBlueprint QuickAdd(IInventoryEditorContext ctx)
        {
            var db = ctx.Database;
            ctx.RecordUndo("快速添加蓝图");
            var bp = db.CraftingBlueprints[db.CraftingBlueprints.Count - 1].Clone();
            bp.id  = GenerateBlueprintId(db);
            db.CraftingBlueprints.Add(bp);
            ctx.MarkDirty();
            return bp;
        }

        private string GenerateBlueprintId(InventoryDatabase db)
            => GenerateId(db, "blueprint_", id => db.GetCraftingBlueprint(id) != null);

        #endregion
    }
}

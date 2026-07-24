using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 商店列表面板（中间列）：商店模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 商店行列表。
    /// 每行显示：拖拽句柄、模板色点、商店 ID（粗体，重复红色高亮）、名称、描述、类型、商品组数、删除按钮。
    /// 骨架来自 <see cref="EditorEntityListPanel{TEntity,TTemplate}"/>。
    /// </summary>
    public class ShopListPanel : EditorEntityListPanel<Shop, ShopTemplate>
    {
        private const float IdColW   = 90f;
        private const float NameColW = 96f;
        private const float DescColW = 120f;
        private const float TypeColW = 64f;
        private const float GrpColW  = 48f;

        public ShopListPanel() : base("ShopListDrag") { }

        #region 列表配置

        protected override List<Shop>         Entities(InventoryDatabase db)  => db.Shops;
        protected override List<ShopTemplate> Templates(InventoryDatabase db) => db.ShopTemplates;
        protected override string TemplateName(ShopTemplate t) => t.name;
        protected override string TemplateRefOf(Shop e)        => e.templateRef;
        protected override string IdOf(Shop e)                 => e.id;
        protected override EInventoryEntityKind Kind           => EInventoryEntityKind.Shop;
        protected override string Noun                         => "商店";

        protected override Color RowDotColor(InventoryDatabase db, Shop e)
        {
            var tmpl = db.GetShopTemplate(e.templateRef);
            return tmpl != null ? tmpl.color : Color.gray;
        }

        protected override bool Matches(InventoryDatabase db, Shop shop, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(shop.id) &&
                shop.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string name = shop.displayNameText != null ? shop.displayNameText.GetTextValue() : null;
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region 行列布局

        protected override void DrawRowColumns(InventoryDatabase db, Shop shop,
            Rect keyRow, float cx, float contentRight, float vy, float vh)
        {
            // ── 上行：列名表头 ──────────────────────────────────────────────────
            float kx = cx;
            GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,   KeyRowH - 2), "ID",         KeyStyle); kx += IdColW   + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, NameColW, KeyRowH - 2), Tr("名称"),   KeyStyle); kx += NameColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, DescColW, KeyRowH - 2), Tr("描述"),   KeyStyle); kx += DescColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, TypeColW, KeyRowH - 2), Tr("类型"),   KeyStyle); kx += TypeColW + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, GrpColW,  KeyRowH - 2), Tr("商品组"), KeyStyle);

            // ── 下行：值 ────────────────────────────────────────────────────────
            GUI.Label(new Rect(cx, vy, IdColW, vh),
                string.IsNullOrWhiteSpace(shop.id) ? Tr("(空 ID)") : shop.id, IdStyle);
            cx += IdColW + Pad;

            string shopName = shop.displayNameText != null ? shop.displayNameText.GetTextValue() : null;
            GUI.Label(new Rect(cx, vy, NameColW, vh),
                string.IsNullOrEmpty(shopName) ? "—" : shopName, SubStyle);
            cx += NameColW + Pad;

            string shopDesc = shop.descriptionText != null ? shop.descriptionText.GetTextValue() : null;
            GUI.Label(new Rect(cx, vy, DescColW, vh),
                string.IsNullOrEmpty(shopDesc) ? "—" : shopDesc, SubStyle);
            cx += DescColW + Pad;

            GUI.Label(new Rect(cx, vy, TypeColW, vh), TrEnum(shop.shopType), SubStyle);
            cx += TypeColW + Pad;

            GUI.Label(new Rect(cx, vy, GrpColW, vh), shop.groups.Count.ToString(), SubStyle);
        }

        #endregion

        #region 新增

        protected override Shop AddFromTemplate(IInventoryEditorContext ctx, string templateName)
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

        protected override Shop QuickAdd(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            ctx.RecordUndo("快速添加商店");
            var shop = db.Shops[db.Shops.Count - 1].Clone();
            shop.id  = GenerateShopId(db);
            db.Shops.Add(shop);
            ctx.MarkDirty();
            return shop;
        }

        private string GenerateShopId(InventoryDatabase db)
            => GenerateId(db, "shop_", id => db.GetShop(id) != null);

        #endregion
    }
}

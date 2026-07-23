using Ale.Inventory.Runtime;
using UnityEditor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 商店 Inspector（右侧列）：商店身份（ID / 名称 / 来源模板）、共享可配置项（<see cref="ShopConfigDrawer"/>）、
    /// 自定义属性值。共享配置部分与「商店模板」复用同一套绘制。
    /// </summary>
    public class ShopInspectorPanel
    {
        public void DrawInspector(IInventoryEditorContext ctx, Shop shop)
        {
            if (shop == null)
            {
                EditorGUILayout.LabelField("请在中间列表选中一个商店。");
                return;
            }

            DrawBasic(ctx, shop);
            EditorGUILayout.Space(6);
            ShopConfigDrawer.DrawAll(ctx, shop);
            EditorGUILayout.Space(6);
            DrawCustomAttributes(ctx, shop);
        }

        // ── 商店身份（实例独有，不进模板）──────────────────────────────────────────

        private static void DrawBasic(IInventoryEditorContext ctx, Shop shop)
        {
            EditorGUILayout.LabelField("基础属性", InventoryEditorStyles.Header);

            EditorEntityHeader.DrawIdField(ctx, "商店", shop.id,
                ctx.DuplicateIdsOf(EInventoryEntityKind.Shop), v => shop.id = v);

            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器）
            AttributeFieldDrawer.Draw(ctx, "名称", shop.displayNameText, null);
            AttributeFieldDrawer.Draw(ctx, "描述", shop.descriptionText, null);

            EditorEntityHeader.DrawTemplateRefReadonly(shop.templateRef);
        }

        // ── 自定义属性 ──────────────────────────────────────────────────────────────

        private static void DrawCustomAttributes(IInventoryEditorContext ctx, Shop shop)
        {
            var template = ctx.Database.GetShopTemplate(shop.templateRef);
            EditorEntityHeader.DrawCustomAttributes(ctx, shop.values, template?.attributes,
                "（该商店暂无自定义属性字段；可在左侧「商店模板」中添加）");
        }
    }
}

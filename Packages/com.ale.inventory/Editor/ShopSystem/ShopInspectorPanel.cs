using InventorySystem.Runtime;
using UnityEditor;

namespace InventorySystem.Editor
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

            bool isDup = ctx.ShopDuplicateIds.Contains(
                string.IsNullOrWhiteSpace(shop.id) ? string.Empty : shop.id);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID");
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField(
                shop.id, isDup ? InventoryEditorStyles.RedField : EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改商店 ID");
                shop.id = newId;
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            if (isDup)
                EditorGUILayout.LabelField("⚠ ID 重复或为空", InventoryEditorStyles.StatusError);

            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器）
            AttributeFieldDrawer.Draw(ctx, "名称", shop.displayNameText, null);
            AttributeFieldDrawer.Draw(ctx, "描述", shop.descriptionText, null);

            using (new EditorGUI.DisabledScope(true))
            {
                string tmplDisplay = string.IsNullOrEmpty(shop.templateRef) ? "（无）" : shop.templateRef;
                EditorGUILayout.TextField("来源模板", tmplDisplay);
            }
        }

        // ── 自定义属性 ──────────────────────────────────────────────────────────────

        private static void DrawCustomAttributes(IInventoryEditorContext ctx, Shop shop)
        {
            var db = ctx.Database;

            EditorGUILayout.LabelField("自定义属性", InventoryEditorStyles.Header);

            if (shop.values.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "（该商店暂无自定义属性字段；可在左侧「商店模板」中添加）", EditorStyles.miniLabel);
                return;
            }

            var template = db.GetShopTemplate(shop.templateRef);
            foreach (var entry in shop.values)
            {
                AttributeDefinition def = null;
                if (template != null)
                    foreach (var d in template.attributes)
                        if (d.id == entry.id) { def = d; break; }

                var enumType = def != null && def.type == EFieldType.Enum
                    ? db.GetEnumType(def.enumTypeRef) : null;
                AttributeFieldDrawer.Draw(ctx, entry.id, entry.value, enumType);
            }
        }
    }
}

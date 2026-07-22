using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 装备组 Inspector（右侧列）：身份（ID / 名称 / 本地化 / 描述 / 来源模板）、共享可配置项
    /// （槽位列表 + 装备属性字段列表，<see cref="EquipmentConfigDrawer"/>）、来自模板的自定义属性值。
    /// 仿 <see cref="CraftingInspectorPanel"/>。
    /// </summary>
    public class EquipmentInspectorPanel
    {
        private readonly EquipmentConfigDrawer _config = new EquipmentConfigDrawer();

        public void DrawInspector(IInventoryEditorContext ctx, EquipmentGroup group)
        {
            if (group == null)
            {
                EditorGUILayout.LabelField("请在中间列表选中一个装备组。");
                return;
            }

            DrawBasic(ctx, group);
            EditorGUILayout.Space(6);
            _config.Draw(ctx, group);
            EditorGUILayout.Space(6);
            DrawCustomAttributes(ctx, group);
        }

        // ── 身份 ──────────────────────────────────────────────────────────────────

        private static void DrawBasic(IInventoryEditorContext ctx, EquipmentGroup group)
        {
            EditorGUILayout.LabelField("基础属性", InventoryEditorStyles.Header);

            bool isDup = ctx.EquipmentDuplicateIds.Contains(
                string.IsNullOrWhiteSpace(group.id) ? string.Empty : group.id);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID");
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField(
                group.id, isDup ? InventoryEditorStyles.RedField : EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改装备组 ID");
                group.id = newId;
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            if (isDup)
                EditorGUILayout.LabelField("⚠ ID 重复或为空", InventoryEditorStyles.StatusError);

            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器；名称为空时 UI 退回使用 ID）
            AttributeFieldDrawer.Draw(ctx, "名称", group.displayNameText, null);
            AttributeFieldDrawer.Draw(ctx, "描述", group.descriptionText, null);

            using (new EditorGUI.DisabledScope(true))
            {
                string tmplDisplay = string.IsNullOrEmpty(group.templateRef) ? "（无）" : group.templateRef;
                EditorGUILayout.TextField("来源模板", tmplDisplay);
            }
        }

        // ── 自定义属性 ──────────────────────────────────────────────────────────────

        private static void DrawCustomAttributes(IInventoryEditorContext ctx, EquipmentGroup group)
        {
            var db = ctx.Database;

            EditorGUILayout.LabelField("自定义属性", InventoryEditorStyles.Header);

            if (group.values.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "（该装备组暂无自定义属性字段；可在左侧「装备组模板」中添加）", EditorStyles.miniLabel);
                return;
            }

            var template = db.GetEquipmentGroupTemplate(group.templateRef);
            foreach (var entry in group.values)
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

using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

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
                EditorGUILayout.LabelField(Tr("请在中间列表选中一个装备组。"));
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
            EditorGUILayout.LabelField(Tr("基础属性"), InventoryEditorStyles.Header);

            EditorEntityHeader.DrawIdField(ctx, "装备组", group.id,
                ctx.DuplicateIdsOf(EInventoryEntityKind.Equipment), v => group.id = v);

            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器；名称为空时 UI 退回使用 ID）
            AttributeFieldDrawer.Draw(ctx, Tr("名称"), group.displayNameText, null);
            AttributeFieldDrawer.Draw(ctx, Tr("描述"), group.descriptionText, null);

            EditorEntityHeader.DrawTemplateRefReadonly(group.templateRef);
        }

        // ── 自定义属性 ──────────────────────────────────────────────────────────────

        private static void DrawCustomAttributes(IInventoryEditorContext ctx, EquipmentGroup group)
        {
            var template = ctx.Database.GetEquipmentGroupTemplate(group.templateRef);
            EditorEntityHeader.DrawCustomAttributes(ctx, group.values, template?.attributes,
                "（该装备组暂无自定义属性字段；可在左侧「装备组模板」中添加）");
        }
    }
}

using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 技能 Inspector（右侧列）：身份（ID / 名称 / 本地化 / 描述 / 图标 / 来源模板）、分组标签（主 + 副）、自定义属性值。
    /// 技能的类型 / 效果 / 数值等承载于自定义属性字段中（由使用方自行约定 attrId）。
    /// 名称 / 描述 / 图标 / 分组标签经 <see cref="SkillConfigDrawer"/> 绘制（与技能模板共用）。仿 <see cref="CraftingInspectorPanel"/>。
    /// </summary>
    public class SkillInspectorPanel
    {
        public void DrawInspector(IInventoryEditorContext ctx, Skill skill)
        {
            if (skill == null)
            {
                EditorGUILayout.LabelField(Tr("请在中间列表选中一个技能。"));
                return;
            }

            DrawBasic(ctx, skill);
            EditorGUILayout.Space(6);
            SkillConfigDrawer.DrawGroupTags(ctx, skill);
            EditorGUILayout.Space(6);
            DrawCustomAttributes(ctx, skill);
        }

        // ── 身份 ──────────────────────────────────────────────────────────────────

        private static void DrawBasic(IInventoryEditorContext ctx, Skill skill)
        {
            EditorGUILayout.LabelField(Tr("基础属性"), InventoryEditorStyles.Header);

            EditorEntityHeader.DrawIdField(ctx, "技能", skill.id,
                ctx.DuplicateIdsOf(EInventoryEntityKind.Skill), v => skill.id = v);

            // 名称 / 本地化名 / 描述 / 本地化描述 / 图标（与技能模板共用绘制）
            SkillConfigDrawer.DrawDisplayFields(ctx, skill);

            EditorEntityHeader.DrawTemplateRefReadonly(skill.templateRef);
        }

        // ── 自定义属性 ──────────────────────────────────────────────────────────────

        private static void DrawCustomAttributes(IInventoryEditorContext ctx, Skill skill)
        {
            var template = ctx.Database.GetSkillTemplate(skill.templateRef);
            EditorEntityHeader.DrawCustomAttributes(ctx, skill.values, template?.attributes,
                "（该技能暂无自定义属性字段；可在左侧「技能模板」中添加）");
        }
    }
}

using InventorySystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
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
                EditorGUILayout.LabelField("请在中间列表选中一个技能。");
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
            EditorGUILayout.LabelField("基础属性", InventoryEditorStyles.Header);

            bool isDup = ctx.SkillDuplicateIds.Contains(
                string.IsNullOrWhiteSpace(skill.id) ? string.Empty : skill.id);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID");
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField(
                skill.id, isDup ? InventoryEditorStyles.RedField : EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改技能 ID");
                skill.id = newId;
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            if (isDup)
                EditorGUILayout.LabelField("⚠ ID 重复或为空", InventoryEditorStyles.StatusError);

            // 名称 / 本地化名 / 描述 / 本地化描述 / 图标（与技能模板共用绘制）
            SkillConfigDrawer.DrawDisplayFields(ctx, skill);

            using (new EditorGUI.DisabledScope(true))
            {
                string tmplDisplay = string.IsNullOrEmpty(skill.templateRef) ? "（无）" : skill.templateRef;
                EditorGUILayout.TextField("来源模板", tmplDisplay);
            }
        }

        // ── 自定义属性 ──────────────────────────────────────────────────────────────

        private static void DrawCustomAttributes(IInventoryEditorContext ctx, Skill skill)
        {
            var db = ctx.Database;

            EditorGUILayout.LabelField("自定义属性", InventoryEditorStyles.Header);

            if (skill.values.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "（该技能暂无自定义属性字段；可在左侧「技能模板」中添加）", EditorStyles.miniLabel);
                return;
            }

            var template = db.GetSkillTemplate(skill.templateRef);
            foreach (var entry in skill.values)
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

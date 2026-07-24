using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 技能模板面板：左侧主列表（模板行，可拖拽排序）+ 右侧 Inspector（名称、颜色、自定义属性字段 schema）。
    /// 技能模板仅定义自定义属性字段（技能的类型 / 效果 / 数值等由使用方自行约定 attrId 存放）。仿 <see cref="CraftingTemplatePanel"/>。
    /// </summary>
    public class SkillTemplatePanel : EditorMasterListPanel<SkillTemplate>
    {
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        #region 主列表配置

        protected override List<SkillTemplate> GetList(InventoryDatabase db) => db.SkillTemplates;
        protected override string Noun        => "技能模板";
        protected override bool   HasColorDot => true;
        protected override Color  RowColor(SkillTemplate t) => t.color;
        protected override string RowLabel(SkillTemplate t) => t.name;

        protected override SkillTemplate CreateNew(InventoryDatabase db, List<SkillTemplate> list)
            => new SkillTemplate(Tr("新技能模板"));

        protected override void OnInvalidate() => _attrDefsDrawer.Invalidate();

        #endregion

        // ── Inspector ────────────────────────────────────────────────────────────

        public override void DrawInspector(IInventoryEditorContext ctx, SkillTemplate template)
        {
            if (template == null)
            {
                EditorGUILayout.LabelField(Tr("请选择或新建一个技能模板。"));
                return;
            }

            EditorGUI.BeginChangeCheck();
            string newName  = EditorGUILayout.TextField(Tr("模板名称"), template.name);
            Color  newColor = EditorGUILayout.ColorField(Tr("标识颜色"), template.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改技能模板基本信息");
                template.name  = newName;
                template.color = newColor;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // 技能默认信息（从模板创建技能时复制到新技能，之后可在技能条目上独立修改）
            EditorGUILayout.LabelField(Tr("技能默认信息（从模板创建时复制）"), InventoryEditorStyles.Header);
            SkillConfigDrawer.DrawDisplayFields(ctx, template);

            EditorGUILayout.Space(6);
            SkillConfigDrawer.DrawGroupTags(ctx, template);

            EditorGUILayout.Space(6);
            _attrDefsDrawer.Draw(ctx, template.attributes, Tr("自定义属性字段"));
        }
    }
}

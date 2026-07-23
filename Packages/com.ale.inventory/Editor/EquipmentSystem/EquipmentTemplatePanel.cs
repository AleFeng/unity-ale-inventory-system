using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 装备组模板面板：左侧主列表（模板行，可拖拽排序）+ 右侧 Inspector（名称、颜色、自定义属性字段定义）。
    /// 装备组模板为轻量模板，仅持有自定义属性字段 schema（结构性配置由装备组自身持有）。仿 <see cref="CraftingTemplatePanel"/>。
    /// </summary>
    public class EquipmentTemplatePanel : EditorMasterListPanel<EquipmentGroupTemplate>
    {
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();
        private readonly EquipmentConfigDrawer         _configDrawer  = new EquipmentConfigDrawer();

        #region 主列表配置

        protected override List<EquipmentGroupTemplate> GetList(InventoryDatabase db) => db.EquipmentGroupTemplates;
        protected override string Noun => "装备组模板";
        protected override string RowLabel(EquipmentGroupTemplate t) => t.name;
        protected override bool   HasColorDot => true;
        protected override Color  RowColor(EquipmentGroupTemplate t) => t.color;

        protected override EquipmentGroupTemplate CreateNew(InventoryDatabase db, List<EquipmentGroupTemplate> list)
            => new EquipmentGroupTemplate("新装备组模板");

        protected override void OnInvalidate() => _attrDefsDrawer.Invalidate();

        #endregion

        // ── Inspector ────────────────────────────────────────────────────────────

        public override void DrawInspector(IInventoryEditorContext ctx, EquipmentGroupTemplate template)
        {
            if (template == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个装备组模板。");
                return;
            }

            EditorGUI.BeginChangeCheck();
            string newName  = EditorGUILayout.TextField("模板名称", template.name);
            Color  newColor = EditorGUILayout.ColorField("标识颜色", template.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改装备组模板基本信息");
                template.name  = newName;
                template.color = newColor;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // 共享可配置项（与装备组一致：槽位列表 + 装备属性字段列表 + 整理排序），作为创建装备组时复制的默认值。
            _configDrawer.Draw(ctx, template);

            EditorGUILayout.Space(6);

            _attrDefsDrawer.Draw(ctx, template.attributes, "自定义属性字段");
        }
    }
}

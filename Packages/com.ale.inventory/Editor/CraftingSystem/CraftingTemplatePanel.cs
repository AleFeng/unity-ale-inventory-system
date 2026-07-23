using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 蓝图模板面板：左侧主列表（模板行，可拖拽排序）+ 右侧 Inspector（名称、颜色、共享配置、自定义属性字段）。
    /// 仿 <see cref="ShopTemplatePanel"/>。
    /// </summary>
    public class CraftingTemplatePanel : EditorMasterListPanel<CraftingBlueprintTemplate>
    {
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        #region 主列表配置

        protected override List<CraftingBlueprintTemplate> GetList(InventoryDatabase db) => db.CraftingBlueprintTemplates;
        protected override string Noun        => "蓝图模板";
        protected override bool   HasColorDot => true;
        protected override Color  RowColor(CraftingBlueprintTemplate t) => t.color;
        protected override string RowLabel(CraftingBlueprintTemplate t) => t.name;

        protected override CraftingBlueprintTemplate CreateNew(InventoryDatabase db, List<CraftingBlueprintTemplate> list)
            => new CraftingBlueprintTemplate("新蓝图模板");

        protected override void OnInvalidate() => _attrDefsDrawer.Invalidate();

        #endregion

        // ── Inspector ────────────────────────────────────────────────────────────

        public void DrawInspector(IInventoryEditorContext ctx, CraftingBlueprintTemplate template)
        {
            if (template == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个蓝图模板。");
                return;
            }

            EditorGUI.BeginChangeCheck();
            string newName  = EditorGUILayout.TextField("模板名称", template.name);
            Color  newColor = EditorGUILayout.ColorField("标识颜色", template.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改蓝图模板基本信息");
                template.name  = newName;
                template.color = newColor;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // 共享可配置项（与蓝图实例一致：制作参数 / 制作仓库 / UI 配置）
            CraftingConfigDrawer.DrawAll(ctx, template);

            EditorGUILayout.Space(6);

            // 整理设置：仅模板持有，作为该模板下所有蓝图在 UI 列表中的排序依据（蓝图条目不再单独配置）。
            EditorGUILayout.LabelField("整理设置", InventoryEditorStyles.Header);
            EditorGUILayout.LabelField("此模板下所有蓝图在 UI 列表中按「整理列表」的配置与优先级排序。",
                EditorStyles.miniLabel);
            SortSettingsDrawer.Draw(ctx, template.sortPriorities, template.sortTiebreakers);

            EditorGUILayout.Space(6);

            _attrDefsDrawer.Draw(ctx, template.attributes, "自定义属性字段");
        }
    }
}

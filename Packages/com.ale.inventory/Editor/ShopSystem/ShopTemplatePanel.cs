using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 商店模板面板：左侧主列表（模板行，可拖拽排序）+ 右侧 Inspector（名称、颜色、自定义属性字段）。
    /// 仿 <see cref="InventoryTemplatePanel"/>，但商店模板仅含名称/颜色/属性字段。
    /// </summary>
    public class ShopTemplatePanel : EditorMasterListPanel<ShopTemplate>
    {
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        #region 主列表配置

        protected override List<ShopTemplate> GetList(InventoryDatabase db) => db.ShopTemplates;
        protected override string Noun        => "商店模板";
        protected override bool   HasColorDot => true;
        protected override Color  RowColor(ShopTemplate t) => t.color;
        protected override string RowLabel(ShopTemplate t) => t.name;

        protected override ShopTemplate CreateNew(InventoryDatabase db, List<ShopTemplate> list)
            => new ShopTemplate(Tr("新商店模板"));

        protected override void OnInvalidate() => _attrDefsDrawer.Invalidate();

        #endregion

        // ── Inspector ────────────────────────────────────────────────────────────

        public override void DrawInspector(IInventoryEditorContext ctx, ShopTemplate template)
        {
            if (template == null)
            {
                EditorGUILayout.LabelField(Tr("请选择或新建一个商店模板。"));
                return;
            }

            EditorGUI.BeginChangeCheck();
            string newName  = EditorGUILayout.TextField(Tr("模板名称"), template.name);
            Color  newColor = EditorGUILayout.ColorField(Tr("标识颜色"), template.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改商店模板基本信息");
                template.name  = newName;
                template.color = newColor;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // 共享可配置项（与商店实例一致：类型 / 交易仓库 / 过滤 / UI 配置 / 商品组）
            ShopConfigDrawer.DrawAll(ctx, template);

            EditorGUILayout.Space(6);

            _attrDefsDrawer.Draw(ctx, template.attributes, Tr("自定义属性字段"));
        }
    }
}

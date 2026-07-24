using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 道具模板面板：左侧主列表（模板行，可拖拽排序）+ 右侧 Inspector（名称、颜色、属性字段定义列表）。
    /// </summary>
    public class ItemTemplatePanel : EditorMasterListPanel<ItemTemplate>
    {
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        #region 主列表配置

        protected override List<ItemTemplate> GetList(InventoryDatabase db) => db.ItemTemplates;
        protected override string Noun => "道具模板";
        protected override string RowLabel(ItemTemplate t) => t.name;
        protected override bool   HasColorDot => true;
        protected override Color  RowColor(ItemTemplate t) => t.color;

        protected override ItemTemplate CreateNew(InventoryDatabase db, List<ItemTemplate> list)
            => new ItemTemplate(Tr("新模板"));

        protected override void OnInvalidate() => _attrDefsDrawer.Invalidate();

        #endregion

        // ── Inspector ────────────────────────────────────────────────────────────

        public override void DrawInspector(IInventoryEditorContext ctx, ItemTemplate template)
        {
            if (template == null)
            {
                EditorGUILayout.LabelField(Tr("请选择或新建一个道具模板。"));
                return;
            }

            var db = ctx.Database;

            // ── 模板名称 + 颜色 ──────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string newName  = EditorGUILayout.TextField(Tr("模板名称"), template.name);
            Color  newColor = EditorGUILayout.ColorField(Tr("标识颜色"), template.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改模板基本信息");
                template.name  = newName;
                template.color = newColor;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 默认功能标签 ─────────────────────────────────────────────────
            EditorGUILayout.LabelField(Tr("默认功能标签"), InventoryEditorStyles.Header);
            EditorTagToggleList.Draw(ctx, template.tagRefs,
                "模板添加功能标签", "模板移除功能标签",
                emptyHint: Tr("（暂无功能标签，请先在左侧「功能标签」中创建）"));

            EditorGUILayout.Space(6);

            // ── 仓库属性 ─────────────────────────────────────────────────────
            EditorGUILayout.LabelField(Tr("仓库属性"), InventoryEditorStyles.Header);
            EditorGUI.BeginChangeCheck();
            float newWeight     = EditorGUILayout.FloatField(Tr("重量"), template.weight);
            int   newStackLimit = EditorGUILayout.IntField(Tr("堆叠上限"), template.stackLimit);
            bool  newHideInInventory = EditorGUILayout.Toggle(Tr("仓库中隐藏"), template.hideInInventory);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改模板仓库属性");
                template.weight     = Mathf.Max(0f, newWeight);
                template.stackLimit = Mathf.Max(0, newStackLimit);
                template.hideInInventory = newHideInInventory;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 属性字段定义 ─────────────────────────────────────────────────
            _attrDefsDrawer.Draw(ctx, template.attributes, Tr("属性字段"));
        }
    }
}

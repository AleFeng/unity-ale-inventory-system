using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 功能标签面板：左侧主列表（功能标签行，可拖拽排序）+ 右侧 Inspector（名称、说明、属性字段定义列表）。
    /// 功能标签的顺序会影响「整理列表」中「功能标签」的排序依据。
    /// </summary>
    public class FunctionTagPanel : EditorMasterListPanel<FunctionTag>
    {
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        #region 主列表配置

        protected override List<FunctionTag> GetList(InventoryDatabase db) => db.FunctionTags;
        protected override string Noun => "功能标签";
        protected override string RowLabel(FunctionTag t) => t.name;

        protected override FunctionTag CreateNew(InventoryDatabase db, List<FunctionTag> list)
            => new FunctionTag("新标签");

        protected override void OnInvalidate() => _attrDefsDrawer.Invalidate();

        #endregion

        // ── Inspector ────────────────────────────────────────────────────────────

        public void DrawInspector(IInventoryEditorContext ctx, FunctionTag tag)
        {
            if (tag == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个功能标签。");
                return;
            }

            // ── 基础信息 ─────────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string name = EditorGUILayout.TextField("标签ID", tag.name);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改功能标签");
                tag.name = name;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 功能标签属性（UI 显示配置）──────────────────────────────────────
            EditorGUILayout.LabelField("功能标签属性", InventoryEditorStyles.Header);

            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器）
            AttributeFieldDrawer.Draw(ctx, "名称", tag.displayNameText, null);
            AttributeFieldDrawer.Draw(ctx, "描述", tag.descriptionText, null);

            // 背景图：直接模式 ObjectField / 授权模式原生 AssetReference 选择器
            if (InventoryAssetRefField.DrawSprite("背景图", tag, "tagBg", tag.backgroundSprite, tag.backgroundSpriteAddress,
                    out var newBg, out var newBgAddr))
            {
                ctx.RecordUndo("修改功能标签背景图");
                tag.backgroundSprite        = newBg;
                tag.backgroundSpriteAddress = newBgAddr;
                ctx.MarkDirty();
            }

            EditorGUI.BeginChangeCheck();
            var bgColor   = EditorGUILayout.ColorField("背景颜色", tag.backgroundColor);
            bool hideInUI = EditorGUILayout.Toggle("UI中隐藏", tag.hideInUI);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改功能标签属性");
                tag.backgroundColor = bgColor;
                tag.hideInUI        = hideInUI;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 道具属性字段 ──────────────────────────────────────────────────────
            _attrDefsDrawer.Draw(ctx, tag.attributes, "道具属性字段");
            EditorGUILayout.HelpBox("附加到道具后，会自动添加至道具的「属性字段」列表中", MessageType.None);
        }
    }
}

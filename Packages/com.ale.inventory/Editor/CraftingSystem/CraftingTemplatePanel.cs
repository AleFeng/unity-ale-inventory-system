using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 蓝图模板面板：左侧主列表（模板行，可拖拽排序）+ 右侧 Inspector（名称、颜色、共享配置、自定义属性字段）。
    /// 仿 <see cref="ShopTemplatePanel"/>。
    /// </summary>
    public class CraftingTemplatePanel
    {
        private ReorderableList               _masterList;
        private List<CraftingBlueprintTemplate> _boundMasterList;
        private int                           _masterSelectedIndex = -1;
        private int                           _pendingDeleteIndex  = -1;
        private IInventoryEditorContext       _masterCtx;

        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        // ── 主列表 ────────────────────────────────────────────────────────────────

        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            var db   = ctx.Database;
            var list = db.CraftingBlueprintTemplates;
            _masterCtx = ctx;

            if (_masterList == null || !ReferenceEquals(_boundMasterList, list))
            {
                _masterSelectedIndex = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                BuildMasterList(list);
            }
            else
            {
                int clamped = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                if (_masterSelectedIndex != clamped)
                {
                    _masterSelectedIndex = clamped;
                    _masterList.index    = clamped;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("蓝图模板", InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加蓝图模板");
                list.Add(new CraftingBlueprintTemplate("新蓝图模板"));
                ctx.MarkDirty();
                _masterSelectedIndex = list.Count - 1;
                _masterList.index    = _masterSelectedIndex;
            }
            EditorGUILayout.EndHorizontal();

            _masterList.DoLayoutList();

            if (_pendingDeleteIndex >= 0)
            {
                int di = _pendingDeleteIndex;
                _pendingDeleteIndex = -1;
                if (di < list.Count)
                {
                    ctx.RecordUndo("删除蓝图模板");
                    list.RemoveAt(di);
                    ctx.MarkDirty();
                    _masterSelectedIndex = Mathf.Clamp(_masterSelectedIndex, -1, list.Count - 1);
                    _masterList.index    = _masterSelectedIndex;
                }
            }

            return _masterSelectedIndex;
        }

        private void BuildMasterList(List<CraftingBlueprintTemplate> list)
        {
            _boundMasterList = list;
            _masterList = new ReorderableList(list, typeof(CraftingBlueprintTemplate),
                draggable: true, displayHeader: false,
                displayAddButton: false, displayRemoveButton: false);

            _masterList.elementHeight = 22f;
            _masterList.index         = _masterSelectedIndex;

            _masterList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (active)
                    InventoryEditorStyles.DrawRowBackground(rect, InventoryEditorStyles.SelectedColor);
            };

            _masterList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= list.Count) return;
                var t    = list[index];
                float cy = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;

                var dotRect = new Rect(rect.x, cy, 14f, EditorGUIUtility.singleLineHeight);
                InventoryEditorStyles.DrawColorDot(dotRect, t.color);

                var delRect   = new Rect(rect.xMax - 22, cy, 20f, EditorGUIUtility.singleLineHeight);
                var labelRect = new Rect(dotRect.xMax + 3, cy,
                    rect.xMax - 22 - dotRect.xMax - 7, EditorGUIUtility.singleLineHeight);
                GUI.Label(labelRect, t.name);

                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    _pendingDeleteIndex = index;
            };

            _masterList.onSelectCallback = rl => _masterSelectedIndex = rl.index;

            _masterList.onReorderCallback = _ =>
            {
                _masterCtx.RecordUndo("调整蓝图模板顺序");
                _masterCtx.MarkDirty();
            };
        }

        public void Invalidate()
        {
            _masterList          = null;
            _boundMasterList     = null;
            _masterSelectedIndex = -1;
            _pendingDeleteIndex  = -1;
            _attrDefsDrawer.Invalidate();
        }

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

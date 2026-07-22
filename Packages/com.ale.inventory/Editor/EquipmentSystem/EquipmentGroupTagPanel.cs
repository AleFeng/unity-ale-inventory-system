using System.Collections.Generic;
using InventorySystem.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 装备-分组标签面板：左侧主列表（分组标签行，可拖拽排序）+ 右侧 Inspector（ID / 名称 / 描述 / 本地化 / 色点）。
    /// 分组标签用于对装备组的「装备属性字段」条目分组显示，仅承载基础信息。仿 <see cref="CraftingGroupTagPanel"/>。
    /// </summary>
    public class EquipmentGroupTagPanel
    {
        private ReorderableList         _masterList;
        private List<EquipmentGroupTag> _boundList;
        private int                     _selectedIndex      = -1;
        private int                     _pendingDeleteIndex = -1;
        private IInventoryEditorContext _masterCtx;

        // ── 主列表 ────────────────────────────────────────────────────────────────

        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            var db   = ctx.Database;
            var list = db.EquipmentGroupTags;
            _masterCtx = ctx;

            if (_masterList == null || !ReferenceEquals(_boundList, list))
            {
                _selectedIndex = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                BuildMasterList(list);
            }
            else
            {
                int clamped = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                if (_selectedIndex != clamped)
                {
                    _selectedIndex    = clamped;
                    _masterList.index = clamped;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("分组标签", InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加分组标签");
                list.Add(new EquipmentGroupTag(GenerateId(db), "新分组"));
                ctx.MarkDirty();
                _selectedIndex    = list.Count - 1;
                if (_masterList != null) _masterList.index = _selectedIndex;
            }
            EditorGUILayout.EndHorizontal();

            if (_masterList != null)
            {
                _masterList.DoLayoutList();

                if (_pendingDeleteIndex >= 0)
                {
                    int di = _pendingDeleteIndex;
                    _pendingDeleteIndex = -1;
                    if (di < list.Count)
                    {
                        ctx.RecordUndo("删除分组标签");
                        list.RemoveAt(di);
                        ctx.MarkDirty();
                        _selectedIndex = Mathf.Clamp(_selectedIndex, -1, list.Count - 1);
                        _masterList.index = _selectedIndex;
                    }
                }
            }

            return _selectedIndex;
        }

        private void BuildMasterList(List<EquipmentGroupTag> list)
        {
            _boundList  = list;
            _masterList = new ReorderableList(list, typeof(EquipmentGroupTag),
                draggable: true, displayHeader: false,
                displayAddButton: false, displayRemoveButton: false);

            _masterList.elementHeight = 22f;
            _masterList.index         = _selectedIndex;

            _masterList.drawElementBackgroundCallback = (rect, _, active, _) =>
            {
                if (active)
                    InventoryEditorStyles.DrawRowBackground(rect, InventoryEditorStyles.SelectedColor);
            };

            _masterList.drawElementCallback = (rect, index, _, _) =>
            {
                if (index < 0 || index >= list.Count) return;
                var t    = list[index];
                float cy = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;

                var dotRect = new Rect(rect.x, cy, 14f, EditorGUIUtility.singleLineHeight);
                InventoryEditorStyles.DrawColorDot(dotRect, t.color);

                var delRect   = new Rect(rect.xMax - 22, cy, 20f, EditorGUIUtility.singleLineHeight);
                var labelRect = new Rect(dotRect.xMax + 3, cy,
                    rect.xMax - 22 - dotRect.xMax - 7, EditorGUIUtility.singleLineHeight);
                GUI.Label(labelRect, string.IsNullOrEmpty(t.id) ? "(空 ID)" : t.id);

                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    _pendingDeleteIndex = index;
            };

            _masterList.onSelectCallback = rl => _selectedIndex = rl.index;

            _masterList.onReorderCallback = _ =>
            {
                _masterCtx.RecordUndo("调整分组标签顺序");
                _masterCtx.MarkDirty();
            };
        }

        public void Invalidate()
        {
            _masterList         = null;
            _boundList          = null;
            _selectedIndex      = -1;
            _pendingDeleteIndex = -1;
        }

        // ── Inspector ────────────────────────────────────────────────────────────

        public void DrawInspector(IInventoryEditorContext ctx, EquipmentGroupTag tag)
        {
            if (tag == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个分组标签。");
                return;
            }

            EditorGUILayout.LabelField("基础信息", InventoryEditorStyles.Header);

            EditorGUI.BeginChangeCheck();
            string id    = EditorGUILayout.TextField("ID", tag.id);
            Color  color = EditorGUILayout.ColorField("标识颜色", tag.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改分组标签");
                tag.id    = id;
                tag.color = color;
                ctx.MarkDirty();
            }

            // 名称 / 描述为 Text 类型属性值（纯文本 fallback + 可选本地化引用），复用统一属性绘制器。
            tag.NormalizeTextFields();
            AttributeFieldDrawer.Draw(ctx, "名称", tag.displayName, null);
            AttributeFieldDrawer.Draw(ctx, "描述", tag.description, null);
        }

        private static string GenerateId(InventoryDatabase db)
        {
            int n = db.EquipmentGroupTags.Count + 1;
            string id;
            do { id = "equip_tag_" + n; n++; }
            while (db.GetEquipmentGroupTag(id) != null);
            return id;
        }
    }
}

using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「制作系统」页签：三列布局，与商店系统页签对称。
    /// 左列 = 子页签（分组标签 / 蓝图模板）+ 主列表；中列 = 蓝图列表；右列 = 上下文 Inspector。
    /// </summary>
    public class CraftingSystemTab
    {
        private const float LeftWidth      = 260f;
        private const float MiddleWidthMin = 320f;
        private const float RightWidth     = 380f;
        private const float Padding        = 4f;

        private static readonly string[] LeftSubTabs = { "分组标签", "蓝图模板" };

        private enum RightMode { Entity, Blueprint }

        private int               _leftSubTab            = 0;
        private int               _selectedGroupTagIndex = -1;
        private int               _selectedTemplateIndex = -1;
        private CraftingBlueprint _selectedBlueprint     = null;
        private RightMode         _rightMode             = RightMode.Entity;

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        private bool _pendingDeleteBlueprint;

        private readonly CraftingGroupTagPanel  _groupTagPanel  = new CraftingGroupTagPanel();
        private readonly CraftingTemplatePanel  _templatePanel  = new CraftingTemplatePanel();
        private readonly CraftingListPanel      _listPanel      = new CraftingListPanel();
        private readonly CraftingInspectorPanel _inspectorPanel = new CraftingInspectorPanel();

        private void ActivateEntity()
        {
            _selectedBlueprint = null;
            _rightMode         = RightMode.Entity;
        }

        private void ActivateBlueprint(CraftingBlueprint bp)
        {
            _selectedGroupTagIndex = -1;
            _selectedTemplateIndex = -1;
            _selectedBlueprint     = bp;
            _rightMode             = bp == null ? RightMode.Entity : RightMode.Blueprint;
        }

        public void OnDatabaseChanged(IInventoryEditorContext ctx)
        {
            _selectedGroupTagIndex = -1;
            _selectedTemplateIndex = -1;
            _selectedBlueprint     = null;
            _rightMode             = RightMode.Entity;
            _groupTagPanel.Invalidate();
            _templatePanel.Invalidate();
        }

        public void OnUndoRedo()
        {
            _groupTagPanel.Invalidate();
            _templatePanel.Invalidate();
        }

        public void OnGUI(Rect rect, IInventoryEditorContext ctx)
        {
            if (Event.current.type == EventType.Layout)
            {
                if (_pendingDeleteBlueprint)
                {
                    _pendingDeleteBlueprint = false;
                    if (_selectedBlueprint != null && ctx.Database.CraftingBlueprints.Contains(_selectedBlueprint))
                    {
                        ctx.RecordUndo("删除蓝图");
                        ctx.Database.CraftingBlueprints.Remove(_selectedBlueprint);
                        ctx.MarkDirty();
                    }
                    ActivateEntity();
                }

                var pending = _listPanel.ConsumePendingSelect();
                if (pending != null)
                    ActivateBlueprint(pending);
            }

            float middleWidth = Mathf.Max(MiddleWidthMin,
                rect.width - LeftWidth - RightWidth - Padding * 4);

            var leftRect   = new Rect(rect.x + Padding,          rect.y + Padding, LeftWidth,   rect.height - Padding * 2);
            var middleRect = new Rect(leftRect.xMax + Padding,   rect.y + Padding, middleWidth, rect.height - Padding * 2);
            var rightRect  = new Rect(middleRect.xMax + Padding, rect.y + Padding,
                rect.width - middleRect.xMax - Padding * 2, rect.height - Padding * 2);

            DrawLeft(leftRect, ctx);
            DrawMiddle(middleRect, ctx);
            DrawRight(rightRect, ctx);
        }

        private void DrawLeft(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            int prev = _leftSubTab;
            _leftSubTab = GUILayout.Toolbar(_leftSubTab, LeftSubTabs);
            if (_leftSubTab != prev)
            {
                _selectedGroupTagIndex = -1;
                _selectedTemplateIndex = -1;
                _selectedBlueprint     = null;
                _rightMode             = RightMode.Entity;
            }

            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            if (_leftSubTab == 0)
            {
                int sel = _groupTagPanel.DrawMasterList(ctx, _selectedGroupTagIndex);
                if (sel != _selectedGroupTagIndex)
                {
                    _selectedGroupTagIndex = sel;
                    ActivateEntity();
                }
            }
            else
            {
                int sel = _templatePanel.DrawMasterList(ctx, _selectedTemplateIndex);
                if (sel != _selectedTemplateIndex)
                {
                    _selectedTemplateIndex = sel;
                    ActivateEntity();
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMiddle(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            CraftingBlueprint displaySelected = _rightMode == RightMode.Blueprint ? _selectedBlueprint : null;
            CraftingBlueprint sel = _listPanel.DrawList(ctx, displaySelected);

            if (sel != displaySelected)
                ActivateBlueprint(sel);

            GUILayout.EndArea();
        }

        private void DrawRight(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            bool drawBlueprint = _rightMode == RightMode.Blueprint
                && _selectedBlueprint != null
                && ctx.Database.CraftingBlueprints.Contains(_selectedBlueprint);

            if (!drawBlueprint && _rightMode == RightMode.Blueprint)
                ActivateEntity();

            if (drawBlueprint)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("蓝图 Inspector", InventoryEditorStyles.Header);
                var delStyle = new GUIStyle(EditorStyles.miniButton)
                    { normal = { textColor = new Color(0.9f, 0.45f, 0.45f) } };
                if (GUILayout.Button("删除蓝图", delStyle, GUILayout.Width(64)))
                    _pendingDeleteBlueprint = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            // 隐藏横向滚动条：内容自适应填满 Inspector 宽度。
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, false, false,
                GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);

            if (drawBlueprint)
            {
                _inspectorPanel.DrawInspector(ctx, _selectedBlueprint);
            }
            else if (_leftSubTab == 0)
            {
                var db = ctx.Database;
                CraftingGroupTag selected = null;
                if (_selectedGroupTagIndex >= 0 && _selectedGroupTagIndex < db.CraftingGroupTags.Count)
                    selected = db.CraftingGroupTags[_selectedGroupTagIndex];
                _groupTagPanel.DrawInspector(ctx, selected);
            }
            else
            {
                var db = ctx.Database;
                CraftingBlueprintTemplate selected = null;
                if (_selectedTemplateIndex >= 0 && _selectedTemplateIndex < db.CraftingBlueprintTemplates.Count)
                    selected = db.CraftingBlueprintTemplates[_selectedTemplateIndex];
                _templatePanel.DrawInspector(ctx, selected);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

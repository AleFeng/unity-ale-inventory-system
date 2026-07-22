using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「装备系统」页签：三列布局，与制作 / 商店系统页签对称。
    /// 左列 = 子页签（分组标签 / 装备组模板）+ 主列表；中列 = 装备组列表；右列 = 上下文 Inspector。
    /// </summary>
    public class EquipmentSystemTab
    {
        private const float LeftWidth      = 260f;
        private const float MiddleWidthMin = 320f;
        private const float RightWidth     = 380f;
        private const float Padding        = 4f;

        private static readonly string[] LeftSubTabs = { "分组标签", "装备组模板" };

        private enum RightMode { Entity, Group }

        private int            _leftSubTab            = 0;
        private int            _selectedGroupTagIndex = -1;
        private int            _selectedTemplateIndex = -1;
        private EquipmentGroup _selectedGroup         = null;
        private RightMode      _rightMode             = RightMode.Entity;

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        private bool _pendingDeleteGroup;

        private readonly EquipmentGroupTagPanel  _groupTagPanel  = new EquipmentGroupTagPanel();
        private readonly EquipmentTemplatePanel  _templatePanel  = new EquipmentTemplatePanel();
        private readonly EquipmentListPanel      _listPanel      = new EquipmentListPanel();
        private readonly EquipmentInspectorPanel _inspectorPanel = new EquipmentInspectorPanel();

        private void ActivateEntity()
        {
            _selectedGroup = null;
            _rightMode     = RightMode.Entity;
        }

        private void ActivateGroup(EquipmentGroup g)
        {
            _selectedGroupTagIndex = -1;
            _selectedTemplateIndex = -1;
            _selectedGroup         = g;
            _rightMode             = g == null ? RightMode.Entity : RightMode.Group;
        }

        public void OnDatabaseChanged(IInventoryEditorContext ctx)
        {
            _selectedGroupTagIndex = -1;
            _selectedTemplateIndex = -1;
            _selectedGroup         = null;
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
                if (_pendingDeleteGroup)
                {
                    _pendingDeleteGroup = false;
                    if (_selectedGroup != null && ctx.Database.EquipmentGroups.Contains(_selectedGroup))
                    {
                        ctx.RecordUndo("删除装备组");
                        ctx.Database.EquipmentGroups.Remove(_selectedGroup);
                        ctx.MarkDirty();
                    }
                    ActivateEntity();
                }

                var pending = _listPanel.ConsumePendingSelect();
                if (pending != null)
                    ActivateGroup(pending);
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
                _selectedGroup         = null;
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

            EquipmentGroup displaySelected = _rightMode == RightMode.Group ? _selectedGroup : null;
            EquipmentGroup sel = _listPanel.DrawList(ctx, displaySelected);

            if (sel != displaySelected)
                ActivateGroup(sel);

            GUILayout.EndArea();
        }

        private void DrawRight(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            bool drawGroup = _rightMode == RightMode.Group
                && _selectedGroup != null
                && ctx.Database.EquipmentGroups.Contains(_selectedGroup);

            if (!drawGroup && _rightMode == RightMode.Group)
                ActivateEntity();

            if (drawGroup)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("装备组 Inspector", InventoryEditorStyles.Header);
                var delStyle = new GUIStyle(EditorStyles.miniButton)
                    { normal = { textColor = new Color(0.9f, 0.45f, 0.45f) } };
                if (GUILayout.Button("删除装备组", delStyle, GUILayout.Width(72)))
                    _pendingDeleteGroup = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            // 隐藏横向滚动条：内容自适应填满 Inspector 宽度。
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, false, false,
                GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);

            if (drawGroup)
            {
                _inspectorPanel.DrawInspector(ctx, _selectedGroup);
            }
            else if (_leftSubTab == 0)
            {
                var db = ctx.Database;
                EquipmentGroupTag selected = null;
                if (_selectedGroupTagIndex >= 0 && _selectedGroupTagIndex < db.EquipmentGroupTags.Count)
                    selected = db.EquipmentGroupTags[_selectedGroupTagIndex];
                _groupTagPanel.DrawInspector(ctx, selected);
            }
            else
            {
                var db = ctx.Database;
                EquipmentGroupTemplate selected = null;
                if (_selectedTemplateIndex >= 0 && _selectedTemplateIndex < db.EquipmentGroupTemplates.Count)
                    selected = db.EquipmentGroupTemplates[_selectedTemplateIndex];
                _templatePanel.DrawInspector(ctx, selected);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

using InventorySystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 「技能系统」页签：三列布局，与制作系统页签对称。
    /// 左列 = 子页签（分组标签 / 技能模板）+ 主列表；中列 = 技能列表；右列 = 上下文 Inspector。
    /// </summary>
    public class SkillSystemTab
    {
        private const float LeftWidth      = 260f;
        private const float MiddleWidthMin = 320f;
        private const float RightWidth     = 380f;
        private const float Padding        = 4f;

        private static readonly string[] LeftSubTabs = { "分组标签", "技能模板" };

        private enum RightMode { Entity, Skill }

        private int       _leftSubTab            = 0;
        private int       _selectedGroupTagIndex = -1;
        private int       _selectedTemplateIndex = -1;
        private Skill     _selectedSkill         = null;
        private RightMode _rightMode             = RightMode.Entity;

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        private bool _pendingDeleteSkill;

        private readonly SkillGroupTagPanel  _groupTagPanel  = new SkillGroupTagPanel();
        private readonly SkillTemplatePanel  _templatePanel  = new SkillTemplatePanel();
        private readonly SkillListPanel      _listPanel      = new SkillListPanel();
        private readonly SkillInspectorPanel _inspectorPanel = new SkillInspectorPanel();

        private void ActivateEntity()
        {
            _selectedSkill = null;
            _rightMode     = RightMode.Entity;
        }

        private void ActivateSkill(Skill skill)
        {
            _selectedGroupTagIndex = -1;
            _selectedTemplateIndex = -1;
            _selectedSkill         = skill;
            _rightMode             = skill == null ? RightMode.Entity : RightMode.Skill;
        }

        public void OnDatabaseChanged(IInventoryEditorContext ctx)
        {
            _selectedGroupTagIndex = -1;
            _selectedTemplateIndex = -1;
            _selectedSkill         = null;
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
                if (_pendingDeleteSkill)
                {
                    _pendingDeleteSkill = false;
                    if (_selectedSkill != null && ctx.Database.Skills.Contains(_selectedSkill))
                    {
                        ctx.RecordUndo("删除技能");
                        ctx.Database.Skills.Remove(_selectedSkill);
                        ctx.MarkDirty();
                    }
                    ActivateEntity();
                }

                var pending = _listPanel.ConsumePendingSelect();
                if (pending != null)
                    ActivateSkill(pending);
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
                _selectedSkill         = null;
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

            Skill displaySelected = _rightMode == RightMode.Skill ? _selectedSkill : null;
            Skill sel = _listPanel.DrawList(ctx, displaySelected);

            if (sel != displaySelected)
                ActivateSkill(sel);

            GUILayout.EndArea();
        }

        private void DrawRight(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            bool drawSkill = _rightMode == RightMode.Skill
                && _selectedSkill != null
                && ctx.Database.Skills.Contains(_selectedSkill);

            if (!drawSkill && _rightMode == RightMode.Skill)
                ActivateEntity();

            if (drawSkill)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("技能 Inspector", InventoryEditorStyles.Header);
                var delStyle = new GUIStyle(EditorStyles.miniButton)
                    { normal = { textColor = new Color(0.9f, 0.45f, 0.45f) } };
                if (GUILayout.Button("删除技能", delStyle, GUILayout.Width(64)))
                    _pendingDeleteSkill = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            // 隐藏横向滚动条：内容自适应填满 Inspector 宽度。
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, false, false,
                GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);

            if (drawSkill)
            {
                _inspectorPanel.DrawInspector(ctx, _selectedSkill);
            }
            else if (_leftSubTab == 0)
            {
                var db = ctx.Database;
                SkillGroupTag selected = null;
                if (_selectedGroupTagIndex >= 0 && _selectedGroupTagIndex < db.SkillGroupTags.Count)
                    selected = db.SkillGroupTags[_selectedGroupTagIndex];
                _groupTagPanel.DrawInspector(ctx, selected);
            }
            else
            {
                var db = ctx.Database;
                SkillTemplate selected = null;
                if (_selectedTemplateIndex >= 0 && _selectedTemplateIndex < db.SkillTemplates.Count)
                    selected = db.SkillTemplates[_selectedTemplateIndex];
                _templatePanel.DrawInspector(ctx, selected);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

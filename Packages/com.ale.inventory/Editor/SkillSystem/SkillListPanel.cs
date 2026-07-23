using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 技能列表面板（中间列）：技能模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 技能行列表。
    /// 每行显示：拖拽句柄、模板色点、技能 ID（粗体，重复红色高亮）、名称、主分组名、删除按钮。
    /// 左侧拖拽句柄（≡）支持长按拖动调整顺序。仿 <see cref="CraftingListPanel"/>。
    /// </summary>
    public class SkillListPanel
    {
        private const float KeyRowH     = 13f;  // 列名行高
        private const float ValRowH     = 22f;  // 值行高
        private const float DragHandleW = 16f;
        private const float DotW        = 14f;
        private const float IdColW      = 90f;
        private const float NameColW    = 110f;
        private const float DescColW    = 120f;
        private const float GroupColW   = 84f;
        private const float DelBtnW     = 20f;
        private const float Pad         = 4f;

        private Vector2 _scroll;
        private string  _search         = string.Empty;
        private string  _templateFilter = null; // null = "全部"

        private readonly EditorReorderableDrag _drag = new EditorReorderableDrag("SkillListDrag");

        private GUIStyle _keyStyle;
        private GUIStyle _idStyle;
        private GUIStyle _subStyle;
        private GUIStyle KeyStyle  => _keyStyle  ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.6f, 0.85f, 1.0f) }, wordWrap = false };
        private GUIStyle IdStyle  => _idStyle  ??= new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 11, wordWrap = false, clipping = TextClipping.Clip };
        private GUIStyle SubStyle => _subStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.62f, 0.62f, 0.62f) }, wordWrap = false, clipping = TextClipping.Clip };

        private static Skill _pendingSelect;

        /// <summary>绘制列表，返回当前选中的技能引用。</summary>
        public Skill DrawList(IInventoryEditorContext ctx, Skill selectedSkill)
        {
            var db = ctx.Database;

            if (_templateFilter != null && db.GetSkillTemplate(_templateFilter) == null)
                _templateFilter = null;

            _templateFilter = EditorFilterTabs.Draw(_templateFilter, db.SkillTemplates, t => t.name);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("从模板添加", EditorStyles.toolbarDropDown, GUILayout.Width(84)))
                ShowAddFromTemplateMenu(ctx);
            using (new EditorGUI.DisabledScope(db.Skills.Count == 0))
            {
                if (GUILayout.Button("快速添加", EditorStyles.toolbarButton, GUILayout.Width(72)))
                    selectedSkill = QuickAdd(ctx);
            }
            EditorGUILayout.EndHorizontal();

            _drag.BeginFrame();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int deleteIndex = -1;
            var visible = new List<Skill>();   // 本帧可见（已过滤）条目，供键盘上下键导航

            for (int i = 0; i < db.Skills.Count; i++)
            {
                var skill = db.Skills[i];

                if (_templateFilter != null && skill.templateRef != _templateFilter) continue;
                if (!MatchesSearch(skill, _search)) continue;

                visible.Add(skill);

                bool isDup    = ctx.DuplicateIdsOf(EInventoryEntityKind.Skill).Contains(
                    string.IsNullOrWhiteSpace(skill.id) ? string.Empty : skill.id);
                bool selected = (skill == selectedSkill);

                Rect keyRow   = EditorGUILayout.GetControlRect(false, KeyRowH);
                Rect valRow   = EditorGUILayout.GetControlRect(false, ValRowH);
                Rect fullRect = Rect.MinMaxRect(keyRow.xMin, keyRow.yMin, valRow.xMax, valRow.yMax);

                _drag.RecordRow(i, fullRect);

                if (selected)
                    InventoryEditorStyles.DrawRowBackground(fullRect, InventoryEditorStyles.SelectedColor);
                if (isDup)
                    InventoryEditorStyles.DrawRowBackground(fullRect,
                        new Color(InventoryEditorStyles.ErrorColor.r,
                                  InventoryEditorStyles.ErrorColor.g,
                                  InventoryEditorStyles.ErrorColor.b, 0.25f));
                if (_drag.IsDragSource(i))
                    InventoryEditorStyles.DrawRowBackground(fullRect, EditorReorderableDrag.DragSourceTint);

                var delRect = new Rect(fullRect.xMax - DelBtnW, valRow.y + 2, DelBtnW - 2, ValRowH - 4);
                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    deleteIndex = i;

                var dragRect = new Rect(fullRect.xMin, fullRect.yMin, DragHandleW - 2, fullRect.height);
                _drag.DrawHandle(dragRect, i);

                float cx = fullRect.x + DragHandleW;
                float vy = valRow.y + (ValRowH - EditorGUIUtility.singleLineHeight) * 0.5f;
                float vh = EditorGUIUtility.singleLineHeight;

                var tmplObj  = db.GetSkillTemplate(skill.templateRef);
                var groupObj = db.GetSkillGroupTag(skill.primaryGroupTag);
                Color dotClr = tmplObj != null ? tmplObj.color : Color.gray;
                InventoryEditorStyles.DrawColorDot(
                    new Rect(cx, fullRect.y + (fullRect.height - DotW) * 0.5f, DotW, DotW), dotClr);
                cx += DotW + Pad;

                // ── 上行：列名表头 ──────────────────────────────────────────────────
                {
                    float kx = fullRect.x + DragHandleW + DotW + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,    KeyRowH - 2), "ID",    KeyStyle); kx += IdColW    + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, NameColW,  KeyRowH - 2), "名称",  KeyStyle); kx += NameColW  + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, DescColW,  KeyRowH - 2), "描述",  KeyStyle); kx += DescColW  + Pad;
                    GUI.Label(new Rect(kx, keyRow.y + 1, GroupColW, KeyRowH - 2), "主分组", KeyStyle);
                }

                // ── 下行：值 ────────────────────────────────────────────────────────
                GUI.Label(new Rect(cx, vy, IdColW, vh),
                    string.IsNullOrWhiteSpace(skill.id) ? "(空 ID)" : skill.id, IdStyle);
                cx += IdColW + Pad;

                string skillName = skill.displayText != null ? skill.displayText.GetTextValue(0) : null;
                GUI.Label(new Rect(cx, vy, NameColW, vh),
                    string.IsNullOrEmpty(skillName) ? "—" : skillName, SubStyle);
                cx += NameColW + Pad;

                string skillDesc = skill.descriptionText != null ? skill.descriptionText.GetTextValue(0) : null;
                GUI.Label(new Rect(cx, vy, DescColW, vh),
                    string.IsNullOrEmpty(skillDesc) ? "—" : skillDesc, SubStyle);
                cx += DescColW + Pad;

                string groupName = groupObj != null ? groupObj.PlainName() : "—";
                GUI.Label(new Rect(cx, vy, GroupColW, vh), groupName, SubStyle);

                if (Event.current.type == EventType.MouseDown
                    && fullRect.Contains(Event.current.mousePosition)
                    && !delRect.Contains(Event.current.mousePosition)
                    && !dragRect.Contains(Event.current.mousePosition))
                {
                    selectedSkill = skill;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            _drag.EndFrame(ctx, db.Skills, "调整技能顺序", DragHandleW, DelBtnW);

            EditorGUILayout.EndScrollView();
            float viewportHeight = GUILayoutUtility.GetLastRect().height;

            if (deleteIndex >= 0 && deleteIndex < db.Skills.Count)
            {
                var toDelete = db.Skills[deleteIndex];
                if (toDelete == selectedSkill) selectedSkill = null;
                ctx.RecordUndo("删除技能");
                db.Skills.RemoveAt(deleteIndex);
                ctx.MarkDirty();
            }

            // 键盘 上/下 方向键：在可见条目间切换选中，并在越界时自动滚动一行
            float rowPitch = KeyRowH + ValRowH + 2f * EditorGUIUtility.standardVerticalSpacing;
            if (EditorListKeyboardNav.HandleUpDown(visible, selectedSkill, out var navSkill,
                    ref _scroll, rowPitch, viewportHeight))
            {
                selectedSkill = navSkill;
                GUI.FocusControl(null);
                ctx.Repaint();
            }

            return selectedSkill;
        }

        /// <summary>取出并清空待选中技能（由 SkillSystemTab 在每帧 Layout 前调用）。</summary>
        public Skill ConsumePendingSelect()
        {
            var s = _pendingSelect;
            _pendingSelect = null;
            return s;
        }

        private void ShowAddFromTemplateMenu(IInventoryEditorContext ctx)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            if (db.SkillTemplates.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("（无可用技能模板）"));
            }
            else
            {
                foreach (var template in db.SkillTemplates)
                {
                    string name = template.name;
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        var s = AddFromTemplate(ctx, name);
                        _pendingSelect = s;
                        ctx.Repaint();
                    });
                }
            }
            menu.ShowAsContext();
        }

        private static Skill AddFromTemplate(IInventoryEditorContext ctx, string templateName)
        {
            var db = ctx.Database;
            ctx.RecordUndo("从模板添加技能");
            var skill = new Skill(GenerateSkillId(db), templateName);

            // 从模板复制「技能默认信息」（名称 / 描述 均为 Text / 图标 / 分组标签）到新技能；
            // 自定义属性值则由 RebuildAttributes 依模板 schema 的 defaultValue 初始化。
            var tmpl = db.GetSkillTemplate(templateName);
            if (tmpl != null)
            {
                skill.displayText     = tmpl.displayText     != null ? tmpl.displayText.Clone()     : new AttributeValue(EFieldType.Text);
                skill.descriptionText = tmpl.descriptionText != null ? tmpl.descriptionText.Clone() : new AttributeValue(EFieldType.Text);
                skill.icon                = tmpl.icon;
                skill.iconAddress         = tmpl.iconAddress;
                skill.primaryGroupTag              = tmpl.primaryGroupTag;
                skill.secondaryGroupTags           = new List<string>(tmpl.secondaryGroupTags);
            }

            skill.RebuildAttributes(db);
            db.Skills.Add(skill);
            ctx.MarkDirty();
            return skill;
        }

        private Skill QuickAdd(IInventoryEditorContext ctx)
        {
            var db = ctx.Database;
            ctx.RecordUndo("快速添加技能");
            var skill = db.Skills[db.Skills.Count - 1].Clone();
            skill.id  = GenerateSkillId(db);
            db.Skills.Add(skill);
            ctx.MarkDirty();
            return skill;
        }

        private static string GenerateSkillId(InventoryDatabase db)
        {
            int n = db.Skills.Count + 1;
            string id;
            do { id = "skill_" + n; n++; }
            while (db.GetSkill(id) != null);
            return id;
        }

        private static bool MatchesSearch(Skill skill, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(skill.id) &&
                skill.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string name = skill.displayText != null ? skill.displayText.GetTextValue(0) : null;
            if (!string.IsNullOrEmpty(name) &&
                name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}

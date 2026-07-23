using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 技能列表面板（中间列）：技能模板过滤标签 + 搜索栏 + 「从模板添加」/「快速添加」 + 技能行列表。
    /// 每行显示：拖拽句柄、模板色点、技能 ID（粗体，重复红色高亮）、名称、描述、主分组名、删除按钮。
    /// 骨架（过滤 / 工具栏 / 拖拽重排 / 删除 / 键盘导航）来自 <see cref="EditorEntityListPanel{TEntity,TTemplate}"/>。
    /// </summary>
    public class SkillListPanel : EditorEntityListPanel<Skill, SkillTemplate>
    {
        private const float IdColW    = 90f;
        private const float NameColW  = 110f;
        private const float DescColW  = 120f;
        private const float GroupColW = 84f;

        public SkillListPanel() : base("SkillListDrag") { }

        #region 列表配置

        protected override List<Skill>         Entities(InventoryDatabase db)  => db.Skills;
        protected override List<SkillTemplate> Templates(InventoryDatabase db) => db.SkillTemplates;
        protected override string TemplateName(SkillTemplate t) => t.name;
        protected override string TemplateRefOf(Skill e)        => e.templateRef;
        protected override string IdOf(Skill e)                 => e.id;
        protected override EInventoryEntityKind Kind            => EInventoryEntityKind.Skill;
        protected override string Noun                          => "技能";

        protected override Color RowDotColor(InventoryDatabase db, Skill e)
        {
            var tmpl = db.GetSkillTemplate(e.templateRef);
            return tmpl != null ? tmpl.color : Color.gray;
        }

        protected override bool Matches(Skill skill, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            if (!string.IsNullOrEmpty(skill.id) &&
                skill.id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string name = skill.displayText != null ? skill.displayText.GetTextValue(0) : null;
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region 行列布局

        protected override void DrawRowColumns(InventoryDatabase db, Skill skill,
            Rect keyRow, float cx, float vy, float vh)
        {
            // ── 上行：列名表头 ──────────────────────────────────────────────────
            float kx = cx;
            GUI.Label(new Rect(kx, keyRow.y + 1, IdColW,    KeyRowH - 2), "ID",    KeyStyle); kx += IdColW    + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, NameColW,  KeyRowH - 2), "名称",  KeyStyle); kx += NameColW  + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, DescColW,  KeyRowH - 2), "描述",  KeyStyle); kx += DescColW  + Pad;
            GUI.Label(new Rect(kx, keyRow.y + 1, GroupColW, KeyRowH - 2), "主分组", KeyStyle);

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

            var    groupObj  = db.GetSkillGroupTag(skill.primaryGroupTag);
            string groupName = groupObj != null ? groupObj.PlainName() : "—";
            GUI.Label(new Rect(cx, vy, GroupColW, vh), groupName, SubStyle);
        }

        #endregion

        #region 新增

        protected override Skill AddFromTemplate(IInventoryEditorContext ctx, string templateName)
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
                skill.icon               = tmpl.icon;
                skill.iconAddress        = tmpl.iconAddress;
                skill.primaryGroupTag    = tmpl.primaryGroupTag;
                skill.secondaryGroupTags = new List<string>(tmpl.secondaryGroupTags);
            }

            skill.RebuildAttributes(db);
            db.Skills.Add(skill);
            ctx.MarkDirty();
            return skill;
        }

        protected override Skill QuickAdd(IInventoryEditorContext ctx)
        {
            var db = ctx.Database;
            ctx.RecordUndo("快速添加技能");
            var skill = db.Skills[db.Skills.Count - 1].Clone();
            skill.id  = GenerateSkillId(db);
            db.Skills.Add(skill);
            ctx.MarkDirty();
            return skill;
        }

        private string GenerateSkillId(InventoryDatabase db)
            => GenerateId(db, "skill_", id => db.GetSkill(id) != null);

        #endregion
    }
}

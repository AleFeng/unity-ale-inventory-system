using Ale.Toolkit.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 技能模板。作为创建新技能的蓝本，承载「技能默认信息」（名称 / 描述 / 图标 / 本地化 / 分组标签）
    /// 与自定义属性字段定义（schema），同时用于分类筛选。
    /// 从模板创建技能时会复制这些默认信息，并据此由 <see cref="Skill.RebuildAttributes"/> 初始化技能的属性值
    /// （自定义属性的默认值取自各 <see cref="AttributeDefinition"/> 的 defaultValue）。
    /// 与 <see cref="Skill"/> 共享 <see cref="ISkillConfig"/>，使两者默认信息一致、编辑器复用同一套绘制。
    /// <para>名称 / 色点 / 属性字段来自 <see cref="ConfigTemplateBase"/>。</para>
    /// </summary>
    [Serializable]
    public class SkillTemplate : ConfigTemplateBase, ISkillConfig
    {
        // ── 技能默认信息（创建技能时复制）────────────────────────────────────────────
        /// <summary>默认显示名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用）。</summary>
        public AttributeValue displayText = new AttributeValue(EFieldType.Text);

        /// <summary>默认描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用）。</summary>
        public AttributeValue descriptionText = new AttributeValue(EFieldType.Text);

        /// <summary>
        /// 默认图标（<see cref="EFieldType.Sprite"/> 对象类属性值：直接引用或 Addressable 授权 GUID）。
        /// 由通用 Addressable 工具统一做 Object ↔ GUID/Addressable 迁移。
        /// </summary>
        public AttributeValue iconValue = new AttributeValue(EFieldType.Sprite);

        /// <summary>默认主分组标签 ID。</summary>
        public string primaryGroupTag;

        /// <summary>默认副分组标签 ID 列表。</summary>
        public List<string> secondaryGroupTags = new List<string>();

        // ── ISkillConfig（映射到上述序列化字段，供编辑器共享绘制与「从模板复制」）────────────
        AttributeValue ISkillConfig.DisplayName => displayText;
        AttributeValue ISkillConfig.Description => descriptionText;
        Sprite ISkillConfig.Icon                     { get => iconValue.GetObject(0) as Sprite; set => iconValue.SetObject(0, value); }
        string ISkillConfig.IconAddress              { get => iconValue.GetObjAddress(0);       set => iconValue.SetObjAddress(0, value); }
        string ISkillConfig.PrimaryGroupTag          { get => primaryGroupTag; set => primaryGroupTag = value; }
        List<string> ISkillConfig.SecondaryGroupTags => secondaryGroupTags;

        public SkillTemplate()
        {
        }

        public SkillTemplate(string nameArg) : base(nameArg)
        {
        }

        /// <summary>深拷贝。</summary>
        public SkillTemplate Clone()
        {
            var clone = new SkillTemplate
            {
                displayText        = displayText     != null ? displayText.Clone()     : new AttributeValue(EFieldType.Text),
                descriptionText    = descriptionText != null ? descriptionText.Clone() : new AttributeValue(EFieldType.Text),
                iconValue          = iconValue != null ? iconValue.Clone() : new AttributeValue(EFieldType.Sprite),
                primaryGroupTag    = primaryGroupTag,
                secondaryGroupTags = new List<string>(secondaryGroupTags),
            };
            CopyTo(clone);   // 名称 / 色点 / 属性字段
            return clone;
        }
    }
}

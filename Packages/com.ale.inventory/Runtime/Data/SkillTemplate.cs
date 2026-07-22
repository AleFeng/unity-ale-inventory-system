using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 技能模板。作为创建新技能的蓝本，承载「技能默认信息」（名称 / 描述 / 图标 / 本地化 / 分组标签）
    /// 与自定义属性字段定义（schema），同时用于分类筛选。
    /// 从模板创建技能时会复制这些默认信息，并据此由 <see cref="Skill.RebuildAttributes"/> 初始化技能的属性值
    /// （自定义属性的默认值取自各 <see cref="AttributeDefinition"/> 的 defaultValue）。
    /// 与 <see cref="Skill"/> 共享 <see cref="ISkillConfig"/>，使两者默认信息一致、编辑器复用同一套绘制。
    /// </summary>
    [Serializable]
    public class SkillTemplate : ISkillConfig
    {
        /// <summary>模板名称（同时作为技能的 templateRef 引用键）。</summary>
        public string name;

        /// <summary>模板标识颜色（用于列表中的圆形色点，便于快速区分来源）。</summary>
        public Color color = Color.gray;

        // ── 技能默认信息（创建技能时复制）────────────────────────────────────────────
        /// <summary>默认显示名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用）。</summary>
        public AttributeValue displayText = new AttributeValue(EFieldType.Text);

        /// <summary>默认描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用）。</summary>
        public AttributeValue descriptionText = new AttributeValue(EFieldType.Text);

        /// <summary>默认图标。</summary>
        public Sprite icon;

        /// <summary>默认图标的 Addressable 授权 GUID（启用 IS_ADDRESSABLE 且以 AssetReference 授权时用；否则空，走 <see cref="icon"/> 直接引用）。</summary>
        public string iconAddress;

        /// <summary>默认主分组标签 ID。</summary>
        public string primaryGroupTag;

        /// <summary>默认副分组标签 ID 列表。</summary>
        public List<string> secondaryGroupTags = new List<string>();

        /// <summary>模板所定义的自定义属性字段（技能据此协调其属性值集合）。</summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        // ── ISkillConfig（映射到上述序列化字段，供编辑器共享绘制与「从模板复制」）────────────
        AttributeValue ISkillConfig.DisplayName => displayText;
        AttributeValue ISkillConfig.Description => descriptionText;
        Sprite ISkillConfig.Icon                     { get => icon;            set => icon = value; }
        string ISkillConfig.IconAddress              { get => iconAddress;     set => iconAddress = value; }
        string ISkillConfig.PrimaryGroupTag          { get => primaryGroupTag; set => primaryGroupTag = value; }
        List<string> ISkillConfig.SecondaryGroupTags => secondaryGroupTags;

        public SkillTemplate()
        {
        }

        public SkillTemplate(string nameArg)
        {
            name = nameArg;
        }

        /// <summary>深拷贝。</summary>
        public SkillTemplate Clone()
        {
            var clone = new SkillTemplate(name)
            {
                color              = color,
                displayText        = displayText     != null ? displayText.Clone()     : new AttributeValue(EFieldType.Text),
                descriptionText    = descriptionText != null ? descriptionText.Clone() : new AttributeValue(EFieldType.Text),
                icon               = icon,
                iconAddress        = iconAddress,
                primaryGroupTag    = primaryGroupTag,
                secondaryGroupTags = new List<string>(secondaryGroupTags),
            };
            foreach (var attr in attributes)
                clone.attributes.Add(attr.Clone());
            return clone;
        }
    }
}

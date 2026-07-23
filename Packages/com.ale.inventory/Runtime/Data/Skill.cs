using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 技能配置条目。携带唯一 <see cref="id"/>、显示信息（名称 / 描述 / 图标 + 本地化）、来源模板引用、
    /// 分组标签，以及来自模板的自定义属性值（承载技能类型 / 效果 / 数值等，由使用方自行约定 attrId）。
    /// 技能是配置目录；主要赋予给装备类道具（道具在某个属性字段中引用技能 <see cref="id"/>），也可赋予其他道具。
    ///
    /// <para>继承 <see cref="AttributeOwner"/> 以复用带缓存的 <see cref="AttributeOwner.GetEntry"/> 与支持
    /// LocalizedString 的 <see cref="AttributeOwner.GetAttributeValue{T}"/>——技能详情弹窗据此按 Key 取自定义字段文本。</para>
    /// </summary>
    [Serializable]
    public class Skill : AttributeOwner, ISkillConfig
    {
        /// <summary>唯一标识（在技能列表中做重复检查；道具通过此 ID 引用技能）。</summary>
        public string id;

        /// <summary>显示名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；为空时回退 <see cref="id"/>）。</summary>
        public AttributeValue displayText = new AttributeValue(EFieldType.Text);

        /// <summary>技能描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；详情弹窗显示）。</summary>
        public AttributeValue descriptionText = new AttributeValue(EFieldType.Text);

        /// <summary>技能图标（直接引用；导出时按与属性值对象槽相同的约定转为 GUID / Addressable 地址）。</summary>
        public Sprite icon;

        /// <summary>图标的 Addressable 授权 GUID（启用 IS_ADDRESSABLE 且以 AssetReference 授权时用；否则空，走 <see cref="icon"/> 直接引用）。</summary>
        public string iconAddress;

        /// <summary>来源技能模板名称（可为空）。</summary>
        public string templateRef;

        /// <summary>主分组标签 ID（单选，引用 <see cref="InventoryDatabase.SkillGroupTags"/> 的 id）。</summary>
        public string primaryGroupTag;

        /// <summary>副分组标签 ID 列表（可多选）。</summary>
        public List<string> secondaryGroupTags = new List<string>();

        /// <summary>来自模板的自定义属性值。</summary>
        public List<AttributeEntry> values = new List<AttributeEntry>();

        // 实现基类 AttributeOwner 的抽象属性，将 values 列表暴露给基类的懒加载字典缓存。
        protected override List<AttributeEntry> AttributeEntries => values;

        // ── ISkillConfig（映射到上述序列化字段，供编辑器共享绘制与「从模板复制」）────────────
        AttributeValue ISkillConfig.DisplayName => displayText;
        AttributeValue ISkillConfig.Description => descriptionText;
        Sprite ISkillConfig.Icon                     { get => icon;            set => icon = value; }
        string ISkillConfig.IconAddress              { get => iconAddress;     set => iconAddress = value; }
        string ISkillConfig.PrimaryGroupTag          { get => primaryGroupTag; set => primaryGroupTag = value; }
        List<string> ISkillConfig.SecondaryGroupTags => secondaryGroupTags;

        public Skill()
        {
        }

        public Skill(string newId, string newTemplateRef = null)
        {
            id          = newId;
            templateRef = newTemplateRef;
        }

        /// <summary>
        /// 根据当前模板协调自定义属性值集合：
        /// 为模板新增字段追加默认值条目；移除模板已不存在的字段条目；已存在字段保留现有值
        /// （类型 / 数组形态 / 枚举类型引用变化时重置为新类型默认值）。
        /// </summary>
        public void RebuildAttributes(InventoryDatabase db)
        {
            if (!db) return;

            var template = db.GetSkillTemplate(templateRef);
            AttributeSync.Sync(values, template != null ? template.attributes : null);

            // values 已完整重建，使缓存失效；下次 GetEntry 调用时将从最终状态重建字典。
            InvalidateEntryCache();
        }

        /// <summary>深拷贝。</summary>
        public Skill Clone()
        {
            var clone = new Skill(id, templateRef)
            {
                displayText        = displayText     != null ? displayText.Clone()     : new AttributeValue(EFieldType.Text),
                descriptionText    = descriptionText != null ? descriptionText.Clone() : new AttributeValue(EFieldType.Text),
                icon               = icon,
                iconAddress        = iconAddress,
                primaryGroupTag    = primaryGroupTag,
                secondaryGroupTags = new List<string>(secondaryGroupTags),
            };
            foreach (var e in values)
                clone.values.Add(e.Clone());
            return clone;
        }
    }
}

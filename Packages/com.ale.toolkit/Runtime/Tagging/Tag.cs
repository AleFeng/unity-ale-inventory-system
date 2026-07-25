using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 标签：可附加到配置对象（如道具）上的一枚具名类别标记。附加时把标签定义的一组属性字段
    /// （<see cref="AttributeDefinition"/>）自动合并到对象的属性字段列表；同时携带 UI 呈现
    /// （显示名 / 描述 / 背景图 / 颜色 / 隐藏）。例如给道具打「消耗品」「材料」「装备」等标签。
    ///
    /// <para>标签在列表中的顺序可用作排序依据（见 <see cref="SortFieldKeys.TagOrder"/>）。</para>
    /// </summary>
    [Serializable]
    public class Tag
    {
        /// <summary>标签名称（唯一键，如 消耗品、材料、装备、任务物品）。</summary>
        public string name;

        // ── UI 呈现配置 ────────────────────────────────────────────────────────────

        /// <summary>UI 中显示的名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；为空时回退 <see cref="name"/>）。</summary>
        public AttributeValue displayNameText = new AttributeValue(EFieldType.Text);

        /// <summary>描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；正式配置数据，UI 显示为「描述」）。</summary>
        public AttributeValue descriptionText = new AttributeValue(EFieldType.Text);

        /// <summary>标签背景 Sprite。</summary>
        public Sprite backgroundSprite;

        /// <summary>背景 Sprite 的 Addressable 授权 GUID（启用 IS_ADDRESSABLE 且以 AssetReference 授权时用；否则空，走 <see cref="backgroundSprite"/> 直接引用）。</summary>
        public string backgroundSpriteAddress;

        /// <summary>标签背景颜色，默认纯白。</summary>
        public Color backgroundColor = Color.white;

        /// <summary>UI 中隐藏此标签（不渲染到标签 UI）。</summary>
        public bool hideInUI;

        // ── 属性字段 schema ────────────────────────────────────────────────────────

        /// <summary>该标签定义的属性字段，附加到对象后会自动合并进对象的属性字段列表。</summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        public Tag()
        {
        }

        public Tag(string name, string description = null)
        {
            this.name = name;
            if (!string.IsNullOrEmpty(description))
                descriptionText.SetTextValue(0, description);
        }

        public Tag Clone()
        {
            var clone = new Tag(name)
            {
                displayNameText         = displayNameText != null ? displayNameText.Clone() : new AttributeValue(EFieldType.Text),
                descriptionText         = descriptionText != null ? descriptionText.Clone() : new AttributeValue(EFieldType.Text),
                backgroundSprite        = backgroundSprite,
                backgroundSpriteAddress = backgroundSpriteAddress,
                backgroundColor         = backgroundColor,
                hideInUI                = hideInUI,
            };
            foreach (var attr in attributes)
                clone.attributes.Add(attr.Clone());
            return clone;
        }
    }
}

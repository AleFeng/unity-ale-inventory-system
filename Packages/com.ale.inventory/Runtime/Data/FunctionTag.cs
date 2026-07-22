using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 功能标签。一个道具可具有多个功能标签，每个标签定义一组属性字段（<see cref="AttributeDefinition"/>）。
    /// 给道具添加/移除标签时，会自动添加/移除该标签定义的属性字段。
    /// 例如：消耗品（效果值）、可交易（价值）、装备（属性加成）等。
    /// </summary>
    [Serializable]
    public class FunctionTag
    {
        /// <summary>功能标签名称（如 消耗品、材料、装备、任务物品）。</summary>
        public string name;

        // ── 功能标签属性（UI 显示配置）──────────────────────────────────────────

        /// <summary>UI 中显示的名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；为空时回退 <see cref="name"/>）。</summary>
        public AttributeValue displayNameText = new AttributeValue(EFieldType.Text);

        /// <summary>描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；正式游戏配置数据，UI 显示为「描述」）。</summary>
        public AttributeValue descriptionText = new AttributeValue(EFieldType.Text);

        /// <summary>标签背景 Sprite。</summary>
        public Sprite backgroundSprite;

        /// <summary>背景 Sprite 的 Addressable 授权 GUID（启用 IS_ADDRESSABLE 且以 AssetReference 授权时用；否则空，走 <see cref="backgroundSprite"/> 直接引用）。</summary>
        public string backgroundSpriteAddress;

        /// <summary>标签背景颜色，默认纯白。</summary>
        public Color backgroundColor = Color.white;

        /// <summary>UI 中隐藏此标签（不渲染到 UiwItemLabel）。</summary>
        public bool hideInUI;

        // ── 道具属性字段 ──────────────────────────────────────────────────────────

        /// <summary>该标签所定义的道具属性字段，附加到道具后会自动添加至道具的属性字段列表中。</summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        public FunctionTag()
        {
        }

        public FunctionTag(string name, string description = null)
        {
            this.name = name;
            if (!string.IsNullOrEmpty(description))
                descriptionText.SetTextValue(0, description);
        }

        public FunctionTag Clone()
        {
            var clone = new FunctionTag(name)
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

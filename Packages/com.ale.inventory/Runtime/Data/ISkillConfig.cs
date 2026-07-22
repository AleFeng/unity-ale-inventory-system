using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 技能可配置项的共享契约。由技能实例 <see cref="Skill"/> 与技能模板 <see cref="SkillTemplate"/> 共同实现，
    /// 使两者的「技能默认信息」（名称 / 描述 / 图标 / 分组标签）一致，
    /// 编辑器得以复用同一套绘制（<c>SkillConfigDrawer</c>）；从模板创建技能时复制这些默认值到新技能。
    ///
    /// <para>说明：唯一 <c>id</c>、来源模板引用 <c>templateRef</c>、自定义属性值 <c>values</c> 为技能实例独有，不在此共享接口中
    /// （自定义属性的<b>默认值</b>由模板的属性字段 schema <c>attributes</c> 承载，创建时经 <see cref="Skill.RebuildAttributes"/> 复制）。</para>
    /// </summary>
    public interface ISkillConfig
    {
        /// <summary>显示名称（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用）。</summary>
        AttributeValue DisplayName { get; }

        /// <summary>描述（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用）。</summary>
        AttributeValue Description { get; }

        /// <summary>图标（直接引用；直接模式 / 编辑器预览）。</summary>
        Sprite Icon { get; set; }

        /// <summary>图标的 Addressable 授权 GUID（启用 IS_ADDRESSABLE 且以 AssetReference 授权时用；否则空，走 <see cref="Icon"/> 直接引用）。</summary>
        string IconAddress { get; set; }

        /// <summary>主分组标签 ID（单选）。</summary>
        string PrimaryGroupTag { get; set; }

        /// <summary>副分组标签 ID 列表（可多选）。</summary>
        List<string> SecondaryGroupTags { get; }
    }
}

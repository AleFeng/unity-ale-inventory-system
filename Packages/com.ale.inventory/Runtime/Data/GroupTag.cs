using System;
using UnityEngine;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 分组标签基类。承载三大系统分组标签（制作 <see cref="CraftingGroupTag"/> / 装备 <see cref="EquipmentGroupTag"/> /
    /// 技能 <see cref="SkillGroupTag"/>）共享的基础信息：唯一 ID、名称、描述、列表色点。
    /// 仅承载基础信息，不携带自定义属性字段；各系统按自身用途以 1 主分组 + 若干副分组的形式引用。
    ///
    /// <para>名称 / 描述采用 <see cref="EFieldType.Text"/> 类型的 <see cref="AttributeValue"/>：其内部始终携带纯文本
    /// fallback，并在启用 IS_LOCALIZATION 时额外携带 Unity Localization 引用（表 + 条目），
    /// 因此无需再单独声明 LocalizedString 字段。运行时显示用 <see cref="ResolveDisplayName"/>（本地化优先），
    /// 编辑器列表 / 下拉用 <see cref="PlainName"/>（纯文本，编辑期稳定、不依赖运行时语言环境）。</para>
    /// </summary>
    [Serializable]
    public class GroupTag
    {
        /// <summary>唯一标识（各系统条目按此 ID 引用分组标签）。</summary>
        public string id;

        /// <summary>显示名称（Text：纯文本 fallback + 可选本地化引用）。为空时各解析方法回退 <see cref="id"/>。</summary>
        public AttributeValue displayName = new AttributeValue(EFieldType.Text);

        /// <summary>功能说明（Text：纯文本 fallback + 可选本地化引用；可选，用于编辑器提示）。</summary>
        public AttributeValue description = new AttributeValue(EFieldType.Text);

        /// <summary>列表标识颜色（用于编辑器列表中的圆形色点，便于快速区分）。</summary>
        public Color color = Color.gray;

        protected GroupTag()
        {
        }

        protected GroupTag(string newId, string newDisplayName = null)
        {
            id = newId;
            if (!string.IsNullOrEmpty(newDisplayName))
                displayName.SetTextValue(0, newDisplayName);
        }

        /// <summary>
        /// 解析用于 UI 显示的名称：启用 IS_LOCALIZATION 且本地化引用可解析出非空文本时取本地化文本，
        /// 否则取纯文本 fallback；均为空时回退 <see cref="id"/>。供运行时显示使用。
        /// </summary>
        public string ResolveDisplayName() => ResolveTextOr(displayName, id);

        /// <summary>解析用于 UI 显示的描述：语义同 <see cref="ResolveDisplayName"/>，但为空时回退空串。</summary>
        public string ResolveDescription() => ResolveTextOr(description, string.Empty);

        /// <summary>
        /// 取纯文本名称（不做本地化解析）：纯文本 fallback，为空时回退 <see cref="id"/>。
        /// 供编辑器列表 / 下拉标签使用（编辑期稳定、不依赖运行时语言环境）。
        /// </summary>
        public string PlainName()
        {
            string plain = displayName != null ? displayName.GetTextValue() : null;
            return !string.IsNullOrEmpty(plain) ? plain : id;
        }

        /// <summary>
        /// 确保名称 / 描述为标量 <see cref="EFieldType.Text"/> 类型（修正为 null / 类型漂移的旧数据；供编辑器绘制前调用）。
        /// </summary>
        public void NormalizeTextFields()
        {
            displayName = EnsureText(displayName);
            description = EnsureText(description);
        }

        /// <summary>把一个 Text 属性值解析为显示文本（本地化优先 → 纯文本），均为空时返回 <paramref name="fallback"/>。</summary>
        private static string ResolveTextOr(AttributeValue text, string fallback)
        {
            string s = text != null ? text.ResolveText() : null;
            return !string.IsNullOrEmpty(s) ? s : fallback;
        }

        /// <summary>把 <paramref name="value"/> 归一为标量 Text 属性值（null 则新建，类型 / 数组不符则就地转换）。</summary>
        private static AttributeValue EnsureText(AttributeValue value)
        {
            if (value == null) return new AttributeValue(EFieldType.Text);
            if (value.Type != EFieldType.Text || value.IsArray)
                value.ChangeType(EFieldType.Text, false);
            return value;
        }

        /// <summary>把本标签的公共字段拷贝到 <paramref name="dest"/>（供子类 Clone 复用）。</summary>
        protected void CopyTo(GroupTag dest)
        {
            dest.id          = id;
            dest.displayName = displayName != null ? displayName.Clone() : new AttributeValue(EFieldType.Text);
            dest.description = description != null ? description.Clone() : new AttributeValue(EFieldType.Text);
            dest.color       = color;
        }
    }
}

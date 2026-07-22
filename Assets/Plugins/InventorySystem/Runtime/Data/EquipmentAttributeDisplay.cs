using System;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 装备组「装备属性字段列表」的一条配置：指定道具上的某个属性字段作为装备组的总属性加成数据，
    /// 在 UI 上据此显示（如 物品等级 / 攻击力 / 防御力 / 生命值）。
    /// 通过 <see cref="groupTag"/> 指定该条目所属的分组标签，用于 UI 分组显示。
    /// </summary>
    [Serializable]
    public class EquipmentAttributeDisplay
    {
        /// <summary>关联的属性字段 ID（道具系统中道具模板/功能标签定义的属性字段）。</summary>
        public string attrId;

        /// <summary>所属分组标签 ID（引用 <see cref="InventoryDatabase.EquipmentGroupTags"/> 的 id；可为空）。</summary>
        public string groupTag;

        /// <summary>显示名覆盖（Text：纯文本 fallback + 可选本地化引用；可选，为空时由属性定义/道具回退）。</summary>
        public AttributeValue label = new AttributeValue(EFieldType.Text);

        /// <summary>
        /// 仅对 <see cref="EFieldType.EnumIntPair"/> 类型属性有效：指定<b>枚举项</b>上用作显示名的
        /// 自定义属性字段 ID（枚举项的 String / LocalizedString 字段，如"名称"）。
        /// 为空时回退使用枚举项自身的名称（<see cref="EnumItem.name"/>）。
        /// <para>汇总时每个枚举 Key 会单独成为一条 <see cref="EquipmentBonus"/>，其显示名即经此字段解析。</para>
        /// </summary>
        public string enumLabelAttrId;

        public EquipmentAttributeDisplay()
        {
        }

        public EquipmentAttributeDisplay(string attrId, string groupTag = null, string labelText = null,
            string enumLabelAttrId = null)
        {
            this.attrId          = attrId;
            this.groupTag        = groupTag;
            if (!string.IsNullOrEmpty(labelText))
                label.SetTextValue(0, labelText);
            this.enumLabelAttrId = enumLabelAttrId;
        }

        /// <summary>
        /// 解析显示名：本地化优先 → 纯文本（见 <see cref="AttributeValue.ResolveText"/>）；均为空时回退 <paramref name="fallback"/>。
        /// </summary>
        public string ResolveLabel(string fallback)
        {
            string s = label != null ? label.ResolveText() : null;
            return !string.IsNullOrEmpty(s) ? s : fallback;
        }

        /// <summary>确保 <see cref="label"/> 为标量 <see cref="EFieldType.Text"/> 类型（修正 null / 类型漂移的旧数据；供编辑器绘制前调用）。</summary>
        public void NormalizeLabel()
        {
            if (label == null) { label = new AttributeValue(EFieldType.Text); return; }
            if (label.Type != EFieldType.Text || label.IsArray)
                label.ChangeType(EFieldType.Text, false);
        }

        /// <summary>深拷贝。</summary>
        public EquipmentAttributeDisplay Clone()
        {
            var clone = new EquipmentAttributeDisplay(attrId, groupTag, null, enumLabelAttrId);
            clone.label = label != null ? label.Clone() : new AttributeValue(EFieldType.Text);
            return clone;
        }
    }
}

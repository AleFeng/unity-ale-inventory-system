using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 枚举项。<see cref="value"/> 在添加时由系统自动分配、不可修改、永不复用，
    /// 用于在属性值中稳定引用枚举（与显示顺序解耦）。
    ///
    /// <para>每个枚举项可携带一组自定义属性值（由所属 <see cref="EnumType.attributes"/> 定义 schema，
    /// 此列表存储该项的具体值）。例如"品质"枚举的每一项可携带"多语言名称""背景框 Sprite"等。</para>
    ///
    /// <para>继承自 <see cref="AttributeOwner"/>，可直接使用
    /// <see cref="AttributeOwner.GetEntry"/>、<see cref="AttributeOwner.GetAttributeValue{T}"/>、
    /// <see cref="AttributeOwner.SetAttributeValue{T}"/> 等便捷 API。</para>
    /// </summary>
    [Serializable]
    public class EnumItem : AttributeOwner
    {
        /// <summary>枚举项显示名称（如 粗糙/普通/稀有）。</summary>
        public string name;

        /// <summary>不可变的整数值，由 <see cref="EnumType.AddItem"/> 自动分配。编辑器上只读。</summary>
        public int value;

        /// <summary>
        /// 该枚举项自身携带的属性值列表。
        /// Schema（字段定义）由所属 <see cref="EnumType.attributes"/> 维护；
        /// 本列表仅存储与定义对应的 <see cref="AttributeEntry"/>（key + value）。
        /// 通过 <see cref="EnumType.RebuildItemAttributes"/> 保持与定义同步。
        /// </summary>
        public List<AttributeEntry> attributeValues = new List<AttributeEntry>();

        // 实现基类 AttributeOwner 的抽象属性，将 attributeValues 列表暴露给基类的懒加载字典缓存。
        protected override List<AttributeEntry> AttributeEntries => attributeValues;

        public EnumItem()
        {
        }

        public EnumItem(string name, int value)
        {
            this.name  = name;
            this.value = value;
        }

        /// <summary>不可变整数值访问器（与字段 <see cref="value"/> 区分，供编辑器使用）。</summary>
        public int Value => value;

        public EnumItem Clone()
        {
            var clone = new EnumItem(name, value);
            foreach (var entry in attributeValues)
                clone.attributeValues.Add(entry.Clone());
            return clone;
        }
    }
}

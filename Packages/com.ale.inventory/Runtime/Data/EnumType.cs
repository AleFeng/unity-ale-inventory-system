using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 用户自定义枚举类型。包含一个有序的 <see cref="EnumItem"/> 列表与一组属性字段定义。
    ///
    /// <b>属性字段：</b>通过 <see cref="attributes"/> 定义该枚举类型所有枚举项共享的 schema；
    /// 每个 <see cref="EnumItem"/> 在 <see cref="EnumItem.attributeValues"/> 中存储对应的具体值。
    /// 调用 <see cref="RebuildItemAttributes"/> 可保证所有枚举项与定义同步。
    /// </summary>
    [Serializable]
    public class EnumType
    {
        /// <summary>枚举类型名称（如 品质、身体部位）。</summary>
        public string name;

        /// <summary>枚举项列表，索引即显示顺序。</summary>
        public List<EnumItem> items = new List<EnumItem>();

        /// <summary>下一个可分配的枚举值，单调递增、永不回收。</summary>
        public int nextValue;

        /// <summary>
        /// 该枚举类型下所有枚举项共享的属性字段定义（schema）。
        /// 每个 <see cref="EnumItem"/> 会根据此列表维护自己的 <see cref="EnumItem.attributeValues"/>。
        /// </summary>
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        public EnumType()
        {
        }

        public EnumType(string name)
        {
            this.name = name;
        }

        // ── 枚举项管理 ───────────────────────────────────────────────────────────────

        /// <summary>追加一个新枚举项，自动分配不可变值，并为其填充属性默认值。返回新建项。</summary>
        public EnumItem AddItem(string itemName)
        {
            var item = new EnumItem(itemName, nextValue);
            nextValue++;
            // 为新项初始化属性默认值
            foreach (var def in attributes)
                item.attributeValues.Add(new AttributeEntry(def.id, def.CreateValue()));
            items.Add(item);
            return item;
        }

        /// <summary>按显示顺序索引移除一个枚举项（不回收其值）。</summary>
        public void RemoveItemAt(int index)
        {
            if (index >= 0 && index < items.Count)
                items.RemoveAt(index);
        }

        /// <summary>按不可变值查找枚举项，未找到返回 null。</summary>
        public EnumItem GetItemByValue(int value)
        {
            foreach (var item in items)
                if (item.value == value) return item;
            return null;
        }

        /// <summary>按不可变值返回显示名称，未找到返回空字符串。</summary>
        public string GetDisplayName(int value)
        {
            var item = GetItemByValue(value);
            return item != null ? item.name : string.Empty;
        }

        /// <summary>
        /// 同步所有枚举项的 <see cref="EnumItem.attributeValues"/> 与 <see cref="attributes"/> 定义：
        /// 为缺失 key 添加默认值条目；移除已删除 key 的孤立条目；类型 / 数组形态 / 枚举类型引用
        /// 变化的条目重置为新默认值（保留 key，旧值不再兼容）。
        /// 此操作幂等，可在编辑器每帧调用。
        /// </summary>
        public void RebuildItemAttributes()
        {
            foreach (var item in items)
            {
                AttributeSync.Sync(item.attributeValues, attributes);

                // attributeValues 已完整重建，使枚举项的属性缓存失效。
                item.InvalidateEntryCache();
            }
        }

        /// <summary>
        /// 用一组名称初始化枚举项（0, 1, 2…）。仅在列表为空时填充，供默认数据使用。
        /// </summary>
        public void SeedItems(params string[] names)
        {
            if (items.Count > 0) return;
            foreach (var n in names)
                AddItem(n);
        }

        public EnumType Clone()
        {
            var clone = new EnumType(name) { nextValue = nextValue };
            foreach (var def in attributes)
                clone.attributes.Add(def.Clone());
            foreach (var item in items)
                clone.items.Add(item.Clone());
            return clone;
        }
    }
}

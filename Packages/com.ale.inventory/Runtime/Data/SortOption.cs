using System;
using System.Collections.Generic;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 整理选项。对应仓库模板中 <see cref="SortPriority.field"/> 的一个唯一值，
    /// 由 <see cref="InventoryDatabase.RebuildSortOptions"/> 自动生成与同步，不可手动增删。
    ///
    /// <para>持有两个内置专属字段：<see cref="displayName"/>（<see cref="EFieldType.Text"/>：排序下拉的显示名，
    /// 纯文本 fallback + 可选本地化引用）与 <see cref="ignoreIds"/>（排序时跳过的条目 ID 列表）。
    /// 二者由运行时排序 / 整理栏读取（见 <see cref="ResolveDisplayName"/> / <see cref="EffectiveIgnoreIds"/>）。</para>
    ///
    /// <para>仍继承自 <see cref="AttributeOwner"/> 以承载可选的额外自定义属性（schema 定义在
    /// <see cref="InventoryDatabase.SortOptionAttributes"/>），并用于把旧版把「名称」「忽略ID」
    /// 存为通用属性值的数据迁移到上述内置字段（见 <see cref="InventoryDatabase.RebuildSortOptions"/>）。</para>
    /// </summary>
    [Serializable]
    public class SortOption : AttributeOwner
    {
        /// <summary>旧版「显示名」通用属性字段 ID（迁移到 <see cref="displayName"/> 前的存储；仅供兼容 / 迁移）。</summary>
        public const string LegacyNameAttrId = "名称";

        /// <summary>旧版「忽略ID」通用属性字段 ID（迁移到 <see cref="ignoreIds"/> 前的存储；仅供兼容 / 迁移）。</summary>
        public const string LegacyIgnoreAttrId = "忽略ID";

        /// <summary>
        /// 排序字段标识。对应 <see cref="SortPriority.field"/> 的值：
        /// 可以是 <c>"__id__"</c>、<c>"__tagOrder__"</c> 或某个 <c>AttributeDefinition.id</c>。
        /// </summary>
        public string field;

        /// <summary>
        /// 内置：排序下拉显示名（<see cref="EFieldType.Text"/>：纯文本 fallback + 可选本地化引用；为空时回退字段名）。
        /// </summary>
        public AttributeValue displayName = new AttributeValue(EFieldType.Text);

        /// <summary>
        /// 内置：排序时忽略（跳过）的条目 ID 列表。语义随排序字段而定：
        /// 按道具ID排序 = 道具ID；功能页签排序 = 功能标签名；按属性排序 = 属性值。
        /// </summary>
        public List<string> ignoreIds = new List<string>();

        /// <summary>该整理选项的（可选）额外属性值列表，schema 由 <see cref="InventoryDatabase.SortOptionAttributes"/> 定义。</summary>
        public List<AttributeEntry> attributeValues = new List<AttributeEntry>();

        // 实现基类 AttributeOwner 的抽象属性。
        protected override List<AttributeEntry> AttributeEntries => attributeValues;

        public SortOption() { }

        public SortOption(string field)
        {
            this.field = field;
        }

        /// <summary>确保 <see cref="displayName"/> 为标量 <see cref="EFieldType.Text"/> 类型（修正 null / 类型漂移的旧数据）。</summary>
        public void NormalizeDisplayName()
        {
            if (displayName == null) { displayName = new AttributeValue(EFieldType.Text); return; }
            if (displayName.Type != EFieldType.Text || displayName.IsArray)
                displayName.ChangeType(EFieldType.Text, false);
        }

        /// <summary>
        /// 解析排序下拉显示名：本地化优先 → 纯文本（见 <see cref="AttributeValue.ResolveText"/>）；
        /// 内置为空时回退读取旧版通用属性值（兼容未迁移数据），仍为空则返回 <paramref name="fallback"/>。
        /// </summary>
        public string ResolveDisplayName(string fallback)
        {
            string s = displayName != null ? displayName.ResolveText() : null;
            if (string.IsNullOrEmpty(s))
                s = GetAttributeValue<string>(LegacyNameAttrId); // 兼容：未迁移旧数据仍从通用属性值取
            return !string.IsNullOrEmpty(s) ? s : fallback;
        }

        /// <summary>
        /// 有效忽略ID列表：内置 <see cref="ignoreIds"/> 非空时用之；否则回退读取旧版通用属性值（兼容未迁移数据）。
        /// </summary>
        public IReadOnlyList<string> EffectiveIgnoreIds
        {
            get
            {
                if (ignoreIds != null && ignoreIds.Count > 0) return ignoreIds;
                return GetAttributeValue(LegacyIgnoreAttrId)?.StringArray; // 兼容：未迁移旧数据
            }
        }

        public SortOption Clone()
        {
            var clone = new SortOption(field);
            clone.displayName = displayName != null ? displayName.Clone() : new AttributeValue(EFieldType.Text);
            clone.ignoreIds   = new List<string>(ignoreIds);
            foreach (var entry in attributeValues)
                clone.attributeValues.Add(entry.Clone());
            return clone;
        }
    }
}

using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 属性值集合与属性字段定义（schema）之间的同步工具。
    ///
    /// <para>「实体持有一组 <see cref="AttributeEntry"/>、其形状由模板 / 标签 / 枚举类型的
    /// <see cref="AttributeDefinition"/> 列表决定」这一模式在包内出现 7 次
    /// （<see cref="Item"/> / <see cref="Skill"/> / <see cref="Inventory"/> / <see cref="Shop"/> /
    /// <see cref="EquipmentGroup"/> / <see cref="CraftingBlueprint"/> / <see cref="EnumType"/>），
    /// 各自的 <c>RebuildAttributes</c> 曾逐字重复同一段协调逻辑。此处收口为单一实现。</para>
    /// </summary>
    public static class AttributeSync
    {
        private static readonly List<AttributeDefinition> EmptyDefs = new List<AttributeDefinition>();

        #region 同步

        /// <summary>
        /// 依据字段定义 <paramref name="defs"/> 协调属性值集合 <paramref name="values"/>：
        /// 移除定义中已不存在的条目；为新增定义追加默认值条目；
        /// 已存在条目在「类型 / 数组形态 / 枚举类型引用」发生漂移时重置为新类型的默认值（保留 id）。
        /// <para>空 id 的定义与条目一律忽略 / 移除。本方法幂等，可在编辑器中每帧调用。</para>
        /// </summary>
        /// <param name="values">实体自身存储的属性值列表（就地修改）。</param>
        /// <param name="defs">期望的字段定义列表；null 视为空列表（即清空 <paramref name="values"/>）。</param>
        /// <param name="reorder">
        /// true = 同时把 <paramref name="values"/> 重排为与 <paramref name="defs"/> 一致的顺序
        /// （仅 <see cref="Item"/> 需要：其字段来自「模板 + 多个功能标签」，顺序需与左侧标签面板一致）。
        /// </param>
        public static void Sync(List<AttributeEntry> values, List<AttributeDefinition> defs, bool reorder = false)
        {
            if (values == null) return;
            if (defs == null) defs = EmptyDefs;

            // 一次性建立 id → 条目 的查找表：原实现在 defs 循环内逐条线性扫描 values，为 O(values × defs)。
            // 同 id 重复时保留首个，与原线性扫描（找到即返回）语义一致。
            var lookup = new Dictionary<string, AttributeEntry>(values.Count);
            foreach (var e in values)
                if (e != null && !string.IsNullOrEmpty(e.id) && !lookup.ContainsKey(e.id))
                    lookup[e.id] = e;

            var desiredIds = new HashSet<string>();
            foreach (var def in defs)
                if (def != null && !string.IsNullOrEmpty(def.id)) desiredIds.Add(def.id);

            // 移除不再期望的条目。lookup 建于此之前，但被移除的 id 必然不在 desiredIds 中，
            // 而下面只按 desiredIds 中的 id 查表，因此不会命中已移除的条目。
            values.RemoveAll(e => e == null || !desiredIds.Contains(e.id));

            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;

                if (!lookup.TryGetValue(def.id, out var existing) || existing == null)
                {
                    var added = new AttributeEntry(def.id, def.CreateValue());
                    values.Add(added);
                    lookup[def.id] = added;
                }
                else if (IsMismatched(existing.value, def))
                {
                    existing.value = def.CreateValue();
                }
            }

            if (reorder) Reorder(values, defs);
        }

        #endregion

        #region 内部辅助

        /// <summary>
        /// 判断已有属性值是否与字段定义不再匹配（须重置为新默认值）。
        /// <para>枚举类型引用漂移也计入：改掉模板字段所引用的枚举类型后，旧枚举整数值不再有意义。
        /// 此前仅 <see cref="Item"/> / <see cref="Skill"/> 做该检查，其余四类实体会残留陈旧枚举值。</para>
        /// </summary>
        private static bool IsMismatched(AttributeValue value, AttributeDefinition def)
        {
            if (value == null) return true;
            if (value.Type != def.type) return true;
            if (value.IsArray != def.isArray) return true;
            return (def.type == EFieldType.Enum || def.type == EFieldType.EnumIntPair)
                   && value.EnumTypeRef != def.enumTypeRef;
        }

        /// <summary>把 <paramref name="values"/> 就地重排为与 <paramref name="defs"/> 相同的顺序。</summary>
        private static void Reorder(List<AttributeEntry> values, List<AttributeDefinition> defs)
        {
            int slot    = 0;
            var placed  = new HashSet<string>();

            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.id) || !placed.Add(def.id)) continue;

                int cur = -1;
                for (int j = slot; j < values.Count; j++)
                    if (values[j].id == def.id) { cur = j; break; }
                if (cur < 0) continue;

                if (cur > slot)
                {
                    var tmp = values[cur];
                    values.RemoveAt(cur);
                    values.Insert(slot, tmp);
                }
                slot++;
            }
        }

        #endregion
    }
}

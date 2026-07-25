using System.Collections.Generic;

namespace Ale.Toolkit.Runtime
{
    /// <summary>
    /// 整理选项列表与「可用排序字段」的通用同步（与领域无关）。据宿主给出的有序可用字段，
    /// 对 <see cref="SortOption"/> 列表做增补 / 移除 / 重排，并按 schema 同步各选项的额外属性值；
    /// 同时一次性迁移旧版把「名称 / 忽略ID」存为通用属性值的数据到内置字段。
    ///
    /// <para>宿主（如 <c>InventoryDatabase.RebuildSortOptions</c>）只负责收集领域字段
    /// （主键 / 各属性 / 标签序号等）并给出有序列表，其余同步逻辑全部收口于此。</para>
    /// </summary>
    public static class SortOptionSync
    {
        /// <summary>
        /// 据有序可用字段 <paramref name="orderedFields"/> 同步 <paramref name="sortOptions"/>：
        /// 移除已消失字段的条目、追加新字段条目、按字段顺序重排、迁移旧版内置字段、按 <paramref name="schema"/> 同步属性值。
        /// </summary>
        /// <param name="sortOptions">被就地同步的整理选项列表。</param>
        /// <param name="orderedFields">有序的可用排序字段 ID（宿主收集，通常已去重）。</param>
        /// <param name="schema">整理选项的额外属性字段定义（每个选项按此增补 / 移除属性值；迁移时会移除其中的保留定义）。</param>
        public static void Rebuild(List<SortOption> sortOptions, IReadOnlyList<string> orderedFields,
            List<AttributeDefinition> schema)
        {
            if (sortOptions == null) return;

            // 字段序号表（去重）：既作「是否仍可用」判定，也作排序时的 O(1) 序号查询。
            var fieldOrder = new Dictionary<string, int>();
            if (orderedFields != null)
                foreach (var id in orderedFields)
                    if (!string.IsNullOrEmpty(id) && !fieldOrder.ContainsKey(id))
                        fieldOrder[id] = fieldOrder.Count;

            // 移除不再是可用排序字段的条目（field 为 null 的脏数据一并清掉）。
            sortOptions.RemoveAll(so => so.field == null || !fieldOrder.ContainsKey(so.field));

            // 追加新出现的字段，按首次出现顺序插入。
            // 先把现有 field 收进集合，避免逐个线性查找（原为 O(n²)）。
            var existing = new HashSet<string>();
            foreach (var so in sortOptions)
                existing.Add(so.field);
            if (orderedFields != null)
                foreach (var field in orderedFields)
                    if (fieldOrder.ContainsKey(field) && existing.Add(field))
                        sortOptions.Add(new SortOption(field));

            // 按字段顺序重排（用序号字典而非 List.IndexOf：后者在比较器内是 O(n)，会让整个排序退化为 O(n² log n)）。
            sortOptions.Sort((a, b) =>
            {
                int ia = a.field != null && fieldOrder.TryGetValue(a.field, out int va) ? va : -1;
                int ib = b.field != null && fieldOrder.TryGetValue(b.field, out int vb) ? vb : -1;
                return ia.CompareTo(ib);
            });

            // 一次性迁移旧版内置字段（幂等；迁移完毕后为空操作）。
            MigrateBuiltinFields(sortOptions, schema);

            // 对每个 SortOption 按 schema 同步 attributeValues（增补缺失、移除孤立、类型漂移重置）。
            foreach (var so in sortOptions)
            {
                AttributeSync.Sync(so.attributeValues, schema);
                so.InvalidateEntryCache();
            }
        }

        /// <summary>
        /// 一次性迁移旧版整理选项数据：把存为通用属性值的「名称」「忽略ID」搬进内置字段
        /// （<see cref="SortOption.displayName"/> / <see cref="SortOption.ignoreIds"/>），
        /// 再从 <paramref name="schema"/> 移除这两个保留定义。仅当内置字段为空时迁移，避免覆盖已编辑值；
        /// 迁移完成后（schema 中已无这两项、各选项残留值被同步移除）本方法为空操作。
        /// </summary>
        private static void MigrateBuiltinFields(List<SortOption> sortOptions, List<AttributeDefinition> schema)
        {
            foreach (var so in sortOptions)
            {
                so.NormalizeDisplayName();

                // 名称 → displayName（内置纯文本 / 本地化引用均为空才迁移）
                var nameEntry = so.GetEntry(SortOption.LegacyNameAttrId);
                if (nameEntry?.value != null)
                {
                    var (t, k) = so.displayName.GetLocalizedStringRef();
                    bool targetEmpty = string.IsNullOrEmpty(so.displayName.GetTextValue())
                                       && string.IsNullOrEmpty(t) && string.IsNullOrEmpty(k);
                    if (targetEmpty)
                    {
                        var v = nameEntry.value;
                        if (v.Type == EFieldType.Text)
                        {
                            so.displayName.SetTextValue(0, v.GetTextValue());
                            var (vt, vk) = v.GetLocalizedStringRef();
                            so.displayName.SetLocalizedStringRef(0, vt, vk);
                        }
                        else
                        {
                            so.displayName.SetTextValue(0, v.AsString ?? string.Empty);
                        }
                    }
                }

                // 忽略ID → ignoreIds（内置列表为空才迁移；跳过旧版默认的空占位串，使默认条目数为 0）
                var ignoreEntry = so.GetEntry(SortOption.LegacyIgnoreAttrId);
                if (ignoreEntry?.value?.StringArray != null
                    && (so.ignoreIds == null || so.ignoreIds.Count == 0))
                {
                    if (so.ignoreIds == null) so.ignoreIds = new List<string>();
                    foreach (var s in ignoreEntry.value.StringArray)
                        if (!string.IsNullOrWhiteSpace(s))
                            so.ignoreIds.Add(s);
                }
            }

            // 移除保留定义，使其不再作为通用属性字段出现（上方 / 下方同步会清掉各选项对应残留值）。
            if (schema != null)
                schema.RemoveAll(d => d != null
                    && (d.id == SortOption.LegacyNameAttrId || d.id == SortOption.LegacyIgnoreAttrId));
        }
    }
}

using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 仓库整理排序服务（静态）。承载全部与实例状态无关的排序逻辑：
    /// 槽位 / 任意列表的原地排序、按字段比较、忽略列表判定、功能标签序号，
    /// 以及一次排序内复用的字段查表 <see cref="SortLookup"/>。
    ///
    /// <para>此前这些全部挂在 <see cref="InventoryRuntimeManager"/> 上（占该文件约三分之一），
    /// 但零实例状态依赖。<see cref="InventoryRuntimeManager"/> 上的同名 <c>public static</c>
    /// 成员已保留为薄转发，项目层的既有调用不受影响。</para>
    ///
    /// <para><b>批量排序请用 <see cref="SortSlots"/> / <see cref="SortByItemId{T}"/></b>：
    /// 它们整次排序只构建一份 <see cref="SortLookup"/>；而接收 <c>InventoryDatabase</c> 的
    /// 单次比较重载每次调用都会新建一份。</para>
    /// </summary>
    public static class InventorySortService
    {
        #region 原地排序入口

        /// <summary>
        /// 对任意 slot 列表按指定优先级排序（不触发事件，不写运行时状态）。
        /// 供 UI 层 autoSort 显示排序使用。空槽排末尾。
        /// </summary>
        public static void SortSlots(List<RuntimeItemSlot> slots, List<SortPriority> priorities,
            InventoryDatabase db)
        {
            if (priorities == null || priorities.Count == 0 || slots == null || slots.Count <= 1 || !db) return;

            // 整次排序共用一份字段查表（见 SortLookup）。
            var lookup = new SortLookup(db);
            slots.Sort((a, b) =>
            {
                bool aE = string.IsNullOrEmpty(a.itemId), bE = string.IsNullOrEmpty(b.itemId);
                if (aE && bE) return 0;
                if (aE)       return 1;
                if (bE)       return -1;
                return CompareSlots(a, b, priorities, lookup);
            });
        }

        #endregion

        #region 字段查表

        /// <summary>
        /// 一次排序过程内复用的字段查表。把原先在<b>每次两两比较</b>里重复做的线性扫描
        /// —— 整理选项忽略列表（扫 <c>db.SortOptions</c>）、属性字段定义（扫全部模板 × 属性）、
        /// 道具模板与枚举类型、功能标签序号（扫 <c>db.FunctionTags</c>）—— 预先算成字典，
        /// 使比较器内的查找降到 O(1)。
        ///
        /// <para>只在单次排序期间存活、随即丢弃，因此不存在「数据改动后缓存过期」的问题。</para>
        /// </summary>
        internal sealed class SortLookup
        {
            private readonly InventoryDatabase _db;
            private readonly Dictionary<string, IReadOnlyList<string>> _ignoreIds
                = new Dictionary<string, IReadOnlyList<string>>();
            private readonly Dictionary<string, AttributeDefinition> _attrDefs
                = new Dictionary<string, AttributeDefinition>();
            private readonly Dictionary<string, ItemTemplate> _templates
                = new Dictionary<string, ItemTemplate>();
            private readonly Dictionary<string, EnumType> _enumTypes
                = new Dictionary<string, EnumType>();
            // 惰性：仅当排序用到 "__tagOrder__" 字段时才构建。
            private Dictionary<string, int> _tagOrder;

            internal SortLookup(InventoryDatabase db) => _db = db;

            /// <summary>取该排序字段的忽略 ID 列表（未配置返回 null）。</summary>
            internal IReadOnlyList<string> IgnoreIds(string field)
            {
                if (_ignoreIds.TryGetValue(field, out var ids)) return ids;
                ids = _db ? _db.GetSortOption(field)?.EffectiveIgnoreIds : null;
                _ignoreIds[field] = ids;
                return ids;
            }

            /// <summary>取属性字段定义（模板与功能标签中先到先得；未找到返回 null）。</summary>
            internal AttributeDefinition AttrDef(string attrId)
            {
                if (_attrDefs.TryGetValue(attrId, out var def)) return def;
                def = FindAttrDef(attrId, _db);
                _attrDefs[attrId] = def;
                return def;
            }

            /// <summary>取道具模板（未找到返回 null）。</summary>
            internal ItemTemplate Template(string templateName)
            {
                if (string.IsNullOrEmpty(templateName)) return null;
                if (_templates.TryGetValue(templateName, out var t)) return t;
                t = _db ? _db.GetTemplate(templateName) : null;
                _templates[templateName] = t;
                return t;
            }

            /// <summary>取枚举类型（未找到返回 null）。</summary>
            internal EnumType EnumTypeOf(string enumName)
            {
                if (string.IsNullOrEmpty(enumName)) return null;
                if (_enumTypes.TryGetValue(enumName, out var e)) return e;
                e = _db ? _db.GetEnumType(enumName) : null;
                _enumTypes[enumName] = e;
                return e;
            }

            /// <summary>功能标签名 → 在 <c>db.FunctionTags</c> 中的序号（越小优先级越高；未定义返回 int.MaxValue）。</summary>
            internal int TagOrder(string tagName)
            {
                if (_tagOrder == null)
                {
                    _tagOrder = new Dictionary<string, int>();
                    if (_db)
                        for (int i = 0; i < _db.FunctionTags.Count; i++)
                        {
                            string n = _db.FunctionTags[i]?.name;
                            if (!string.IsNullOrEmpty(n) && !_tagOrder.ContainsKey(n))
                                _tagOrder[n] = i;
                        }
                }
                return !string.IsNullOrEmpty(tagName) && _tagOrder.TryGetValue(tagName, out int idx)
                    ? idx : int.MaxValue;
            }
        }

        #endregion

        #region 比较

        /// <summary>
        /// 按道具 ID 对任意列表做<b>显示排序</b>（原地排序，不触发事件、不写运行时状态）。
        /// 供 UI 层把「商品 / 蓝图 / 候选装备」等条目按其道具属性排序时使用。
        ///
        /// <para>相比在比较器里自行 <c>new RuntimeItemSlot(...)</c> 再调 <see cref="CompareSlots(RuntimeItemSlot,RuntimeItemSlot,List{SortPriority},InventoryDatabase)"/>，
        /// 本方法整次排序只建<b>一份</b>字段查表、只用<b>两个</b>复用的临时槽位——
        /// 省掉每次比较的两次对象分配与多次线性扫描。</para>
        /// </summary>
        /// <param name="list">待原地排序的列表。</param>
        /// <param name="itemIdSelector">从元素取出用于属性比较的道具 ID。</param>
        /// <param name="priorities">排序优先级（按顺序比较，取首个非零结果）。</param>
        /// <param name="db">属性定义与整理选项的来源数据库。</param>
        public static void SortByItemId<T>(List<T> list, Func<T, string> itemIdSelector,
            List<SortPriority> priorities, InventoryDatabase db)
        {
            if (list == null || list.Count <= 1 || itemIdSelector == null
                || priorities == null || priorities.Count == 0 || !db) return;

            var lookup = new SortLookup(db);
            // 两个复用的临时槽位：比较器只读 itemId，List.Sort 为单线程同步执行，复用安全。
            var sa = new RuntimeItemSlot(null, null, 0);
            var sb = new RuntimeItemSlot(null, null, 0);
            list.Sort((x, y) =>
            {
                sa.itemId = itemIdSelector(x);
                sb.itemId = itemIdSelector(y);
                return CompareSlots(sa, sb, priorities, lookup);
            });
        }

        /// <summary>
        /// 比较两个 道具槽位。
        /// 优先级列表按顺序尝试比较，直到找到第一个非零结果返回；如果所有优先级都相等则返回 0。
        ///
        /// <para><b>批量排序请改用 <see cref="SortSlots"/> / <see cref="SortByItemId{T}"/></b>：
        /// 它们整次排序只构建一份字段查表，而本重载<b>每次调用</b>都会新建一份。</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="priorities"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static int CompareSlots(RuntimeItemSlot a, RuntimeItemSlot b,
            List<SortPriority> priorities, InventoryDatabase db)
            => CompareSlots(a, b, priorities, new SortLookup(db));

        /// <summary>比较两个道具槽位（复用调用方预建的字段查表）。</summary>
        internal static int CompareSlots(RuntimeItemSlot a, RuntimeItemSlot b,
            List<SortPriority> priorities, SortLookup lookup)
        {
            foreach (var sp in priorities)
            {
                int cmp = CompareByField(a, b, sp.field, sp.ascending, lookup);
                if (cmp != 0) return cmp;
            }
            return 0;
        }

        /// <summary>
        /// 根据指定字段比较两个道具槽位。
        /// 支持特殊字段 "__id__"（按 itemId 字典序）和 "__tagOrder__"（按第一个标签在数据库定义的顺序）。
        /// 支持数值和字符串类型，字符串按长度比较。
        /// </summary>
        /// <param name="slotA"></param>
        /// <param name="slotB"></param>
        /// <param name="field"></param>
        /// <param name="ascending"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static int CompareByField(RuntimeItemSlot slotA, RuntimeItemSlot slotB,
            string field, bool ascending, InventoryDatabase db)
            => CompareByField(slotA, slotB, field, ascending, new SortLookup(db));

        /// <summary>按指定字段比较两个道具槽位（复用调用方预建的字段查表）。</summary>
        internal static int CompareByField(RuntimeItemSlot slotA, RuntimeItemSlot slotB,
            string field, bool ascending, SortLookup lookup)
        {
            int sign = ascending ? 1 : -1;

            // 中文别名 → 内部特殊键
            if (field == "道具ID")  field = "__id__";
            if (field == "功能标签") field = "__tagOrder__";

            // 读取该字段对应整理选项的忽略列表（内置 ignoreIds，兼容未迁移旧数据）
            IReadOnlyList<string> ignoreIds = lookup.IgnoreIds(field);

            if (field == "__id__")
            {
                bool aIgn = ignoreIds != null && ContainsStr(ignoreIds, slotA.itemId);
                bool bIgn = ignoreIds != null && ContainsStr(ignoreIds, slotB.itemId);
                if (aIgn != bIgn) return aIgn ? 1 : -1;
                if (aIgn) return 0;
                int c = string.Compare(slotA.itemId ?? "", slotB.itemId ?? "",
                    StringComparison.Ordinal);
                return c * sign;
            }

            if (field == "__tagOrder__")
            {
                int oa = GetTagOrder(slotA.itemId, lookup, ignoreIds);
                int ob = GetTagOrder(slotB.itemId, lookup, ignoreIds);
                if (oa == int.MaxValue && ob == int.MaxValue) return 0;
                if (oa == int.MaxValue) return 1;
                if (ob == int.MaxValue) return -1;
                return oa.CompareTo(ob) * sign;
            }

            var itemA  = InventoryDataManager.Instance.GetItem(slotA.itemId);
            var itemB  = InventoryDataManager.Instance.GetItem(slotB.itemId);
            var entryA = itemA?.GetEntry(field);
            var entryB = itemB?.GetEntry(field);

            bool aIgnored = IsIgnoredByField(entryA, field, ignoreIds, lookup);
            bool bIgnored = IsIgnoredByField(entryB, field, ignoreIds, lookup);
            if (aIgnored != bIgnored) return aIgnored ? 1 : -1;
            if (aIgnored) return 0;

            if (entryA?.value?.Type == EFieldType.String
             || entryB?.value?.Type == EFieldType.String)
            {
                string sa = entryA?.value?.AsString ?? string.Empty;
                string sb = entryB?.value?.AsString ?? string.Empty;
                int lenCmp = sa.Length.CompareTo(sb.Length);
                if (lenCmp != 0) return lenCmp * sign;
                return string.Compare(sa, sb, StringComparison.Ordinal) * sign;
            }

            double ka = InventoryRuntimeManager.GetAttrNumeric(entryA);
            double kb = InventoryRuntimeManager.GetAttrNumeric(entryB);
            return ka.CompareTo(kb) * sign;
        }
        
        #endregion

        #region 忽略列表与标签序号

        /// <summary>
        /// 判断属性值是否在整理选项的忽略列表中。
        /// 对于枚举类型会将数值转换为名称进行匹配。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="field"></param>
        /// <param name="ignoreIds"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static bool IsIgnoredByField(AttributeEntry entry, string field,
            IReadOnlyList<string> ignoreIds, InventoryDatabase db)
            => IsIgnoredByField(entry, field, ignoreIds, new SortLookup(db));

        /// <summary>判断属性值是否在忽略列表中（复用调用方预建的字段查表）。</summary>
        internal static bool IsIgnoredByField(AttributeEntry entry, string field,
            IReadOnlyList<string> ignoreIds, SortLookup lookup)
        {
            if (ignoreIds == null || ignoreIds.Count == 0 || entry?.value == null) return false;
            var v = entry.value;
            switch (v.Type)
            {
                case EFieldType.String:
                    return ContainsStr(ignoreIds, v.AsString);
                case EFieldType.Enum:
                {
                    // 枚举存的是 EnumItem.value（自增、永不回收的不可变值），不是 items 的下标——
                    // 删过枚举项后二者会错位，必须按值查找（与 AttributeValue.EnumValueName 一致）。
                    // 枚举类型引用优先取属性定义，取不到时回退属性值自身持久化的 EnumTypeRef。
                    var    def      = lookup.AttrDef(field);
                    string enumRef  = !string.IsNullOrEmpty(def?.enumTypeRef) ? def.enumTypeRef : v.EnumTypeRef;
                    var    enumType = lookup.EnumTypeOf(enumRef);
                    string name     = enumType?.GetItemByValue(v.AsEnumValue)?.name ?? v.AsEnumValue.ToString();
                    return ContainsStr(ignoreIds, name);
                }
                case EFieldType.Int:
                    return ContainsStr(ignoreIds, v.AsInt.ToString());
                case EFieldType.Bool:
                    return ContainsStr(ignoreIds, v.AsInt != 0 ? "true" : "false");
                case EFieldType.Float:
                    return ContainsStr(ignoreIds, v.AsFloat.ToString("G"));
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 查找属性定义。
        /// 优先从第一个数据库中查询到的定义返回。
        /// </summary>
        /// <param name="attrId"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static AttributeDefinition FindAttrDef(string attrId, InventoryDatabase db)
        {
            if (!db) return null;
            foreach (var tmpl in db.ItemTemplates)
                foreach (var def in tmpl.attributes)
                    if (def.id == attrId) return def;
            foreach (var tag in db.FunctionTags)
                foreach (var def in tag.attributes)
                    if (def.id == attrId) return def;
            return null;
        }
        
        /// <summary>
        /// 判断 字符串列表是否包含指定值。
        /// 用于整理选项的忽略列表匹配。
        /// </summary>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ContainsStr(IReadOnlyList<string> list, string value)
        {
            foreach (var s in list)
                if (s == value) return true;
            return false;
        }
        
        /// <summary>
        /// 获取 道具的 功能标签序号。
        /// 返回该道具的第一个标签在数据库定义的 FunctionTags 列表中的索引（越小优先级越高）。
        /// 如果没有标签或所有标签都不在列表中，则返回 int.MaxValue。
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="db"></param>
        /// <param name="ignoreIds"></param>
        /// <returns></returns>
        public static int GetTagOrder(string itemId, InventoryDatabase db,
            IReadOnlyList<string> ignoreIds)
            => GetTagOrder(itemId, new SortLookup(db), ignoreIds);

        /// <summary>取道具的功能标签序号（复用调用方预建的字段查表）。</summary>
        internal static int GetTagOrder(string itemId, SortLookup lookup,
            IReadOnlyList<string> ignoreIds)
        {
            var item = InventoryDataManager.Instance.GetItem(itemId);
            if (item == null) return int.MaxValue;

            // 道具自身标签优先，再回退到模板继承标签（与编辑器展示行为一致）
            foreach (string tag in item.tagRefs)
            {
                if (ignoreIds != null && ContainsStr(ignoreIds, tag)) continue;
                int order = lookup.TagOrder(tag);
                if (order != int.MaxValue) return order;
            }

            // 模板只在道具自身标签全部落空时才需要，故延后解析。
            var tmpl = lookup.Template(item.templateRef);
            if (tmpl != null)
                foreach (string tag in tmpl.tagRefs)
                {
                    if (ignoreIds != null && ContainsStr(ignoreIds, tag)) continue;
                    int order = lookup.TagOrder(tag);
                    if (order != int.MaxValue) return order;
                }

            return int.MaxValue;
        }
        #endregion

    }
}

using Ale.Toolkit.Runtime;
using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 库存领域的排序上下文：把通用排序引擎 <see cref="AttributeSortService"/> 所需的领域信息
    /// （属性载体 / 字段定义 / 整理选项 / 特殊字段）接到 <see cref="InventoryDatabase"/> 与
    /// <see cref="InventoryDataManager"/> 上。<typeparamref name="TData"/> 经构造时传入的 itemId 取值器
    /// 取出道具 ID，再解析为道具定义（<see cref="Item"/>，即属性载体）。
    ///
    /// <para>内建一次排序过程内复用的字段查表缓存（整理选项 / 属性定义 / 道具模板 / 功能标签序号），
    /// 把原先每次两两比较里的线性扫描降到 O(1)；只在单次排序期间存活、随即丢弃，不存在缓存过期问题。
    /// 处理两个特殊排序字段 <c>__id__</c>（道具 ID 字典序）/ <c>__tagOrder__</c>（功能标签序号）及其中文别名。</para>
    /// </summary>
    /// <typeparam name="TData">列表数据元素类型（如 <see cref="RuntimeItemSlot"/> / 商品条目 / 蓝图）。</typeparam>
    public sealed class InventorySortContext<TData> : ISortContext<TData>
    {
        private readonly InventoryDatabase   _db;
        private readonly Func<TData, string> _itemIdOf;

        // 一次排序内复用的字段查表缓存。
        private readonly Dictionary<string, SortOption>          _options   = new Dictionary<string, SortOption>();
        private readonly Dictionary<string, AttributeDefinition> _attrDefs  = new Dictionary<string, AttributeDefinition>();
        private readonly Dictionary<string, ItemTemplate>        _templates = new Dictionary<string, ItemTemplate>();
        private Dictionary<string, int>                          _tagOrder;   // 惰性：仅当排序用到 __tagOrder__ 时构建

        /// <param name="db">属性定义与整理选项的来源数据库。</param>
        /// <param name="itemIdOf">从数据元素取出用于属性比较的道具 ID。</param>
        public InventorySortContext(InventoryDatabase db, Func<TData, string> itemIdOf)
        {
            _db       = db;
            _itemIdOf = itemIdOf;
        }

        // ── ISortContext 实现 ──────────────────────────────────────────────────────

        /// <summary>取该条数据对应的道具定义（属性载体）；无则 null。</summary>
        public AttributeOwner OwnerOf(TData data)
        {
            string id = _itemIdOf != null ? _itemIdOf(data) : null;
            return string.IsNullOrEmpty(id) ? null : InventoryDataManager.Instance.GetItem(id);
        }

        /// <summary>取排序字段的属性定义（道具模板与功能标签中先到先得；缓存复用）。</summary>
        public AttributeDefinition FindDefinition(string field)
        {
            if (string.IsNullOrEmpty(field)) return null;
            if (_attrDefs.TryGetValue(field, out var def)) return def;
            def = InventorySortService.FindAttrDef(field, _db);
            _attrDefs[field] = def;
            return def;
        }

        /// <summary>取该字段的整理选项（缓存复用）。</summary>
        public SortOption OptionOf(string field)
        {
            if (string.IsNullOrEmpty(field)) return null;
            if (_options.TryGetValue(field, out var opt)) return opt;
            opt = _db ? _db.GetSortOption(field) : null;
            _options[field] = opt;
            return opt;
        }

        /// <summary>处理特殊排序字段 <c>__id__</c> / <c>__tagOrder__</c>（含中文别名「道具ID」/「功能标签」）。</summary>
        public bool TryCompareSpecial(TData a, TData b, string field, bool ascending, out int result)
        {
            // 中文别名 → 内部特殊键
            if (field == "道具ID")  field = "__id__";
            if (field == "功能标签") field = "__tagOrder__";

            if (field != "__id__" && field != "__tagOrder__") { result = 0; return false; }

            int sign = ascending ? 1 : -1;
            var ignoreIds = OptionOf(field)?.EffectiveIgnoreIds;
            string ida = _itemIdOf != null ? _itemIdOf(a) : null;
            string idb = _itemIdOf != null ? _itemIdOf(b) : null;

            if (field == "__id__")
            {
                bool aIgn = ignoreIds != null && AttributeSortService.Contains(ignoreIds, ida);
                bool bIgn = ignoreIds != null && AttributeSortService.Contains(ignoreIds, idb);
                if (aIgn != bIgn) { result = aIgn ? 1 : -1; return true; }
                if (aIgn)         { result = 0; return true; }
                result = string.Compare(ida ?? "", idb ?? "", StringComparison.Ordinal) * sign;
                return true;
            }

            // __tagOrder__：功能标签序号（缺失 / 被忽略者恒排末尾，不受升降序影响）。
            int oa = TagOrderOf(ida, ignoreIds);
            int ob = TagOrderOf(idb, ignoreIds);
            if (oa == int.MaxValue && ob == int.MaxValue) { result = 0;  return true; }
            if (oa == int.MaxValue)                        { result = 1;  return true; }
            if (ob == int.MaxValue)                        { result = -1; return true; }
            result = oa.CompareTo(ob) * sign;
            return true;
        }

        // ── 功能标签序号（缓存复用） ─────────────────────────────────────────────────

        /// <summary>
        /// 取道具的功能标签序号：其第一个（未被忽略）标签在 <c>db.FunctionTags</c> 中的索引（越小优先级越高）；
        /// 道具自身标签优先，全部落空再回退模板继承标签；无则返回 <see cref="int.MaxValue"/>。
        /// </summary>
        internal int TagOrderOf(string itemId, IReadOnlyList<string> ignoreIds)
        {
            var item = InventoryDataManager.Instance.GetItem(itemId);
            if (item == null) return int.MaxValue;

            // 道具自身标签优先，再回退到模板继承标签（与编辑器展示行为一致）。
            foreach (string tag in item.tagRefs)
            {
                if (ignoreIds != null && AttributeSortService.Contains(ignoreIds, tag)) continue;
                int order = TagOrder(tag);
                if (order != int.MaxValue) return order;
            }

            // 模板只在道具自身标签全部落空时才需要，故延后解析。
            var tmpl = Template(item.templateRef);
            if (tmpl != null)
                foreach (string tag in tmpl.tagRefs)
                {
                    if (ignoreIds != null && AttributeSortService.Contains(ignoreIds, tag)) continue;
                    int order = TagOrder(tag);
                    if (order != int.MaxValue) return order;
                }

            return int.MaxValue;
        }

        /// <summary>取道具模板（缓存复用；未找到返回 null）。</summary>
        private ItemTemplate Template(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            if (_templates.TryGetValue(templateName, out var t)) return t;
            t = _db ? _db.GetTemplate(templateName) : null;
            _templates[templateName] = t;
            return t;
        }

        /// <summary>功能标签名 → 在 <c>db.FunctionTags</c> 中的序号（惰性构建；未定义返回 <see cref="int.MaxValue"/>）。</summary>
        private int TagOrder(string tagName)
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
}

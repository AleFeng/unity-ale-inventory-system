using Ale.Toolkit.Runtime;
using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 库存领域的排序上下文：把通用排序引擎 <see cref="AttributeSortService"/> 所需的领域信息
    /// （属性载体 / 字段定义 / 整理选项 / 标签数据）接到 <see cref="InventoryDatabase"/> 与
    /// <see cref="InventoryDataManager"/> 上。主键（<see cref="SortFieldKeys.Id"/>）比较、别名归一化、以及
    /// 标签序号（<see cref="SortFieldKeys.TagOrder"/>）排序的通用部分均由基类 <see cref="TagSortContextBase{TData}"/>
    /// 承担；本类仅补全领域绑定与「取元素有序标签名」的领域组合（道具自身标签 → 模板继承标签）。
    ///
    /// <para><typeparamref name="TData"/> 经构造时传入的 itemId 取值器取出道具 ID，再解析为道具定义
    /// （<see cref="Item"/>，即属性载体）。内建一次排序过程内复用的字段查表缓存（整理选项 / 属性定义 /
    /// 道具模板；标签序号表由基类 <see cref="TagSortContextBase{TData}.TagMap"/> 复用），把原先每次两两比较里的
    /// 线性扫描降到 O(1)；只在单次排序期间存活、随即丢弃，不存在缓存过期问题。</para>
    /// </summary>
    /// <typeparam name="TData">列表数据元素类型（如 <see cref="RuntimeItemSlot"/> / 商品条目 / 蓝图）。</typeparam>
    public sealed class InventorySortContext<TData> : TagSortContextBase<TData>
    {
        private readonly InventoryDatabase   _db;
        private readonly Func<TData, string> _itemIdOf;

        // 一次排序内复用的字段查表缓存。
        private readonly Dictionary<string, SortOption>          _options   = new Dictionary<string, SortOption>();
        private readonly Dictionary<string, AttributeDefinition> _attrDefs  = new Dictionary<string, AttributeDefinition>();
        private readonly Dictionary<string, ItemTemplate>        _templates = new Dictionary<string, ItemTemplate>();

        /// <param name="db">属性定义 / 整理选项 / 功能标签的来源数据库。</param>
        /// <param name="itemIdOf">从数据元素取出用于属性比较的道具 ID。</param>
        public InventorySortContext(InventoryDatabase db, Func<TData, string> itemIdOf)
        {
            _db       = db;
            _itemIdOf = itemIdOf;
        }

        // ── 排序主键：库存以构造时传入的取值器取道具 ID（随上下文而变，故改写基类默认）───────────
        protected override string IdOf(TData data) => _itemIdOf != null ? _itemIdOf(data) : null;

        // ── 领域绑定（实现基类抽象）────────────────────────────────────────────────

        /// <summary>取该条数据对应的道具定义（属性载体）；无则 null。</summary>
        public override AttributeOwner OwnerOf(TData data)
        {
            string id = IdOf(data);
            return string.IsNullOrEmpty(id) ? null : InventoryDataManager.Instance.GetItem(id);
        }

        /// <summary>取排序字段的属性定义（道具模板与功能标签中先到先得；缓存复用）。</summary>
        public override AttributeDefinition FindDefinition(string field)
        {
            if (string.IsNullOrEmpty(field)) return null;
            if (_attrDefs.TryGetValue(field, out var def)) return def;
            def = InventorySortService.FindAttrDef(field, _db);
            _attrDefs[field] = def;
            return def;
        }

        /// <summary>取该字段的整理选项（缓存复用）。</summary>
        public override SortOption OptionOf(string field)
        {
            if (string.IsNullOrEmpty(field)) return null;
            if (_options.TryGetValue(field, out var opt)) return opt;
            opt = _db ? _db.GetSortOption(field) : null;
            _options[field] = opt;
            return opt;
        }

        // ── 领域别名 ───────────────────────────────────────────────────────────────

        /// <summary>把中文别名归一化为保留键：「道具ID」→ <see cref="SortFieldKeys.Id"/>；「功能标签」→ <see cref="SortFieldKeys.TagOrder"/>。</summary>
        protected override string NormalizeField(string field)
        {
            if (field == "道具ID")  return SortFieldKeys.Id;
            if (field == "功能标签") return SortFieldKeys.TagOrder;
            return field;
        }

        // ── 标签排序数据（供基类的 __tagOrder__ 通用逻辑使用）────────────────────────

        /// <summary>标签顺序来源：数据库的功能标签列表（下标即序号）。</summary>
        protected override IReadOnlyList<Tag> TagOrderList => _db ? _db.FunctionTags : null;

        /// <summary>取道具的有序标签名：道具自身标签优先，其后接模板继承标签（与编辑器展示行为一致）。</summary>
        protected override IEnumerable<string> TagsOf(TData data)
        {
            var item = InventoryDataManager.Instance.GetItem(IdOf(data));
            return item != null ? EnumerateTags(item) : null;
        }

        /// <summary>惰性产出「道具自身标签 → 模板继承标签」；命中即止，故模板在自身标签全落空前不会被解析。</summary>
        private IEnumerable<string> EnumerateTags(Item item)
        {
            foreach (string tag in item.tagRefs)
                yield return tag;

            var tmpl = Template(item.templateRef);
            if (tmpl != null)
                foreach (string tag in tmpl.tagRefs)
                    yield return tag;
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
    }
}

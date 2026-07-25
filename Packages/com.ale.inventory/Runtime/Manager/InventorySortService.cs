using Ale.Toolkit.Runtime;
using System;
using System.Collections.Generic;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 仓库整理排序服务（静态）。排序算法核心已下沉到通用引擎 <see cref="AttributeSortService"/>；
    /// 本类退化为库存侧薄封装：构造领域排序上下文 <see cref="InventorySortContext{TData}"/> 并转发，
    /// 保留既有公开签名（<see cref="SortSlots"/> / <see cref="SortByItemId{T}"/> / <see cref="CompareSlots"/> 等）
    /// 使项目层调用点不受影响。
    ///
    /// <para><see cref="InventoryRuntimeManager"/> 上的同名 <c>public static</c> 成员仍保留为薄转发，
    /// 项目层的既有调用不受影响。</para>
    ///
    /// <para><b>批量排序请用 <see cref="SortSlots"/> / <see cref="SortByItemId{T}"/></b>：它们整次排序只构建一份
    /// <see cref="InventorySortContext{TData}"/>（内建字段查表缓存）；而接收 <c>InventoryDatabase</c> 的单次比较重载
    /// 每次调用都会新建一份。</para>
    /// </summary>
    public static class InventorySortService
    {
        /// <summary>槽位排序上下文：以 <c>slot.itemId</c> 作为属性载体主键。</summary>
        private static InventorySortContext<RuntimeItemSlot> SlotContext(InventoryDatabase db)
            => new InventorySortContext<RuntimeItemSlot>(db, s => s?.itemId);

        #region 原地排序入口

        /// <summary>
        /// 对任意 slot 列表按指定优先级排序（不触发事件，不写运行时状态）。空槽排末尾。供 UI 层 autoSort 显示排序使用。
        /// </summary>
        public static void SortSlots(List<RuntimeItemSlot> slots, List<SortPriority> priorities,
            InventoryDatabase db)
        {
            if (priorities == null || priorities.Count == 0 || slots == null || slots.Count <= 1 || !db) return;

            var ctx = SlotContext(db);   // 整次排序共用一份领域上下文（内建字段查表缓存）。
            slots.Sort((a, b) =>
            {
                bool aE = string.IsNullOrEmpty(a.itemId), bE = string.IsNullOrEmpty(b.itemId);
                if (aE && bE) return 0;
                if (aE)       return 1;
                if (bE)       return -1;
                return AttributeSortService.Compare(a, b, priorities, ctx);
            });
        }

        /// <summary>
        /// 按道具 ID 对任意列表做<b>显示排序</b>（原地排序，不触发事件、不写运行时状态）。
        /// 供 UI 层把「商品 / 蓝图 / 候选装备」等条目按其道具属性排序时使用。
        /// 整次排序只构建<b>一份</b>领域上下文（内建字段查表缓存），省掉每次比较的对象分配与线性扫描。
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

            AttributeSortService.Sort(list, priorities, new InventorySortContext<T>(db, itemIdSelector));
        }

        #endregion

        #region 比较（薄封装，保留公开签名）

        /// <summary>
        /// 比较两个道具槽位：优先级列表按顺序尝试，直到找到第一个非零结果返回；全部相等则返回 0。
        /// <para><b>批量排序请改用 <see cref="SortSlots"/> / <see cref="SortByItemId{T}"/></b>：本重载每次调用都会新建一份上下文。</para>
        /// </summary>
        public static int CompareSlots(RuntimeItemSlot a, RuntimeItemSlot b,
            List<SortPriority> priorities, InventoryDatabase db)
            => AttributeSortService.Compare(a, b, priorities, SlotContext(db));

        /// <summary>
        /// 按指定字段比较两个道具槽位。支持特殊字段 <c>__id__</c>（按 itemId 字典序）和
        /// <c>__tagOrder__</c>（按第一个功能标签的定义顺序），以及中文别名「道具ID」/「功能标签」。
        /// </summary>
        public static int CompareByField(RuntimeItemSlot slotA, RuntimeItemSlot slotB,
            string field, bool ascending, InventoryDatabase db)
            => AttributeSortService.CompareByField(slotA, slotB, field, ascending, SlotContext(db));

        #endregion

        #region 忽略列表与标签序号（薄封装 / 领域辅助）

        /// <summary>
        /// 判断属性值是否在整理选项的忽略列表中。对枚举类型会把数值转换为名称进行匹配。
        /// </summary>
        public static bool IsIgnoredByField(AttributeEntry entry, string field,
            IReadOnlyList<string> ignoreIds, InventoryDatabase db)
            => AttributeSortService.IsIgnored(entry, field, ignoreIds, f => FindAttrDef(f, db));

        /// <summary>
        /// 查找属性定义：优先从道具模板中命中，再从功能标签中命中；未找到返回 null。
        /// </summary>
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

        /// <summary>判断字符串列表是否包含指定值（用于整理选项的忽略列表匹配）。</summary>
        public static bool ContainsStr(IReadOnlyList<string> list, string value)
            => AttributeSortService.Contains(list, value);

        /// <summary>
        /// 获取道具的功能标签序号：返回该道具第一个（未被忽略）标签在数据库 FunctionTags 列表中的索引
        /// （越小优先级越高）；没有标签或全部落空则返回 <see cref="int.MaxValue"/>。
        /// </summary>
        public static int GetTagOrder(string itemId, InventoryDatabase db,
            IReadOnlyList<string> ignoreIds)
            => new InventorySortContext<string>(db, s => s).TagOrderOf(itemId, ignoreIds);

        #endregion
    }
}

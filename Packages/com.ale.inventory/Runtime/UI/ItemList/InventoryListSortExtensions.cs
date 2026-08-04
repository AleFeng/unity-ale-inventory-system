using Ale.Toolkit.Runtime;
using Ale.Toolkit.Runtime.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 库存侧对通用虚拟列表 <see cref="UiwVirtualListBase{TData,TCell}"/> 的排序配置扩展。
    /// 通用基类的 <c>ConfigureSort</c> 只认领域无关的 <see cref="ISortContext{TData}"/>；本扩展保留库存既有
    /// 调用形状（排序键 + 数据库 + 排序条件），内部据此构造 <see cref="InventorySortContext{TData}"/> 后转发，
    /// 使各视图调用点一字不改（在子类内部调用时需以 <c>this.</c> 限定，扩展方法方可参与解析）。
    /// </summary>
    public static class InventoryListSortExtensions
    {
        /// <summary>
        /// 配置显示 / 写运行时排序：排序键取 data → 道具 ID，据 <paramref name="db"/> 构造库存排序上下文后转发到基类。
        /// <paramref name="writeRuntime"/> 非空 = 「写运行时」模式；为空 = 「显示排序」模式（详见基类 ConfigureSort 说明）。
        /// </summary>
        public static void ConfigureSort<TData, TCell>(this UiwVirtualListBase<TData, TCell> list,
            Func<TData, string> sortKeySelector, InventoryDatabase db,
            IReadOnlyList<SortPriority> priorities, IReadOnlyList<SortPriority> tiebreakers,
            Action<List<SortPriority>> writeRuntime = null)
            where TCell : Component
        {
            if (!list) return;
            var ctx = new InventorySortContext<TData>(db, sortKeySelector);
            list.ConfigureSort(ctx, priorities, tiebreakers, writeRuntime);
        }
    }
}

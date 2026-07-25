using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Editor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 库存编辑器面板与主窗口之间的交互契约。通用部分（数据库 / SerializedObject / 解析器 / Undo / 标脏 / 重绘）
    /// 由 toolkit 的 <see cref="IEditorDbContext{TDb}"/> 提供并闭合为 <see cref="InventoryDatabase"/>；
    /// 本接口只在其上补充库存领域专有的「按种类查重复 ID」。名字与用法不变，六大系统面板零改动。
    /// </summary>
    public interface IInventoryEditorContext : IEditorDbContext<InventoryDatabase>
    {
        /// <summary>
        /// 取该种类当前重复（或空）的 ID 集合，用于列表与 Inspector 的红色高亮。
        /// <para>此前是六个并列属性（<c>DuplicateIds</c> / <c>InventoryDuplicateIds</c> / …），
        /// 新增一个系统就要同步改接口、窗口字段、扫描器、缓存刷新与状态栏五处；
        /// 收成按种类查表后，遗漏某一处不再可能。</para>
        /// </summary>
        HashSet<string> DuplicateIdsOf(EInventoryEntityKind kind);
    }
}

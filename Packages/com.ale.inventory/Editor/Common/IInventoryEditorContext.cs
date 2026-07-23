using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.Serialization;
using UnityEditor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 编辑器面板与主窗口之间的交互契约。面板通过它访问数据库、记录 Undo、标记脏、查询重复 ID 等，
    /// 而无需直接耦合到窗口类型。
    /// </summary>
    public interface IInventoryEditorContext
    {
        /// <summary>当前编辑的数据库（可能为 null）。</summary>
        InventoryDatabase Database { get; }

        /// <summary>数据库对应的 SerializedObject（用于属性绘制路径）。</summary>
        SerializedObject Serialized { get; }

        /// <summary>导出/资源引用解析器（编辑器实现）。</summary>
        IAssetRefResolver Resolver { get; }

        /// <summary>
        /// 取该种类当前重复（或空）的 ID 集合，用于列表与 Inspector 的红色高亮。
        /// <para>此前是六个并列属性（<c>DuplicateIds</c> / <c>InventoryDuplicateIds</c> / …），
        /// 新增一个系统就要同步改接口、窗口字段、扫描器、缓存刷新与状态栏五处；
        /// 收成按种类查表后，遗漏某一处不再可能。</para>
        /// </summary>
        HashSet<string> DuplicateIdsOf(EInventoryEntityKind kind);

        /// <summary>在修改前调用，记录 Undo。</summary>
        void RecordUndo(string actionName);

        /// <summary>在修改后调用，标记数据库为脏并触发重复 ID 重算。</summary>
        void MarkDirty();

        /// <summary>请求重绘窗口。</summary>
        void Repaint();
    }
}

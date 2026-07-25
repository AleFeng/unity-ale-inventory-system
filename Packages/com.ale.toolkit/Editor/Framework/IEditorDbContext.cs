using Ale.Toolkit.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 编辑器面板与主窗口之间的通用交互契约（领域无关）。面板通过它访问数据库、对应的
    /// <see cref="SerializedObject"/>、资源引用解析器，并记录 Undo / 标脏 / 请求重绘，而无需直接耦合窗口类型。
    ///
    /// <para>宿主插件把自己的编辑器上下文接口派生自本接口并闭合 <typeparamref name="TDb"/>
    /// （如库存的 <c>IInventoryEditorContext : IEditorDbContext&lt;InventoryDatabase&gt;</c>），
    /// 即可复用 toolkit 的三列框架基类与 <see cref="EditorMutate"/> 等通用件。</para>
    /// </summary>
    /// <typeparam name="TDb">宿主的数据库资产类型（<see cref="ScriptableObject"/>）。</typeparam>
    public interface IEditorDbContext<TDb> where TDb : ScriptableObject
    {
        /// <summary>当前编辑的数据库（可能为 null）。</summary>
        TDb Database { get; }

        /// <summary>数据库对应的 <see cref="SerializedObject"/>（用于属性绘制路径）。</summary>
        SerializedObject Serialized { get; }

        /// <summary>导出 / 资源引用解析器（编辑器实现）。</summary>
        IAssetRefResolver Resolver { get; }

        /// <summary>在修改前调用，记录 Undo。</summary>
        void RecordUndo(string actionName);

        /// <summary>在修改后调用，标记数据库为脏并触发相关重算。</summary>
        void MarkDirty();

        /// <summary>请求重绘窗口。</summary>
        void Repaint();
    }
}

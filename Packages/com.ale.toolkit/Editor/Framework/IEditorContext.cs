using Ale.Toolkit.Runtime;
using UnityEditor;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 编辑器面板 / 绘制器与宿主窗口之间的<b>通用</b>交互契约（不含具体数据库类型）：提供
    /// <see cref="SerializedObject"/>、资源引用解析器，以及 Undo / 标脏 / 重绘。
    ///
    /// <para>只需「改数据 + 记 Undo」而不访问具体数据库的绘制器（如各属性字段绘制器）用本接口即可，
    /// 无需泛型化；需要读写具体数据库的场合用 <see cref="IEditorDbContext{TDb}"/>。</para>
    /// </summary>
    public interface IEditorContext
    {
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

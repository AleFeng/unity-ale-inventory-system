using UnityEngine;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 在通用 <see cref="IEditorContext"/> 之上补充对具体数据库资产的访问。宿主把自己的编辑器上下文接口
    /// 派生自本接口并闭合 <typeparamref name="TDb"/>（如库存的
    /// <c>IInventoryEditorContext : IEditorDbContext&lt;InventoryDatabase&gt;</c>），即可复用 toolkit 的三列框架
    /// 基类与 <see cref="EditorMutate"/> 等通用件。
    /// </summary>
    /// <typeparam name="TDb">宿主的数据库资产类型（<see cref="ScriptableObject"/>）。</typeparam>
    public interface IEditorDbContext<TDb> : IEditorContext where TDb : ScriptableObject
    {
        /// <summary>当前编辑的数据库（可能为 null）。</summary>
        TDb Database { get; }
    }
}

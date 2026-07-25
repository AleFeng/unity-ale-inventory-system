using Ale.Inventory.Runtime;
using Ale.Toolkit.Editor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 库存编辑器框架的领域别名：把 toolkit 的泛型基类闭合到 <see cref="InventoryDatabase"/>，保住原有类名，
    /// 使各子类的声明与用法不变（同名不同元数 + 不同命名空间，C# 不冲突）。
    ///
    /// <para>工具窗口基类闭合于此；三列框架的三个面板基类别名在框架下沉（S8 面板部分）时补入本文件。</para>
    /// </summary>
    public abstract class InventoryToolWindowBase : EditorToolWindowBase<InventoryDatabase> { }
}

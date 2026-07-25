using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Editor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 库存编辑器框架的领域别名：把 toolkit 的泛型基类闭合到 <see cref="InventoryDatabase"/>，保住原有类名，
    /// 使各子类的声明与用法不变（同名不同元数 + 不同命名空间，C# 不冲突）。
    ///
    /// <para>三个面板别名对「吃 <see cref="IInventoryEditorContext"/> 的抽象方法」做<b>桥接</b>：
    /// 密封 override 泛型基类的 <c>IEditorDbContext&lt;InventoryDatabase&gt;</c> 版本，转调本别名重新声明的
    /// <c>IInventoryEditorContext</c> 版本——从而 11+6+6 个子类的 <c>DrawInspector</c> / <c>AddFromTemplate</c> /
    /// <c>DrawEntityList</c> 等 override 一字不改。运行时 <c>ctx</c> 恒为 <see cref="IInventoryEditorContext"/>，向下转型安全。</para>
    /// </summary>
    public abstract class InventoryToolWindowBase : EditorToolWindowBase<InventoryDatabase> { }

    /// <summary>左栏主列表面板的库存别名。</summary>
    /// <typeparam name="T">列表元素类型。</typeparam>
    public abstract class EditorMasterListPanel<T>
        : Ale.Toolkit.Editor.EditorMasterListPanel<InventoryDatabase, T> where T : class
    {
        public sealed override void DrawInspector(IEditorDbContext<InventoryDatabase> ctx, T item)
            => DrawInspector((IInventoryEditorContext)ctx, item);

        /// <summary>绘制选中项 Inspector（<paramref name="item"/> 为 null 表示未选中）。</summary>
        public abstract void DrawInspector(IInventoryEditorContext ctx, T item);

        protected sealed override void BeforeDrawList(IEditorDbContext<InventoryDatabase> ctx)
            => BeforeDrawList((IInventoryEditorContext)ctx);

        /// <summary>绘制列表之前的钩子（如按需重建数据源）。默认空。</summary>
        protected virtual void BeforeDrawList(IInventoryEditorContext ctx) { }
    }

    /// <summary>中栏实体列表面板的库存别名。重复 ID 经 <see cref="Kind"/> + <see cref="IInventoryEditorContext.DuplicateIdsOf"/> 提供。</summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TTemplate">其模板类型。</typeparam>
    public abstract class EditorEntityListPanel<TEntity, TTemplate>
        : Ale.Toolkit.Editor.EditorEntityListPanel<InventoryDatabase, TEntity, TTemplate>
        where TEntity : class where TTemplate : class
    {
        /// <param name="dragId">拖拽状态机的稳定标识串。</param>
        protected EditorEntityListPanel(string dragId) : base(dragId) { }

        /// <summary>实体种类（用于取重复 ID 集合）。</summary>
        protected abstract EInventoryEntityKind Kind { get; }

        protected sealed override HashSet<string> DuplicateIds(IEditorDbContext<InventoryDatabase> ctx)
            => ((IInventoryEditorContext)ctx).DuplicateIdsOf(Kind);

        protected sealed override TEntity AddFromTemplate(IEditorDbContext<InventoryDatabase> ctx, string templateName)
            => AddFromTemplate((IInventoryEditorContext)ctx, templateName);

        /// <summary>从模板新建一个实体并加入数据库，返回之（含 RecordUndo / MarkDirty）。</summary>
        protected abstract TEntity AddFromTemplate(IInventoryEditorContext ctx, string templateName);

        protected sealed override TEntity QuickAdd(IEditorDbContext<InventoryDatabase> ctx)
            => QuickAdd((IInventoryEditorContext)ctx);

        /// <summary>复制末尾条目新建一个实体并加入数据库，返回之（含 RecordUndo / MarkDirty）。</summary>
        protected abstract TEntity QuickAdd(IInventoryEditorContext ctx);
    }

    /// <summary>三列系统页签的库存别名。</summary>
    /// <typeparam name="TEntity">中列实体类型。</typeparam>
    public abstract class EditorThreeColumnTab<TEntity>
        : Ale.Toolkit.Editor.EditorThreeColumnTab<InventoryDatabase, TEntity> where TEntity : class
    {
        protected sealed override TEntity DrawEntityList(IEditorDbContext<InventoryDatabase> ctx, TEntity displaySelected)
            => DrawEntityList((IInventoryEditorContext)ctx, displaySelected);

        /// <summary>绘制中列实体列表，返回其当前选中项。</summary>
        protected abstract TEntity DrawEntityList(IInventoryEditorContext ctx, TEntity displaySelected);

        protected sealed override void DrawEntityInspector(IEditorDbContext<InventoryDatabase> ctx, TEntity entity)
            => DrawEntityInspector((IInventoryEditorContext)ctx, entity);

        /// <summary>绘制右列的实体 Inspector。</summary>
        protected abstract void DrawEntityInspector(IInventoryEditorContext ctx, TEntity entity);
    }
}

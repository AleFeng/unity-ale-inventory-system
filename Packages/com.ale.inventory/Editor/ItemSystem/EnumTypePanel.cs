using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Runtime;
using Ale.Toolkit.Editor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 枚举类型面板（库存闭合）：把编辑机制全部下沉至 <see cref="EditorEnumTypePanel{TDb}"/>，
    /// 本类仅绑定 <see cref="InventoryDatabase.EnumTypes"/>。schema 内枚举引用经 <see cref="InventoryDatabase"/>
    /// 实现的 <see cref="IEnumTypeSource"/> 解析。
    /// </summary>
    public class EnumTypePanel : EditorEnumTypePanel<InventoryDatabase>
    {
        protected override List<EnumType> GetList(InventoryDatabase db) => db.EnumTypes;
    }
}

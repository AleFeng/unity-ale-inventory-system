using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Runtime;
using Ale.Toolkit.Editor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 功能标签面板（库存闭合）：编辑机制全部下沉至 <see cref="EditorTagPanel{TDb}"/>，本类仅绑定
    /// <see cref="InventoryDatabase.FunctionTags"/>。功能标签的顺序会影响「整理列表」中「功能标签」的排序依据。
    /// </summary>
    public class FunctionTagPanel : EditorTagPanel<InventoryDatabase>
    {
        protected override List<Tag> GetList(InventoryDatabase db) => db.FunctionTags;
    }
}

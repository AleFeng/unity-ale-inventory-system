using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Runtime;
using Ale.Toolkit.Editor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 数字格式配置面板（库存闭合）：把编辑机制全部下沉至 <see cref="EditorNumberFormatConfigPanel{TDb}"/>，
    /// 本类仅绑定 <see cref="InventoryDatabase.NumberFormatConfigs"/>。
    /// </summary>
    public class NumberFormatConfigPanel : EditorNumberFormatConfigPanel<InventoryDatabase>
    {
        protected override List<NumberFormatConfig> GetList(InventoryDatabase db) => db.NumberFormatConfigs;
    }
}

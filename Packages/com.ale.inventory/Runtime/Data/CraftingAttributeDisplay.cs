using System;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 蓝图 UI 设置中的「属性字段显示」配置项：在蓝图条目/详情上显示主产出道具的某个属性值，
    /// 形如「Label 属性值」（例如「等级 5」「价值 120」）。
    /// </summary>
    [Serializable]
    public class CraftingAttributeDisplay
    {
        /// <summary>显示在 UI 上的标签名称（如「等级」「价值」）。</summary>
        public string label;

        /// <summary>关联的属性字段 ID（道具系统中道具模板/功能标签定义的属性字段）。</summary>
        public string attrId;

        public CraftingAttributeDisplay()
        {
        }

        public CraftingAttributeDisplay(string label, string attrId)
        {
            this.label  = label;
            this.attrId = attrId;
        }

        /// <summary>深拷贝。</summary>
        public CraftingAttributeDisplay Clone() => new CraftingAttributeDisplay(label, attrId);
    }
}

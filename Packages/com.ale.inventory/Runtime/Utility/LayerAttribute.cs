using UnityEngine;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 标记一个 <c>int</c> 字段为「单个 Layer 选择」，在 Inspector 中以 Layer 单选下拉呈现
    /// （效果与 GameObject 右上角的 Layer 下拉一致）。由 Editor 层的 LayerDrawer 绘制。
    /// 字段值为 Layer 索引（0~31）。
    /// </summary>
    public class LayerAttribute : PropertyAttribute { }
}

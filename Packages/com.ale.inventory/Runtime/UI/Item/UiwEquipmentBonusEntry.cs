#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 装备属性加成条目显示组件。一行 = 标签 + 数值（如「攻击力  120」）。
    /// 同一预制体亦可用作分组标题行（数值留空，仅显示分组标签名）。
    /// 由 <see cref="UiwEquipmentBonusPanel"/> 实例化并填充。
    /// </summary>
    public class UiwEquipmentBonusEntry : MonoBehaviour
    {
        [Header("属性加成条目")]
        [Tooltip("标签文本（属性名 / 分组标题）。")]
        public InventoryText labelText;
        [Tooltip("数值文本（分组标题行可留空）。")]
        public InventoryText valueText;

        /// <summary>填充标签与数值文本（value 传 null/空 用于分组标题行）。</summary>
        public void SetData(string label, string value)
        {
            if (labelText) labelText.text = label ?? string.Empty;
            if (valueText) valueText.text = value ?? string.Empty;
        }
    }
}

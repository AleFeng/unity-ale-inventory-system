#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using InventorySystem.Runtime;
using UnityEngine;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 仓库页签按钮（MonoBehaviour）。
    /// 显示仓库名称（<see cref="Inventory.displayNameText"/>，为空时退回 <see cref="Inventory.id"/>）并反映选中状态。
    /// 由 <see cref="UiwInventoryView"/> 统一创建和管理。
    /// </summary>
    public class UiwInventoryTab : MonoBehaviour
    {
        [Header("子组件引用")]
        [Tooltip("显示仓库名称的文本组件。")]
        public InventoryText label;

        [Tooltip("选中状态指示器（选中时激活，未选中时隐藏）。")]
        public GameObject selectedIndicator;

        /// <summary>当前绑定的仓库 ID。</summary>
        public string InventoryId { get; private set; }

        /// <summary>设置页签显示数据。</summary>
        /// <param name="inventoryId">对应 Inventory.id。</param>
        /// <param name="displayName">UI 显示名称；为空时退回使用 <paramref name="inventoryId"/>。</param>
        /// <param name="isSelected">是否处于选中状态。</param>
        public void SetData(string inventoryId, string displayName, bool isSelected)
        {
            InventoryId = inventoryId;

            if (label != null)
            {
                string shown = !string.IsNullOrEmpty(displayName) ? displayName
                             : (string.IsNullOrEmpty(inventoryId) ? "—" : inventoryId);
                label.text = shown;
            }

            if (selectedIndicator != null)
                selectedIndicator.SetActive(isSelected);
        }
    }
}

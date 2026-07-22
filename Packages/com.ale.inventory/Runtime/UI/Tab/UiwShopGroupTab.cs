#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using UnityEngine;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 商店商品组页签按钮（MonoBehaviour）。
    /// 显示商品组名称（含固定首位「全部」）并反映选中状态。
    /// 由 <see cref="UiwShopViewBase"/> 统一创建和管理，结构与 <see cref="UiwInventoryTab"/> 对称。
    /// </summary>
    public class UiwShopGroupTab : MonoBehaviour
    {
        [Header("子组件引用")]
        [Tooltip("显示商品组名称的文本组件。")]
        public InventoryText label;

        [Tooltip("选中状态指示器（选中时激活，未选中时隐藏）。")]
        public GameObject selectedIndicator;

        /// <summary>当前页签对应的商品组（「全部」页签为 null）。</summary>
        public ShopCommodityGroup Group { get; private set; }

        /// <summary>设置页签显示数据。</summary>
        /// <param name="group">对应商品组；null 表示「全部」聚合页签。</param>
        /// <param name="displayName">页签显示名称。</param>
        /// <param name="isSelected">是否处于选中状态。</param>
        public void SetData(ShopCommodityGroup group, string displayName, bool isSelected)
        {
            Group = group;

            if (label != null)
                label.text = string.IsNullOrEmpty(displayName) ? "—" : displayName;

            if (selectedIndicator != null)
                selectedIndicator.SetActive(isSelected);
        }
    }
}

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
    /// 道具格子简单显示组件（MonoBehaviour）。
    /// 仅显示图标和数量，用于背包顶部货币道具等简洁场景。
    /// 图标、数量、数字格式字段继承自 <see cref="UiwInventoryItemBase"/>。
    /// </summary>
    public class UiwInventoryItemSimple : UiwInventoryItemBase
    {
        /// <summary>更新显示指定道具及数量。</summary>
        public void SetItem(string itemId, int count)
        {
            ApplyIcon(InventoryDataManager.Instance.GetItem(itemId));
            if (countText) countText.text = FormatNumber(count);
            SetTooltipItemId(itemId);
            gameObject.SetActive(true);
        }

        /// <summary>清空显示。</summary>
        public void SetEmpty()
        {
            ClearIcon();
            if (countText) countText.text = string.Empty;
            SetTooltipItemId(null);
        }
    }
}

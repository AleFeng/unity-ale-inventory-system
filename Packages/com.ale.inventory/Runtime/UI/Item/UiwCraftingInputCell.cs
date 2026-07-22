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
    /// 制作蓝图详情中的「消耗道具」行（MonoBehaviour）。显示道具图标、名称、需求数量、当前持有数量
    /// （跨蓝图制作仓库统计）。持有不足时数量文本变色。图标 / 名称 / 数字格式继承自 <see cref="UiwInventoryItemBase"/>。
    /// </summary>
    public class UiwCraftingInputCell : UiwInventoryItemBase
    {
        [Header("消耗信息")]
        [Tooltip("需求数量文本（制作一次需要的数量）。可空。")]
        public InventoryText requireText;
        [Tooltip("当前持有数量文本（制作仓库中的可用数量）。可空。")]
        public InventoryText ownedText;
        [Tooltip("合并显示文本（持有/需求），格式见 amountFormat。可空。")]
        public InventoryText amountText;
        [Tooltip("合并显示格式：{0}=持有, {1}=需求。")]
        public string amountFormat = "{0}/{1}";
        [Tooltip("持有充足时颜色。")]
        public Color enoughColor = Color.white;
        [Tooltip("持有不足时颜色。")]
        public Color shortColor = new Color(0.9f, 0.3f, 0.3f, 1f);

        /// <summary>绑定到指定蓝图的某条消耗道具，刷新显示。</summary>
        public void Bind(CraftingBlueprint bp, CraftingItemAmount input)
        {
            if (bp == null || input == null) { SetEmpty(); return; }

            var item = InventoryDataManager.Instance.GetItem(input.itemId);
            ApplyIcon(item);
            ApplyName(item, input.itemId);
            SetTooltipItemId(input.itemId);

            int  need   = Mathf.Max(1, input.count);
            int  owned  = CraftingRuntimeManager.Instance.GetOwnedAcross(bp, input.itemId);
            bool enough = owned >= need;
            Color clr   = enough ? enoughColor : shortColor;

            if (requireText) requireText.text = FormatNumber(need);
            if (ownedText)  { ownedText.text  = FormatNumber(owned); ownedText.color  = clr; }
            if (amountText) { amountText.text = string.Format
                (amountFormat, FormatNumber(need), FormatNumber(owned)); amountText.color = clr; }

            gameObject.SetActive(true);
        }

        /// <summary>设为空态（隐藏，从对象池回收时调用）。</summary>
        public void SetEmpty()
        {
            ClearIcon();
            ClearNameAndQuality();
            SetTooltipItemId(null);
            if (requireText) requireText.text = string.Empty;
            if (ownedText)   ownedText.text   = string.Empty;
            if (amountText)  amountText.text  = string.Empty;
            gameObject.SetActive(false);
        }
    }
}

#if ATK_TMP
using UiText = TMPro.TMP_Text;
#else
using UiText = UnityEngine.UI.Text;
#endif

using Ale.Toolkit.Runtime.UI;
using Ale.Toolkit.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具格子详细信息显示组件（MonoBehaviour）。
    /// 由虚拟列表 <see cref="UiwInventoryItemOrderList"/> 统一驱动 <see cref="SetSlot"/> / <see cref="SetEmpty"/>。
    ///
    /// <para>显示内容：品质背景、图标、名称、描述、数量、价格货币列表、已购数量、悬停高亮、堆叠已满提示。</para>
    /// <para>图标 / 数量 / 数字格式 / 名称 / 品质 / 悬停 / 堆叠提示 等公共字段均继承自
    /// <see cref="UiwInventoryItemSlotBase"/>。</para>
    /// </summary>
    public class UiwInventoryItemDetail : UiwInventoryItemSlotBase
    {
        #region Detail 专有属性字段 ID

        [Header("属性字段 ID")]
        [Tooltip("描述属性 ID。")]
        public string descAttrId          = "描述";
        [Tooltip("价格属性 ID。")]
        public string priceAttrId         = "货币ID:价格";
        [Tooltip("已购数量属性 ID（格式 \"已购/上限\"）。")]
        public string purchaseCountAttrId;


        #endregion

        #region Detail 专有 UI 引用

        [Header("信息")]
        [Tooltip("描述文本。")]
        public UiText descText;
        
        [Header("价格")]
        [Tooltip("货币格子 预制体。")]
        public UiwInventoryItemSimple priceCurrencyPrefab;
        [Tooltip("货币格子 父容器；未挂载该容器、或道具无价格数据时整个价格区不显示。")]
        public Transform priceContainer;

        /// <summary>价格货币格子实例池（只增不减，多余的实例 SetActive(false) 复用）。</summary>
        private readonly UiwWidgetPool<UiwInventoryItemSimple> _pricePool = new UiwWidgetPool<UiwInventoryItemSimple>();

        // 回收功能标签行：除隐藏外还要释放 Addressable 背景图句柄。无捕获，编译器缓存为静态委托。


        #endregion

        #region 公开接口


        /// <summary>将此格子绑定到指定仓库的指定 slot，刷新所有显示。</summary>
        public void SetSlot(string inventoryId, RuntimeItemSlot slot)
        {
            if (slot == null) { SetEmpty(); return; }

            // 池化实例首次显示（滚入，GO 尚未激活）→ 播放淡入；就地刷新（已激活）→ 不淡，直接换。
            bool firstShow = !gameObject.activeSelf;

            SetBoundSlot(inventoryId, slot);   // 记录来源仓库 / 槽位 / 道具 ID / 数量 + 悬停弹窗目标（基类共用）

            var item = InventoryDataManager.Instance.GetItem(slot.itemId);

            ApplyQualityBackground(item, animate: firstShow);
            ApplyIcon(item, animate: firstShow);

            // 名称
            ApplyName(item, slot.itemId);

            // 描述（未挂载 descText 组件即不显示）
            if (descText)
                descText.text = (item != null && !string.IsNullOrEmpty(descAttrId))
                    ? (item.GetEntry(descAttrId)?.value?.AsString ?? string.Empty)
                    : string.Empty;

            // 数量
            if (countText) countText.text = FormatNumber(slot.count);

            // 价格（StringIntPair 数组：key = 货币道具 ID，value = 数量）
            if (priceContainer)
            {
                // 未挂载货币格子 Prefab 或道具无价格属性 → 价格区不显示
                bool show = priceCurrencyPrefab
                            && item != null && !string.IsNullOrEmpty(priceAttrId);
                if (show)
                {
                    var av    = item.GetAttributeValue(priceAttrId);
                    int count = av != null ? av.Count : 0;

                    _pricePool.Configure(priceCurrencyPrefab, priceContainer);
                    _pricePool.Begin();
                    for (int i = 0; i < count; i++)
                    {
                        var cell = _pricePool.Next();
                        if (!cell) break;
                        var (currencyId, amount) = av.GetStringIntPair(i);
                        cell.numberFormat = numberFormat;   // 与商店详情行一致：价格按本行的数字格式显示（1.16.0）
                        cell.SetItem(currencyId, amount);
                    }
                    _pricePool.End();

                    priceContainer.gameObject.SetActive(count > 0);
                }
                else
                {
                    priceContainer.gameObject.SetActive(false);
                }
            }
            
            // 功能标签
            ApplyItemLabels(item);

            gameObject.SetActive(true);
            // 根 CanvasGroup 淡入由列表引擎在生成后统一调 PlayShowFade 驱动（见 UiwListFadeCell / UiwVirtualListBase）；
            // 本处仅按 firstShow 决定图标 / 背景框的加载门控淡入（上面 ApplyIcon/ApplyQualityBackground(animate)）。

            bool isFull = item != null && item.stackLimit > 0 && slot.count >= item.stackLimit;
            SetStackFull(isFull, animate: true);
        }

        /// <summary>将此格子设为空态（隐藏内容，清空数据）。</summary>
        public void SetEmpty()
        {
            ResetRootVisible();   // 打断在途根淡变并复位（回收前保险；本方法末尾会停用本行）
            ClearBoundSlot();   // 清除道具标识 + 悬停弹窗目标（基类共用）
            ClearIcon();
            ClearNameAndQuality();
            if (descText)           descText.text            = string.Empty;
            if (countText)          countText.text           = string.Empty;
            if (priceContainer)     priceContainer.gameObject.SetActive(false);
            ClearItemLabels();
            _pricePool.RecycleAll();
            ClearStackFull();
            gameObject.SetActive(false);
        }

        // 右键快速装备（广播「道具右键」事件）已上移至基类 UiwInventoryItemSlotBase.OnPointerClick，本类无需重复实现。


        #endregion


    }
}

using System;
using System.Collections.Generic;
using InventorySystem.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 制作蓝图列表条目（MonoBehaviour）。左侧显示主产出道具图标，右上显示蓝图名称，
    /// 右下按蓝图模板「UI 设置 - 属性字段显示」逐行显示「Label 值」（值取自主产出道具的属性）。
    /// 点击选中并通过 <see cref="OnClicked"/> 通知列表。图标 / 名称继承自 <see cref="UiwInventoryItemBase"/>。
    /// </summary>
    public class UiwCraftingBlueprintCell : UiwInventoryItemBase, IPointerClickHandler
    {
        [Header("蓝图条目")]
        [Tooltip("选中状态指示器（选中时激活）。可空。")]
        public GameObject selectedIndicator;
        [Tooltip("属性显示行容器。可空。")]
        public Transform attrLineContainer;
        [Tooltip("属性显示行预制体（UiwTextLabel；以「Label 值」文本显示）。可空。")]
        public UiwTextLabel attrLinePrefab;

        /// <summary>当前绑定的蓝图。</summary>
        public CraftingBlueprint Blueprint { get; private set; }

        /// <summary>点击选中事件。</summary>
        public event Action<CraftingBlueprint> OnClicked;

        private readonly List<UiwTextLabel> _lines = new List<UiwTextLabel>();

        /// <summary>绑定蓝图并刷新显示。</summary>
        public void Bind(CraftingBlueprint bp, bool selected)
        {
            Blueprint = bp;
            var main = bp != null ? bp.PrimaryOutput : null;
            var item = main != null ? InventoryDataManager.Instance.GetItem(main.itemId) : null;

            ApplyIcon(item);
            ApplyQualityBackground(item);
            if (nameText) nameText.text = ResolveName(bp);
            SetTooltipItemId(main != null ? main.itemId : null);

            ApplyAttrLines(bp, item);
            if (selectedIndicator) selectedIndicator.SetActive(selected);
            gameObject.SetActive(true);
        }

        /// <summary>仅更新选中高亮（不重绑数据）。</summary>
        public void SetSelected(bool selected)
        {
            if (selectedIndicator) selectedIndicator.SetActive(selected);
        }

        /// <summary>设为空态（隐藏，从对象池回收时调用）。</summary>
        public void SetEmpty()
        {
            Blueprint = null;
            ClearIcon();
            ClearNameAndQuality();
            SetTooltipItemId(null);
            foreach (var l in _lines) if (l) l.gameObject.SetActive(false);
            if (selectedIndicator) selectedIndicator.SetActive(false);
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Blueprint != null) OnClicked?.Invoke(Blueprint);
        }

        private void ApplyAttrLines(CraftingBlueprint bp, Item item)
        {
            if (!attrLineContainer || !attrLinePrefab) return;

            int idx = 0;
            if (bp != null && bp.attributeDisplays != null)
            {
                foreach (var ad in bp.attributeDisplays)
                {
                    if (ad == null || string.IsNullOrEmpty(ad.attrId)) continue;
                    string val  = item != null ? (item.GetEntry(ad.attrId)?.value?.ToDisplayString() ?? string.Empty) : string.Empty;
                    string text = string.IsNullOrEmpty(ad.label) ? val : (ad.label + " " + val);

                    while (_lines.Count <= idx)
                        _lines.Add(Instantiate(attrLinePrefab, attrLineContainer));
                    _lines[idx].Setup(null, Color.clear, text);
                    _lines[idx].gameObject.SetActive(true);
                    idx++;
                }
            }
            for (int i = idx; i < _lines.Count; i++)
                if (_lines[i]) _lines[i].gameObject.SetActive(false);
        }

        private static string ResolveName(CraftingBlueprint bp)
        {
            if (bp == null) return string.Empty;
            string name = bp.displayText != null ? bp.displayText.ResolveText() : null;
            return string.IsNullOrEmpty(name) ? bp.id : name;
        }
    }
}

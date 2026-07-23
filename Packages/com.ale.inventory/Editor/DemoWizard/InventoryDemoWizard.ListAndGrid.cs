using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.UI;

#if  IS_TMP
using TMPro;
#endif

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;

    /// <summary>文本标签 / 价格格 / 道具列表 / 网格列表等预制体的构建。</summary>
    public static partial class InventoryDemoWizard
    {
        #region UI预制体 对齐新增（Label / Price / ListPanel / Grid）

        /// <summary>构建 PF_ItemLabel（功能标签：背景图 + 文本，对齐 Demo 现有预制体）。</summary>
        static void BuildItemLabelPrefab()
        {
            string path = BeginPrefab(KPfItemLabel);

            var root = NewGameObject(KPfItemLabel);
            SetRectSize(root.AddComponent<RectTransform>(), 28f, 16f);
            var comp = root.AddComponent<UiwTextLabel>();
            SetHlg(root, new RectOffset(4, 4, 0, 0), 0f, TextAnchor.UpperLeft,
                controlW: true, controlH: true, expandW: false, expandH: true);

            // ImgBack（九宫格背景 + 默认精灵）
            var bgGo = ChildGameObject("ImgBack", root.transform);
            Stretch(bgGo.AddComponent<RectTransform>());
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color   = new Color(0.44276467f, 1f, 0.28773582f, 0.9019608f);
            bgImg.sprite  = LoadSprite(SpriteBackSphere);
            bgImg.type    = Image.Type.Sliced;
            bgImg.pixelsPerUnitMultiplier = 12f;
            SetLayoutElement(bgGo, ignore: true);

            // NameText
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = nameRt.anchorMax = Vector2.zero;
            nameRt.sizeDelta = Vector2.zero;
            var nameTxt = AddText(nameGo, "Name", 8, Color.white);

            SetSerializedRef(comp, "backgroundImage", bgImg);
            SetSerializedRef(comp, "labelText", nameTxt);

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_ItemPrice（价格货币格：图标 + 数量，UiwInventoryItemSimple）。</summary>
        static void BuildItemPricePrefab(NumberFormatConfig numFmt)
        {
            string path = BeginPrefab(KPfItemPrice);

            var root = NewGameObject(KPfItemPrice);
            SetRectSize(root.AddComponent<RectTransform>(), 36f, 16f);
            var comp = root.AddComponent<UiwInventoryItemSimple>();
            comp.iconAttrId   = "图标";
            comp.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            var rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(0.18f, 0.18f, 0.25f, 0.85f);
            SetHlg(root, new RectOffset(4, 5, 0, 0), 2f, TextAnchor.MiddleLeft, true, true, false, false);

            // Icon
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = Vector2.zero;
            iconRt.pivot     = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = Vector2.zero;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true;
            comp.iconImage = iconImg;
            SetLayoutElement(iconGo, prefW: 10, prefH: 10);

            // CountText
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = qRt.anchorMax = Vector2.zero;
            qRt.sizeDelta = Vector2.zero;
            var qTxt = AddText(qGo, "999", 10, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(comp, "countText", qTxt);

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_InventoryListPanel（独立列表面板：UiwInventoryList + ScrollRect + Scrollbar）。</summary>
        static void BuildInventoryListPanelPrefab(UiwInventoryItemDetail detailPrefab)
        {
            string path = BeginPrefab(KPfInventoryOrderList);

            var root   = NewGameObject(KPfInventoryOrderList);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = Vector2.zero;
            SetLayoutElement(root, prefH: 9000, flexH: 1);
            var listComp = root.AddComponent<UiwInventoryItemOrderList>();
            listComp.bufferCount = 1;
            listComp.cellPrefab  = detailPrefab;

            // ScrollRect + Viewport + Content + 竖直滚动条骨架（见 MakeVerticalScrollView）
            var sr = MakeVerticalScrollView(root, out var contentRt);
            listComp.scrollRect = sr;
            listComp.content    = contentRt;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_InventoryGridPanel（网格面板：ScrollRect + 虚拟滚动 UiwInventoryItemGridList，纵向滚动·自动列数）。</summary>
        static void BuildInventoryGridPrefab(UiwInventoryItemCell cellPrefab)
        {
            string path = BeginPrefab(KPfInventoryGridList);

            var root   = NewGameObject(KPfInventoryGridList);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = Vector2.zero;
            SetLayoutElement(root, prefH: 9000, flexH: 1);
            var comp = root.AddComponent<UiwInventoryItemGridList>();
            comp.bufferCount     = 1;
            comp.cellPrefab      = cellPrefab;
            comp.scrollDirection = EListScrollDirection.Vertical;
            comp.spacing         = new Vector2(6f, 6f);
            comp.padding         = new Vector2(6f, 6f);

            // ScrollRect + Viewport + Content + 竖直滚动条骨架（见 MakeVerticalScrollView）
            var sr = MakeVerticalScrollView(root, out var contentRt);
            comp.scrollRect = sr;
            comp.content    = contentRt;

            SavePrefab(root, path);
        }

        #endregion
    }
}

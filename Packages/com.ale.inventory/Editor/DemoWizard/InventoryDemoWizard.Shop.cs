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

    /// <summary>商店相关预制体（商品组页签 / 商店商品条目 / 商店面板）的构建。</summary>
    public static partial class InventoryDemoWizard
    {
        #region UI预制体 商店（ShopGroupTab / ShopItemDetail / ShopPanel）

        /// <summary>构建 PF_ShopGroupTab（商店商品组页签：Button + UiwShopGroupTab，对齐 PF_InventoryTab）。</summary>
        static void BuildShopGroupTabPrefab()
        {
            string path = Pfb(KPfShopGroupTab);
            DeleteIfExists(path);

            var root = NewGameObject(KPfShopGroupTab);
            SetRectSize(root.AddComponent<RectTransform>(), 96, 32);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = Hex("2D3148");
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            SetButtonColors(btn, Hex("2D3148"), Hex("3D4268"), Hex("22253A"));
            var tab = root.AddComponent<UiwShopGroupTab>();

            var labelGo = ChildGameObject("Label", root.transform);
            Stretch(labelGo.AddComponent<RectTransform>());
            var labelTxt = AddText(labelGo, "全部", 13, Color.white);
            SetSerializedRef(tab, "label", labelTxt);

            var selGo = ChildGameObject("SelectedIndicator", root.transform);
            var selRt = selGo.AddComponent<RectTransform>();
            selRt.anchorMin = new Vector2(0f, 0f); selRt.anchorMax = new Vector2(1f, 0f);
            selRt.pivot = new Vector2(0.5f, 0f); selRt.sizeDelta = new Vector2(0f, 3f);
            selRt.anchoredPosition = Vector2.zero;
            selGo.AddComponent<Image>().color = Hex("FFCC55");
            selGo.SetActive(false);
            tab.selectedIndicator = selGo;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>
        /// 构建 PF_UiwNumberCounter（通用「数量计数器」：- [值] + 一行，由 <see cref="UiwNumberCounter"/> 统一驱动
        /// +/- 步进、长按连发与数值显示）。商店条目、制作详情等界面通过实例化本预制体复用「调数量」功能。
        /// </summary>
        static void BuildNumberCounterPrefab()
        {
            string path = Pfb(KPfNumberCounter);
            DeleteIfExists(path);

            // 根：横向一行（- 值 +），并带 LayoutElement 便于嵌入父级布局组时给出自然尺寸
            var root = NewGameObject(KPfNumberCounter);
            SetRectSize(root.AddComponent<RectTransform>(), 104f, 28f);
            SetLayoutElement(root, minW: 104, prefW: 104, minH: 28, prefH: 28);
            SetHlg(root, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.MiddleCenter,
                true, true, false, true);

            var counter = root.AddComponent<UiwNumberCounter>();

            // 减少按钮
            var minusBtn = MakeMiniButton("MinusButton", root.transform, "-", Hex("3A2A2A"), Hex("5A3A3A"), Hex("2A1E1E"));
            SetLayoutElement(minusBtn.gameObject, minW: 28, prefW: 28, minH: 28, prefH: 28);
            counter.minusButton = minusBtn;

            // 当前值文本（可在生成后另行挂接 InputField 以支持键入）
            var valueGo = ChildGameObject("ValueText", root.transform);
            valueGo.AddComponent<RectTransform>();
            SetLayoutElement(valueGo, minW: 40, prefW: 40, flexW: 1);
            var valueTxt = AddText(valueGo, "0", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetSerializedRef(counter, "valueText", valueTxt);

            // 增加按钮
            var plusBtn = MakeMiniButton("PlusButton", root.transform, "+", Hex("2A3A2A"), Hex("3A5A3A"), Hex("1E2A1E"));
            SetLayoutElement(plusBtn.gameObject, minW: 28, prefW: 28, minH: 28, prefH: 28);
            counter.plusButton = plusBtn;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_ShopItemDetail（商店商品条目：品质背景 + 图标 + 名称/单价 + 剩余 + -/次数/+，UiwShopItemDetail）。</summary>
        static void BuildShopItemDetailPrefab(NumberFormatConfig numFmt, UiwInventoryItemSimple pricePrefab,
            UiwNumberCounter counterPrefab)
        {
            string path = Pfb(KPfShopItemDetail);
            DeleteIfExists(path);

            var root = NewGameObject(KPfShopItemDetail);
            SetRectSize(root.AddComponent<RectTransform>(), 460f, 56f);
            SetLayoutElement(root, minH: 56, prefH: 56);
            var rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(0.14f, 0.14f, 0.20f, 0.85f);
            var cg = root.AddComponent<CanvasGroup>();
            SetHlg(root, new RectOffset(6, 6, 4, 4), 6f, TextAnchor.MiddleLeft, 
                true, true, false, false);

            var cell = root.AddComponent<UiwShopItemDetail>();
            cell.iconAttrId   = "图标";
            cell.nameAttrId   = "名称";
            cell.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            cell.priceCurrencyPrefab = pricePrefab;
            cell.interactableGroup   = cg;

            // 图标 + 品质背景框（IconFrame 占据 HLG 槽位，内含品质底图 + 图标）
            var frameGo = ChildGameObject("IconFrame", root.transform);
            frameGo.AddComponent<RectTransform>();
            SetLayoutElement(frameGo, minW: 44, prefW: 44, minH: 44, prefH: 44);

            var qualityGo = ChildGameObject("QualityBackground", frameGo.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = Color.white; qualityImg.preserveAspect = true;
            qualityImg.sprite = LoadSprite(SpriteQualityPoor);
            cell.qualityBackground = qualityImg;

            var iconGo = ChildGameObject("Icon", frameGo.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4f, 4f); iconRt.offsetMax = new Vector2(-4f, -4f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true;
            cell.iconImage = iconImg;

            // 信息列（名称 + 单价）
            var infoGo = ChildGameObject("Info", root.transform);
            infoGo.AddComponent<RectTransform>();
            SetLayoutElement(infoGo, flexW: 1, prefH: 43);

            var nameGo = ChildGameObject("NameText", infoGo.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 18, prefH: 18);
            var nameTxt = AddText(nameGo, "商品名称", 13, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(cell, "nameText", nameTxt);

            var priceGo = ChildGameObject("PriceContainer", infoGo.transform);
            var priceRt = priceGo.AddComponent<RectTransform>();
            // 定位到（父级 Info）RectTransform 的左下角
            priceRt.anchorMin        = new Vector2(0f, 0f);
            priceRt.anchorMax        = new Vector2(0f, 0f);
            priceRt.pivot            = new Vector2(0f, 0f);
            priceRt.anchoredPosition = Vector2.zero;
            priceRt.sizeDelta        = new Vector2(200f, 16f);
            SetHlg(priceGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleLeft, false, true, false, true);
            cell.priceContainer = priceGo.transform;

            // 每次交易数量（×N）
            var countGo = ChildGameObject("CountText", root.transform);
            countGo.AddComponent<RectTransform>();
            SetLayoutElement(countGo, minW: 28, prefW: 28);
            var countTxt = AddText(countGo, "×1", 11, new Color(0.7f, 0.8f, 0.9f));
            SetSerializedRef(cell, "countText", countTxt);

            // 剩余可交易次数
            var remGo = ChildGameObject("RemainingText", root.transform);
            remGo.AddComponent<RectTransform>();
            SetLayoutElement(remGo, minW: 64, prefW: 64);
            var remTxt = AddText(remGo, "剩余 ∞", 11, new Color(0.7f, 0.8f, 0.9f));
            SetSerializedRef(cell, "remainingText", remTxt);

            // 数量计数器（实例化通用 PF_UiwNumberCounter，统一驱动 +/- 与显示）
            if (counterPrefab)
            {
                var counterInst = (UiwNumberCounter)PrefabUtility.InstantiatePrefab(counterPrefab, root.transform);
                cell.counter = counterInst;
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwNumberCounter，商店条目无法调整交易次数。");

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_ShopPanel（商店主界面：标题 + 商品组页签 + 货币栏 + 商品列表 + 结算栏，UiwSellShopView）。</summary>
        static void BuildShopPanelPrefab(UiwShopGroupTab groupTabPrefab,
            UiwShopItemDetail cellPrefab, UiwInventoryItemSimple itemSimplePrefab)
        {
            string path = Pfb(KPfShopPanel);
            DeleteIfExists(path);

            // 面板根（居中，480×620）
            var panelGo = NewGameObject(KPfShopPanel);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480f, 620f);
            panelRt.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.97f);

            var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;  vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 4f; vlg.padding = new RectOffset(6, 6, 6, 6);

            // 商店视图已按类型拆分；Demo 生成售卖店视图（杂货商店为售卖类型）。
            // 其它类型请在对应预制体上改挂 UiwRecycleShopView / UiwBarterShopView。
            var view = panelGo.AddComponent<UiwSellShopView>();

            // 标题
            var headerRow = MakeRow("Header", panelGo.transform, 40f, Hex("0D0D17"));
            SetHlg(headerRow, new RectOffset(10, 10, 0, 0), 0f, TextAnchor.MiddleLeft, true, true, true, true);
            var titleGo = ChildGameObject("TitleText", headerRow.transform);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var titleTxt = AddText(titleGo, "商店  [编辑器测试 — Play 后自动打开]", 13,
                new Color(0.85f, 0.85f, 0.92f), TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(view, "titleLabel", titleTxt);

            // 商品组页签
            var tabRow = MakeRow("GroupTabRow", panelGo.transform, 38f, new Color(0f, 0f, 0f, 0f));
            SetHlg(tabRow, new RectOffset(4, 4, 2, 2), 3f, TextAnchor.MiddleLeft, false, true, false, true);
            view.groupTabContainer = tabRow.transform;
            view.groupTabPrefab    = groupTabPrefab;

            // 货币栏
            var currencyRow = MakeRow("CurrencyRow", panelGo.transform, 24f, new Color(0f, 0f, 0f, 0f));
            SetHlg(currencyRow, new RectOffset(4, 4, 2, 2), 4f, TextAnchor.MiddleLeft, false, true, false, true);
            var currencyContGo = ChildGameObject("CurrencyContainer", currencyRow.transform);
            currencyContGo.AddComponent<RectTransform>();
            currencyContGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            SetHlg(currencyContGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleLeft, false, true, false, true);
            var currencyBar = currencyContGo.AddComponent<UiwCurrencyBar>();
            currencyBar.currencyContainer = currencyContGo.transform;
            currencyBar.currencyPrefab    = itemSimplePrefab;
            currencyBar.currencyItemIds   = new[] { "金币", "银币", "铜币" };
            view.currencyBar = currencyBar;

            // 商品列表（垂直滚动）
            var areaGo = ChildGameObject("CommodityArea", panelGo.transform);
            areaGo.AddComponent<RectTransform>();
            var areaLe = areaGo.AddComponent<LayoutElement>();
            areaLe.flexibleHeight = 1f; areaLe.preferredHeight = 9000f;
            areaGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            var sr = areaGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;

            var vpGo = ChildGameObject("Viewport", areaGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;

            sr.viewport = vpRt; sr.content = contentRt;

            // 商品列表（虚拟滚动 UiwShopCommodityList；条目手动定位，不挂 VerticalLayoutGroup / SizeFitter）
            var commodityList = areaGo.AddComponent<UiwShopCommodityList>();
            commodityList.cellPrefab  = cellPrefab;
            commodityList.bufferCount = 1;
            commodityList.scrollRect  = sr;
            commodityList.content     = contentRt;
            view.commodityList = commodityList;

            // 结算栏（总价 + 结算按钮）
            var footerRow = MakeRow("FooterRow", panelGo.transform, 40f, Hex("0D0D17"));
            SetHlg(footerRow, new RectOffset(10, 10, 2, 2), 6f, TextAnchor.MiddleLeft, true, true, false, true);
            var totalGo = ChildGameObject("TotalText", footerRow.transform);
            totalGo.AddComponent<RectTransform>();
            totalGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var totalTxt = AddText(totalGo, "总价：0", 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(view, "totalLabel", totalTxt);

            var settleGo = ChildGameObject("SettleButton", footerRow.transform);
            settleGo.AddComponent<RectTransform>();
            var settleLe = settleGo.AddComponent<LayoutElement>();
            settleLe.minWidth = 72f; settleLe.preferredWidth = 72f;
            var settleImg = settleGo.AddComponent<Image>();
            settleImg.color = Hex("2A4A2A");
            var settleBtn = settleGo.AddComponent<Button>();
            settleBtn.targetGraphic = settleImg;
            SetButtonColors(settleBtn, Hex("2A4A2A"), Hex("3A6A3A"), Hex("1E361E"));
            var settleLblGo = ChildGameObject("Label", settleGo.transform);
            Stretch(settleLblGo.AddComponent<RectTransform>());
            AddText(settleLblGo, "结算", 13, Color.white);
            view.settleButton = settleBtn;

            // 提示行
            var hintRow = MakeRow("HintRow", panelGo.transform, 20f, new Color(0f, 0f, 0f, 0f));
            SetHlg(hintRow, new RectOffset(10, 10, 0, 0), 0f, TextAnchor.MiddleLeft, true, true, true, true);
            var hintGo = ChildGameObject("HintText", hintRow.transform);
            hintGo.AddComponent<RectTransform>();
            hintGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var hintTxt = AddText(hintGo, "", 11, new Color(0.95f, 0.75f, 0.40f), TextAnchor.MiddleLeft);
            SetSerializedRef(view, "hintLabel", hintTxt);

            // 编辑器测试：自动打开「杂货商店」
            var vso = new SerializedObject(view);
            var autoOpenProp = vso.FindProperty("autoOpenOnStart");
            var shopIdProp   = vso.FindProperty("_shopId");   // 商店 ID 已并入暴露的序列化字段
            if (autoOpenProp != null) autoOpenProp.boolValue = true;
            if (shopIdProp   != null) shopIdProp.stringValue = "杂货商店";
            vso.ApplyModifiedPropertiesWithoutUndo();

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(panelGo);
#endif

            MovePrimaryUiwToTop(panelGo);
            PrefabUtility.SaveAsPrefabAsset(panelGo, path);
            Object.DestroyImmediate(panelGo);
            Debug.Log("[InventoryDemoWizard] 商店面板 Prefab 已保存：" + path);
        }

        /// <summary>添加并设置 VerticalLayoutGroup（参数对齐 <see cref="SetHlg"/>）。</summary>
        static void SetVlg(GameObject go, RectOffset padding, float spacing,
            TextAnchor align, bool controlW, bool controlH, bool expandW, bool expandH)
        {
            var g = go.AddComponent<VerticalLayoutGroup>();
            g.padding = padding; g.spacing = spacing; g.childAlignment = align;
            g.childControlWidth = controlW; g.childControlHeight = controlH;
            g.childForceExpandWidth = expandW; g.childForceExpandHeight = expandH;
            g.childScaleWidth = false; g.childScaleHeight = false;
        }

        /// <summary>创建一个带居中文本的小按钮，返回 Button。</summary>
        static Button MakeMiniButton(string name, Transform parent, string label,
            Color normal, Color highlight, Color pressed)
        {
            var go = ChildGameObject(name, parent);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = normal;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            SetButtonColors(btn, normal, highlight, pressed);
            var lblGo = ChildGameObject("Label", go.transform);
            Stretch(lblGo.AddComponent<RectTransform>());
            AddText(lblGo, label, 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return btn;
        }

        #endregion
    }
}

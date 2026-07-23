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

    /// <summary>道具单元格预制体（列表格子 / 网格格子 / 货币栏格子）的构建。</summary>
    public static partial class InventoryDemoWizard
    {
        #region UI预制体 道具单元格
        #region UiwInventoryItemDetail（列表格子）

        /// <summary>
        /// 构建 UiwInventoryItemDetail 的 Prefab，适用于列表布局的行显示。
        /// </summary>
        /// <param name="numFmt"></param>
        /// <param name="labelPrefab"></param>
        /// <param name="pricePrefab"></param>
        /// <returns></returns>
        static void BuildItemDetailPrefab(NumberFormatConfig numFmt,
            UiwTextLabel labelPrefab, UiwInventoryItemSimple pricePrefab)
        {
            string path = BeginPrefab(KPfItemDetail);

            // root：500×80
            var root = NewGameObject(KPfItemDetail);
            SetRectSize(root.AddComponent<RectTransform>(), 500f, 80f);
            root.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.22f, 0.95f);
            var comp = root.AddComponent<UiwInventoryItemDetail>();
            comp.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;

            // HoverBorder（偏蓝底色，接收射线）
            comp.hoverBorder = MakeHoverBorder(root.transform, new Color(0.2f, 0.2f, 0.3f, 0f));
            comp.hoverFadeDuration = 0.12f;

            // StackFullIcon（右上角）
            var sfGo = ChildGameObject("StackFullIcon", root.transform);
            var sfRt = sfGo.AddComponent<RectTransform>();
            sfRt.anchorMin = sfRt.anchorMax = Vector2.one; sfRt.pivot = Vector2.one;
            sfRt.sizeDelta = new Vector2(14f, 14f);
            sfRt.anchoredPosition = new Vector2(-8f, -8f);
            var sfImg = sfGo.AddComponent<Image>();
            sfImg.color  = new Color(1f, 0.15566039f, 0.1996756f, 1f);
            sfImg.sprite = LoadSprite(SpriteBackSphere);
            comp.stackFullIcon = sfImg;
            comp.stackFullFadeDuration = 0.12f;

            // QualityBackground（左侧 74×74，含 Icon + IdText）
            var qualityGo = ChildGameObject("QualityBackground", root.transform);
            var qualityRt = qualityGo.AddComponent<RectTransform>();
            qualityRt.anchorMin = qualityRt.anchorMax = new Vector2(0f, 0.5f);
            qualityRt.pivot = new Vector2(0f, 0.5f);
            qualityRt.sizeDelta = new Vector2(74f, 74f);
            qualityRt.anchoredPosition = new Vector2(4f, 0f);
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color  = Color.white;
            qualityImg.sprite = LoadSprite(SpriteQualityPoor);
            qualityImg.preserveAspect = true;
            comp.qualityBackground = qualityImg;

            //   Icon（缩进 8px）— iconImage
            var iconGo = ChildGameObject("Icon", qualityGo.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.sizeDelta = new Vector2(-16f, -16f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color  = Color.white;
            iconImg.sprite = LoadSprite(SpriteItemGoldCoin);
            iconImg.preserveAspect = true;
            comp.iconImage = iconImg;

            //   IdText（底部小字）
            var idGo = ChildGameObject("IdText", qualityGo.transform);
            var idRt = idGo.AddComponent<RectTransform>();
            idRt.anchorMin = idRt.anchorMax = new Vector2(0.5f, 0f); idRt.pivot = new Vector2(0.5f, 0f);
            idRt.sizeDelta = new Vector2(50f, 10f);
            AddText(idGo, "id", 8, new Color(0.7f, 0.7f, 0.7f, 0.6f), TextAnchor.LowerCenter);

            // NameText（含 ContentSizeFitter + ItemTagsContainer）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = Vector2.zero; nameRt.anchorMax = Vector2.one;
            nameRt.pivot = new Vector2(0f, 0.5f);
            nameRt.anchoredPosition = new Vector2(80f, 22f);
            nameRt.sizeDelta = new Vector2(0f, -56f);
            var nameTxt = AddText(nameGo, "道具名称", 14, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            SetSerializedRef(comp, "nameText", nameTxt);
            SetContentSizeFitter(nameGo, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.Unconstrained);

            //   ItemTagsContainer（名称右侧标签容器）
            var tagsGo = ChildGameObject("ItemTagsContainer", nameGo.transform);
            var tagsRt = tagsGo.AddComponent<RectTransform>();
            tagsRt.anchorMin = tagsRt.anchorMax = new Vector2(1f, 0.5f); tagsRt.pivot = new Vector2(0f, 0.5f);
            tagsRt.anchoredPosition = new Vector2(8f, 0f);
            tagsRt.sizeDelta = new Vector2(200f, 20f);
            SetHlg(tagsGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleLeft, true, false, false, false);
            comp.textTagsPrefab    = labelPrefab;
            comp.itemTagsContainer = tagsGo.transform;

            // DescText
            var descGo = ChildGameObject("DescText", root.transform);
            var descRt = descGo.AddComponent<RectTransform>();
            descRt.anchorMin = Vector2.zero; descRt.anchorMax = Vector2.one;
            descRt.anchoredPosition = new Vector2(-32f, -13f);
            descRt.sizeDelta = new Vector2(-224f, -34f);
            var descTxt = AddText(descGo, "道具的详细描述。", 11, Color.white, TextAnchor.UpperLeft);
            SetSerializedRef(comp, "descText", descTxt);

            // CountText（右侧）
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = qRt.anchorMax = new Vector2(1f, 0.5f); qRt.pivot = new Vector2(1f, 0.5f);
            qRt.anchoredPosition = new Vector2(-10f, 0f);
            qRt.sizeDelta = new Vector2(50f, 30f);
            var qTxt = AddText(qGo, "×9999", 14, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleRight, FontStyle.Bold);
            SetSerializedRef(comp, "countText", qTxt);

            // Divider（底部分隔线）
            var lineGo = ChildGameObject("Divider", root.transform);
            var lineRt = lineGo.AddComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 0f); lineRt.anchorMax = new Vector2(1f, 0f);
            lineRt.pivot = new Vector2(0.5f, 0f);
            lineRt.sizeDelta = new Vector2(-8f, 1f);
            lineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

            // PriceContainer（右下角价格容器）
            var priceGo = ChildGameObject("PriceContainer", root.transform);
            var priceRt = priceGo.AddComponent<RectTransform>();
            priceRt.anchorMin = priceRt.anchorMax = new Vector2(1f, 0f); priceRt.pivot = new Vector2(1f, 0f);
            priceRt.anchoredPosition = new Vector2(-6f, 6f);
            priceRt.sizeDelta = new Vector2(150f, 16f);
            SetHlg(priceGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleRight, true, false, false, false);
            comp.priceCurrencyPrefab = pricePrefab;
            comp.priceContainer      = priceGo.transform;

            SavePrefab(root, path);
        }
        #endregion
        
        #region UiwInventoryItemCell（网格方形格子）
        
        /// <summary>
        /// 构建 UiwInventoryItemCell 的 Prefab，适用于网格布局的格子显示。
        /// </summary>
        /// <param name="numFmt"></param>
        static void BuildItemCellPrefab(NumberFormatConfig numFmt)
        {
            string path = BeginPrefab(KPfItemCell);

            const float slotSize = 72f;
            var root   = NewGameObject(KPfItemCell);
            SetRectSize(root.AddComponent<RectTransform>(), slotSize, slotSize);

            // QualityBackground（全覆盖底层）
            var qualityGo  = ChildGameObject("QualityBackground", root.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = Color.white; qualityImg.preserveAspect = false;

            // Icon（四边缩进 6px）
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(6f, 6f); iconRt.offsetMax = new Vector2(-6f, -6f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.49056602f, 0.49056602f, 0.49056602f, 1f);
            iconImg.preserveAspect = true;

            var comp = root.AddComponent<UiwInventoryItemCell>();
            comp.numberFormat      = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            comp.iconImage         = iconImg;
            comp.qualityBackground = qualityImg;

            // NameText（顶部居中）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f); nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.sizeDelta = new Vector2(0f, 16f);
            nameRt.anchoredPosition = new Vector2(0f, -4f);
            var nameTxt = AddText(nameGo, "道具名称", 9, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperCenter);
            SetSerializedRef(comp, "nameText", nameTxt);

            // CountText（右下角）
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(1f, 0f); qRt.anchorMax = new Vector2(1f, 0f);
            qRt.pivot = new Vector2(1f, 0f);
            qRt.sizeDelta = new Vector2(50.4f, 16f);
            qRt.anchoredPosition = new Vector2(-8f, 4f);
            var qTxt = AddText(qGo, "9999", 10, new Color(1f, 1f, 1f, 0.9019608f), TextAnchor.LowerRight);
            SetSerializedRef(comp, "countText", qTxt);

            // HoverBorder（全覆盖，alpha=0，接收射线）
            comp.hoverBorder = MakeHoverBorder(root.transform, new Color(1f, 1f, 1f, 0f));

            // StackFullIcon（右上角红点）
            var sfGo = ChildGameObject("StackFullIcon", root.transform);
            var sfRt = sfGo.AddComponent<RectTransform>();
            sfRt.anchorMin = new Vector2(1f, 1f); sfRt.anchorMax = new Vector2(1f, 1f);
            sfRt.pivot = Vector2.one;
            sfRt.sizeDelta = new Vector2(10f, 10f);
            sfRt.anchoredPosition = new Vector2(-4f, -4f);
            var sfImg = sfGo.AddComponent<Image>();
            sfImg.color = new Color(1f, 0.2f, 0.2f, 1f);
            comp.stackFullIcon = sfImg;

            SavePrefab(root, path);
        }
        
        #endregion
        
        #region UiwInventoryItemSimple（货币栏用）
        /// <summary>
        /// 构建 UiwInventoryItemSimple 的 Prefab，适用于货币栏或极简显示场景。
        /// </summary>
        /// <param name="numFmt"></param>
        static void BuildItemSimplePrefab(NumberFormatConfig numFmt)
        {
            string path = BeginPrefab(KPfItemSimple);

            // root：54×20，横向布局（图标 + 数量）
            var root = NewGameObject(KPfItemSimple);
            SetRectSize(root.AddComponent<RectTransform>(), 54f, 20f);
            var comp = root.AddComponent<UiwInventoryItemSimple>();
            comp.iconAttrId   = "图标";
            comp.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            root.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.25f, 0.85f);
            SetHlg(root, new RectOffset(4, 6, 0, 0), 4f, TextAnchor.MiddleLeft, true, true, false, false);

            // Icon（固定 14×14，由 LayoutElement 控制）
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = Vector2.zero;
            iconRt.pivot     = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = Vector2.zero;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true;
            comp.iconImage = iconImg;
            SetLayoutElement(iconGo, prefW: 14, prefH: 14);

            // CountText
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = qRt.anchorMax = Vector2.zero;
            qRt.sizeDelta = Vector2.zero;
            var qTxt = AddText(qGo, "9999", 12, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(comp, "countText", qTxt);

            SavePrefab(root, path);
        }
#endregion
        #endregion
    }
}

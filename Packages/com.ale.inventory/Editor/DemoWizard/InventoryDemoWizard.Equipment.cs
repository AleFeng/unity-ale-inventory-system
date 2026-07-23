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

    /// <summary>装备系统预制体（装备槽 / 候选格子 / 加成条目 / 槽位列表 / 各面板）的构建。</summary>
    public static partial class InventoryDemoWizard
    {
        #region UI预制体 装备系统（叶子格子：Slot / CandidateCell / BonusEntry）

        /// <summary>构建 PF_UiwEquipmentSlot（装备槽：品质背景 + 图标 + 名称 + 悬停 + 选中指示 + 绿/红有效性叠加）。</summary>
        static void BuildEquipmentSlotPrefab(NumberFormatConfig numFmt)
        {
            string path = Pfb(KPfEquipSlot);
            DeleteIfExists(path);

            const float slotSize = 72f;
            var root = NewGameObject(KPfEquipSlot);
            SetRectSize(root.AddComponent<RectTransform>(), slotSize, slotSize);

            // QualityBackground（全覆盖底层，兼作射线接收）
            var qualityGo  = ChildGameObject("QualityBackground", root.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = new Color(1f, 1f, 1f, 0.10f);

            // Icon（四边缩进 6px）
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(6f, 6f); iconRt.offsetMax = new Vector2(-6f, -6f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.49f, 0.49f, 0.49f, 1f);
            iconImg.preserveAspect = true;

            var comp = root.AddComponent<UiwEquipmentSlot>();
            comp.numberFormat      = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            comp.iconImage         = iconImg;
            comp.qualityBackground = qualityImg;

            // NameText（顶部居中，空槽显示槽位名）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f); nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.sizeDelta = new Vector2(0f, 16f);
            nameRt.anchoredPosition = new Vector2(0f, -4f);
            var nameTxt = AddText(nameGo, "槽位", 9, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperCenter);
            SetSerializedRef(comp, "nameText", nameTxt);

            // CountText（右下角；装备槽恒 1 件，留空）
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(1f, 0f); qRt.anchorMax = new Vector2(1f, 0f);
            qRt.pivot = new Vector2(1f, 0f);
            qRt.sizeDelta = new Vector2(50f, 16f);
            qRt.anchoredPosition = new Vector2(-8f, 4f);
            var qTxt = AddText(qGo, string.Empty, 10, new Color(1f, 1f, 1f, 0.9f), TextAnchor.LowerRight);
            SetSerializedRef(comp, "countText", qTxt);

            // HoverBorder（全覆盖，alpha=0）
            var hoverGo = ChildGameObject("HoverBorder", root.transform);
            Stretch(hoverGo.AddComponent<RectTransform>());
            var hoverImg = hoverGo.AddComponent<Image>();
            hoverImg.color = new Color(1f, 1f, 1f, 0f);
            hoverImg.raycastTarget = false;
            comp.hoverBorder = hoverImg;

            // SelectedIndicator（选中边框，默认隐藏）
            var selGo = ChildGameObject("SelectedIndicator", root.transform);
            Stretch(selGo.AddComponent<RectTransform>());
            var selImg = selGo.AddComponent<Image>();
            selImg.color = new Color(1f, 0.85f, 0.2f, 0.5f);
            selImg.raycastTarget = false;
            selGo.SetActive(false);
            comp.selectedIndicator = selGo;

            // ValidityOverlay（拖拽悬停绿/红，默认禁用）
            var valGo = ChildGameObject("ValidityOverlay", root.transform);
            Stretch(valGo.AddComponent<RectTransform>());
            var valImg = valGo.AddComponent<Image>();
            valImg.color = comp.validColor;
            valImg.raycastTarget = false;
            valImg.enabled = false;
            comp.validityOverlay = valImg;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentCandidateCell（可装备道具格子：UiwInventoryItemCell + GridCellDragHandler；品质背景 + 图标 + 名称 + 数量 + 悬停；右键快速装备 / 左键拖拽装备）。</summary>
        static void BuildEquipmentCandidateCellPrefab(NumberFormatConfig numFmt)
        {
            string path = Pfb(KPfEquipCandidateCell);
            DeleteIfExists(path);

            const float slotSize = 64f;
            var root = NewGameObject(KPfEquipCandidateCell);
            SetRectSize(root.AddComponent<RectTransform>(), slotSize, slotSize);

            // QualityBackground
            var qualityGo  = ChildGameObject("QualityBackground", root.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = new Color(1f, 1f, 1f, 0.10f);

            // Icon
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(5f, 5f); iconRt.offsetMax = new Vector2(-5f, -5f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.49f, 0.49f, 0.49f, 1f);
            iconImg.preserveAspect = true;

            // 候选格子复用仓库格子 UiwInventoryItemCell（显示）+ GridCellDragHandler（未接入网格列表时驱动装备拖拽）。
            var comp = root.AddComponent<UiwInventoryItemCell>();
            comp.dragHandler       = root.AddComponent<GridCellDragHandler>();
            comp.numberFormat      = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            comp.iconImage         = iconImg;
            comp.qualityBackground = qualityImg;

            // NameText（顶部）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f); nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.sizeDelta = new Vector2(0f, 14f);
            nameRt.anchoredPosition = new Vector2(0f, -3f);
            var nameTxt = AddText(nameGo, "道具名称", 9, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperCenter);
            SetSerializedRef(comp, "nameText", nameTxt);

            // CountText（右下角）
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(1f, 0f); qRt.anchorMax = new Vector2(1f, 0f);
            qRt.pivot = new Vector2(1f, 0f);
            qRt.sizeDelta = new Vector2(46f, 14f);
            qRt.anchoredPosition = new Vector2(-6f, 3f);
            var qTxt = AddText(qGo, "9", 10, new Color(1f, 1f, 1f, 0.9f), TextAnchor.LowerRight);
            SetSerializedRef(comp, "countText", qTxt);

            // HoverBorder
            var hoverGo = ChildGameObject("HoverBorder", root.transform);
            Stretch(hoverGo.AddComponent<RectTransform>());
            var hoverImg = hoverGo.AddComponent<Image>();
            hoverImg.color = new Color(1f, 1f, 1f, 0f);
            hoverImg.raycastTarget = false;
            comp.hoverBorder = hoverImg;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentBonusEntry（属性加成条目：左标签 + 右数值，一行）。亦用作分组标题行（数值留空）。</summary>
        static void BuildEquipmentBonusEntryPrefab()
        {
            string path = Pfb(KPfEquipBonusEntry);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipBonusEntry);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 22f);
            SetLayoutElement(root, minH: 22f, prefH: 22f, flexW: 1f);

            var comp = root.AddComponent<UiwEquipmentBonusEntry>();

            // Label（左 60%）
            var lblGo = ChildGameObject("Label", root.transform);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f); lblRt.anchorMax = new Vector2(0.6f, 1f);
            lblRt.offsetMin = new Vector2(8f, 0f); lblRt.offsetMax = Vector2.zero;
            var lblTxt = AddText(lblGo, "属性", 12, new Color(0.85f, 0.85f, 0.92f), TextAnchor.MiddleLeft);
            SetSerializedRef(comp, "labelText", lblTxt);

            // Value（右 40%）
            var valGo = ChildGameObject("Value", root.transform);
            var valRt = valGo.AddComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.6f, 0f); valRt.anchorMax = new Vector2(1f, 1f);
            valRt.offsetMin = Vector2.zero; valRt.offsetMax = new Vector2(-8f, 0f);
            var valTxt = AddText(valGo, "0", 12, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            SetSerializedRef(comp, "valueText", valTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentSlotList（槽位列表：名称 + HorizontalLayoutGroup 装备槽容器）。</summary>
        static void BuildEquipmentSlotListPrefab(UiwEquipmentSlot slotPrefab)
        {
            string path = Pfb(KPfEquipSlotList);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipSlotList);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 100f);
            SetVlg(root, new RectOffset(4, 4, 4, 4), 4f, TextAnchor.UpperLeft, true, false, true, false);
            SetContentSizeFitter(root, ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize);

            var comp = root.AddComponent<UiwEquipmentSlotList>();

            // 名称
            var nameGo = ChildGameObject("NameText", root.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 18f, prefH: 18f, flexW: 1f);
            var nameTxt = AddText(nameGo, "槽位列表", 13, new Color(0.9f, 0.9f, 0.95f), TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(comp, "nameText", nameTxt);

            // 装备槽容器（HorizontalLayoutGroup）
            var contGo = ChildGameObject("SlotContainer", root.transform);
            contGo.AddComponent<RectTransform>();
            SetHlg(contGo, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.MiddleLeft, false, false, false, false);
            SetLayoutElement(contGo, minH: 78f, prefH: 78f, flexW: 1f);

            comp.slotPrefab    = slotPrefab;
            comp.slotContainer = contGo.transform;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentCandidateList（可装备道具列表：ScrollRect + 虚拟滚动 UiwEquipmentCandidateList，纵向滚动·自动列数）。</summary>
        static void BuildEquipmentCandidateListPrefab(UiwInventoryItemCell cellPrefab)
        {
            string path = Pfb(KPfEquipCandidateList);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipCandidateList);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 160f);
            root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            SetLayoutElement(root, minH: 120f, flexW: 1f, flexH: 1f);

            var comp = root.AddComponent<UiwEquipmentCandidateList>();
            comp.cellPrefab      = cellPrefab;
            comp.bufferCount     = 1;
            comp.scrollDirection = EListScrollDirection.Vertical;
            comp.spacing         = new Vector2(6f, 6f);
            comp.padding         = new Vector2(6f, 6f);

            // ScrollRect（纵向；虚拟滚动手动定位，不用 GridLayoutGroup）
            var srGo = ChildGameObject("ScrollRect", root.transform);
            var srRt = srGo.AddComponent<RectTransform>();
            srRt.anchorMin = Vector2.zero; srRt.anchorMax = Vector2.one;
            srRt.offsetMin = Vector2.zero; srRt.offsetMax = Vector2.zero;
            var sr = srGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;

            // Viewport（占满，带遮罩）
            var vpGo = ChildGameObject("Viewport", srGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.pivot = new Vector2(0.5f, 0.5f);
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content（顶部对齐；格子由虚拟滚动手动定位，不挂 GridLayoutGroup）
            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;

            sr.content  = contentRt;
            sr.viewport = vpRt;

            comp.scrollRect = sr;
            comp.content    = contentRt;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentGroupPanel（装备组面板：名称 + VerticalLayoutGroup 槽位列表容器）。</summary>
        static void BuildEquipmentGroupPanelPrefab(UiwEquipmentSlotList slotListPrefab)
        {
            string path = Pfb(KPfEquipGroupPanel);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipGroupPanel);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320f, 400f);
            root.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.6f);
            SetVlg(root, new RectOffset(8, 8, 8, 8), 8f, TextAnchor.UpperLeft, true, false, true, false);

            var comp = root.AddComponent<UiwEquipmentGroupPanel>();

            // 装备组名称
            var nameGo = ChildGameObject("GroupName", root.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 22f, prefH: 22f, flexW: 1f);
            var nameTxt = AddText(nameGo, "装备组", 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(comp, "groupNameText", nameTxt);

            // 槽位列表容器（VerticalLayoutGroup）
            var contGo = ChildGameObject("SlotListContainer", root.transform);
            contGo.AddComponent<RectTransform>();
            SetVlg(contGo, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.UpperLeft, true, false, true, false);
            SetLayoutElement(contGo, flexW: 1f, flexH: 1f);

            comp.slotListPrefab    = slotListPrefab;
            comp.slotListContainer = contGo.transform;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentBonusPanel（属性加成面板：标题 + VerticalLayoutGroup 条目容器；分组标题复用条目预制体）。</summary>
        static void BuildEquipmentBonusPanelPrefab(UiwEquipmentBonusEntry entryPrefab)
        {
            string path = Pfb(KPfEquipBonusPanel);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipBonusPanel);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 400f);
            root.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.6f);
            SetVlg(root, new RectOffset(8, 8, 8, 8), 6f, TextAnchor.UpperLeft, true, false, true, false);

            var comp = root.AddComponent<UiwEquipmentBonusPanel>();

            // 标题
            var titleGo = ChildGameObject("Title", root.transform);
            titleGo.AddComponent<RectTransform>();
            SetLayoutElement(titleGo, minH: 22f, prefH: 22f, flexW: 1f);
            AddText(titleGo, "属性加成", 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);

            // 条目容器（VerticalLayoutGroup）
            var contGo = ChildGameObject("EntryContainer", root.transform);
            contGo.AddComponent<RectTransform>();
            SetVlg(contGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, true, false, true, false);
            SetLayoutElement(contGo, flexW: 1f, flexH: 1f);

            comp.entryPrefab       = entryPrefab;
            comp.entryContainer    = contGo.transform;
            comp.groupHeaderPrefab = entryPrefab;   // 复用同一预制体作分组标题行（数值留空）

            SavePrefab(root, path);
        }

        /// <summary>装备 UI 用的简易文本按钮（Image + Button + 居中文本）。</summary>
        static Button MakeEquipButton(string name, Transform parent, string label, Color bg)
        {
            var go = ChildGameObject(name, parent);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            SetButtonColors(btn, bg, bg * 1.2f, bg * 0.8f);

            var lblGo = ChildGameObject("Text", go.transform);
            Stretch(lblGo.AddComponent<RectTransform>());
            AddText(lblGo, label, 13, Color.white);
            return btn;
        }

        /// <summary>
        /// 构建 PF_UiwEquipmentSelectPanel（装备选择面板：切换栏 prev/name/pos/next + 中间槽位列表实例 +
        /// 底部候选道具列表实例 + 退出按钮；根 Image 作右键空白退出的射线接收）。
        /// </summary>
        static void BuildEquipmentSelectPanelPrefab(GameObject slotListPrefab, GameObject candidateListPrefab)
        {
            string path = Pfb(KPfEquipSelectPanel);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipSelectPanel);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(360f, 460f);
            root.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.96f); // 根射线背景（右键空白退出）

            var comp = root.AddComponent<UiwEquipmentSelectPanel>();

            // ── 顶部切换栏 ──
            var barGo = ChildGameObject("SwitchBar", root.transform);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f); barRt.anchorMax = new Vector2(1f, 1f); barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(-16f, 32f); barRt.anchoredPosition = new Vector2(0f, -8f);

            var prevBtn = MakeEquipButton("PrevButton", barGo.transform, "<", Hex("2C3D50"));
            var prevRt  = (RectTransform)prevBtn.transform;
            prevRt.anchorMin = new Vector2(0f, 0f); prevRt.anchorMax = new Vector2(0f, 1f); prevRt.pivot = new Vector2(0f, 0.5f);
            prevRt.sizeDelta = new Vector2(30f, 0f); prevRt.anchoredPosition = Vector2.zero;
            comp.prevButton = prevBtn;

            var nextBtn = MakeEquipButton("NextButton", barGo.transform, ">", Hex("2C3D50"));
            var nextRt  = (RectTransform)nextBtn.transform;
            nextRt.anchorMin = new Vector2(1f, 0f); nextRt.anchorMax = new Vector2(1f, 1f); nextRt.pivot = new Vector2(1f, 0.5f);
            nextRt.sizeDelta = new Vector2(30f, 0f); nextRt.anchoredPosition = Vector2.zero;
            comp.nextButton = nextBtn;

            var nameGo = ChildGameObject("SlotListName", barGo.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.4f); nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(34f, 0f); nameRt.offsetMax = new Vector2(-34f, 0f);
            var nameTxt = AddText(nameGo, "槽位组", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetSerializedRef(comp, "slotListNameText", nameTxt);

            var posGo = ChildGameObject("Position", barGo.transform);
            var posRt = posGo.AddComponent<RectTransform>();
            posRt.anchorMin = new Vector2(0f, 0f); posRt.anchorMax = new Vector2(1f, 0.4f);
            posRt.offsetMin = new Vector2(34f, 0f); posRt.offsetMax = new Vector2(-34f, 0f);
            var posTxt = AddText(posGo, "1/1", 9, new Color(0.7f, 0.7f, 0.78f));
            SetSerializedRef(comp, "positionText", posTxt);

            // ── 中间：当前槽位列表（实例化；关掉其 ContentSizeFitter 改用固定锚定）──
            var slView = (GameObject)PrefabUtility.InstantiatePrefab(slotListPrefab, root.transform);
            var slCsf  = slView.GetComponent<ContentSizeFitter>(); if (slCsf) slCsf.enabled = false;
            var slRt   = (RectTransform)slView.transform;
            slRt.anchorMin = new Vector2(0f, 1f); slRt.anchorMax = new Vector2(1f, 1f); slRt.pivot = new Vector2(0.5f, 1f);
            slRt.sizeDelta = new Vector2(-16f, 110f); slRt.anchoredPosition = new Vector2(0f, -48f);
            comp.slotListView = slView.GetComponent<UiwEquipmentSlotList>();

            // ── 底部：可装备道具列表（实例化，填充中部到退出按钮之间）──
            var candView = (GameObject)PrefabUtility.InstantiatePrefab(candidateListPrefab, root.transform);
            var candRt   = (RectTransform)candView.transform;
            candRt.anchorMin = new Vector2(0f, 0f); candRt.anchorMax = new Vector2(1f, 1f);
            candRt.offsetMin = new Vector2(8f, 46f); candRt.offsetMax = new Vector2(-8f, -166f);
            comp.candidateList = candView.GetComponent<UiwEquipmentCandidateList>();

            // ── 退出按钮（底部）──
            var exitBtn = MakeEquipButton("ExitButton", root.transform, "返回", Hex("3A2530"));
            var exitRt  = (RectTransform)exitBtn.transform;
            exitRt.anchorMin = new Vector2(0f, 0f); exitRt.anchorMax = new Vector2(1f, 0f); exitRt.pivot = new Vector2(0.5f, 0f);
            exitRt.sizeDelta = new Vector2(-16f, 30f); exitRt.anchoredPosition = new Vector2(0f, 8f);
            comp.exitButton = exitBtn;

            SavePrefab(root, path);
        }

        /// <summary>
        /// 构建 PF_UiwEquipmentView（装备主界面：标题 + 左装备组面板 / 选择面板（叠加，选择面板初始隐藏） + 右属性加成面板）。
        /// </summary>
        static void BuildEquipmentViewPrefab(GameObject groupPanelPrefab, GameObject bonusPanelPrefab, GameObject selectPanelPrefab)
        {
            string path = Pfb(KPfEquipView);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipView);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(720f, 500f);
            root.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.92f);

            var comp = root.AddComponent<UiwEquipmentView>();

            // 标题
            var titleGo = ChildGameObject("Title", root.transform);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(1f, 1f); titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-16f, 30f); titleRt.anchoredPosition = new Vector2(0f, -6f);
            var titleTxt = AddText(titleGo, "装备", 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(comp, "titleLabel", titleTxt);

            // 左：装备组面板（实例化）
            var groupInst = (GameObject)PrefabUtility.InstantiatePrefab(groupPanelPrefab, root.transform);
            var groupRt   = (RectTransform)groupInst.transform;
            groupRt.anchorMin = new Vector2(0f, 0f); groupRt.anchorMax = new Vector2(0.62f, 1f);
            groupRt.offsetMin = new Vector2(8f, 8f); groupRt.offsetMax = new Vector2(-4f, -42f);
            comp.groupPanel = groupInst.GetComponent<UiwEquipmentGroupPanel>();

            // 左（叠加）：装备选择面板（实例化，初始隐藏；运行时由 View 打开/还原）
            var selInst = (GameObject)PrefabUtility.InstantiatePrefab(selectPanelPrefab, root.transform);
            var selRt   = (RectTransform)selInst.transform;
            selRt.anchorMin = new Vector2(0f, 0f); selRt.anchorMax = new Vector2(0.62f, 1f);
            selRt.offsetMin = new Vector2(8f, 8f); selRt.offsetMax = new Vector2(-4f, -42f);
            selInst.SetActive(false);
            comp.selectPanel = selInst.GetComponent<UiwEquipmentSelectPanel>();

            // 右：属性加成面板（实例化）
            var bonusInst = (GameObject)PrefabUtility.InstantiatePrefab(bonusPanelPrefab, root.transform);
            var bonusRt   = (RectTransform)bonusInst.transform;
            bonusRt.anchorMin = new Vector2(0.62f, 0f); bonusRt.anchorMax = new Vector2(1f, 1f);
            bonusRt.offsetMin = new Vector2(4f, 8f); bonusRt.offsetMax = new Vector2(-8f, -42f);
            comp.bonusPanel = bonusInst.GetComponent<UiwEquipmentBonusPanel>();

            SavePrefab(root, path);
        }

        #endregion
    }
}

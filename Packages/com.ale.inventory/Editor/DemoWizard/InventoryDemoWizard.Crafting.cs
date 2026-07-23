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

    /// <summary>制作系统预制体（Tooltip / 消耗行 / 蓝图条目 / 蓝图列表 / 制作面板）的构建。</summary>
    public static partial class InventoryDemoWizard
    {
        #region UI预制体 制作系统（Tooltip / InputCell / BlueprintCell / BlueprintList / CraftingView）

        /// <summary>构建 PF_UiwItemTooltip（通用道具悬停弹窗：内嵌一个 UiwInventoryItemDetail 渲染详情）。</summary>
        static void BuildItemTooltipPrefab(GameObject detailPrefab)
        {
            string path = BeginPrefab(KPfItemTooltip);

            var root = NewGameObject(KPfItemTooltip);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, 90f);
            rt.pivot     = new Vector2(0f, 1f);  // 左上角，便于按光标定位
            root.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.96f);
            var cg = root.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            var tooltip = root.AddComponent<UiwItemTooltip>();
            tooltip.panel       = rt;
            tooltip.canvasGroup = cg;

            if (detailPrefab)
            {
                var detailInst = (GameObject)PrefabUtility.InstantiatePrefab(detailPrefab, root.transform);
                Stretch((RectTransform)detailInst.transform);
                tooltip.detail = detailInst.GetComponent<UiwInventoryItemDetail>();
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwInventoryItemDetail，悬停弹窗内容为空。");

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwCraftingInputCell（消耗道具行：图标 + 名称 + 持有/需求，支持悬停弹窗）。</summary>
        static void BuildCraftingInputCellPrefab(NumberFormatConfig numFmt)
        {
            string path = BeginPrefab(KPfCraftingInputCell);

            var root = NewGameObject(KPfCraftingInputCell);
            SetRectSize(root.AddComponent<RectTransform>(), 300f, 32f);
            SetLayoutElement(root, minH: 32, prefH: 32);
            root.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.20f, 0.70f);
            SetHlg(root, new RectOffset(6, 6, 2, 2), 6f, TextAnchor.MiddleLeft, true, true, false, false);

            var cell = root.AddComponent<UiwCraftingInputCell>();
            cell.iconAttrId        = "图标";
            cell.nameAttrId        = "名称";
            cell.numberFormat      = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            cell.showDetailTooltip = true;

            var iconGo = ChildGameObject("Icon", root.transform);
            iconGo.AddComponent<RectTransform>();
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            cell.iconImage = iconImg;
            SetLayoutElement(iconGo, minW: 26, prefW: 26, minH: 26, prefH: 26);

            var nameGo = ChildGameObject("NameText", root.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, flexW: 1, minH: 24, prefH: 24);
            var nameTxt = AddText(nameGo, "材料名", 12, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(cell, "nameText", nameTxt);

            var amtGo = ChildGameObject("AmountText", root.transform);
            amtGo.AddComponent<RectTransform>();
            SetLayoutElement(amtGo, minW: 64, prefW: 64);
            var amtTxt = AddText(amtGo, "0/0", 12, Color.white, TextAnchor.MiddleRight);
            SetSerializedRef(cell, "amountText", amtTxt);

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwCraftingBlueprintCell（蓝图条目：主产出图标 + 蓝图名 + 属性显示行 + 选中条 + 点击选中）。</summary>
        static void BuildCraftingBlueprintCellPrefab(NumberFormatConfig numFmt, UiwTextLabel labelPrefab)
        {
            string path = BeginPrefab(KPfCraftingBlueprintCell);

            var root = NewGameObject(KPfCraftingBlueprintCell);
            SetRectSize(root.AddComponent<RectTransform>(), 320f, 72f);
            root.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.22f, 0.95f); // 兼作点击射线目标
            SetHlg(root, new RectOffset(8, 6, 4, 4), 8f, TextAnchor.MiddleLeft, true, true, false, false);

            var cell = root.AddComponent<UiwCraftingBlueprintCell>();
            cell.iconAttrId = "图标";
            cell.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;

            // 选中指示条（左侧竖条）
            var selGo = ChildGameObject("SelectedIndicator", root.transform);
            var selRt = selGo.AddComponent<RectTransform>();
            selRt.anchorMin = new Vector2(0f, 0f); selRt.anchorMax = new Vector2(0f, 1f);
            selRt.pivot = new Vector2(0f, 0.5f); selRt.sizeDelta = new Vector2(4f, 0f);
            selRt.anchoredPosition = Vector2.zero;
            var selImg = selGo.AddComponent<Image>(); selImg.color = Hex("66CCFF"); selImg.raycastTarget = false;
            selGo.SetActive(false);
            SetLayoutElement(selGo, ignore: true);
            cell.selectedIndicator = selGo;

            // 主产出图标 + 品质背景框（IconFrame 占据 HLG 槽位，内含品质底图 + 图标）
            var frameGo = ChildGameObject("IconFrame", root.transform);
            frameGo.AddComponent<RectTransform>();
            SetLayoutElement(frameGo, minW: 56, prefW: 56, minH: 56, prefH: 56);

            var qualityGo = ChildGameObject("QualityBackground", frameGo.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = Color.white; qualityImg.preserveAspect = true; qualityImg.raycastTarget = false;
            qualityImg.sprite = LoadSprite(SpriteQualityPoor);
            cell.qualityBackground = qualityImg;

            var iconGo = ChildGameObject("Icon", frameGo.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4f, 4f); iconRt.offsetMax = new Vector2(-4f, -4f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            cell.iconImage = iconImg;

            // 信息列（名称 + 属性行）
            var infoGo = ChildGameObject("Info", root.transform);
            infoGo.AddComponent<RectTransform>();
            SetLayoutElement(infoGo, flexW: 1, prefH: 64);
            SetVlg(infoGo, new RectOffset(0, 0, 2, 2), 2f, TextAnchor.UpperLeft, true, true, true, false);

            var nameGo = ChildGameObject("NameText", infoGo.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 20, prefH: 20);
            var nameTxt = AddText(nameGo, "蓝图名称", 14, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(cell, "nameText", nameTxt);

            var attrGo = ChildGameObject("AttrLines", infoGo.transform);
            attrGo.AddComponent<RectTransform>();
            SetLayoutElement(attrGo, flexH: 1, prefH: 36);
            SetVlg(attrGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, false, true, false, false);
            cell.attrLineContainer = attrGo.transform;
            cell.attrLinePrefab    = labelPrefab;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwCraftingBlueprintList（蓝图虚拟列表：UiwCraftingBlueprintList + ScrollRect + Scrollbar）。</summary>
        static void BuildCraftingBlueprintListPrefab(UiwCraftingBlueprintCell cellPrefab)
        {
            string path = BeginPrefab(KPfCraftingBlueprintList);

            var root   = NewGameObject(KPfCraftingBlueprintList);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = Vector2.zero;
            SetLayoutElement(root, prefH: 9000, flexH: 1, flexW: 1);
            var listComp = root.AddComponent<UiwCraftingBlueprintList>();
            listComp.bufferCount = 1;
            listComp.cellPrefab  = cellPrefab;

            // ScrollRect + Viewport + Content + 竖直滚动条骨架（见 MakeVerticalScrollView）
            var sr = MakeVerticalScrollView(root, out var contentRt);
            listComp.scrollRect = sr;
            listComp.content    = contentRt;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwCraftingView（制作主界面：模板页签 + 搜索/分组 + 排序/列表 + 详情）。</summary>
        static void BuildCraftingViewPrefab(GameObject listPrefab, UiwCraftingInputCell inputCellPrefab,
            GameObject detailPrefab, UiwInventoryItemSimple simplePrefab, UiwInventoryTab tabPrefab, UiwFoldTab foldTabPrefab,
            UiwNumberCounter counterPrefab)
        {
            string path = BeginPrefab(KPfCraftingView);

            var panelGo = NewGameObject(KPfCraftingView);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(900f, 560f);
            panelRt.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.97f);
            SetVlg(panelGo, new RectOffset(6, 6, 6, 6), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var view = panelGo.AddComponent<UiwCraftingView>();

            // 标题
            var headerRow = MakeRow("Header", panelGo.transform, 36f, Hex("0D0D17"));
            SetHlg(headerRow, new RectOffset(10, 10, 0, 0), 0f, TextAnchor.MiddleLeft, true, true, true, true);
            var titleGo = ChildGameObject("TitleText", headerRow.transform);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var titleTxt = AddText(titleGo, "制作  [编辑器测试 — Play 后自动打开]", 13,
                new Color(0.85f, 0.85f, 0.92f), TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(view, "titleLabel", titleTxt);

            // 模板页签行
            var tabRow = MakeRow("TemplateTabRow", panelGo.transform, 34f, new Color(0f, 0f, 0f, 0f));
            SetHlg(tabRow, new RectOffset(4, 4, 2, 2), 3f, TextAnchor.MiddleLeft, false, true, false, true);
            view.templateTabContainer = tabRow.transform;
            view.templateTabPrefab    = tabPrefab;

            // 主体三列
            var bodyGo = ChildGameObject("Body", panelGo.transform);
            bodyGo.AddComponent<RectTransform>();
            var bodyLe = bodyGo.AddComponent<LayoutElement>(); bodyLe.flexibleHeight = 1f; bodyLe.preferredHeight = 9000f;
            SetHlg(bodyGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.UpperLeft, true, true, false, true);

            // ── 左列：搜索 + 分组折叠 ──
            var leftCol = ChildGameObject("LeftColumn", bodyGo.transform);
            leftCol.AddComponent<RectTransform>();
            SetLayoutElement(leftCol, minW: 180, prefW: 180);
            leftCol.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            SetVlg(leftCol, new RectOffset(4, 4, 4, 4), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var searchInput = MakeInputField("SearchInput", leftCol.transform, "搜索蓝图名称…");
            SetLayoutElement(searchInput.gameObject, minH: 26, prefH: 26);
            view.searchInput = searchInput;

            var groupGo = ChildGameObject("GroupFilter", leftCol.transform);
            groupGo.AddComponent<RectTransform>();
            var groupLe = groupGo.AddComponent<LayoutElement>(); groupLe.flexibleHeight = 1f; groupLe.preferredHeight = 9000f;
            SetVlg(groupGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, true, true, true, false);
            var groupFilter = groupGo.AddComponent<UiwCraftingGroupFilter>();
            groupFilter.container      = groupGo.transform;
            groupFilter.uiwFoldTabPrefab = foldTabPrefab;
            view.groupFilter = groupFilter;

            // ── 中列：排序栏 + 蓝图列表 ──
            var midCol = ChildGameObject("MidColumn", bodyGo.transform);
            midCol.AddComponent<RectTransform>();
            SetLayoutElement(midCol, flexW: 1, prefW: 340);
            SetVlg(midCol, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var sortRow = MakeRow("SortRow", midCol.transform, 28f, new Color(0f, 0f, 0f, 0f));
            SetHlg(sortRow, new RectOffset(2, 2, 0, 0), 4f, TextAnchor.MiddleLeft, true, true, false, true);
            var sortDd = MakeDropdown("SortDropdown", sortRow.transform);
            sortDd.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var sdBtnGo = ChildGameObject("SortDirectionButton", sortRow.transform);
            sdBtnGo.AddComponent<RectTransform>();
            var sdLe = sdBtnGo.AddComponent<LayoutElement>(); sdLe.minWidth = 54f; sdLe.preferredWidth = 54f;
            var sdImg = sdBtnGo.AddComponent<Image>(); sdImg.color = Hex("2A2A4A");
            var sdBtn = sdBtnGo.AddComponent<Button>(); sdBtn.targetGraphic = sdImg;
            SetButtonColors(sdBtn, Hex("2A2A4A"), Hex("3A3A6A"), Hex("1E1E36"));
            var sdLblGo = ChildGameObject("Label", sdBtnGo.transform);
            Stretch(sdLblGo.AddComponent<RectTransform>());
            var sdLbl = AddText(sdLblGo, "降序", 11, Color.white);
            var sortTb = sortRow.AddComponent<UiwSortToolbar>();
            sortTb.sortDropdown        = sortDd;
            sortTb.sortDirectionButton = sdBtn;
            SetSerializedRef(sortTb, "sortDirectionLabel", sdLbl);

            if (listPrefab)
            {
                var listInst = (GameObject)PrefabUtility.InstantiatePrefab(listPrefab, midCol.transform);
                var listLe = listInst.GetComponent<LayoutElement>() ?? listInst.AddComponent<LayoutElement>();
                listLe.flexibleHeight = 1f; listLe.preferredHeight = 9000f;
                view.blueprintList = listInst.GetComponent<UiwCraftingBlueprintList>();
                // 排序栏引用配置在蓝图列表组件上（排序管线由 UiwInventoryListBase 内建）。
                if (view.blueprintList) view.blueprintList.sortToolbar = sortTb;
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwCraftingBlueprintList。");

            // ── 右列：详情 ──
            var rightCol = ChildGameObject("RightColumn", bodyGo.transform);
            rightCol.AddComponent<RectTransform>();
            SetLayoutElement(rightCol, minW: 320, prefW: 320);
            rightCol.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            SetVlg(rightCol, new RectOffset(6, 6, 6, 6), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var detail = rightCol.AddComponent<UiwCraftingDetail>();

            if (detailPrefab)
            {
                var mInst = (GameObject)PrefabUtility.InstantiatePrefab(detailPrefab, rightCol.transform);
                SetLayoutElement(mInst, minH: 80, prefH: 80);
                detail.mainOutputDetail = mInst.GetComponent<UiwInventoryItemDetail>();
            }

            var secRow = MakeRow("SecondaryOutputs", rightCol.transform, 40f, new Color(0f, 0f, 0f, 0f));
            SetHlg(secRow, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleLeft, false, true, false, false);
            detail.secondaryOutputContainer = secRow.transform;
            detail.secondaryOutputPrefab    = simplePrefab;

            var inputsGo = ChildGameObject("Inputs", rightCol.transform);
            inputsGo.AddComponent<RectTransform>();
            var inputsLe = inputsGo.AddComponent<LayoutElement>(); inputsLe.flexibleHeight = 1f; inputsLe.preferredHeight = 9000f;
            SetVlg(inputsGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, true, true, true, false);
            detail.inputContainer  = inputsGo.transform;
            detail.inputCellPrefab = inputCellPrefab;

            // 可制作 / 持有
            var infoRow = MakeRow("InfoRow", rightCol.transform, 22f, new Color(0f, 0f, 0f, 0f));
            SetHlg(infoRow, new RectOffset(2, 2, 0, 0), 8f, TextAnchor.MiddleLeft, true, true, false, true);
            var craftableGo = ChildGameObject("CraftableText", infoRow.transform);
            craftableGo.AddComponent<RectTransform>();
            craftableGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var craftableTxt = AddText(craftableGo, "可制作：0", 12, new Color(0.8f, 0.9f, 0.8f), TextAnchor.MiddleLeft);
            SetSerializedRef(detail, "craftableCountText", craftableTxt);
            var ownedGo = ChildGameObject("OwnedText", infoRow.transform);
            ownedGo.AddComponent<RectTransform>();
            ownedGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var ownedTxt = AddText(ownedGo, "持有：0", 12, new Color(0.8f, 0.8f, 0.9f), TextAnchor.MiddleRight);
            SetSerializedRef(detail, "ownedCountText", ownedTxt);

            // 制作次数：实例化通用 PF_UiwNumberCounter（统一驱动 +/- 与显示）
            var countRow = MakeRow("CountRow", rightCol.transform, 30f, new Color(0f, 0f, 0f, 0f));
            SetHlg(countRow, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.MiddleCenter, false, true, false, true);
            if (counterPrefab)
            {
                var craftCounter = (UiwNumberCounter)PrefabUtility.InstantiatePrefab(counterPrefab, countRow.transform);
                detail.counter = craftCounter;
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwNumberCounter，制作界面无法调整制作次数。");

            // 制作 / 停止 按钮
            var craftRow = MakeRow("CraftRow", rightCol.transform, 34f, new Color(0f, 0f, 0f, 0f));
            SetHlg(craftRow, new RectOffset(0, 0, 0, 0), 0f, TextAnchor.MiddleCenter, true, true, true, true);
            var craftGo = ChildGameObject("CraftButton", craftRow.transform);
            craftGo.AddComponent<RectTransform>();
            craftGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var craftImg = craftGo.AddComponent<Image>(); craftImg.color = Hex("2A4A2A");
            var craftBtn = craftGo.AddComponent<Button>(); craftBtn.targetGraphic = craftImg;
            SetButtonColors(craftBtn, Hex("2A4A2A"), Hex("3A6A3A"), Hex("1E361E"));
            detail.craftButton = craftBtn;
            var craftLblGo = ChildGameObject("Label", craftGo.transform);
            Stretch(craftLblGo.AddComponent<RectTransform>());
            var craftLbl = AddText(craftLblGo, "制作", 14, Color.white);
            SetSerializedRef(detail, "craftButtonLabel", craftLbl);

            // 进度条
            var progRow = MakeRow("ProgressRow", rightCol.transform, 10f, new Color(0f, 0f, 0f, 0.3f));
            var progFillGo = ChildGameObject("Fill", progRow.transform);
            var progRt = progFillGo.AddComponent<RectTransform>();
            progRt.anchorMin = Vector2.zero; progRt.anchorMax = Vector2.one;
            progRt.offsetMin = Vector2.zero; progRt.offsetMax = Vector2.zero;
            var progImg = progFillGo.AddComponent<Image>();
            progImg.color       = Hex("66CC66");
            progImg.sprite      = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            progImg.type        = Image.Type.Filled;
            progImg.fillMethod  = Image.FillMethod.Horizontal;
            progImg.fillOrigin  = (int)Image.OriginHorizontal.Left;
            progImg.fillAmount  = 0f;
            detail.progressFill = progImg;

            view.detail = detail;

            // 编辑器测试：进入 Play 自动打开
            var vso = new SerializedObject(view);
            var autoOpenProp = vso.FindProperty("autoOpenOnStart");
            if (autoOpenProp != null) autoOpenProp.boolValue = true;
            vso.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(panelGo, path);
        }

        /// <summary>创建一个标准 UI InputField（文本组件为 UnityEngine.UI.Text，与 UiwCraftingView.searchInput 类型一致）。</summary>
        static InputField MakeInputField(string name, Transform parent, string placeholder)
        {
            var go = ChildGameObject(name, parent);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = Hex("1C2533");
            var input = go.AddComponent<InputField>();
            input.targetGraphic = img;

            var txtGo = ChildGameObject("Text", go.transform);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(6f, 2f); txtRt.offsetMax = new Vector2(-6f, -2f);
            var txt = txtGo.AddComponent<Text>();
            txt.fontSize = 11; txt.color = new Color(0.9f, 0.9f, 0.95f);
            txt.alignment = TextAnchor.MiddleLeft; txt.supportRichText = false;

            var phGo = ChildGameObject("Placeholder", go.transform);
            var phRt = phGo.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(6f, 2f); phRt.offsetMax = new Vector2(-6f, -2f);
            var ph = phGo.AddComponent<Text>();
            ph.fontSize = 11; ph.color = new Color(0.6f, 0.6f, 0.65f);
            ph.alignment = TextAnchor.MiddleLeft; ph.fontStyle = FontStyle.Italic; ph.text = placeholder;

            input.textComponent = txt;
            input.placeholder   = ph;
            return input;
        }

        #endregion
    }
}

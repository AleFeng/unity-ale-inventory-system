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

    /// <summary>技能系统预制体（条目 / 列表 / Tooltip / 技能面板）的构建。</summary>
    public static partial class InventoryDemoWizard
    {
        #region UI预制体 技能系统（条目：Cell / Detail；列表：Grid / Order；Tooltip；View）

        /// <summary>构建 PF_UiwSkillCell（技能网格条目：位阶背景框 + 图标 + 名称，支持悬停弹窗）。</summary>
        static void BuildSkillCellPrefab()
        {
            string path = BeginPrefab(KPfSkillCell);

            var root = NewGameObject(KPfSkillCell);
            SetRectSize(root.AddComponent<RectTransform>(), 72f, 72f);
            root.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.20f, 0.90f); // 兼作悬停射线目标

            var entry = root.AddComponent<UiwSkillEntry>();
            entry.rankAttrId           = "位阶";
            entry.rankBackgroundAttrId = "背景框";
            entry.fallbackToId         = true;
            entry.showTooltip          = true;

            // 位阶背景框（铺满整格，位于图标之下；无位阶数据时运行时自动隐藏）
            var rankGo = ChildGameObject("RankBackground", root.transform);
            Stretch(rankGo.AddComponent<RectTransform>());
            var rankImg = rankGo.AddComponent<Image>();
            rankImg.color = Color.white; rankImg.raycastTarget = false; rankImg.enabled = false;
            entry.rankBackground = rankImg;

            // 图标（顶部居中，略内缩）
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -6f);
            iconRt.sizeDelta = new Vector2(44f, 44f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            entry.iconImage = iconImg;

            // 名称（底部）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f); nameRt.anchorMax = new Vector2(1f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.sizeDelta = new Vector2(-4f, 18f);
            nameRt.anchoredPosition = new Vector2(0f, 2f);
            var nameTxt = AddText(nameGo, "技能名", 10, Color.white);
            SetSerializedRef(entry, "nameText", nameTxt);

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwSkillDetail（技能列表条目：图标(含位阶背景框) + 名称 + 描述，支持悬停弹窗）。</summary>
        static void BuildSkillDetailPrefab()
        {
            string path = BeginPrefab(KPfSkillDetail);

            var root = NewGameObject(KPfSkillDetail);
            SetRectSize(root.AddComponent<RectTransform>(), 320f, 60f);
            SetLayoutElement(root, minH: 60, prefH: 60, flexW: 1);
            root.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.20f, 0.85f);
            SetHlg(root, new RectOffset(6, 6, 4, 4), 8f, TextAnchor.MiddleLeft, true, true, false, false);

            var entry = root.AddComponent<UiwSkillEntry>();
            entry.rankAttrId           = "位阶";
            entry.rankBackgroundAttrId = "背景框";
            entry.fallbackToId         = true;
            entry.showTooltip          = true;

            // 图标容器（位阶背景框 + 图标）
            var iconRoot = ChildGameObject("IconRoot", root.transform);
            iconRoot.AddComponent<RectTransform>();
            SetLayoutElement(iconRoot, minW: 48, prefW: 48, minH: 48, prefH: 48);

            var rankGo = ChildGameObject("RankBackground", iconRoot.transform);
            Stretch(rankGo.AddComponent<RectTransform>());
            var rankImg = rankGo.AddComponent<Image>();
            rankImg.color = Color.white; rankImg.raycastTarget = false; rankImg.enabled = false;
            entry.rankBackground = rankImg;

            var iconGo = ChildGameObject("Icon", iconRoot.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4f, 4f); iconRt.offsetMax = new Vector2(-4f, -4f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            entry.iconImage = iconImg;

            // 文本块（名称 + 描述，纵向）
            var textCol = ChildGameObject("TextColumn", root.transform);
            textCol.AddComponent<RectTransform>();
            SetLayoutElement(textCol, flexW: 1, minH: 48, prefH: 48);
            SetVlg(textCol, new RectOffset(0, 0, 2, 2), 2f, TextAnchor.UpperLeft, true, true, true, false);

            var nameGo = ChildGameObject("NameText", textCol.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 20, prefH: 20);
            var nameTxt = AddText(nameGo, "技能名", 14, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(entry, "nameText", nameTxt);

            var descGo = ChildGameObject("DescText", textCol.transform);
            descGo.AddComponent<RectTransform>();
            SetLayoutElement(descGo, flexH: 1, minH: 20, prefH: 22);
            var descTxt = AddText(descGo, "技能描述", 11, new Color(0.72f, 0.72f, 0.80f), TextAnchor.UpperLeft);
            SetSerializedRef(entry, "descText", descTxt);

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwSkillGridList（技能网格列表：ScrollRect + 虚拟滚动 UiwSkillGridList，纵向滚动·自动列数）。</summary>
        static void BuildSkillGridListPrefab(UiwSkillEntry cellPrefab)
        {
            string path = BeginPrefab(KPfSkillGridList);

            var root = NewGameObject(KPfSkillGridList);
            Stretch(root.AddComponent<RectTransform>());
            var comp = root.AddComponent<UiwSkillGridList>();
            comp.cellPrefab      = cellPrefab;
            comp.bufferCount     = 1;
            comp.scrollDirection = EListScrollDirection.Vertical;
            comp.spacing         = new Vector2(6f, 6f);
            comp.padding         = new Vector2(6f, 6f);
            if (!cellPrefab) Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillCell，技能网格列表条目为空。");

            // ScrollRect + Viewport + Content + 竖直滚动条骨架（技能列表沿用 Unity 默认惯性衰减 0.135）
            var sr = MakeVerticalScrollView(root, out var contentRt, decelerationRate: 0.135f);
            comp.content    = contentRt;
            comp.scrollRect = sr;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwSkillOrderList（技能顺序列表：ScrollRect + 虚拟滚动 UiwSkillOrderList）。</summary>
        static void BuildSkillOrderListPrefab(UiwSkillEntry detailPrefab)
        {
            string path = BeginPrefab(KPfSkillOrderList);

            var root = NewGameObject(KPfSkillOrderList);
            Stretch(root.AddComponent<RectTransform>());
            var comp = root.AddComponent<UiwSkillOrderList>();
            comp.cellPrefab  = detailPrefab;
            comp.bufferCount = 1;
            if (!detailPrefab) Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillDetail，技能顺序列表条目为空。");

            // ScrollRect + Viewport + Content + 竖直滚动条骨架（技能列表沿用 Unity 默认惯性衰减 0.135）
            var sr = MakeVerticalScrollView(root, out var contentRt, decelerationRate: 0.135f);
            comp.content    = contentRt;
            comp.scrollRect = sr;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwSkillTooltip（技能悬停弹窗：图标 + 名称 + 位阶名 + 描述；由 InventoryManager 全局实例化）。</summary>
        static void BuildSkillTooltipPrefab()
        {
            string path = BeginPrefab(KPfSkillTooltip);

            var root = NewGameObject(KPfSkillTooltip);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 150f);
            rt.pivot     = new Vector2(0f, 1f);   // 左上角，便于按光标定位
            root.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.96f);
            var cg = root.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            SetVlg(root, new RectOffset(10, 10, 8, 8), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var tip = root.AddComponent<UiwSkillTooltip>();
            tip.rankAttrId     = "位阶";
            tip.rankNameAttrId = "名称";
            tip.panel          = rt;
            tip.canvasGroup    = cg;

            // 顶部行：图标 + 名称 + 位阶名
            var headRow = ChildGameObject("Header", root.transform);
            headRow.AddComponent<RectTransform>();
            SetLayoutElement(headRow, minH: 32, prefH: 32);
            SetHlg(headRow, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.MiddleLeft, true, true, false, false);

            var iconGo = ChildGameObject("Icon", headRow.transform);
            iconGo.AddComponent<RectTransform>();
            SetLayoutElement(iconGo, minW: 30, prefW: 30, minH: 30, prefH: 30);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            tip.iconImage = iconImg;

            var nameGo = ChildGameObject("NameText", headRow.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, flexW: 1, minH: 28, prefH: 28);
            var nameTxt = AddText(nameGo, "技能名", 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(tip, "nameText", nameTxt);

            var rankGo = ChildGameObject("RankNameText", headRow.transform);
            rankGo.AddComponent<RectTransform>();
            SetLayoutElement(rankGo, minW: 72, prefW: 72, minH: 28, prefH: 28);
            var rankTxt = AddText(rankGo, "位阶", 12, Hex("FFD24D"), TextAnchor.MiddleRight);
            SetSerializedRef(tip, "rankNameText", rankTxt);

            // 描述
            var descGo = ChildGameObject("DescText", root.transform);
            descGo.AddComponent<RectTransform>();
            SetLayoutElement(descGo, flexH: 1, minH: 40, prefH: 60);
            var descTxt = AddText(descGo, "技能描述", 12, new Color(0.80f, 0.80f, 0.88f), TextAnchor.UpperLeft);
            SetSerializedRef(tip, "descText", descTxt);

            // 自定义属性字段容器（预留：在组件上填 customFieldKeys + customFieldLinePrefab 即逐行显示；此处默认空，不显示）
            var fieldsGo = ChildGameObject("CustomFields", root.transform);
            fieldsGo.AddComponent<RectTransform>();
            SetLayoutElement(fieldsGo, minH: 0, prefH: 0);
            SetVlg(fieldsGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, true, true, true, false);
            tip.customFieldContainer = fieldsGo.transform;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwSkillView（技能主界面：标题 + 视图切换 + 搜索 + 主/副分组页签 + 网格/顺序列表）。</summary>
        static void BuildSkillViewPrefab(GameObject gridListPrefab, GameObject orderListPrefab, Button filterButtonPrefab)
        {
            string path = BeginPrefab(KPfSkillView);

            var panelGo = NewGameObject(KPfSkillView);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(420f, 560f);
            panelRt.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.97f);
            SetVlg(panelGo, new RectOffset(6, 6, 6, 6), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var view = panelGo.AddComponent<UiwSkillView>();
            view.source         = ESkillSource.InventoryDatabase;
            view.titleText      = "技能";
            view.skillRefAttrId = "技能";
            view.showAllTab     = true;

            // 标题行（标题 + 视图切换按钮）
            var headerRow = MakeRow("Header", panelGo.transform, 36f, Hex("0D0D17"));
            SetHlg(headerRow, new RectOffset(10, 6, 0, 0), 6f, TextAnchor.MiddleLeft, true, true, false, true);
            var titleGo = ChildGameObject("TitleText", headerRow.transform);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var titleTxt = AddText(titleGo, "技能  [编辑器测试 — Play 后自动打开]", 13,
                new Color(0.85f, 0.85f, 0.92f), TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(view, "titleLabel", titleTxt);

            var toggleGo = ChildGameObject("ViewModeToggle", headerRow.transform);
            toggleGo.AddComponent<RectTransform>();
            SetLayoutElement(toggleGo, minW: 56, prefW: 56, minH: 26, prefH: 26);
            var toggleImg = toggleGo.AddComponent<Image>(); toggleImg.color = Hex("2A2A4A");
            var toggleBtn = toggleGo.AddComponent<Button>(); toggleBtn.targetGraphic = toggleImg;
            SetButtonColors(toggleBtn, Hex("2A2A4A"), Hex("3A3A6A"), Hex("1E1E36"));
            var toggleLblGo = ChildGameObject("Label", toggleGo.transform);
            Stretch(toggleLblGo.AddComponent<RectTransform>());
            var toggleLbl = AddText(toggleLblGo, "列表", 12, Color.white);
            view.viewModeToggleButton = toggleBtn;
            SetSerializedRef(view, "viewModeToggleLabel", toggleLbl);

            // 搜索行
            var searchInput = MakeInputField("SearchInput", panelGo.transform, "搜索技能名称…");
            SetLayoutElement(searchInput.gameObject, minH: 26, prefH: 26);
            view.searchInput = searchInput;

            // 主 / 副分组页签行：各一横向 ScrollView（标签过多可横向滚动，不超范围不滚动），按钮复用 PF_FilterTabBtn。
            // 主分组、副分组为两个 AND 筛选条件（两者都满足的技能才显示）。
            BuildFilterTabScroll("PrimaryGroupTabScroll", panelGo.transform, filterButtonPrefab, 30f, out var primaryBar);
            BuildFilterTabScroll("SecondaryGroupTabScroll", panelGo.transform, filterButtonPrefab, 30f, out var secondaryBar);
            if (!filterButtonPrefab) Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_FilterTabBtn，技能分组页签无按钮。");

            // 列表容器（网格 + 顺序两个列表实例叠放，由视图切换显示其一）
            var listHost = ChildGameObject("ListHost", panelGo.transform);
            listHost.AddComponent<RectTransform>();
            var listHostLe = listHost.AddComponent<LayoutElement>();
            listHostLe.flexibleHeight = 1f; listHostLe.preferredHeight = 9000f;

            if (orderListPrefab)
            {
                var orderInst = (GameObject)PrefabUtility.InstantiatePrefab(orderListPrefab, listHost.transform);
                Stretch((RectTransform)orderInst.transform);
                view.orderList = orderInst.GetComponent<UiwSkillOrderList>();
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillOrderList。");

            if (gridListPrefab)
            {
                var gridInst = (GameObject)PrefabUtility.InstantiatePrefab(gridListPrefab, listHost.transform);
                Stretch((RectTransform)gridInst.transform);
                view.gridList = gridInst.GetComponent<UiwSkillGridList>();
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillGridList。");

            // 主 / 副分组过滤栏引用配置在技能列表组件上（网格 + 顺序两列表各引用同一对栏；仅激活的列表响应其事件）。
            if (view.orderList) { view.orderList.filterBar = primaryBar; view.orderList.secondaryFilterBar = secondaryBar; }
            if (view.gridList)  { view.gridList.filterBar  = primaryBar; view.gridList.secondaryFilterBar  = secondaryBar; }

            SavePrefab(panelGo, path);
        }

        #endregion
    }
}

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

    /// <summary>仓库主面板 UiwInventoryView 预制体的构建。</summary>
    public static partial class InventoryDemoWizard
    {
        #region UI预制体 InventoryPanel
        /// <summary>
        /// 构建并保存 PF_InventoryPanel 独立预制体。
        /// <para>
        /// 面板以根节点形式创建（无 Canvas 父节点），可直接拖入已有 Canvas 使用。
        /// 保存完毕后临时 GameObject 会被销毁；调用方通过加载返回的资产路径获取预制体实例。
        /// </para>
        /// </summary>
        /// <returns>已保存的 PF_InventoryPanel 预制体的资产路径。</returns>
        static void BuildInventoryPanelPrefab(
            UiwInventoryTab        tabPrefab,
            Button                 filterBtnPrefab,
            UiwInventoryItemSimple itemSimplePrefab,
            GameObject             listPanelPrefab,
            GameObject             gridPrefab)
        {
            string panelPath = Pfb(KPfInventoryPanel);
            DeleteIfExists(panelPath);

            // ── 面板根节点（锚定居中，固定尺寸 540×660）─────────────────────────
            var panelGo = NewGameObject(KPfInventoryPanel);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRt.pivot            = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta        = new Vector2(540f, 660f);
            panelRt.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.97f);

            // VerticalLayoutGroup 管理行堆叠
            var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth      = true; vlg.childControlHeight     = true;
            vlg.childForceExpandWidth  = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(6, 6, 6, 6);

            // UiwInventoryView 组件
            var viewComp = panelGo.AddComponent<UiwInventoryView>();

            // ── Row: 标题栏 ───────────────────────────────────────────────────
            var headerRow = MakeRow("Header", panelGo.transform, 40f, Hex("0D0D17"));
            var hHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            hHlg.childControlWidth = true; hHlg.childForceExpandWidth = true;
            hHlg.childControlHeight = true; hHlg.childForceExpandHeight = true;
            hHlg.padding = new RectOffset(10, 10, 0, 0);

            var titleGo = ChildGameObject("TitleText", headerRow.transform);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddText(titleGo,
                    "仓库系统  [编辑器测试 — Play 后自动填入道具]",
                    13, new Color(0.85f, 0.85f, 0.92f),
                    TextAnchor.MiddleLeft, FontStyle.Bold);

            // ── Row: 页签 (tabContainer) ─────────────────────────────────────
            var tabRow = MakeRow("TabRow", panelGo.transform, 50f, new Color(0f, 0f, 0f, 0f));
            var tHlg = tabRow.AddComponent<HorizontalLayoutGroup>();
            tHlg.childControlWidth = false; tHlg.childForceExpandWidth = false;
            tHlg.childControlHeight = true; tHlg.childForceExpandHeight = true;
            tHlg.spacing = 3f; tHlg.padding = new RectOffset(4, 4, 2, 2);
            viewComp.tabContainer = tabRow.transform;
            viewComp.tabPrefab    = tabPrefab;

            // ── Row: 过滤按钮 ─────────────────────────────────────────────────
            var filterRow = MakeRow("FilterRow", panelGo.transform, 30f, new Color(0f, 0f, 0f, 0f));
            var fHlg = filterRow.AddComponent<HorizontalLayoutGroup>();
            fHlg.childControlWidth = false; fHlg.childForceExpandWidth = false;
            fHlg.childControlHeight = true; fHlg.childForceExpandHeight = true;
            fHlg.spacing = 4f; fHlg.padding = new RectOffset(4, 4, 2, 2);

            // FilterContainer（弹性撑满）
            var filterContGo = ChildGameObject("FilterContainer", filterRow.transform);
            filterContGo.AddComponent<RectTransform>();
            filterContGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var fcHlg = filterContGo.AddComponent<HorizontalLayoutGroup>();
            fcHlg.childControlWidth = false; fcHlg.childForceExpandWidth = false;
            fcHlg.childControlHeight = true; fcHlg.childForceExpandHeight = true;
            fcHlg.spacing = 2f;
            // 过滤页签栏组件（通用 UiwFilterTabBar）
            var filterBar = filterContGo.AddComponent<UiwFilterTabBar>();
            filterBar.filterContainer    = filterContGo.transform;
            filterBar.filterButtonPrefab = filterBtnPrefab;
            viewComp.filterBar = filterBar;

            // ── Row: 排序（下拉 + 升降序 + 整理） ─────────────────────────────
            var sortRow = MakeRow("SortRow", panelGo.transform, 30f, new Color(0f, 0f, 0f, 0f));
            var srHlg = sortRow.AddComponent<HorizontalLayoutGroup>();
            srHlg.childControlWidth      = true;
            srHlg.childForceExpandWidth  = false;
            srHlg.childControlHeight     = true;
            srHlg.childForceExpandHeight = true;
            srHlg.spacing = 4f; srHlg.padding = new RectOffset(4, 4, 2, 2);

            // SortDropdown（弹性撑满左侧）
            // 选项来自仓库"整理列表"（sortPriorities），运行时由 UiwInventoryView.BuildSortDropdown 填充
            var sortDd = MakeDropdown("SortDropdown", sortRow.transform);
            sortDd.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // SortDirection 按钮（固定宽度，点击切换"升序"/"降序"）
            var sdBtnGo = ChildGameObject("SortDirectionButton", sortRow.transform);
            sdBtnGo.AddComponent<RectTransform>();
            var sdLe = sdBtnGo.AddComponent<LayoutElement>();
            sdLe.minWidth = 54f; sdLe.preferredWidth = 54f; sdLe.flexibleWidth = 0f;
            var sdImg = sdBtnGo.AddComponent<Image>();
            sdImg.color = Hex("2A2A4A");
            var sdBtn = sdBtnGo.AddComponent<Button>();
            sdBtn.targetGraphic = sdImg;
            SetButtonColors(sdBtn, Hex("2A2A4A"), Hex("3A3A6A"), Hex("1E1E36"));
            var sdLblGo = ChildGameObject("Label", sdBtnGo.transform);
            Stretch(sdLblGo.AddComponent<RectTransform>());
            var sdLblComp = AddText(sdLblGo, "升序", 11, Color.white);

            // AutoSort 按钮（固定宽度）
            var asBtnGo = ChildGameObject("AutoSortButton", sortRow.transform);
            asBtnGo.AddComponent<RectTransform>();
            var asLe = asBtnGo.AddComponent<LayoutElement>();
            asLe.minWidth = 54f; asLe.preferredWidth = 54f; asLe.flexibleWidth = 0f;
            var asImg = asBtnGo.AddComponent<Image>();
            asImg.color = Hex("2A4A2A");
            var asBtn = asBtnGo.AddComponent<Button>();
            asBtn.targetGraphic = asImg;
            SetButtonColors(asBtn, Hex("2A4A2A"), Hex("3A6A3A"), Hex("1E361E"));
            var asLblGo = ChildGameObject("Label", asBtnGo.transform);
            Stretch(asLblGo.AddComponent<RectTransform>());
            AddText(asLblGo, "整理", 11, Color.white);

            // 排序整理栏组件（通用 UiwSortToolbar：下拉 + 升降序 + 自动整理）
            var sortTb = sortRow.AddComponent<UiwSortToolbar>();
            sortTb.sortDropdown        = sortDd;
            sortTb.sortDirectionButton = sdBtn;
            SetSerializedRef(sortTb, "sortDirectionLabel", sdLblComp);
            sortTb.autoSortButton      = asBtn;
            viewComp.sortToolbar = sortTb;

            // ViewToggle 按钮（列表 / 网格 切换，固定宽度）
            var vtBtnGo = ChildGameObject("ViewToggleButton", sortRow.transform);
            vtBtnGo.AddComponent<RectTransform>();
            var vtLe = vtBtnGo.AddComponent<LayoutElement>();
            vtLe.minWidth = 54f; vtLe.preferredWidth = 54f; vtLe.flexibleWidth = 0f;
            var vtImg = vtBtnGo.AddComponent<Image>();
            vtImg.color = Hex("2A2A4A");
            var vtBtn = vtBtnGo.AddComponent<Button>();
            vtBtn.targetGraphic = vtImg;
            SetButtonColors(vtBtn, Hex("2A2A4A"), Hex("3A3A6A"), Hex("1E1E36"));
            var vtLblGo = ChildGameObject("Label", vtBtnGo.transform);
            Stretch(vtLblGo.AddComponent<RectTransform>());
            var vtLblComp = AddText(vtLblGo, "网格", 11, Color.white);   // 当前为列表模式 → 按钮显示"网格"
            viewComp.viewModeToggleButton = vtBtn;
            SetSerializedRef(viewComp, "viewModeToggleLabel", vtLblComp);

            // ── Row: 货币栏（CurrencyContainer；货币 ID 对应脚本数据库定义）──────
            var currencyRow = MakeRow("CurrencyRow", panelGo.transform, 24f, new Color(0f, 0f, 0f, 0f));
            var crHlg = currencyRow.AddComponent<HorizontalLayoutGroup>();
            crHlg.childControlWidth = false; crHlg.childForceExpandWidth = false;
            crHlg.childControlHeight = true; crHlg.childForceExpandHeight = true;
            crHlg.childAlignment = TextAnchor.MiddleLeft;
            crHlg.spacing = 4f; crHlg.padding = new RectOffset(4, 4, 2, 2);

            var currencyContGo = ChildGameObject("CurrencyContainer", currencyRow.transform);
            currencyContGo.AddComponent<RectTransform>();
            currencyContGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var ccHlg = currencyContGo.AddComponent<HorizontalLayoutGroup>();
            ccHlg.childControlWidth = false; ccHlg.childForceExpandWidth = false;
            ccHlg.childControlHeight = true; ccHlg.childForceExpandHeight = true;
            ccHlg.spacing = 4f;
            // 货币栏组件（通用 UiwCurrencyBar）
            var currencyBar = currencyContGo.AddComponent<UiwCurrencyBar>();
            currencyBar.currencyContainer = currencyContGo.transform;
            currencyBar.currencyPrefab    = itemSimplePrefab;
            currencyBar.currencyItemIds   = new[] { "金币", "银币", "铜币" };
            viewComp.currencyBar = currencyBar;

            // ── 道具区（弹性占满）：内嵌 列表面板 + 网格面板 实例 ─────────────────
            var itemAreaGo = ChildGameObject("ItemArea", panelGo.transform);
            itemAreaGo.AddComponent<RectTransform>();
            var areaLe = itemAreaGo.AddComponent<LayoutElement>();
            areaLe.flexibleHeight = 1f; areaLe.preferredHeight = 9000f;

            if (listPanelPrefab)
            {
                var listInst = (GameObject)PrefabUtility.InstantiatePrefab(listPanelPrefab, itemAreaGo.transform);
                Stretch((RectTransform)listInst.transform);
                viewComp.itemOrderList = listInst.GetComponent<UiwInventoryItemOrderList>();
            }
            if (gridPrefab)
            {
                var gridInst = (GameObject)PrefabUtility.InstantiatePrefab(gridPrefab, itemAreaGo.transform);
                Stretch((RectTransform)gridInst.transform);
                gridInst.SetActive(false);   // 默认列表模式，由 UiwInventoryView.ApplyViewMode 校正
                viewComp.itemGridList = gridInst.GetComponent<UiwInventoryItemGridList>();
            }

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(panelGo);
#endif

            MovePrimaryUiwToTop(panelGo);
            PrefabUtility.SaveAsPrefabAsset(panelGo, panelPath);
            Object.DestroyImmediate(panelGo);   // 临时 GO 使命完成，立即销毁
            Debug.Log("[InventoryDemoWizard] 面板 Prefab 已保存：" + panelPath);
        }
        #endregion
    }
}

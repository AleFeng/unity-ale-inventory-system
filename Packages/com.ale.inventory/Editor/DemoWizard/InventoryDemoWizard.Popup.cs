using Ale.Toolkit.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Ale.Inventory.Runtime.UI;
using static Ale.Toolkit.Editor.UiPrefabBuilder;
using static Ale.Toolkit.Editor.UiTextBuilder;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 道具操作弹窗类预制体（右键菜单条目行 / 右键菜单 / 详情弹窗 / 丢弃弹窗）的构建。
    ///
    /// <para>四者都由 InventoryRuntimeManager 持有预制体、运行时全局实例化一次
    /// （与道具悬停弹窗 PF_UiwItemTooltip 同一宿主模式），故<b>根节点一律以未激活状态保存</b>：
    /// 显示与隐藏由各自的 Open / Close 驱动，不该在场景里常驻拦截射线。</para>
    ///
    /// <para>菜单 / 弹窗的根都是<b>全屏</b>的：其下第一层是全屏遮罩（拦截下方点击、并作为「点击外部关闭」的判定面），
    /// 第二层才是面板。</para>
    /// </summary>
    public static partial class InventoryDemoWizard
    {
        #region UI预制体 道具操作弹窗（ContextMenuRow / ItemContextMenu / ItemDetailPopup / ItemDiscardPopup）

        // 弹窗配色（与既有面板保持同一套深色调）
        static Color PopupPanelColor  => new Color(0.08f, 0.09f, 0.13f, 0.98f);
        static Color PopupHeaderColor => new Color(0.85f, 0.85f, 0.92f);

        /// <summary>
        /// 把 root 搭成「全屏遮罩 + 面板」的弹窗骨架，并接好 UiwModalPopupBase 的
        /// 遮罩 / 面板 / CanvasGroup 三个引用。返回面板节点。
        /// </summary>
        /// <param name="root">弹窗根节点（本方法会把它拉伸为全屏）。</param>
        /// <param name="popup">根节点上的弹窗组件。</param>
        /// <param name="panelSize">面板尺寸。</param>
        /// <param name="blockerAlpha">遮罩不透明度（右键菜单用 0：只拦点击、不压暗画面）。</param>
        static GameObject MakePopupShell(GameObject root, UiwModalPopupBase popup, Vector2 panelSize, float blockerAlpha)
        {
            Stretch(root.GetComponent<RectTransform>());
            var cg = root.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;              // 初始隐藏；由 Open 淡入
            cg.blocksRaycasts = false;

            var blocker = MakeFullScreenBlocker("Blocker", root.transform, blockerAlpha);

            var panelGo = ChildGameObject("Panel", root.transform);
            var panelRt = panelGo.AddComponent<RectTransform>();
            SetRectSize(panelRt, panelSize.x, panelSize.y);
            panelGo.AddComponent<Image>().color = PopupPanelColor;

            popup.panel       = panelRt;
            popup.blocker     = blocker;
            popup.canvasGroup = cg;
            return panelGo;
        }

        /// <summary>构建 PF_UiwContextMenuRow（右键菜单的一行：按钮 + 图标 + 文本，由菜单在运行时池化复用）。</summary>
        static void BuildContextMenuRowPrefab()
        {
            string path = BeginPrefab(KPfContextMenuRow);

            var root = NewGameObject(KPfContextMenuRow);
            SetRectSize(root.AddComponent<RectTransform>(), 132f, 30f);
            SetLayoutElement(root, minW: 132, prefW: 132, minH: 30, prefH: 30);
            SetHlg(root, new RectOffset(10, 10, 0, 0), 6f, TextAnchor.MiddleLeft, true, true, false, true);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0f);          // 常态透明，靠 Button 的高亮色出效果
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            SetButtonColors(btn, new Color(1f, 1f, 1f, 0f), Hex("2C3D50"), Hex("1C2533"));

            var row = root.AddComponent<UiwContextMenuRow>();

            // 图标（可选）：条目未提供图标时由 UiwContextMenuRow 在运行时整节点隐藏
            var iconGo = ChildGameObject("Icon", root.transform);
            iconGo.AddComponent<RectTransform>();
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            SetLayoutElement(iconGo, minW: 18, prefW: 18, minH: 18, prefH: 18);
            iconGo.SetActive(false);

            var lblGo = ChildGameObject("Label", root.transform);
            lblGo.AddComponent<RectTransform>();
            SetLayoutElement(lblGo, minW: 80, flexW: 1);
            var lbl = AddText(lblGo, "条目", 14, PopupHeaderColor, TextAnchor.MiddleLeft);

            // 三个字段都是 [SerializeField] private，故经 SerializedObject 赋值（同时兼容 ATK_TMP 的文本类型差异）
            SetSerializedRef(row, "button",    btn);
            SetSerializedRef(row, "labelText", lbl);
            SetSerializedRef(row, "iconImage", iconImg);

            SavePrefab(root, path);
        }

        /// <summary>
        /// 构建 PF_UiwItemContextMenu（道具右键操作菜单：查看 / 使用 / 丢弃 + 上层贡献的条目）。
        /// 面板轴心取左上，使菜单自光标向右下展开；高度由 ContentSizeFitter 随条目数自适应。
        /// </summary>
        static void BuildItemContextMenuPrefab(UiwContextMenuRow rowPrefab)
        {
            string path = BeginPrefab(KPfItemContextMenu);

            var root = NewGameObject(KPfItemContextMenu);
            root.AddComponent<RectTransform>();
            var menu = root.AddComponent<UiwItemContextMenu>();

            // 遮罩全透明：右键菜单只需拦住外部点击，不该把整个界面压暗。
            var panelGo = MakePopupShell(root, menu, new Vector2(132f, 30f), 0f);

            var panelRt = (RectTransform)panelGo.transform;
            panelRt.pivot = new Vector2(0f, 1f);           // 左上角：贴着光标向右下展开
            SetVlg(panelGo, new RectOffset(2, 2, 2, 2), 1f, TextAnchor.UpperLeft, true, true, true, false);
            SetContentSizeFitter(panelGo, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize);

            menu.rowContainer = panelGo.transform;
            menu.rowPrefab    = rowPrefab;
            if (!rowPrefab)
                Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwContextMenuRow，右键菜单将无条目可显示。");

            root.SetActive(false);   // 关闭态保存：显示由 Open 驱动
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwItemDetailPopup（右键菜单「查看」：内嵌 UiwInventoryItemDetail + 右上角关闭按钮）。</summary>
        static void BuildItemDetailPopupPrefab(GameObject detailPrefab)
        {
            string path = BeginPrefab(KPfItemDetailPopup);

            var root = NewGameObject(KPfItemDetailPopup);
            root.AddComponent<RectTransform>();
            var popup = root.AddComponent<UiwItemDetailPopup>();

            var panelGo = MakePopupShell(root, popup, new Vector2(560f, 150f), 0.5f);

            // 内嵌详情：四边内缩，顶部多留一段给关闭按钮
            if (detailPrefab)
            {
                var detailInst = (GameObject)PrefabUtility.InstantiatePrefab(detailPrefab, panelGo.transform);
                var dRt = (RectTransform)detailInst.transform;
                dRt.anchorMin = Vector2.zero; dRt.anchorMax = Vector2.one;
                dRt.offsetMin = new Vector2(10f, 10f);
                dRt.offsetMax = new Vector2(-10f, -34f);
                popup.detail = detailInst.GetComponent<UiwInventoryItemDetail>();
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwInventoryItemDetail，详情弹窗内容为空。");

            // 右上角关闭按钮
            var closeBtn = MakeMiniButton("CloseButton", panelGo.transform, "X",
                Hex("3A2A2A"), Hex("5A3A3A"), Hex("2A1E1E"));
            var cRt = (RectTransform)closeBtn.transform;
            cRt.anchorMin = cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot     = new Vector2(1f, 1f);
            cRt.sizeDelta = new Vector2(26f, 26f);
            cRt.anchoredPosition = new Vector2(-6f, -4f);
            popup.closeButton = closeBtn;

            root.SetActive(false);
            SavePrefab(root, path);
        }

        /// <summary>
        /// 构建 PF_UiwItemDiscardPopup（右键菜单「丢弃」：道具预览 + 数量滑杆 + 丢弃 / 取消）。
        /// 滑杆范围与取整由 UiwItemDiscardPopup 在打开时按该槽实时数量设定，此处只搭结构。
        /// </summary>
        static void BuildItemDiscardPopupPrefab(GameObject itemSimplePrefab)
        {
            string path = BeginPrefab(KPfItemDiscardPopup);

            var root = NewGameObject(KPfItemDiscardPopup);
            root.AddComponent<RectTransform>();
            var popup = root.AddComponent<UiwItemDiscardPopup>();

            var panelGo = MakePopupShell(root, popup, new Vector2(420f, 250f), 0.5f);
            SetVlg(panelGo, new RectOffset(16, 16, 14, 14), 10f, TextAnchor.UpperCenter, true, false, true, false);

            // 标题（纯装饰，无需引用）
            var titleGo = ChildGameObject("TitleText", panelGo.transform);
            titleGo.AddComponent<RectTransform>();
            SetLayoutElement(titleGo, minH: 24, prefH: 24);
            AddText(titleGo, "丢弃道具", 16, PopupHeaderColor, TextAnchor.MiddleCenter, FontStyle.Bold);

            // 道具预览（复用简易格子）
            if (itemSimplePrefab)
            {
                var previewInst = (GameObject)PrefabUtility.InstantiatePrefab(itemSimplePrefab, panelGo.transform);
                SetLayoutElement(previewInst, minH: 48, prefH: 48);
                popup.itemPreview = previewInst.GetComponent<UiwInventoryItemSimple>();
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwInventoryItemSimple，丢弃弹窗无道具预览。");

            // 数量文本
            var amountGo = ChildGameObject("AmountText", panelGo.transform);
            amountGo.AddComponent<RectTransform>();
            SetLayoutElement(amountGo, minH: 24, prefH: 24);
            var amountTxt = AddText(amountGo, "1 / 1", 15, Color.white, TextAnchor.MiddleCenter);
            SetSerializedRef(popup, "amountText", amountTxt);

            // 数量滑杆
            var slider = MakeSlider("AmountSlider", panelGo.transform);
            SetLayoutElement(slider.gameObject, minH: 22, prefH: 22);
            popup.amountSlider = slider;

            // 底部按钮行：丢弃 / 取消
            var btnRow = ChildGameObject("Buttons", panelGo.transform);
            btnRow.AddComponent<RectTransform>();
            SetLayoutElement(btnRow, minH: 34, prefH: 34);
            SetHlg(btnRow, new RectOffset(0, 0, 0, 0), 12f, TextAnchor.MiddleCenter, true, true, true, true);

            popup.confirmButton = MakeEquipButton("ConfirmButton", btnRow.transform, "丢弃", Hex("8A3A3A"));
            popup.cancelButton  = MakeEquipButton("CancelButton",  btnRow.transform, "取消", Hex("32404F"));

            root.SetActive(false);
            SavePrefab(root, path);
        }

        #endregion
    }
}

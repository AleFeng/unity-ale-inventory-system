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

    /// <summary>页签类预制体（仓库页签 / 过滤按钮 / 折叠页签）的构建。</summary>
    public static partial class InventoryDemoWizard
    {
        #region UI预制体 页签
        /// <summary>
        /// 仓库页签，在多个仓库之间进行切换。
        /// </summary>
        /// <returns></returns>
        static void BuildTabPrefab()
        {
            string path = Pfb(KPfInventoryTab);
            DeleteIfExists(path);

            // root: Button + Image + UiwInventoryTab
            var root = NewGameObject(KPfInventoryTab);
            SetRectSize(root.AddComponent<RectTransform>(), 108, 36);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = Hex("2D3148");
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            SetButtonColors(btn, Hex("2D3148"), Hex("3D4268"), Hex("22253A"));
            var tab = root.AddComponent<UiwInventoryTab>();

            // Label
            var labelGo = ChildGameObject("Label", root.transform);
            Stretch(labelGo.AddComponent<RectTransform>());
            var labelTxt = AddText(labelGo, "仓库", 14, Color.white);
            SetSerializedRef(tab, "label", labelTxt);   // 兼容 IS_TMP 时改类型

            // SelectedIndicator (底部高亮横条，默认隐藏)
            var selGo = ChildGameObject("SelectedIndicator", root.transform);
            var selRt = selGo.AddComponent<RectTransform>();
            selRt.anchorMin = new Vector2(0f, 0f);
            selRt.anchorMax = new Vector2(1f, 0f);
            selRt.pivot = new Vector2(0.5f, 0f);
            selRt.sizeDelta = new Vector2(0f, 3f);
            selRt.anchoredPosition = Vector2.zero;
            selGo.AddComponent<Image>().color = Hex("66CCFF");
            selGo.SetActive(false);
            tab.selectedIndicator = selGo;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }
        
        /// <summary>
        /// 过滤按钮，适用于 InventoryView 的过滤按钮。
        /// </summary>
        /// <returns></returns>
        static void BuildFilterButtonPrefab()
        {
            string path = Pfb(KPfFilterButton);
            DeleteIfExists(path);

            var root = NewGameObject(KPfFilterButton);
            SetRectSize(root.AddComponent<RectTransform>(), 72, 28);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = Hex("292936");
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            SetButtonColors(btn, Hex("292936"), Hex("3A3A55"), Hex("1E1E2C"));

            var labelGo = ChildGameObject("Label", root.transform);
            Stretch(labelGo.AddComponent<RectTransform>());
            AddText(labelGo, "全部", 12, Color.white);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>
        /// 构建 PF_UiwFoldTab（通用折叠页签：可点击 Button，横向 [左侧图标 Image][右侧名称 Text]，<see cref="UiwFoldTab"/>）。
        /// 默认隐藏左侧图标（宿主按需 SetIcon 显示），供 <see cref="UiwCraftingGroupFilter"/> 等以实例化方式复用。
        /// </summary>
        static void BuildFoldTabPrefab()
        {
            string path = Pfb(KPfFoldTab);
            DeleteIfExists(path);

            // 根：可点击页签（背景图 + Button），横向布局 [图标][文本]
            var root = NewGameObject(KPfFoldTab);
            SetRectSize(root.AddComponent<RectTransform>(), 172f, 26f);
            SetLayoutElement(root, minH: 24, prefH: 24);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = Hex("292936");
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            SetButtonColors(btn, Hex("292936"), Hex("3A3A55"), Hex("1E1E2C"));
            SetHlg(root, new RectOffset(4, 4, 0, 0), 2f, TextAnchor.MiddleLeft, true, true, false, true);

            var foldTab = root.AddComponent<UiwFoldTab>();
            foldTab.button = btn;

            // 左侧折叠 / 自定义图标（默认隐藏；宿主通过 UiwFoldTab.SetIcon 显示）
            var iconGo = ChildGameObject("Icon", root.transform);
            iconGo.AddComponent<RectTransform>();
            SetLayoutElement(iconGo, minW: 16, prefW: 16, minH: 16, prefH: 16);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;   // 不拦截按钮点击
            iconImg.preserveAspect = true;
            foldTab.icon = iconImg;
            iconGo.SetActive(false);

            // 右侧名称文本
            var labelGo = ChildGameObject("Label", root.transform);
            labelGo.AddComponent<RectTransform>();
            SetLayoutElement(labelGo, flexW: 1);
            var labelTxt = AddText(labelGo, "全部", 12, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(foldTab, "label", labelTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }
        #endregion
    }
}

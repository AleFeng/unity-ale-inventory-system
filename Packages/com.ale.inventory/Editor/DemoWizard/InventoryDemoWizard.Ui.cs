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

    /// <summary>底层 UI 原语与通用构建辅助（节点 / 布局 / 图片 / 预制体保存等）。</summary>
    public static partial class InventoryDemoWizard
    {
        #region 工具方法

        // 创建不带 Transform 的 GameObject（自动拥有 Transform）
        static GameObject NewGameObject(string name)
        {
            var go = new GameObject(name);
            return go;
        }

        // 创建子节点（自动 AddComponent<RectTransform> 不在此处，外部显式 Add）
        static GameObject ChildGameObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        // 固定尺寸（锚点中心）
        static void SetRectSize(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
        }

        // 四边拉伸（零偏移）
        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 在 <paramref name="host"/> 下构建一套纵向虚拟滚动骨架并接线：
        /// <c>ScrollRect</c>（半透背景 Image）→ <c>Viewport</c>（遮罩，右留 20px 给滚动条）→ <c>Content</c>（顶部对齐），
        /// 外加常显的竖直 <c>Scrollbar Vertical</c>（Sliding Area + Handle）。返回 <see cref="ScrollRect"/>，
        /// Content 经 <paramref name="content"/> 传出；调用方随后把两者接到自己的虚拟滚动组件上即可。
        /// <para>五处纵向列表（道具顺序 / 网格、蓝图、技能网格 / 顺序）此前各写了一遍同一骨架。
        /// 滚动条与滑块颜色统一取全精度值（此前各处 0.38f / 0.38180846f 精度不一，现对齐为后者）。</para>
        /// </summary>
        /// <param name="host">滚动骨架的父节点（列表根）。</param>
        /// <param name="content">传出 Content 的 <see cref="RectTransform"/>（虚拟滚动组件的内容容器）。</param>
        /// <param name="decelerationRate">
        /// 惯性衰减率。道具 / 蓝图列表用 0.01（更「跟手」的低惯性）；技能列表沿用 Unity 默认 0.135f。
        /// </param>
        static ScrollRect MakeVerticalScrollView(GameObject host, out RectTransform content,
            float decelerationRate = 0.01f)
        {
            var srGo = ChildGameObject("ScrollRect", host.transform);
            Stretch(srGo.AddComponent<RectTransform>());
            srGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            var sr = srGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.elasticity = 0.06f;
            sr.inertia = true; sr.decelerationRate = decelerationRate; sr.scrollSensitivity = 40f;

            // Viewport（右留 20px 给滚动条）
            var vpGo = ChildGameObject("Viewport", srGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.pivot = new Vector2(0.5f, 0.5f);
            vpRt.anchoredPosition = new Vector2(-10f, 0f);
            vpRt.sizeDelta = new Vector2(-20f, 0f);
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content（顶部对齐；条目由虚拟滚动手动定位，不挂 LayoutGroup / SizeFitter）
            var contentGo = ChildGameObject("Content", vpGo.transform);
            content = contentGo.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;

            // Scrollbar Vertical
            var sbGo = ChildGameObject("Scrollbar Vertical", srGo.transform);
            var sbRt = sbGo.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f); sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 1f); sbRt.sizeDelta = new Vector2(16f, 0f);
            var sbImg = sbGo.AddComponent<Image>();
            sbImg.color  = new Color(0.38180846f, 0.38180846f, 0.49056602f, 1f);
            sbImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sbImg.type   = Image.Type.Sliced;
            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            // Sliding Area（无图） + Handle
            var saGo = ChildGameObject("Sliding Area", sbGo.transform);
            var saRt = saGo.AddComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero; saRt.anchorMax = Vector2.one;
            saRt.sizeDelta = new Vector2(-2f, -2f);
            var handleGo = ChildGameObject("Handle", saGo.transform);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero; handleRt.anchorMax = Vector2.zero;
            handleRt.sizeDelta = new Vector2(-4f, -4f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color  = new Color(0.101960786f, 0.101960786f, 0.16078432f, 1f);
            handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            handleImg.type   = Image.Type.Sliced;

            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect    = handleRt;

            sr.content  = content;
            sr.viewport = vpRt;
            sr.verticalScrollbar = scrollbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return sr;
        }

        // 创建固定高度的行节点（带 LayoutElement）
        static GameObject MakeRow(string name, Transform parent, float height, Color bgColor)
        {
            var go = ChildGameObject(name, parent);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, height);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height; le.flexibleWidth = 1f;
            if (bgColor.a > 0.001f)
            {
                var img = go.AddComponent<Image>();
                img.color = bgColor;
            }
            return go;
        }

        // Button 颜色状态
        static void SetButtonColors(Button btn, Color normal, Color highlight, Color pressed)
        {
            var colors = btn.colors;
            colors.normalColor      = normal;
            colors.highlightedColor = highlight;
            colors.pressedColor     = pressed;
            colors.selectedColor    = highlight;
            btn.colors = colors;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 创建带完整模板的标准 UI Dropdown。
        // Dropdown.captionText / itemText 必须是 UnityEngine.UI.Text（与 IS_TMP 无关），
        // 因为 UiwSortToolbar.sortDropdown 使用的是 UnityEngine.UI.Dropdown。
        // ─────────────────────────────────────────────────────────────────────
        static Dropdown MakeDropdown(string goName, Transform parent)
        {
            // ── 根节点 ────────────────────────────────────────────────────────
            var go = ChildGameObject(goName, parent);
            go.AddComponent<RectTransform>();
            var bgImg = go.AddComponent<Image>();
            bgImg.color = Hex("1C2533");
            var dropdown = go.AddComponent<Dropdown>();

            // Caption 文本（显示当前选中项）
            var captionGo = ChildGameObject("Label", go.transform);
            var captionRt = captionGo.AddComponent<RectTransform>();
            captionRt.anchorMin = Vector2.zero;
            captionRt.anchorMax = Vector2.one;
            captionRt.offsetMin = new Vector2(6f,  2f);
            captionRt.offsetMax = new Vector2(-6f, -2f);
            var captionTxt       = captionGo.AddComponent<Text>();
            captionTxt.fontSize  = 11;
            captionTxt.color     = new Color(0.85f, 0.85f, 0.92f);
            captionTxt.alignment = TextAnchor.MiddleLeft;
            dropdown.captionText       = captionTxt;

            // ── 下拉模板（弹出列表） ──────────────────────────────────────────
            // Unity 要求 Template 默认关闭；打开下拉时框架会自动激活它
            var templateGo = ChildGameObject("Template", go.transform);
            var templateRt = templateGo.AddComponent<RectTransform>();
            templateRt.anchorMin        = new Vector2(0f, 0f);
            templateRt.anchorMax        = new Vector2(1f, 0f);
            templateRt.pivot            = new Vector2(0.5f, 1f);
            templateRt.sizeDelta        = new Vector2(0f, 120f);
            templateRt.anchoredPosition = Vector2.zero;
            dropdown.template = templateRt; // 必须赋值，否则打开下拉时报 "template is not assigned"
            templateGo.AddComponent<Image>().color = Hex("1C2533");
            var templateSr = templateGo.AddComponent<ScrollRect>();
            templateSr.horizontal        = false;
            templateSr.vertical          = true;
            templateSr.scrollSensitivity = 20f;
            templateGo.SetActive(false);    // 必须初始为 inactive

            // Viewport
            var vpGo = ChildGameObject("Viewport", templateGo.transform);
            Stretch(vpGo.AddComponent<RectTransform>());
            vpGo.AddComponent<Image>().color     = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            templateSr.viewport = vpGo.GetComponent<RectTransform>();

            // Content
            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt  = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin        = new Vector2(0f, 1f);
            contentRt.anchorMax        = new Vector2(1f, 1f);
            contentRt.pivot            = new Vector2(0.5f, 1f);
            contentRt.sizeDelta        = new Vector2(0f, 28f);
            contentRt.anchoredPosition = Vector2.zero;
            templateSr.content = contentRt;

            // Item 模板（Toggle）
            var itemGo = ChildGameObject("Item", contentGo.transform);
            var itemRt  = itemGo.AddComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 26f);
            var itemToggle = itemGo.AddComponent<Toggle>();
            var itemBg     = itemGo.AddComponent<Image>();
            itemBg.color            = new Color(0f, 0f, 0f, 0f);
            itemToggle.targetGraphic = itemBg;

            // Item Background（选中高亮）
            var itemBgGo = ChildGameObject("Item Background", itemGo.transform);
            Stretch(itemBgGo.AddComponent<RectTransform>());
            var itemBgImg    = itemBgGo.AddComponent<Image>();
            itemBgImg.color  = Hex("2C3D50");
            itemToggle.graphic = itemBgImg;

            // Item Label
            var itemLblGo = ChildGameObject("Item Label", itemGo.transform);
            var itemLblRt  = itemLblGo.AddComponent<RectTransform>();
            itemLblRt.anchorMin = Vector2.zero;
            itemLblRt.anchorMax = Vector2.one;
            itemLblRt.offsetMin = new Vector2(6f, 0f);
            itemLblRt.offsetMax = Vector2.zero;
            var itemLbl       = itemLblGo.AddComponent<Text>();
            itemLbl.fontSize  = 11;
            itemLbl.color     = new Color(0.85f, 0.85f, 0.92f);
            itemLbl.alignment = TextAnchor.MiddleLeft;
            dropdown.itemText       = itemLbl;

            return dropdown;
        }

        // 通过 SerializedObject 设置 objectReference 字段（兼容 IS_TMP 类型差异）
        static void SetSerializedRef(Component comp, string fieldName, Object value)
        {
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 保存 Prefab 到指定路径并销毁临时 GameObject
        static void SavePrefab(GameObject root, string path)
        {
#if IS_TMP && IS_LOCALIZATION
            // 双宏下统一在保存前挂本地化字体事件（此前各 builder 尾部各写一遍；装备类曾漏挂，
            // 收口到此处后自动补齐）。AttachFontEvent 会扫描全部子节点建立字体绑定，故须在层级搭好后调用。
            AttachFontEvent(root);
#endif
            MovePrimaryUiwToTop(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[InventoryDemoWizard] 预制体已保存：" + path);
        }

        /// <summary>
        /// 把根节点上的主 Uiw 组件移到组件列表顶部（紧随 Transform/RectTransform），
        /// 这样在 Inspector 中能第一眼看到核心脚本。多数 builder 在添加完 Image/Button/LayoutGroup
        /// 等组件「之后」才 AddComponent&lt;UiwXxx&gt;()，故默认排在靠后位置，这里统一上移到顶部。
        /// </summary>
        static void MovePrimaryUiwToTop(GameObject root)
        {
            if (!root) return;
            var uiw = root.GetComponents<MonoBehaviour>()
                          .FirstOrDefault(c => c && c.GetType().Name.StartsWith("Uiw"));
            if (!uiw) return;
            // 反复上移直到无法再上移（Transform 始终占据首位，不会被越过）
            while (UnityEditorInternal.ComponentUtility.MoveComponentUp(uiw)) { }
        }

        // 十六进制颜色（"RRGGBB" 或 "RRGGBBAA"）
        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        // 确保文件夹存在（递归创建父链，支持多级子目录）
        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            int sep = path.LastIndexOf('/');
            if (sep < 0) return;
            string parent = path.Substring(0, sep);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(sep + 1));
        }

        // 若资产已存在则删除，保证后续用最新版本重新生成
        static void DeleteIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path))
                AssetDatabase.DeleteAsset(path);
        }

        /// <summary>
        /// 每个 builder 开头的固定两步：由预制体名解析出目标资产路径（<see cref="Pfb"/>），
        /// 并删除同名旧资产（<see cref="DeleteIfExists"/>，保证用最新版本重生成），返回该路径。
        /// </summary>
        static string BeginPrefab(string prefabName)
        {
            string path = BeginPrefab(prefabName);
            return path;
        }

        // ── 对齐辅助（精灵 / 依赖加载 / 布局组件复制）──────────────────────────

        /// <summary>从指定资产路径加载 Sprite（把 Demo 静态精灵赋给 Image）；缺失时告警，不再静默留空。</summary>
        static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (!sprite)
                Debug.LogWarning($"[InventoryDemoWizard] 未找到精灵资产：{assetPath}（对应 Image 将留空）。");
            return sprite;
        }

        /// <summary>加载预制体根节点上的指定组件（用于依赖引用）。</summary>
        static T LoadPrefabComp<T>(string path) where T : Component
            => AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<T>();

        /// <summary>添加并设置 LayoutElement。</summary>
        static void SetLayoutElement(GameObject go, float minW = -1, float minH = -1,
            float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1, bool ignore = false)
        {
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = minW; le.minHeight = minH;
            le.preferredWidth = prefW; le.preferredHeight = prefH;
            le.flexibleWidth = flexW; le.flexibleHeight = flexH;
            le.ignoreLayout = ignore;
        }

        /// <summary>添加并设置 ContentSizeFitter。</summary>
        static void SetContentSizeFitter(GameObject go,
            ContentSizeFitter.FitMode h, ContentSizeFitter.FitMode v)
        {
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = h; csf.verticalFit = v;
        }

        /// <summary>添加并设置 HorizontalLayoutGroup。</summary>
        static void SetHlg(GameObject go, RectOffset padding, float spacing,
            TextAnchor align, bool controlW, bool controlH, bool expandW, bool expandH)
        {
            var g = go.AddComponent<HorizontalLayoutGroup>();
            g.padding = padding; g.spacing = spacing; g.childAlignment = align;
            g.childControlWidth = controlW; g.childControlHeight = controlH;
            g.childForceExpandWidth = expandW; g.childForceExpandHeight = expandH;
            g.childScaleWidth = false; g.childScaleHeight = false;
        }
        #endregion
    }
}

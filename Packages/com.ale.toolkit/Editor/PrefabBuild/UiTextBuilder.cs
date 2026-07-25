using UnityEngine;
using UnityEngine.UI;
using Ale.Toolkit.Runtime.UI;
using static Ale.Toolkit.Editor.UiPrefabBuilder;

#if IS_TMP
using TMPro;
#endif

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 预制体文本构建：IS_TMP 感知的文本组件创建（TMP / 原生 Text 二选一）与带标签按钮。
    /// IS_TMP &amp;&amp; IS_LOCALIZATION 下为每个 TMP 文本挂 <see cref="LocalizedTextEvent"/> 以联动本地化。
    /// 默认字体来源由宿主经 <see cref="DefaultTmpFont"/> 注入（向导侧读其字体偏好）。
    /// </summary>
    public static class UiTextBuilder
    {
#if IS_TMP
        /// <summary>由宿主注入：新建 TMP 文本时套用的默认字体（返回 null 则用 TMP 内置默认字体）。</summary>
        public static System.Func<TMP_FontAsset> DefaultTmpFont;
#endif

        /// <summary>
        /// 向 <paramref name="go"/> 添加文本组件并设置基础属性。
        /// <list type="bullet">
        ///   <item>IS_TMP 宏启用时：使用 <c>TMPro.TextMeshProUGUI</c>，对齐经 <see cref="AnchorToTmp"/> 转换，
        ///         字体样式映射为 <c>TMPro.FontStyles</c>，并关闭自动换行。</item>
        ///   <item>未启用时：使用 <c>UnityEngine.UI.Text</c>，直接赋值原生属性。</item>
        /// </list>
        /// 返回 <c>Component</c>，可直接传给 <see cref="UiPrefabBuilder.SetSerializedRef"/>。
        /// </summary>
        public static Component AddText(
            GameObject go,
            string     text,
            int        fontSize,
            Color      color,
            TextAnchor anchor    = TextAnchor.MiddleCenter,
            FontStyle  fontStyle = FontStyle.Normal)
        {
#if IS_TMP
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text               = text;
            t.fontSize           = fontSize;
            t.color              = color;
            t.alignment          = AnchorToTmp(anchor);
            t.fontStyle          = fontStyle == FontStyle.Bold
                                       ? FontStyles.Bold
                                       : FontStyles.Normal;
#if UNITY_6000_0_OR_NEWER
            t.textWrappingMode = TextWrappingModes.NoWrap; // 不换行
#else
            t.enableWordWrapping = false; // 不换行（Unity 2022 及以下 TMP 接口）
#endif

            // 套用宿主注入的默认字体（留空则 TMP 使用内置默认字体）
            var defaultFont = DefaultTmpFont?.Invoke();
            if (defaultFont) t.font = defaultFont;

#if IS_LOCALIZATION
            // 为每个 TMP 文本节点添加 LocalizedTextEvent，
            // 开发者可在生成后为各节点配置本地化字符串引用。
            go.AddComponent<LocalizedTextEvent>();
#endif

            return t;
#else
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.color     = color;
            t.alignment = anchor;
            t.fontStyle = fontStyle;
            return t;
#endif
        }

#if IS_TMP
        /// <summary>
        /// 将 <see cref="TextAnchor"/> 九宫格枚举转换为等价的 <see cref="TMPro.TextAlignmentOptions"/> 值。
        /// </summary>
        static TextAlignmentOptions AnchorToTmp(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:  return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:   return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:   return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight:  return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft:    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:  return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:   return TextAlignmentOptions.BottomRight;
                default:                      return TextAlignmentOptions.Center;
            }
        }
#endif

        // ── 带标签按钮（文本子节点用 AddText，故置于此文本构建器）───────────────────

        /// <summary>
        /// 在 <paramref name="parent"/> 下建一个「背景 Image + Button + 居中文本子节点」的带标签按钮并返回 Button。
        /// 子节点名、字号、字形、对齐、高亮/按下色均为参数。
        /// </summary>
        public static Button MakeLabeledButton(string name, Transform parent, string label,
            Color normal, Color highlight, Color pressed,
            string textChildName, int fontSize, FontStyle fontStyle, TextAnchor align)
        {
            var go = ChildGameObject(name, parent);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = normal;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            SetButtonColors(btn, normal, highlight, pressed);

            var lblGo = ChildGameObject(textChildName, go.transform);
            Stretch(lblGo.AddComponent<RectTransform>());
            AddText(lblGo, label, fontSize, Color.white, align, fontStyle);
            return btn;
        }

        /// <summary>小按钮（文本子节点名 "Label"，16 号加粗居中）。三色高亮由调用方指定。</summary>
        public static Button MakeMiniButton(string name, Transform parent, string label,
            Color normal, Color highlight, Color pressed)
            => MakeLabeledButton(name, parent, label, normal, highlight, pressed,
                "Label", 16, FontStyle.Bold, TextAnchor.MiddleCenter);

        /// <summary>装备按钮（文本子节点名 "Text"，13 号常规居中）。高亮/按下色由底色派生。</summary>
        public static Button MakeEquipButton(string name, Transform parent, string label, Color bg)
            => MakeLabeledButton(name, parent, label, bg, bg * 1.2f, bg * 0.8f,
                "Text", 13, FontStyle.Normal, TextAnchor.MiddleCenter);
    }
}

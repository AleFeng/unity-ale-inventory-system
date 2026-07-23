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

    /// <summary>IS_TMP && IS_LOCALIZATION 下的字体事件挂载与 TMP 文本辅助。</summary>
    public static partial class InventoryDemoWizard
    {
        #region IS_TMP && IS_LOCALIZATION 字体事件辅助
        
#if IS_TMP && IS_LOCALIZATION
        /// <summary>
        /// 在 <paramref name="root"/> 上挂载 <see cref="InventoryTmpFontEvent"/>，
        /// 将 WelcomeWindow 中配置的本地化字体引用通过 JsonUtility roundtrip 写入组件，
        /// 然后扫描所有子节点以填充 texts / textEvents 列表并建立双向绑定。
        ///
        /// <para>必须在所有子节点（含 <see cref="InventoryTmpTextEvent"/>）都已添加后调用，
        /// 否则 <see cref="InventoryTmpFontEvent.RefreshComponents"/> 扫描结果不完整。</para>
        /// </summary>
        static void AttachFontEvent(GameObject root)
        {
            var fontEvent = root.AddComponent<InventoryTmpFontEvent>();

            // 将 WelcomeWindow 中配置的本地化字体引用写入 LocalizedAssetEvent 基类的
            // AssetReference（即 m_AssetReference），这才是基类实际用于驱动本地化的字段。
            // JsonUtility roundtrip 可正确复制 LocalizedReference 内已标 [SerializeField] 的
            // m_TableCollectionName / m_TableCollectionNameGuid / m_TableEntryReference 等字段，
            // 并触发 ISerializationCallbackReceiver.OnAfterDeserialize 完成内部状态同步。
            var localizedFont = InventoryWelcomeWindow.WizardLocalizedFont;
            if (localizedFont != null && !localizedFont.IsEmpty)
            {
                string json = JsonUtility.ToJson(localizedFont);
                JsonUtility.FromJsonOverwrite(json, fontEvent.AssetReference);
            }

            fontEvent.RefreshComponents();
        }
#endif

        // ═══════════════════════════════════════════════════════════════════════
        // IS_TMP 感知文本辅助
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 向 <paramref name="go"/> 添加文本组件并设置基础属性。
        /// <list type="bullet">
        ///   <item>IS_TMP 宏启用时：使用 <c>TMPro.TextMeshProUGUI</c>，
        ///         对齐通过 <see cref="AnchorToTmp"/> 转换，字体样式映射为
        ///         <c>TMPro.FontStyles</c>，并关闭自动换行（enableWordWrapping = false）。</item>
        ///   <item>未启用时：使用 <c>UnityEngine.UI.Text</c>，直接赋值原生属性。</item>
        /// </list>
        /// 返回 <c>Component</c>，可直接传给 <see cref="SetSerializedRef"/>。
        /// </summary>
        static Component AddText(
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

            // 应用 WelcomeWindow 中配置的默认字体（留空则 TMP 使用内置默认字体）
            var defaultFont = InventoryEditorPrefs.LoadWizardDefaultTmpFont();
            if (defaultFont) t.font = defaultFont;

#if IS_LOCALIZATION
            // 为每个 TMP 文本节点添加 InventoryTmpTextEvent，
            // 开发者可在生成后为各节点配置本地化字符串引用。
            go.AddComponent<InventoryTmpTextEvent>();
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
        /// 将 <see cref="TextAnchor"/> 九宫格枚举转换为等价的
        /// <see cref="TMPro.TextAlignmentOptions"/> 值。
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
        #endregion
    }
}

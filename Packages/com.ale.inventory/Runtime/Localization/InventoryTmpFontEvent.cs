// 本组件是 TMP 专用的字体本地化事件：基类 LocalizedAssetEvent<> 与两个 LocalizedAsset<> 子类
// 都直接依赖 TMPro + Unity Localization，无法在缺任一包时提供退化实现，故整文件受双宏门控
// （与 InventoryTmpTextEvent 的整文件 #if IS_LOCALIZATION 同一写法）。
// 引用方 InventoryTmpTextEvent 与 InventoryWelcomeWindow 对本文件类型的引用亦均在 IS_TMP 块内。
#if IS_TMP && IS_LOCALIZATION

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using TMPro;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 用于本地化 TextMeshPro 字体资源的 LocalizedAsset 封装类型。
    /// </summary>
    [Serializable]
    public class InventoryLocalizedTmpFont : LocalizedAsset<TMP_FontAsset> { }

    /// <summary>
    /// 用于本地化 TextMeshPro 字体材质预设的 LocalizedAsset 封装类型。
    /// </summary>
    [Serializable]
    public class InventoryLocalizedTmpFontMaterial : LocalizedAsset<Material> { }

    /// <summary>
    /// 用于本地化 TextMeshPro 字体资源的事件组件。
    /// 挂载在面板根节点，自动管理根节点及所有子节点的 TMP_Text 字体替换，
    /// 并与子节点的 <see cref="InventoryTmpTextEvent"/> 联动，避免语言切换时出现缺字。
    /// <para>参考 Fs.GameFramework.Common.LocalizationSystem.LocalizeTmpFontEvent，
    /// 针对 InventorySystem 简化（不支持嵌套 FontEvent 子树排除）。</para>
    /// </summary>
    [AddComponentMenu("InventorySystem/UI/Inventory TMP Font Event")]
    [DisallowMultipleComponent]
    public class InventoryTmpFontEvent
        : LocalizedAssetEvent<TMP_FontAsset, InventoryLocalizedTmpFont, UnityEvent<TMP_FontAsset>>
    {
        [SerializeField, Tooltip("受控的 TMP_Text 组件列表（自动扫描子节点）。")]
        private List<TMP_Text> texts;

        [SerializeField, Tooltip("受控的 InventoryTmpTextEvent 组件列表（自动扫描子节点）。")]
        private List<InventoryTmpTextEvent> textEvents;

        [Header("Font Material")]
        [SerializeField, Tooltip("本地化材质预设资源引用。为空则不进行材质本地化。")]
        private InventoryLocalizedTmpFontMaterial fontMaterial = new InventoryLocalizedTmpFontMaterial();

        private TMP_FontAsset _fontCache;
        private Material      _materialCache;
        private string        _localeCodeMark;

        // ── 生命周期 ────────────────────────────────────────────────────────────

        protected override void OnEnable()
        {
            GetComponents();
            if (!fontMaterial.IsEmpty)
                fontMaterial.AssetChanged += HandleMaterialChanged;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (!fontMaterial.IsEmpty)
                fontMaterial.AssetChanged -= HandleMaterialChanged;
            base.OnDisable();
        }

        private void OnDestroy()
        {
            if (textEvents == null) return;
            foreach (var te in textEvents)
                if (te) te.BindFontEvent(null);
        }

        private void Reset()    => RefreshComponents();
        private void OnValidate() => RefreshComponents();

        // ── 公开接口 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 刷新组件引用：重新扫描子节点，更新 texts / textEvents 列表并绑定/解绑关联。
        /// 可通过 Inspector 右键菜单手动触发，也可在编辑器脚本中调用（Wizard 在保存 Prefab 前调用）。
        /// </summary>
        [ContextMenu("Refresh Components")]
        public void RefreshComponents()
        {
            if (GetComponents())
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        // ── 字体更新 ─────────────────────────────────────────────────────────────

        protected override void UpdateAsset(TMP_FontAsset asset)
        {
            var localeCode   = LocalizationSettings.SelectedLocale?.Identifier.Code;
            bool fontChanged   = _fontCache != asset;
            bool localeChanged = _localeCodeMark != localeCode;

            if (!fontChanged && !localeChanged) return;

            if (fontChanged)
                base.UpdateAsset(asset);

            _fontCache      = asset;
            _localeCodeMark = localeCode;

            if (textEvents != null && textEvents.Count > 0)
            {
                // 静态文本（无 TextEvent 驱动）立即应用字体。
                RefreshFont(includeDriven: false);

                // 被驱动文本：已加载的先清空再换字体，然后触发文本事件刷新内容。
                foreach (var te in textEvents)
                {
                    if (!te || !te.enabled) continue;

                    if (te.IsLoadingDone() && te.Text)
                    {
                        te.Text.text = string.Empty;
                        ApplyFontTo(te.Text);
                    }

                    te.RefreshStringAndUpdateMark();
                }
            }
            else
            {
                RefreshFont(includeDriven: true);
            }

            RefreshMaterial();
        }

        private void HandleMaterialChanged(Material material)
        {
            _materialCache = material;
            RefreshMaterial();
        }

        // ── 辅助 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 对受控文本应用当前字体缓存。
        /// </summary>
        /// <param name="includeDriven">是否处理被 TextEvent 驱动的文本。</param>
        internal void RefreshFont(bool includeDriven = true)
        {
            if (texts == null || !_fontCache) return;
            foreach (var t in texts)
            {
                if (!t) continue;
                bool driven = textEvents != null && IsDriven(t);
                if (driven && !includeDriven) continue;
                t.font = _fontCache;
            }
        }

        /// <summary>仅对指定文本应用字体缓存，供 <see cref="InventoryTmpTextEvent"/> 调用。</summary>
        internal void ApplyFontTo(TMP_Text t)
        {
            if (!t || !_fontCache) return;
            t.font = _fontCache;
        }

        private void RefreshMaterial()
        {
            if (!_materialCache || texts == null) return;
            foreach (var t in texts)
                if (t) t.fontSharedMaterial = _materialCache;
        }

        private bool IsDriven(TMP_Text t)
        {
            foreach (var te in textEvents)
                if (te && te.Text == t) return true;
            return false;
        }

        /// <summary>扫描并更新 texts / textEvents 列表，绑定/解绑 TextEvent。</summary>
        private bool GetComponents()
        {
            bool dirty = false;

            // TMP_Text 列表
            var newTexts = new List<TMP_Text>(GetComponentsInChildren<TMP_Text>(true));
            if (!ListEquals(newTexts, texts))
            {
                texts = newTexts;
                dirty = true;
            }

            // InventoryTmpTextEvent 列表
            var newEvents = new List<InventoryTmpTextEvent>(
                GetComponentsInChildren<InventoryTmpTextEvent>(true));

            if (!ListEquals(newEvents, textEvents))
            {
                // 先解绑不再受控的旧事件
                if (textEvents != null)
                    foreach (var te in textEvents)
                        if (te && !newEvents.Contains(te)) te.BindFontEvent(null);

                textEvents = newEvents;
                dirty = true;
            }

            // 绑定（或重新绑定）当前受控的文本事件
            if (textEvents != null)
                foreach (var te in textEvents)
                    if (te) te.BindFontEvent(this);

            return dirty;
        }

        private static bool ListEquals<T>(List<T> a, List<T> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count)    return false;
            for (int i = 0; i < a.Count; i++)
                if (!EqualityComparer<T>.Default.Equals(a[i], b[i])) return false;
            return true;
        }
    }
}

#endif

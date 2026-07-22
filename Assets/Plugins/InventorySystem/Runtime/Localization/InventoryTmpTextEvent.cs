#if IS_LOCALIZATION

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

#if IS_TMP
using TMPro;
#endif

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 用于本地化 TextMeshPro 文本内容的事件组件。
    /// 支持在语言环境变化时被动更新文本内容，配合 <see cref="InventoryTmpFontEvent"/>
    /// 实现先换字体再填充文本的顺序，避免缺字和 Warning。
    ///
    /// <para>参考 Fs.GameFramework.Common.LocalizationSystem.LocalizeTmpTextEvent，
    /// 针对 InventorySystem 宏（IS_TMP / IS_LOCALIZATION）改写。</para>
    /// </summary>
    [AddComponentMenu("InventorySystem/UI/Inventory TMP Text Event")]
    [DisallowMultipleComponent]
    public class InventoryTmpTextEvent : LocalizeStringEvent
    {
#if IS_TMP
        [SerializeField, Tooltip("驱动的 TextMeshPro 文本组件。")]
        private TMP_Text _text;

        [SerializeField,
         Tooltip("驱动此文本的 InventoryTmpFontEvent 组件引用。由字体事件自动反向绑定。")]
        private InventoryTmpFontEvent _fontEvent;

        /// <summary>此组件驱动的 TMP_Text，供 InventoryTmpFontEvent 识别被驱动文本。</summary>
        internal TMP_Text Text => _text;

        /// <summary>
        /// 由 <see cref="InventoryTmpFontEvent"/> 调用：绑定（或解绑）驱动此文本的字体事件，
        /// 同步设置被动更新标志。
        /// </summary>
        internal void BindFontEvent(InventoryTmpFontEvent fe)
        {
            bool passive = fe != null;
            if (_fontEvent == fe && passiveUpdate == passive) return;

            _fontEvent   = fe;
            passiveUpdate = passive;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
#endif

        [SerializeField, HideInInspector,
         Tooltip("是否在语言切换时被动等待字体事件先行更新，再刷新文本，避免缺字。")]
        internal bool passiveUpdate;

        // 记录上次更新时的语言代码，用于判断是否需要刷新。
        private string _localeCodeMark;

        private bool LocaleCodeMarkChange =>
            _localeCodeMark != LocalizationSettings.SelectedLocale?.Identifier.Code;

        // ── 生命周期 ────────────────────────────────────────────────────────────

        private void Awake() { GetComponents(); }

        protected override void OnEnable()
        {
            base.OnEnable();

#if IS_TMP
            if (_text && (string.IsNullOrEmpty(_text.text) || LocaleCodeMarkChange))
            {
                UpdateStringSelf(string.Empty);
                RefreshStringAndUpdateMark();
            }
#endif
        }

        private void Reset()      => GetComponents();
        private void OnValidate() => GetComponents();

        /// <summary>刷新组件引用（右键菜单手动触发或编辑器脚本调用）。</summary>
        [ContextMenu("刷新组件引用 / Refresh Components")]
        public void RefreshComponents()
        {
            if (GetComponents())
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        // ── 文本更新 ─────────────────────────────────────────────────────────────

        protected override void UpdateString(string value)
        {
            // 被动模式且 locale 尚未更新（字体事件还未触发）时拦截，
            // 等字体事件调用 RefreshStringAndUpdateMark 后再次刷新。
            if (passiveUpdate && LocaleCodeMarkChange) return;

            UpdateStringSelf(value);
            base.UpdateString(value);
        }

        private void UpdateStringSelf(string value)
        {
#if IS_TMP
            // 先确保本文本的字体已就绪（仅作用于自身，不影响兄弟文本）。
            _fontEvent?.ApplyFontTo(_text);
            if (_text) _text.text = value;
#endif
        }

        // ── 内部接口（供 InventoryTmpFontEvent 调用）────────────────────────────

        /// <summary>更新语言标记并触发文本刷新（由字体事件调用）。</summary>
        internal void RefreshStringAndUpdateMark()
        {
            _localeCodeMark = LocalizationSettings.SelectedLocale?.Identifier.Code;
            RefreshString();
        }

        /// <summary>当前本地化加载操作是否已完成。</summary>
        internal bool IsLoadingDone() => StringReference.CurrentLoadingOperationHandle.IsDone;

        // ── 公开接口 ─────────────────────────────────────────────────────────────

        /// <summary>运行时设置本地化表和条目引用（供 UI 组件在填充数据时调用）。</summary>
        public void SetReference(TableReference table, TableEntryReference entry,
                                  IList<object> args = null)
        {
            StringReference.Arguments = args;
            StringReference.SetReference(table, entry);
        }

        // ── 辅助 ─────────────────────────────────────────────────────────────────

        private bool GetComponents()
        {
#if IS_TMP
            bool dirty = false;

            if (!_text)
            {
                _text = GetComponentInChildren<TMP_Text>();
                if (_text) dirty = true;
            }

            // _fontEvent 正常由 InventoryTmpFontEvent 通过 BindFontEvent 反向绑定；
            // 此处向父节点查找作为兜底，以覆盖字体事件挂载在父节点的情形。
            if (!_fontEvent)
            {
                _fontEvent = GetComponentInParent<InventoryTmpFontEvent>();
                if (_fontEvent)
                {
                    passiveUpdate = true;
                    dirty = true;
                }
            }

            return dirty;
#else
            return false;
#endif
        }
    }
}

#endif

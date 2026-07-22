#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 技能信息悬停弹窗（实现 <see cref="ISkillTooltip"/>）。渲染技能固定字段（图标 / 名称 / 描述）、「位阶」枚举项名称，
    /// 以及组件上配置的自定义属性字段 Key 列表对应的值。复用 <see cref="UiwItemTooltip"/> 的淡入淡出 / 光标定位 / 队列思路。
    ///
    /// <para>预制体配置在 <see cref="InventoryRuntimeManager"/> 的 <c>skillTooltipPrefab</c> 上，运行时由其全局实例化一次，
    /// 并经 <see cref="InventoryRuntimeManager.ShowSkillTooltip"/> / <see cref="InventoryRuntimeManager.HideSkillTooltip"/> 统一对外调用。
    /// 本物体应常驻激活（可见性由 <see cref="canvasGroup"/> 的 alpha 控制，而非 SetActive）。</para>
    /// </summary>
    public class UiwSkillTooltip : MonoBehaviour, ISkillTooltip
    {
        [Header("内容")]
        [Tooltip("技能图标。")]
        public Image iconImage;
        [Tooltip("技能名称文本。")]
        public InventoryText nameText;
        [Tooltip("技能描述文本。")]
        public InventoryText descText;
        [Tooltip("「位阶」枚举项名称文本（可选）。")]
        public InventoryText rankNameText;

        [Header("位阶")]
        [Tooltip("技能上「位阶」枚举属性字段 ID。")]
        public string rankAttrId = "位阶";
        [Tooltip("位阶枚举项上「名称」属性字段 ID（String / LocalizedString）。")]
        public string rankNameAttrId = "名称";

        [Header("自定义属性字段")]
        [Tooltip("要显示的自定义属性字段 Key 列表（每个非空值生成一行）。")]
        public string[] customFieldKeys;
        [Tooltip("自定义字段行父容器。")]
        public Transform customFieldContainer;
        [Tooltip("自定义字段行预制体（单个 InventoryText）。")]
        public InventoryText customFieldLinePrefab;

        [Header("定位与淡入淡出")]
        [Tooltip("弹窗根 RectTransform（跟随光标定位）；为空则用本物体的 RectTransform。")]
        public RectTransform panel;
        [Tooltip("控制 alpha 与射线阻挡的 CanvasGroup；为空则取本物体上的。自动设为不阻挡射线。")]
        public CanvasGroup canvasGroup;
        [Tooltip("相对光标的像素偏移。")]
        public Vector2 cursorOffset = new Vector2(16f, -16f);
        [Tooltip("淡入 / 淡出时长（秒）。")]
        [Min(0f)] public float fadeDuration = 0.12f;

        private enum State { Idle, FadingIn, Visible, FadingOut }

        private RectTransform _rt;
        private State         _state = State.Idle;
        private Coroutine     _fadeRoutine;

        // 淡出未结束时收到的新 Show 请求：记录下来，等淡出结束后再定位淡入（不打断淡出）。
        private bool    _hasPending;
        private Skill   _pendingSkill;
        private Vector2 _pendingPos;

        private readonly List<GameObject> _customLines = new List<GameObject>();

        private void Awake()
        {
            _rt = panel ? panel : transform as RectTransform;
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup) canvasGroup.blocksRaycasts = false; // 不遮挡下方条目的悬停判定
            ApplyHidden();
        }

        private void OnDisable()
        {
            _fadeRoutine = null;
            _hasPending  = false;
            ApplyHidden();
        }

        /// <summary>在光标处（屏幕坐标）显示指定技能的详情并淡入。skill 为空等同 <see cref="Hide"/>。</summary>
        public void Show(Skill skill, Vector2 screenPos)
        {
            if (skill == null) { Hide(); return; }

            // 正在淡出：不打断，记录为待显示请求；等淡出结束后再定位淡入。
            if (_state == State.FadingOut)
            {
                _hasPending   = true;
                _pendingSkill = skill;
                _pendingPos   = screenPos;
                return;
            }

            _hasPending = false;
            SetContent(skill);
            SetPosition(screenPos);
            BeginFade(1f, State.FadingIn, State.Visible, null);
        }

        /// <summary>开始原位淡出（位置保持不变）。</summary>
        public void Hide()
        {
            if (_state == State.Idle || _state == State.FadingOut) { _hasPending = false; return; }
            BeginFade(0f, State.FadingOut, State.Idle, OnFadeOutComplete);
        }

        private void OnFadeOutComplete()
        {
            ClearContent();

            if (!_hasPending) return;
            _hasPending = false;
            Show(_pendingSkill, _pendingPos);
        }

        // ── 内容 ──────────────────────────────────────────────────────────────────

        // 图标异步加载世代号：改选 / 清空时自增，回调据此丢弃过期结果。
        private int _iconGen;

        private void SetContent(Skill skill)
        {
            if (iconImage)
            {
                var owner = iconImage.gameObject;
                InventoryAssets.Release(owner);
                int gen = ++_iconGen;
                InventoryAssets.Bind<Sprite>(skill.icon, skill.iconAddress, owner, s =>
                {
                    if (gen != _iconGen || !iconImage) return;
                    iconImage.sprite  = s;
                    iconImage.enabled = s;
                });
            }
            if (nameText)  nameText.text = UiwSkillText.ResolveName(skill, true);
            if (descText)  descText.text = UiwSkillText.ResolveDescription(skill);

            if (rankNameText)
            {
                var enumItem = SkillRankUtil.Resolve(skill, rankAttrId);
                string rankName = enumItem != null && !string.IsNullOrEmpty(rankNameAttrId)
                    ? enumItem.GetAttributeValue<string>(rankNameAttrId) : string.Empty;
                rankNameText.text = rankName ?? string.Empty;
            }

            BuildCustomFields(skill);
        }

        private void BuildCustomFields(Skill skill)
        {
            ClearCustomLines();
            if (!customFieldContainer || !customFieldLinePrefab || customFieldKeys == null) return;
            foreach (var key in customFieldKeys)
            {
                string val = UiwSkillText.ResolveCustomField(skill, key);
                if (string.IsNullOrEmpty(val)) continue;
                var line = Instantiate(customFieldLinePrefab, customFieldContainer);
                line.text = val;
                _customLines.Add(line.gameObject);
            }
        }

        private void ClearContent()
        {
            if (iconImage)
            {
                InventoryAssets.Release(iconImage.gameObject);
                _iconGen++;
                iconImage.sprite = null; iconImage.enabled = false;
            }
            if (nameText)     nameText.text = string.Empty;
            if (descText)     descText.text = string.Empty;
            if (rankNameText) rankNameText.text = string.Empty;
            ClearCustomLines();
        }

        private void ClearCustomLines()
        {
            foreach (var go in _customLines) if (go) Destroy(go);
            _customLines.Clear();
        }

        // ── 定位（统一走 UIUtility，按 Canvas RenderMode 换算）──────────────

        /// <summary>光标处定位并夹取回屏幕内（见 <see cref="UIUtility"/>）。</summary>
        private void SetPosition(Vector2 screenPos)
            => UIUtility.PositionAtCursor(_rt, screenPos, cursorOffset);

        // ── 淡入淡出（复用 UiwItemTooltip 的状态机）────────────────────────────────

        private void BeginFade(float targetAlpha, State during, State settled, Action onDone)
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            _state = during;

            if (!canvasGroup || fadeDuration <= 0f || !isActiveAndEnabled)
            {
                if (canvasGroup) canvasGroup.alpha = targetAlpha;
                _state = settled;
                onDone?.Invoke();
                return;
            }
            _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, settled, onDone));
        }

        private IEnumerator FadeRoutine(float targetAlpha, State settled, Action onDone)
        {
            float start   = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed          += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
            _fadeRoutine      = null;
            _state            = settled;
            onDone?.Invoke();
        }

        private void ApplyHidden()
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            _state = State.Idle;
            if (canvasGroup) canvasGroup.alpha = 0f;
            ClearContent();
        }
    }
}

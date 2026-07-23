using System;
using System.Collections;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 悬停弹窗基类。封装「光标定位 + 淡入淡出状态机 + 淡出期间的待显示队列」，
    /// 由 <see cref="UiwItemTooltip"/>（道具）与 <see cref="UiwSkillTooltip"/>（技能）共用 ——
    /// 两者此前是同一套状态机的两份逐字拷贝。
    ///
    /// <para><b>交互：</b>悬停某条目 → 定位到光标处并淡入；移开 → 在原位置淡出（位置不变）。
    /// 若淡出尚未结束又悬停到另一条目，则先等淡出结束，再重新定位并淡入
    /// （不打断淡出，避免位置突变 / 内容闪烁）。</para>
    ///
    /// <para><b>子类契约：</b>实现 <see cref="ApplyContent"/>（写入弹窗内容）与
    /// <see cref="ClearContent"/>（完全隐藏后清空内容、释放图标句柄），
    /// 并对外暴露各自接口形状的 <c>Show</c>（内部转调 <see cref="ShowTooltip"/>）。</para>
    ///
    /// <para><b>本物体应常驻激活</b>：可见性由 <see cref="canvasGroup"/> 的 alpha 控制，而非 SetActive。</para>
    /// </summary>
    /// <typeparam name="TPayload">弹窗内容的载荷类型（道具为 <see cref="RuntimeItemSlot"/>，技能为 <see cref="Skill"/>）。</typeparam>
    public abstract class UiwTooltipBase<TPayload> : MonoBehaviour where TPayload : class
    {
        #region 定位与淡入淡出 配置

        [Header("定位与淡入淡出")]
        [Tooltip("弹窗根 RectTransform（跟随光标定位）；为空则使用本物体的 RectTransform。")]
        public RectTransform panel;
        [Tooltip("控制淡入淡出（alpha）与射线阻挡的 CanvasGroup；为空则尝试取本物体上的。自动设为不阻挡射线。")]
        public CanvasGroup canvasGroup;
        [Tooltip("相对光标的像素偏移。")]
        public Vector2 cursorOffset = new Vector2(16f, -16f);
        [Tooltip("淡入 / 淡出时长（秒）。")]
        [Min(0f)] public float fadeDuration = 0.12f;

        #endregion

        #region 状态

        /// <summary>弹窗的淡入淡出状态。</summary>
        protected enum EFadeState { Idle, FadingIn, Visible, FadingOut }

        private RectTransform _rt;
        private EFadeState    _state = EFadeState.Idle;
        private Coroutine     _fadeRoutine;

        // 淡出未结束时收到的新 Show 请求：记录下来，等淡出结束后再定位淡入（不打断淡出）。
        private bool     _hasPending;
        private TPayload _pendingPayload;
        private Vector2  _pendingPos;

        /// <summary>当前淡入淡出状态（供子类判断可见性）。</summary>
        protected EFadeState FadeState => _state;

        #endregion

        #region 子类契约

        /// <summary>把载荷写入弹窗内容。载荷保证非 null（且通过了 <see cref="IsEmpty"/> 判定）。</summary>
        protected abstract void ApplyContent(TPayload payload);

        /// <summary>完全隐藏后清空弹窗内容（释放图标句柄 / 作废未完成的异步回调）。</summary>
        protected abstract void ClearContent();

        /// <summary>判断载荷是否为「空」（空载荷等同 <see cref="Hide"/>）。默认仅判 null，子类可加码。</summary>
        protected virtual bool IsEmpty(TPayload payload) => payload == null;

        #endregion

        #region 生命周期

        protected virtual void Awake()
        {
            _rt = panel ? panel : transform as RectTransform;
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup) canvasGroup.blocksRaycasts = false; // 不遮挡下方条目的悬停判定
            ApplyHidden();
        }

        // 本物体或其所在 Canvas 被停用时，协程已被 Unity 终止，复位为隐藏态。
        protected virtual void OnDisable()
        {
            _fadeRoutine = null;
            _hasPending  = false;
            ApplyHidden();
        }

        #endregion

        #region 显示与隐藏

        /// <summary>
        /// 在光标处（屏幕坐标）显示载荷并淡入。空载荷（见 <see cref="IsEmpty"/>）等同 <see cref="Hide"/>。
        /// 供子类的对外 <c>Show</c> 方法转调。
        /// </summary>
        protected void ShowTooltip(TPayload payload, Vector2 screenPos)
        {
            if (IsEmpty(payload)) { Hide(); return; }

            // 正在淡出：不打断，记录为待显示请求；等淡出结束后再定位淡入。
            if (_state == EFadeState.FadingOut)
            {
                _hasPending     = true;
                _pendingPayload = payload;
                _pendingPos     = screenPos;
                return;
            }

            // Idle / FadingIn / Visible：立即（重新）定位并淡入。
            _hasPending = false;
            ApplyContent(payload);
            UIUtility.PositionAtCursor(_rt, screenPos, cursorOffset);   // 夹取回屏幕内，按 Canvas RenderMode 换算
            BeginFade(1f, EFadeState.FadingIn, EFadeState.Visible, null);
        }

        /// <summary>开始原位淡出（位置保持不变）。</summary>
        public void Hide()
        {
            // 已隐藏 / 正在淡出：仅取消待显示请求。
            if (_state == EFadeState.Idle || _state == EFadeState.FadingOut) { _hasPending = false; return; }

            // FadingIn / Visible：从当前透明度原位淡出。
            BeginFade(0f, EFadeState.FadingOut, EFadeState.Idle, OnFadeOutComplete);
        }

        private void OnFadeOutComplete()
        {
            ClearContent();   // 完全隐藏后清空内容，释放图标引用

            // 淡出期间若已悬停到新条目：此刻状态已回到 Idle，再定位并淡入。
            if (!_hasPending) return;
            _hasPending = false;
            ShowTooltip(_pendingPayload, _pendingPos);
        }

        #endregion

        #region 淡入淡出

        /// <summary>启动一次淡入 / 淡出：先停掉进行中的动画，再朝 <paramref name="targetAlpha"/> 过渡。</summary>
        private void BeginFade(float targetAlpha, EFadeState during, EFadeState settled, Action onDone)
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            _state = during;

            // 无 CanvasGroup / 时长 ≤ 0 / 物体未激活（无法跑协程）：直接到位。
            if (!canvasGroup || fadeDuration <= 0f || !isActiveAndEnabled)
            {
                if (canvasGroup) canvasGroup.alpha = targetAlpha;
                _state = settled;
                onDone?.Invoke();
                return;
            }
            _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, settled, onDone));
        }

        private IEnumerator FadeRoutine(float targetAlpha, EFadeState settled, Action onDone)
        {
            float start   = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed          += Time.unscaledDeltaTime; // 不受 timeScale 影响（暂停时悬停仍可用）
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
            _fadeRoutine      = null;
            _state            = settled;     // 先落定状态，再回调（onDone 内可能再次 Show）
            onDone?.Invoke();
        }

        /// <summary>立即置为隐藏态（无动画）：停动画、alpha=0、清空内容。</summary>
        private void ApplyHidden()
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            _state = EFadeState.Idle;
            if (canvasGroup) canvasGroup.alpha = 0f;
            ClearContent();
        }

        #endregion
    }
}

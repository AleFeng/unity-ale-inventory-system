using System;
using System.Collections;
using UnityEngine;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 道具信息悬停弹窗（场景全局单例）。复用 <see cref="UiwInventoryItemDetail"/> 渲染道具详情，全局共用一个实例，
    /// 避免大量弹窗预制体常驻内存影响性能。
    ///
    /// <para>预制体配置在 <see cref="InventoryRuntimeManager"/> 上，运行时由其全局实例化一次并经
    /// <see cref="InventoryRuntimeManager.ShowItemTooltip"/> / <see cref="InventoryRuntimeManager.HideItemTooltip"/>
    /// 统一对外调用（实现 <see cref="IItemTooltip"/>）；本组件 Awake 时也注册 <see cref="Instance"/> 供直接访问。</para>
    ///
    /// <para>交互：悬停某道具 → 定位到光标处并淡入；移开 → 在原位置淡出（位置不变）。
    /// 若淡出尚未结束又悬停到另一道具，则先等淡出结束，再重新定位并淡入
    /// （不打断淡出，避免位置突变 / 内容闪烁）。</para>
    ///
    /// <para>本物体应常驻激活（可见性由 <see cref="canvasGroup"/> 的 alpha 控制，而非 SetActive）。</para>
    /// </summary>
    public class UiwItemTooltip : MonoBehaviour, IItemTooltip
    {
        /// <summary>场景全局单例（最近一次 Awake 的实例）。</summary>
        public static UiwItemTooltip Instance { get; private set; }

        [Header("子组件")]
        [Tooltip("渲染道具详情的组件（复用列表格子 UiwInventoryItemDetail）。")]
        public UiwInventoryItemDetail detail;
        [Tooltip("弹窗根 RectTransform（跟随光标定位）；为空则使用本物体的 RectTransform。")]
        public RectTransform panel;
        [Tooltip("控制淡入淡出（alpha）与射线阻挡的 CanvasGroup；为空则尝试取本物体上的。自动设为不阻挡射线。")]
        public CanvasGroup canvasGroup;
        [Tooltip("相对光标的像素偏移。")]
        public Vector2 cursorOffset = new Vector2(16f, -16f);

        [Header("淡入淡出")]
        [Tooltip("淡入 / 淡出时长（秒）。")]
        [Min(0f)] public float fadeDuration = 0.12f;

        private enum State { Idle, FadingIn, Visible, FadingOut }

        private RectTransform _rt;
        private State         _state = State.Idle;
        private Coroutine     _fadeRoutine;

        // 淡出未结束时收到的新 Show 请求：记录下来，等淡出结束后再定位淡入（不打断淡出）。
        private bool    _hasPending;
        private string  _pendingItemId;
        private int     _pendingCount;
        private Vector2 _pendingPos;

        private void Awake()
        {
            Instance = this;
            _rt = panel ? panel : transform as RectTransform;
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup) canvasGroup.blocksRaycasts = false; // 不遮挡下方道具的悬停判定
            ApplyHidden();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 本物体或其所在 Canvas 被停用时，协程已被 Unity 终止，复位为隐藏态。
        private void OnDisable()
        {
            _fadeRoutine = null;
            _hasPending  = false;
            ApplyHidden();
        }

        /// <summary>在光标处（屏幕坐标）显示指定道具的详情弹窗并淡入。count 为持有数量（显示在数量文本）。itemId 为空等同 <see cref="Hide"/>。</summary>
        public void Show(string itemId, int count, Vector2 screenPos)
        {
            if (string.IsNullOrEmpty(itemId)) { Hide(); return; }

            // 正在淡出：不打断，记录为待显示请求；等淡出结束后再定位淡入。
            if (_state == State.FadingOut)
            {
                _hasPending    = true;
                _pendingItemId = itemId;
                _pendingCount  = count;
                _pendingPos    = screenPos;
                return;
            }

            // Idle / FadingIn / Visible：立即（重新）定位并淡入。
            _hasPending = false;
            if (detail) detail.SetSlot(null, new RuntimeItemSlot(null, itemId, count));
            SetPosition(screenPos);
            BeginFade(1f, State.FadingIn, State.Visible, null);
        }

        /// <summary>开始原位淡出（位置保持不变）。</summary>
        public void Hide()
        {
            // 已隐藏 / 正在淡出：仅取消待显示请求。
            if (_state == State.Idle || _state == State.FadingOut) { _hasPending = false; return; }

            // FadingIn / Visible：从当前透明度原位淡出。
            BeginFade(0f, State.FadingOut, State.Idle, OnFadeOutComplete);
        }

        private void OnFadeOutComplete()
        {
            if (detail) detail.SetEmpty();   // 完全隐藏后清空内容，释放图标引用

            // 淡出期间若已悬停到新道具：此刻状态已回到 Idle，再定位并淡入。
            if (!_hasPending) return;
            _hasPending = false;
            Show(_pendingItemId, _pendingCount, _pendingPos);
        }

        /// <summary>光标处定位并夹取回屏幕内（按所在 Canvas 的 RenderMode 换算，见 <see cref="UIUtility"/>）。</summary>
        private void SetPosition(Vector2 screenPos)
            => UIUtility.PositionAtCursor(_rt, screenPos, cursorOffset);

        /// <summary>启动一次淡入 / 淡出：先停掉进行中的动画，再朝 <paramref name="targetAlpha"/> 过渡。</summary>
        private void BeginFade(float targetAlpha, State during, State settled, Action onDone)
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

        private IEnumerator FadeRoutine(float targetAlpha, State settled, Action onDone)
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
            _state = State.Idle;
            if (canvasGroup) canvasGroup.alpha = 0f;
            if (detail) detail.SetEmpty();
        }
    }
}

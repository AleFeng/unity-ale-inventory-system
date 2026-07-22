#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 通用「数字计数器」组件（MonoBehaviour）。一对 +/- 按钮 + 数值显示（可选输入框），在 [<see cref="Min"/>,
    /// <see cref="Max"/>] 范围内调整一个整数值，支持「按下即步进 + 长按连发（多阶段加速）」。值变化时通过
    /// <see cref="OnValueChanged"/> 通知宿主。可在商店、制作等多个界面复用（替代各自内置的次数调整逻辑）。
    ///
    /// <para>宿主用法：<see cref="SetRange"/> / <see cref="Configure"/> 设定范围，<see cref="SetValue"/> 设值，
    /// <see cref="SetInteractable"/> 开关可调整状态，订阅 <see cref="OnValueChanged"/> 获取用户调整结果。</para>
    /// </summary>
    public class UiwNumberCounter : MonoBehaviour
    {
        [Header("按钮 / 显示")]
        [Tooltip("减少按钮。")]
        public Button minusButton;
        [Tooltip("增加按钮。")]
        public Button plusButton;
        [Tooltip("当前值文本。可空。")]
        public InventoryText valueText;
        [Tooltip("当前值输入框（允许键入）。可空。")]
        public InputField valueInput;

        [Header("步进")]
        [Tooltip("每次点按 +/- 的步进量（≥1）。")]
        [Min(1)] public int step = 1;

        [Header("长按连发")]
        [Tooltip("长按 +/- 快速调整的阶段配置：按住时长达到某条「秒数阈值」后，连发速度切换为该条的「每秒次数」。\n" +
                 "请按秒数阈值从小到大配置；第一条阈值即「按住多久后开始连发」。列表留空则只能逐次点按。")]
        public List<LongPressStage> longPressStages = new List<LongPressStage>
        {
            new LongPressStage(0.2f, 5f),
            new LongPressStage(2f, 10f),
            new LongPressStage(5f, 30f),
        };

        /// <summary>值变化事件（用户操作，或 <see cref="SetValue"/> 且 notify=true 时触发）。参数为新值。</summary>
        public event Action<int> OnValueChanged;

        private int  _value;
        private int  _min;
        private int  _max;
        private bool _interactable = true;

        /// <summary>当前值。</summary>
        public int Value => _value;
        /// <summary>当前下限。</summary>
        public int Min => _min;
        /// <summary>当前上限。</summary>
        public int Max => _max;

        private void Awake()
        {
            // +/- 改用「按下即触发 + 长按连发」，不走 onClick（避免按下与点击重复计数）
            SetupHoldButton(minusButton, -1);
            SetupHoldButton(plusButton, +1);
            if (valueInput) valueInput.onEndEdit.AddListener(OnInputEndEdit);
        }

        private void OnDisable() => EndHold();

        #region 对外接口

        /// <summary>设置取值范围并把当前值钳制进范围（不触发事件，仅刷新显示）。</summary>
        public void SetRange(int min, int max)
        {
            _min   = min;
            _max   = Mathf.Max(min, max);
            _value = Mathf.Clamp(_value, _min, _max);
            RefreshUI();
        }

        /// <summary>设置当前值（钳制到 [Min,Max]）。值实际变化且 notify=true 时触发 <see cref="OnValueChanged"/>。</summary>
        public void SetValue(int value, bool notify = true)
        {
            int  clamped = Mathf.Clamp(value, _min, _max);
            bool changed = clamped != _value;
            _value = clamped;
            RefreshUI();
            if (changed && notify) OnValueChanged?.Invoke(_value);
        }

        /// <summary>一次性设定范围与值（钳制；默认不触发事件），便于宿主配置。</summary>
        public void Configure(int min, int max, int value, bool notify = false)
        {
            _min = min;
            _max = Mathf.Max(min, max);
            SetValue(value, notify);
        }

        /// <summary>启用 / 禁用调整（不可交互时 +/- 与输入框均禁用）。</summary>
        public void SetInteractable(bool on)
        {
            _interactable = on;
            RefreshUI();
        }

        /// <summary>按当前值 / 范围 / 可交互状态刷新显示与按钮可用性。</summary>
        public void RefreshUI()
        {
            if (valueText)  valueText.text = _value.ToString();
            // 输入框聚焦（用户正在键入）时不覆盖其文本，避免打断输入
            if (valueInput && !valueInput.isFocused) valueInput.text = _value.ToString();

            if (minusButton) minusButton.interactable = _interactable && _value > _min;
            if (plusButton)  plusButton.interactable  = _interactable && _value < _max;
            if (valueInput)  valueInput.interactable   = _interactable;
        }

        #endregion

        private void OnInputEndEdit(string s)
        {
            if (!_interactable) { RefreshUI(); return; }
            int v = _value;
            int.TryParse(s, out v);
            SetValue(v);   // 用户键入：通知宿主
        }

        #region 长按连发

        private int       _holdDir;       // 当前长按方向（+1 / -1），0 = 未长按
        private float     _holdStartTime; // 长按开始时刻（unscaled 秒）
        private float     _repeatAccum;   // 连发累积器（≥1 触发一次）
        private Coroutine _holdRoutine;   // 连发协程句柄

        /// <summary>为 +/- 按钮安装「按下即触发 + 长按连发」转发器。</summary>
        private void SetupHoldButton(Button button, int dir)
        {
            if (!button) return;
            var relay = button.gameObject.GetComponent<PointerHoldRelay>();
            if (!relay) relay = button.gameObject.AddComponent<PointerHoldRelay>();
            relay.OnPress   = () => BeginHold(dir);
            relay.OnRelease = EndHold;
        }

        /// <summary>按下：立即步进一次；若仍可继续步进则进入长按连发。</summary>
        private void BeginHold(int dir)
        {
            if (!_interactable) return;
            if (!TryStep(dir)) { _holdDir = 0; return; }   // 已到边界（无变化）则不进入连发
            _holdDir       = dir;
            _holdStartTime = Time.unscaledTime;
            _repeatAccum   = 0f;
            if (_holdRoutine != null) StopCoroutine(_holdRoutine);
            _holdRoutine = StartCoroutine(HoldRepeatRoutine());
        }

        /// <summary>抬起 / 移出 / 失活：结束长按连发并停止协程。</summary>
        private void EndHold()
        {
            _holdDir     = 0;
            _repeatAccum = 0f;
            if (_holdRoutine != null) { StopCoroutine(_holdRoutine); _holdRoutine = null; }
        }

        /// <summary>长按连发协程：逐帧按阶段速度连发。</summary>
        private IEnumerator HoldRepeatRoutine()
        {
            while (_holdDir != 0)
            {
                yield return null; // 先等一帧，避免与「按下即触发」在同帧重复

                float elapsed = Time.unscaledTime - _holdStartTime;
                float rate    = GetRepeatRate(elapsed);
                if (rate <= 0f) continue; // 仍处于首个阈值之前的初始延迟

                _repeatAccum += rate * Time.unscaledDeltaTime;
                int steps = Mathf.FloorToInt(_repeatAccum);
                if (steps <= 0) continue;
                _repeatAccum -= steps;

                int before = _value;
                SetValue(_value + _holdDir * step * steps);   // 本帧累积步数一次性应用

                bool atBound = (_holdDir < 0 && _value <= _min) || (_holdDir > 0 && _value >= _max);
                if (_value == before || atBound) { EndHold(); yield break; }
            }
        }

        /// <summary>按当前按住时长取匹配的「每秒次数」（取所有「秒数阈值 ≤ 已按住时长」中阈值最大的一条）。</summary>
        private float GetRepeatRate(float elapsed)
        {
            float rate = 0f, best = -1f;
            if (longPressStages != null)
                foreach (var s in longPressStages)
                    if (s.secondsThreshold <= elapsed && s.secondsThreshold >= best)
                    {
                        best = s.secondsThreshold;
                        rate = Mathf.Max(0f, s.timesPerSecond);
                    }
            return rate;
        }

        /// <summary>步进一次；返回是否实际发生变化（供长按触底自动停止判断）。</summary>
        private bool TryStep(int dir)
        {
            int before = _value;
            SetValue(_value + dir * step);
            return _value != before;
        }

        #endregion

        #region 类定义

        /// <summary>
        /// 长按连发的一个阶段：按住达到 <see cref="secondsThreshold"/> 秒后，
        /// 连发速度切换为 <see cref="timesPerSecond"/> 次/秒。
        /// </summary>
        [System.Serializable]
        public struct LongPressStage
        {
            [Tooltip("秒数阈值：按住时长达到该值后启用本阶段速度。")]
            public float secondsThreshold;
            [Tooltip("每秒次数：本阶段每秒触发 +/- 的次数（连发速度）。")]
            public float timesPerSecond;

            public LongPressStage(float secondsThreshold, float timesPerSecond)
            {
                this.secondsThreshold = secondsThreshold;
                this.timesPerSecond   = timesPerSecond;
            }
        }

        /// <summary>
        /// 轻量指针长按转发组件：运行时挂到 +/- 按钮 GameObject 上，
        /// 把按下 / 抬起 / 移出转发给 <see cref="UiwNumberCounter"/> 以驱动长按连发。
        /// </summary>
        private sealed class PointerHoldRelay : MonoBehaviour,
            IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
        {
            public Action OnPress;
            public Action OnRelease;

            public void OnPointerDown(PointerEventData eventData) => OnPress?.Invoke();
            public void OnPointerUp(PointerEventData eventData)   => OnRelease?.Invoke();
            public void OnPointerExit(PointerEventData eventData) => OnRelease?.Invoke();
        }

        #endregion
    }
}

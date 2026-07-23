using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 库工具窗口基类：抽出「选数据库 + 逐帧时间预算步进 + 进度条 + 可选择日志 + 取消 + 完成收尾」等
    /// 与具体操作无关的通用能力，供 <see cref="InventoryAddressableToolWindow"/>（资源引用迁移）与
    /// <c>InventoryLocalizationToolWindow</c>（本地化建表/生成 Key）等工具窗口复用。
    ///
    /// <para>子类只需：① 覆写 <see cref="DrawOperations"/> 画自己的操作按钮区，用 <see cref="RunSteps"/> 启动一批逐帧步骤；
    /// ② 覆写 <see cref="OnRunComplete"/>（保存/汇总日志）与 <see cref="OnRunFinished"/>（进度条满后弹完成窗）；
    /// ③ 可选覆写 <see cref="DrawHeader"/>（顶部说明）、<see cref="DoneVerb"/>（进度条动词）、
    /// <see cref="OnRunCanceled"/>。每步返回一条日志（无变化返回 null），步内自增 <see cref="Changed"/>。</para>
    ///
    /// <para>本类仅依赖 <see cref="EditorWindow"/> 与 <see cref="InventoryDatabase"/>，无 Addressables/Localization 依赖，
    /// 故不受任何编译宏门控，可被受宏约束的子类程序集继承。</para>
    /// </summary>
    public abstract class InventoryToolWindowBase : EditorWindow
    {
        /// <summary>当前选中的数据库（<c>[SerializeField]</c> 跨域重载保留）。</summary>
        [SerializeField] protected InventoryDatabase database;

        // ── 日志 ─────────────────────────────────────────────────────────────────────
        private readonly List<string> _log = new List<string>();
        private Vector2 _logScroll;
        private bool    _autoScroll = true;
        private const int MaxLogLines = 2000;

        // 日志文本样式：左上对齐、不换行；懒创建（GUIStyle 只能在 GUI 内构造）。
        private GUIStyle _logStyle;
        private GUIStyle LogStyle => _logStyle ?? (_logStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperLeft,
            wordWrap  = false,
            richText  = false,
        });

        #region 日志

        /// <summary>追加一行日志（超过上限自动丢弃最旧行）。</summary>
        protected void Log(string line)
        {
            _log.Add(line);
            if (_log.Count > MaxLogLines)
                _log.RemoveRange(0, _log.Count - MaxLogLines);
        }

        // ── 步进引擎 ─────────────────────────────────────────────────────────────────
        private List<Func<string>> _steps;   // 每步执行一项并返回一条日志（无变化返回 null）
        private int  _stepIndex;
        private bool _running;
        private bool _pendingFinishDialog;    // 已完成、待进度条重绘到 100% 后再弹信息窗

        /// <summary>本次运行的处理计数（子类步骤内自增；进度条与完成文案使用）。</summary>
        protected int Changed;

        /// <summary>是否正在逐帧运行。</summary>
        protected bool IsRunning => _running;

        /// <summary>已处理步数（供子类日志显示进度）。</summary>
        protected int StepIndex => _stepIndex;

        /// <summary>总步数（供子类日志显示进度）。</summary>
        protected int StepCount => _steps?.Count ?? 0;

        /// <summary>每帧步进的处理耗时上限（毫秒）：累计达到即让出该帧，避免卡死编辑器。</summary>
        private const double MaxFrameMillis = 30d;

        /// <summary>整个过程至少分摊到的帧数：每帧处理量不超过总量的 1/该值，保证进度条逐帧可见刷新。</summary>
        private const int MinFrameCount = 10;

        #endregion

        #region 步骤执行

        /// <summary>启动一批逐帧步骤（每步返回一条日志、无变化返回 null）。已在运行则忽略。</summary>
        protected void RunSteps(List<Func<string>> steps, string startLog = null)
        {
            if (_running || steps == null) return;
            _steps     = steps;
            _stepIndex = 0;
            Changed    = 0;
            _running   = true;
            if (!string.IsNullOrEmpty(startLog)) Log(startLog);
            EditorApplication.update += Step;
        }

        /// <summary>取消当前运行。</summary>
        protected void CancelRun()
        {
            if (!_running) return;
            EditorApplication.update -= Step;
            _running = false;
            OnRunCanceled();
            Repaint();
        }

        private void Step()
        {
            if (!_running) return;

            // 时间预算式步进：一帧内尽量多处理，满足任一条件即让出该帧：
            //   1) 累计耗时达 MaxFrameMillis；2) 本帧处理量达总量的 1/MinFrameCount。
            // 条件检查放在处理之后，保证每帧至少推进 1 项。
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int frameStepLimit     = Mathf.Max(1, _steps.Count / MinFrameCount);
            int processedThisFrame = 0;
            while (_stepIndex < _steps.Count)
            {
                string line = _steps[_stepIndex]();
                _stepIndex++;
                processedThisFrame++;
                if (!string.IsNullOrEmpty(line)) Log(line);

                if (sw.Elapsed.TotalMilliseconds >= MaxFrameMillis || processedThisFrame >= frameStepLimit)
                    break;
            }

            Repaint();

            if (_stepIndex >= _steps.Count)
                BeginFinish();
        }

        /// <summary>
        /// 处理完成收尾：停止步进、调用 <see cref="OnRunComplete"/>（子类保存/汇总），把进度条推到 100% 并请求重绘。
        /// 完成信息窗延迟到进度条真正重绘完成后（<see cref="OnGUI"/> 的 Repaint 事件里经 delayCall 触发 <see cref="OnRunFinished"/>）。
        /// </summary>
        private void BeginFinish()
        {
            EditorApplication.update -= Step;
            _running = false;
            OnRunComplete();
            _pendingFinishDialog = true;
            Repaint();
        }

        // ── 子类钩子 ─────────────────────────────────────────────────────────────────

        #endregion

        #region 子类钩子

        /// <summary>顶部说明区（默认空）。</summary>
        protected virtual void DrawHeader() { }

        /// <summary>操作按钮区（子类必须实现；用 <see cref="RunSteps"/> 启动步骤）。</summary>
        protected abstract void DrawOperations();

        /// <summary>进度条动词（默认「已处理」；子类可按当前操作覆写为「已转换」「已生成」等）。</summary>
        protected virtual string DoneVerb => "已处理";

        /// <summary>运行完成（步进结束、进度条尚未重绘到 100% 前）：子类在此 SetDirty/SaveAssets 并打印汇总日志。</summary>
        protected virtual void OnRunComplete() { }

        /// <summary>运行完成信息窗（进度条重绘到 100% 后经 delayCall 调用）：子类在此弹 <see cref="EditorUtility.DisplayDialog"/>。</summary>
        protected virtual void OnRunFinished() { }

        /// <summary>运行被取消：子类可在此 SetDirty/SaveAssets 并打印日志。</summary>
        protected virtual void OnRunCanceled() { }

        // ── 绘制 ─────────────────────────────────────────────────────────────────────

        #endregion

        #region 绘制

        /// <summary>数据库选择字段（默认单个 <c>ObjectField</c>；运行中禁用；子类可覆写以追加字段）。</summary>
        protected virtual void DrawDatabaseField()
        {
            using (new EditorGUI.DisabledScope(_running))
            {
                database = (InventoryDatabase)EditorGUILayout.ObjectField(
                    "数据库", database, typeof(InventoryDatabase), false);
            }
        }

        /// <summary>进度条（<c>stepIndex/total</c> + <see cref="Changed"/> + <see cref="DoneVerb"/>）。</summary>
        protected void DrawProgressBar()
        {
            int   total    = _steps != null ? _steps.Count : 0;
            float progress = total > 0 ? (float)_stepIndex / total : 0f;
            var   barRect  = GUILayoutUtility.GetRect(1f, 20f, GUILayout.ExpandWidth(true));
            string barLabel = total > 0
                ? $"{_stepIndex} / {total}  ({progress:P0})   {DoneVerb} {Changed} 处"
                : (_running ? "准备中…" : "就绪");
            EditorGUI.ProgressBar(barRect, progress, barLabel);
        }

        /// <summary>日志窗口（标题 + 自动滚动开关 + 清空 + 可选择文本；运行时自动滚到底）。</summary>
        protected void DrawLogPanel()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                _autoScroll = GUILayout.Toggle(_autoScroll, "自动滚动", GUILayout.Width(80));
                if (GUILayout.Button("清空", GUILayout.Width(56)))
                    _log.Clear();
            }

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll,
                EditorStyles.helpBox, GUILayout.ExpandHeight(true));
            // 单个 SelectableLabel 承载全部日志（可选择复制）；用相同样式 CalcHeight 精确算高，避免垂直居中留白。
            string logText   = string.Join("\n", _log);
            float  logHeight = logText.Length > 0
                ? LogStyle.CalcHeight(new GUIContent(logText), 4000f)
                : LogStyle.lineHeight;
            EditorGUILayout.SelectableLabel(logText, LogStyle,
                GUILayout.Height(logHeight), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndScrollView();

            if (_running && _autoScroll && Event.current.type == EventType.Repaint)
                _logScroll.y = float.MaxValue;
        }

        // ── 生命周期 / 骨架 ──────────────────────────────────────────────────────────

        protected virtual void OnDisable()
        {
            // 窗口关闭 / 重载时停止步进，避免回调泄漏
            if (_running) EditorApplication.update -= Step;
            _running = false;
            _pendingFinishDialog = false;
        }

        private void OnGUI()
        {
            DrawHeader();

            DrawDatabaseField();
            EditorGUILayout.Space(6);

            DrawOperations();

            if (_running && GUILayout.Button("取消", GUILayout.Height(20)))
                CancelRun();

            EditorGUILayout.Space(6);
            DrawProgressBar();
            EditorGUILayout.Space(6);
            DrawLogPanel();

            // 处理完成后：等进度条这一帧真正重绘到 100%（Repaint）后，再经 delayCall 移出本次 OnGUI 弹完成窗。
            if (_pendingFinishDialog && Event.current.type == EventType.Repaint)
            {
                _pendingFinishDialog = false;
                EditorApplication.delayCall += OnRunFinished;
            }
        }
        #endregion

    }
}

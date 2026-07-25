using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 「可搜索 + 滚动定位」列表的共享编辑器逻辑（与元素类型无关）：按子串搜索定位匹配、
    /// 上 / 下循环跳转、把目标行滚到视口顶部。调用方持一份 <see cref="EditorSearchableListState"/>，
    /// 自行绘制列表行；本类只负责搜索栏、匹配集与滚动定位三样样板。
    ///
    /// <para>每帧顺序：<see cref="DrawSearchBar"/>（搜索栏）→ <c>BeginScrollView(state.Scroll)</c> →
    /// 逐行绘制并对每行调 <see cref="CaptureRow"/> 采集目标 Y → <c>EndScrollView</c> → <see cref="ApplyScroll"/>。
    /// 列表内容变动（增删 / 重排）后调 <see cref="Recompute"/> 刷新匹配集。</para>
    /// </summary>
    public sealed class EditorSearchableListState
    {
        /// <summary>滚动位置（调用方在 <c>BeginScrollView</c> 读写）。</summary>
        public Vector2 Scroll;

        /// <summary>当前搜索词。</summary>
        public string Search = string.Empty;

        /// <summary>命中搜索的元素下标集合（按出现顺序）。</summary>
        public readonly List<int> Matches = new List<int>();

        /// <summary>当前聚焦的匹配在 <see cref="Matches"/> 中的指针；无匹配为 -1。</summary>
        public int MatchPtr = -1;

        /// <summary>请求滚动定位到的元素下标；-1 = 无请求。</summary>
        public int ScrollToIndex = -1;

        /// <summary>当前聚焦的匹配元素下标（用于高亮当前匹配行）；无匹配为 -1。</summary>
        public int CurrentMatch => (MatchPtr >= 0 && MatchPtr < Matches.Count) ? Matches[MatchPtr] : -1;
    }

    /// <summary>见 <see cref="EditorSearchableListState"/>。</summary>
    public static class EditorSearchableList
    {
        /// <summary>
        /// 重算搜索匹配（按 <paramref name="keyOf"/> 取每个元素的键，忽略大小写子串匹配），并定位到首个匹配。
        /// </summary>
        /// <param name="s">列表状态。</param>
        /// <param name="count">元素总数。</param>
        /// <param name="keyOf">取第 i 个元素的匹配键（如道具 ID）。</param>
        public static void Recompute(EditorSearchableListState s, int count, Func<int, string> keyOf)
        {
            s.Matches.Clear();
            string term = s.Search?.Trim();
            if (!string.IsNullOrEmpty(term) && keyOf != null)
                for (int i = 0; i < count; i++)
                {
                    string key = keyOf(i);
                    if (!string.IsNullOrEmpty(key) && key.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                        s.Matches.Add(i);
                }
            s.MatchPtr      = s.Matches.Count > 0 ? 0 : -1;
            s.ScrollToIndex = s.MatchPtr >= 0 ? s.Matches[s.MatchPtr] : -1;
        }

        /// <summary>
        /// 绘制搜索栏：搜索框 + 「{n}/{N}」计数 + 上 / 下循环跳转；无匹配且搜索词非空时显示「无匹配」。
        /// 搜索词变化时即时重算匹配（用 <paramref name="count"/> / <paramref name="keyOf"/>）。
        /// </summary>
        /// <param name="repaint">请求重绘的回调（一般传 <c>ctx.Repaint</c>）。</param>
        public static void DrawSearchBar(EditorSearchableListState s, int count, Func<int, string> keyOf, Action repaint)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Tr("搜索"), GUILayout.Width(30));
            EditorGUI.BeginChangeCheck();
            string newSearch = EditorGUILayout.TextField(s.Search ?? string.Empty);
            if (EditorGUI.EndChangeCheck())
            {
                s.Search = newSearch;
                Recompute(s, count, keyOf);
                repaint?.Invoke();
            }
            if (s.Matches.Count > 0)
            {
                EditorGUILayout.LabelField($"{s.MatchPtr + 1}/{s.Matches.Count}", GUILayout.Width(40));
                if (GUILayout.Button("↑", EditorStyles.miniButtonLeft, GUILayout.Width(24)))
                {
                    s.MatchPtr      = (s.MatchPtr - 1 + s.Matches.Count) % s.Matches.Count;
                    s.ScrollToIndex = s.Matches[s.MatchPtr];
                    repaint?.Invoke();
                }
                if (GUILayout.Button("↓", EditorStyles.miniButtonRight, GUILayout.Width(24)))
                {
                    s.MatchPtr      = (s.MatchPtr + 1) % s.Matches.Count;
                    s.ScrollToIndex = s.Matches[s.MatchPtr];
                    repaint?.Invoke();
                }
            }
            else if (!string.IsNullOrEmpty(s.Search))
            {
                EditorGUILayout.LabelField(Tr("无匹配"), EditorStyles.miniLabel, GUILayout.Width(48));
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 在滚动区内逐行绘制时，对每行调用以采集滚动定位目标 Y。
        /// <para>⚠ 仅在 Repaint 阶段 <paramref name="rowRect"/> 才返回有效坐标；Layout 阶段返回占位 rect(y≈0)，
        /// 若在此采集会用 0 覆盖滚动并提前清掉定位请求，导致「滚不动」。</para>
        /// </summary>
        public static void CaptureRow(EditorSearchableListState s, int index, Rect rowRect, ref float? targetY)
        {
            if (index == s.ScrollToIndex && Event.current.type == EventType.Repaint)
                targetY = rowRect.y;
        }

        /// <summary>
        /// 在 <c>EndScrollView</c> 之后调用：Repaint 成功取得目标 Y 时把目标行滚到视口顶部（下一帧生效），
        /// 并清除定位请求。
        /// </summary>
        public static void ApplyScroll(EditorSearchableListState s, float? targetY, Action repaint)
        {
            if (s.ScrollToIndex >= 0 && targetY.HasValue)
            {
                s.Scroll.y      = Mathf.Max(0f, targetY.Value);
                s.ScrollToIndex = -1;
                repaint?.Invoke();
            }
        }
    }
}

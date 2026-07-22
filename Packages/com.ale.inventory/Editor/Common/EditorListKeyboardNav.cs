using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 条目列表键盘导航：某条目已选中时，按 上/下 方向键切换到可见（已过滤）列表中的相邻条目，
    /// 并在新选中项超出可视区时自动滚动一行把它带回视野。
    /// 供各系统「中间条目列表」面板（道具 / 仓库 / 商店 / 制作 / 装备 / 技能）复用；
    /// 这些面板行结构统一为两行（列名行 + 值行），故行距一致，滚动逐行推进。
    /// </summary>
    public static class EditorListKeyboardNav
    {
        /// <summary>
        /// 处理 上/下 方向键切换选中，并按需自动滚动。
        /// <paramref name="visible"/> 为当前可见（已过滤）条目，按显示顺序；<paramref name="current"/> 为当前选中项。
        /// <paramref name="scroll"/> 为列表滚动位置（就地调整）；<paramref name="rowPitch"/> 为单行条目占用的纵向像素
        /// （含行内/行间间距）；<paramref name="viewportHeight"/> 为滚动视口高度（≤0 时跳过滚动）。
        /// 仅在已有选中项、且未在编辑文本框时生效；命中方向键即消费事件（避免滚动视图 / 其它控件响应）。
        /// </summary>
        /// <returns>选中项发生变化时返回 true，并通过 <paramref name="next"/> 输出新选中项。</returns>
        public static bool HandleUpDown<T>(IList<T> visible, T current, out T next,
            ref Vector2 scroll, float rowPitch, float viewportHeight) where T : class
        {
            next = current;

            var e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return false;
            if (e.keyCode != KeyCode.UpArrow && e.keyCode != KeyCode.DownArrow) return false;
            if (EditorGUIUtility.editingTextField) return false;        // 编辑文本时让位给光标移动
            if (current == null || visible == null || visible.Count == 0) return false;

            int idx = visible.IndexOf(current);
            if (idx < 0) return false;                                   // 选中项不在可见列表中（被过滤），不处理

            int newIdx = Mathf.Clamp(idx + (e.keyCode == KeyCode.DownArrow ? 1 : -1), 0, visible.Count - 1);
            e.Use();                                                     // 消费方向键，避免滚动视图 / 其它控件响应
            if (newIdx == idx) return false;                             // 已在端点，无变化

            next = visible[newIdx];

            // 自动滚动：把新选中行带回可视区（逐行切换时即为滚动一行）。
            // 列表内条目行距一致，可见行 k 的内容顶部 = k * rowPitch（滚动位置以内容顶为 0 计）。
            if (rowPitch > 0f && viewportHeight > 0f)
            {
                float rowTop = newIdx * rowPitch;
                float rowBot = rowTop + rowPitch;
                if (rowTop < scroll.y)                       scroll.y = rowTop;               // 在视口上方 → 上滚
                else if (rowBot > scroll.y + viewportHeight) scroll.y = rowBot - viewportHeight; // 在视口下方 → 下滚
                if (scroll.y < 0f) scroll.y = 0f;
            }
            return true;
        }
    }
}

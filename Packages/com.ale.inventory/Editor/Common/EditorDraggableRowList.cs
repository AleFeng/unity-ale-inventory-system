using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「可拖拽重排的单行列表」共享 IMGUI 绘制骨架：
    /// <c>BeginFrame</c> → 逐行（整行 Rect 记录 + 左侧句柄列 + <b>行内内容</b> + 右侧「✕」）→ <c>EndFrame</c>
    /// → 延迟删除。行内内容由调用方经 <paramref name="drawContent"/> 提供，其余样板全部收口于此。
    ///
    /// <para>此前该骨架在 6 处各写了一遍（仓库引用 / 忽略ID / 副分组标签 ×2 / 功能标签 / 枚举约束 / 属性字段显示），
    /// 其中「句柄垂直居中」的三行 Rect 运算逐字相同 —— 现由 <see cref="CenteredHandleRect"/> 统一。</para>
    ///
    /// <para>拖拽状态机 <see cref="EditorReorderableDrag"/> 由调用方各持一个传入，
    /// 保证同帧内多条列表互不干扰（详见该类文档）。</para>
    /// </summary>
    public static class EditorDraggableRowList
    {
        /// <summary>行尾「✕」删除按钮宽度。</summary>
        public const float RemoveButtonWidth = 22f;

        /// <summary>
        /// 把整行 Rect 换算为左侧拖拽句柄的 Rect：宽度取句柄列宽，高度取单行高并在整行内**垂直居中**，
        /// 使句柄与右侧单行内容对齐。
        /// </summary>
        public static Rect CenteredHandleRect(Rect rowRect) => new Rect(
            rowRect.x,
            rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
            EditorReorderableDrag.HandleWidth,
            EditorGUIUtility.singleLineHeight);

        /// <summary>
        /// 绘制可拖拽重排的单行列表（就地重排 / 删除 <paramref name="list"/>）。列表为空时不绘制任何内容
        /// （标题、「+」按钮、空态提示等由调用方在外层自行处理）。
        /// </summary>
        /// <param name="ctx">编辑器上下文（承担 Undo / MarkDirty / Repaint / Database）。</param>
        /// <param name="list">被绘制的列表，就地重排与删除。</param>
        /// <param name="drag">该列表专属的拖拽重排状态机（调用方持有）。</param>
        /// <param name="undoNoun">Undo 文案词根：重排记作「调整{noun}顺序」，删除记作「移除{noun}」。</param>
        /// <param name="drawContent">
        /// 行内内容绘制回调 <c>(index, item)</c>：在左侧句柄列与右侧「✕」之间绘制，
        /// 已处于 <c>BeginHorizontal</c> / <c>EndHorizontal</c> 之内，只需画自己的控件。
        /// </param>
        /// <param name="removeUndoLabel">删除的 Undo 文案；为空则取「移除{undoNoun}」。</param>
        /// <param name="lineRightInset">拖拽插入指示线相对整行右边缘的内缩（一般取「✕」按钮宽度，或 0 = 画满）。</param>
        public static void Draw<T>(IInventoryEditorContext ctx, IList<T> list, EditorReorderableDrag drag,
            string undoNoun, Action<int, T> drawContent,
            string removeUndoLabel = null, float lineRightInset = 0f)
        {
            if (list == null || list.Count == 0) return;

            drag.BeginFrame();

            int removeIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                // 整行单行内容：左侧预留句柄列，句柄稍后按整行 Rect 垂直居中绘制，与右侧内容横向对齐。
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                drag.RecordRow(i, rowRect);

                GUILayout.Space(EditorReorderableDrag.HandleWidth);
                drawContent(i, list[i]);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(RemoveButtonWidth)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();

                drag.DrawHandle(CenteredHandleRect(rowRect), i);
            }

            drag.EndFrame(ctx, list, $"调整{undoNoun}顺序",
                EditorReorderableDrag.HandleWidth, lineRightInset);

            // 删除延迟到行循环之后，避免在遍历中改动列表。
            if (removeIndex >= 0)
            {
                ctx.RecordUndo(string.IsNullOrEmpty(removeUndoLabel) ? $"移除{undoNoun}" : removeUndoLabel);
                list.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }
    }
}

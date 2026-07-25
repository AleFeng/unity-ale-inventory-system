using System;
using UnityEngine;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 编辑器数据修改的「Undo 三件套」包装：<c>RecordUndo → 改 → MarkDirty → Repaint</c>。
    /// 这一序列在各绘制器 / 面板里散落数十处，收拢后新插件不会漏掉其中任何一步。
    /// </summary>
    public static class EditorMutate
    {
        /// <summary>记录 Undo、执行 <paramref name="mutate"/>、标脏并请求重绘。</summary>
        public static void Apply<TDb>(IEditorDbContext<TDb> ctx, string undoName, Action mutate)
            where TDb : ScriptableObject
        {
            ctx.RecordUndo(undoName);
            mutate();
            ctx.MarkDirty();
            ctx.Repaint();
        }
    }
}

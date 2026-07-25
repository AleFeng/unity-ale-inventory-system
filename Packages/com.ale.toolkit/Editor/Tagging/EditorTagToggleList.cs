using System.Collections.Generic;
using Ale.Toolkit.Runtime;
using UnityEditor;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 「标签多选勾选列表」共享绘制：遍历给定标签集合，逐个 <c>ToggleLeft</c>，勾选状态变化时带 Undo 地
    /// 增删 <paramref name="refs"/> 中的标签名。标题 / 说明行由调用方在外层自行绘制（各处的层级与前置说明不一致）。
    /// </summary>
    public static class EditorTagToggleList
    {
        /// <summary>无可用标签时的默认提示。</summary>
        public const string DefaultEmptyHint = "（暂无可用功能标签）";

        /// <summary>
        /// 绘制标签勾选列表（就地增删 <paramref name="refs"/>）。
        /// </summary>
        /// <param name="ctx">编辑器上下文（承担 Undo / MarkDirty）。</param>
        /// <param name="tags">可勾选的标签集合（如宿主库的标签列表）。</param>
        /// <param name="refs">被勾选的标签名列表。</param>
        /// <param name="undoAddLabel">勾选时的 Undo 文案。</param>
        /// <param name="undoRemoveLabel">取消勾选时的 Undo 文案。</param>
        /// <param name="emptyHint"><paramref name="tags"/> 为空时的提示。</param>
        public static void Draw(IEditorContext ctx, IReadOnlyList<Tag> tags, List<string> refs,
            string undoAddLabel, string undoRemoveLabel, string emptyHint = DefaultEmptyHint)
        {
            if (tags == null || tags.Count == 0)
            {
                EditorGUILayout.LabelField(Tr(emptyHint), EditorStyles.miniLabel);
                return;
            }

            foreach (var tag in tags)
            {
                bool has = refs.Contains(tag.name);
                bool now = EditorGUILayout.ToggleLeft(tag.name, has);
                if (now == has) continue;

                ctx.RecordUndo(now ? undoAddLabel : undoRemoveLabel);
                if (now) refs.Add(tag.name);
                else     refs.Remove(tag.name);
                ctx.MarkDirty();
            }
        }
    }
}

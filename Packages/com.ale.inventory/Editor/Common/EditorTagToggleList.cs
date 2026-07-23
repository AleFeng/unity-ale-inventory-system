using System.Collections.Generic;
using UnityEditor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「功能标签多选勾选列表」共享绘制：遍历数据库全部功能标签，逐个 <c>ToggleLeft</c>，
    /// 勾选状态变化时带 Undo 地增删 <paramref name="refs"/> 中的标签名。
    ///
    /// <para>此前该 12 行循环在 7 处各写了一遍（仓库 Inspector ×2、仓库模板 ×2、道具模板、
    /// 商店配置 ×2），只差 Undo 文案与空态提示。标题 / 说明行仍由调用方在外层自行绘制，
    /// 因为各处的标题层级与前置说明并不一致。</para>
    ///
    /// <para>注意：道具 Inspector 的功能标签勾选**不在**此列 —— 它调的是
    /// <c>Item.AddTag / RemoveTag</c>（会连带重建道具属性 schema），且要把「被模板锁定」的标签
    /// 渲染为只读项，语义与本helper 不同，保持独立实现。</para>
    /// </summary>
    public static class EditorTagToggleList
    {
        /// <summary>无可用功能标签时的默认提示。</summary>
        public const string DefaultEmptyHint = "（暂无可用功能标签）";

        /// <summary>
        /// 绘制功能标签勾选列表（就地增删 <paramref name="refs"/>）。
        /// </summary>
        /// <param name="refs">被勾选的功能标签名列表。</param>
        /// <param name="undoAddLabel">勾选时的 Undo 文案。</param>
        /// <param name="undoRemoveLabel">取消勾选时的 Undo 文案。</param>
        /// <param name="emptyHint">数据库无功能标签时的提示。</param>
        public static void Draw(IInventoryEditorContext ctx, List<string> refs,
            string undoAddLabel, string undoRemoveLabel, string emptyHint = DefaultEmptyHint)
        {
            var db = ctx.Database;
            if (db.FunctionTags.Count == 0)
            {
                EditorGUILayout.LabelField(emptyHint, EditorStyles.miniLabel);
                return;
            }

            foreach (var tag in db.FunctionTags)
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

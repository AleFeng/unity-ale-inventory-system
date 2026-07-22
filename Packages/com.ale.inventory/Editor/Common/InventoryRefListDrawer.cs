using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 「仓库引用列表」（有序，Index 即优先级）的共享 IMGUI 绘制：标题 + 「+」添加下拉 + 可拖拽重排的条目行
    /// （每行：左侧拖拽句柄 + 序号 + 仓库 ID（已删除红字）+ 删除）。
    /// 供 商店「交易仓库」、装备「装备仓库」、制作「制作仓库」复用，行为一致。
    /// 拖拽状态机由各调用方各持一个 <see cref="EditorReorderableDrag"/> 传入，保证每份列表独立。
    /// </summary>
    public static class InventoryRefListDrawer
    {
        /// <summary>
        /// 绘制可拖拽重排的仓库引用列表（就地增删 / 重排 <paramref name="refs"/>）。
        /// </summary>
        /// <param name="ctx">编辑器上下文（承担 Undo / MarkDirty / Repaint / Database）。</param>
        /// <param name="refs">仓库 ID 列表。</param>
        /// <param name="drag">该列表专属的拖拽重排状态机（调用方持有，保证同帧各列表互不干扰）。</param>
        /// <param name="header">区块标题（如「交易仓库」）。</param>
        /// <param name="noun">用于 Undo 名称与「+」菜单文案的名词（如「交易仓库」）。</param>
        /// <param name="hint">标题下方的说明（miniLabel）；为空则不显示。</param>
        /// <param name="emptyHint">列表为空时的提示；为空则不显示。</param>
        public static void Draw(IInventoryEditorContext ctx, List<string> refs, EditorReorderableDrag drag,
            string header, string noun, string hint = null, string emptyHint = null)
        {
            var db = ctx.Database;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
                ShowAddMenu(ctx, refs, noun);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(hint))
                EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);

            if (refs.Count == 0)
            {
                if (!string.IsNullOrEmpty(emptyHint))
                    EditorGUILayout.LabelField(emptyHint, EditorStyles.miniLabel);
                return;
            }

            drag.BeginFrame();

            int removeIndex = -1;
            for (int i = 0; i < refs.Count; i++)
            {
                string invId  = refs[i];
                bool   exists = db.GetInventory(invId) != null;

                // 整行单行内容：左侧预留句柄列，句柄稍后按整行 Rect 垂直居中绘制，与右侧内容横向对齐。
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                drag.RecordRow(i, rowRect);

                GUILayout.Space(EditorReorderableDrag.HandleWidth);
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));
                EditorGUILayout.LabelField(exists ? invId : invId + "（已删除）",
                    exists ? EditorStyles.label : InventoryEditorStyles.StatusError);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();

                // 句柄按整行 Rect 垂直居中（singleLineHeight），与单行内容对齐。
                var handleRect = new Rect(rowRect.x,
                    rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                    EditorReorderableDrag.HandleWidth, EditorGUIUtility.singleLineHeight);
                drag.DrawHandle(handleRect, i);
            }

            drag.EndFrame(ctx, refs, $"调整{noun}顺序");

            if (removeIndex >= 0)
            {
                ctx.RecordUndo($"移除{noun}");
                refs.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }

        /// <summary>「+」下拉：列出数据库中尚未加入 <paramref name="refs"/> 的仓库供添加。</summary>
        private static void ShowAddMenu(IInventoryEditorContext ctx, List<string> refs, string noun)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            bool any = false;
            foreach (var inv in db.Inventories)
            {
                if (string.IsNullOrEmpty(inv.id) || refs.Contains(inv.id)) continue;
                any = true;
                string id = inv.id;
                menu.AddItem(new GUIContent(id), false, () =>
                {
                    ctx.RecordUndo($"添加{noun}");
                    refs.Add(id);
                    ctx.MarkDirty();
                    ctx.Repaint();
                });
            }
            if (!any)
                menu.AddDisabledItem(new GUIContent("（无可添加的仓库）"));
            menu.ShowAsContext();
        }
    }
}

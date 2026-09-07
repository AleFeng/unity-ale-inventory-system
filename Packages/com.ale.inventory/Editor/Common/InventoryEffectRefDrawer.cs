using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

using Ale.Toolkit.Editor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「使用时施加的效果」id 列表绘制器（道具 / 道具模板共用）：标题行 + 「+」菜单（本库效果）、可拖拽重排 / 删除的行、
    /// 底部自由输入行（引用其它库 / 其它系统定义的效果 id——运行时经全局效果注册表解析，故不作悬空校验，只标注「外部」）。
    /// </summary>
    public static class InventoryEffectRefDrawer
    {
        private static readonly GUIStyle ExternalStyle = new GUIStyle(EditorStyles.label)
            { normal = { textColor = new Color(0.56f, 0.71f, 0.88f) } };

        private static string _pendingId = string.Empty;

        public static void Draw(IInventoryEditorContext ctx, List<string> refs, EditorReorderableDrag drag,
            string header, string noun, string hint = null)
        {
            if (refs == null) return;
            var db = ctx.Database;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(header, ToolkitEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
                ShowAddMenu(ctx, refs, noun);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(hint))
                EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);

            if (refs.Count == 0)
                EditorGUILayout.LabelField(Tr("（暂无使用效果）"), EditorStyles.miniLabel);
            else
                EditorDraggableRowList.Draw(ctx, refs, drag, noun, (i, id) =>
                {
                    bool local = db.GetEffect(id) != null;
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));
                    if (local)
                    {
                        var e = db.GetEffect(id);
                        string name = !string.IsNullOrEmpty(e.displayName) && e.displayName != id ? $"{e.displayName} ({id})" : id;
                        EditorGUILayout.LabelField(name);
                    }
                    else
                        EditorGUILayout.LabelField(new GUIContent(id + Tr("（外部）"),
                            Tr("本库未定义；运行时经全局效果注册表（EffectDefinitionRegistry.Default）按 id 解析")), ExternalStyle);
                });

            // 自由输入：引用其它库 / 其它系统的效果 id
            EditorGUILayout.BeginHorizontal();
            _pendingId = EditorGUILayout.TextField(_pendingId);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_pendingId) || refs.Contains(_pendingId.Trim())))
            {
                if (GUILayout.Button(Tr("添加 id"), GUILayout.Width(70)))
                {
                    ctx.RecordUndo($"添加{noun}");
                    refs.Add(_pendingId.Trim());
                    _pendingId = string.Empty;
                    ctx.MarkDirty();
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void ShowAddMenu(IInventoryEditorContext ctx, List<string> refs, string noun)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            bool any = false;
            foreach (var e in db.Effects)
            {
                if (e == null || string.IsNullOrEmpty(e.id) || refs.Contains(e.id)) continue;
                any = true;
                string id    = e.id;
                string label = !string.IsNullOrEmpty(e.displayName) && e.displayName != id ? $"{e.displayName} ({id})" : id;
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    ctx.RecordUndo($"添加{noun}");
                    refs.Add(id);
                    ctx.MarkDirty();
                    ctx.Repaint();
                });
            }
            if (!any)
                menu.AddDisabledItem(new GUIContent(Tr("（本库无可添加的效果；可在下方输入其它系统的效果 id）")));
            menu.ShowAsContext();
        }
    }
}

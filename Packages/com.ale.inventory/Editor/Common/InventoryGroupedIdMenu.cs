using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「分组路径」单选 <see cref="GenericMenu"/> 的共用弹出逻辑：首项为「清空」占位，其后为各 (显示路径, id) 选项，
    /// 选中项经 setter 写回（带 Undo / MarkDirty / Repaint）。收拢商店 / 制作 / 装备三处近似的分组选择菜单。
    /// </summary>
    internal static class InventoryGroupedIdMenu
    {
        /// <summary>
        /// 弹出分组选择菜单。<paramref name="options"/> 为各 (显示路径, id)；去重 / 顺序由调用方决定。
        /// 首项 <paramref name="noneLabel"/> 对应清空（写回空串）；其余项选中即经 <paramref name="setter"/> 写回其 id。
        /// </summary>
        public static void Show(IInventoryEditorContext ctx, Rect rect,
            string current, string undoLabel, string noneLabel,
            IEnumerable<(string path, string id)> options, Action<string> setter)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent(noneLabel), string.IsNullOrEmpty(current),
                () => Apply(ctx, undoLabel, () => setter(string.Empty)));
            menu.AddSeparator(string.Empty);

            foreach (var (path, id) in options)
            {
                string cap = id;
                menu.AddItem(new GUIContent(path), current == id,
                    () => Apply(ctx, undoLabel, () => setter(cap)));
            }

            menu.DropDown(rect);
        }

        /// <summary>道具选项：库内每个有 id 的道具，按「模板/道具id」分组路径（无模板归「（无模板）」）。</summary>
        public static IEnumerable<(string path, string id)> ItemOptions(InventoryDatabase db)
        {
            if (!db) yield break;
            foreach (var item in db.Items)
            {
                if (item == null || string.IsNullOrEmpty(item.id)) continue;
                string group = string.IsNullOrEmpty(item.templateRef) ? Tr("（无模板）") : item.templateRef;
                yield return (group + "/" + item.id, item.id);
            }
        }

        private static void Apply(IInventoryEditorContext ctx, string undoLabel, Action mutate)
        {
            ctx.RecordUndo(undoLabel);
            mutate();
            ctx.MarkDirty();
            ctx.Repaint();
        }
    }
}

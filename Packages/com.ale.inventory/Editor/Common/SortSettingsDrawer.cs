using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 整理设置共享绘制：整理列表（玩家可选排序条件）+ 整理优先级（条件值相同时的次级比较）两个
    /// <see cref="ReorderableList"/>。字段选项与仓库系统一致（道具 ID / 属性 / 功能标签）。
    /// 供制作系统等模块复用，逻辑与 <see cref="InventoryInspectorPanel"/> 的整理设置一致。
    /// </summary>
    public static class SortSettingsDrawer
    {
        private class State
        {
            public ReorderableList          List;
            public string[]                 Displays;
            public string[]                 Values;
            public IInventoryEditorContext  Ctx;
        }

        // 按 List 实例缓存 ReorderableList（绑定对象更换时自然换用新实例）。
        private static readonly Dictionary<List<SortPriority>, State> Cache
            = new Dictionary<List<SortPriority>, State>();

        /// <summary>绘制「整理列表」+「整理优先级」。</summary>
        public static void Draw(IInventoryEditorContext ctx,
            List<SortPriority> priorities, List<SortPriority> tiebreakers)
        {
            BuildFieldOptions(ctx.Database, out var displays, out var values);
            DrawOne(ctx, priorities, displays, values,
                "整理列表（玩家在 UI 中通过下拉菜单选择排序条件）", "整理列表");
            DrawOne(ctx, tiebreakers, displays, values,
                "整理优先级（整理列表条件值相同时，依次对比此列表直至值不同）", "整理优先级");
        }

        private static void DrawOne(IInventoryEditorContext ctx, List<SortPriority> list,
            string[] displays, string[] values, string header, string undoLabel)
        {
            if (list == null) return;
            if (!Cache.TryGetValue(list, out var st))
            {
                st = new State();
                st.List = BuildList(st, list, header, undoLabel);
                Cache[list] = st;
            }
            st.Ctx      = ctx;
            st.Displays = displays;
            st.Values   = values;
            st.List.DoLayoutList();
        }

        private static ReorderableList BuildList(State st, List<SortPriority> list,
            string header, string undoLabel)
        {
            var rl = new ReorderableList(list, typeof(SortPriority),
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            rl.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);

            rl.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= list.Count) return;
                var sp  = list[index];
                rect.y += 2;

                float ascW    = 58f;
                var fieldRect = new Rect(rect.x, rect.y, rect.width - ascW - 4,
                    EditorGUIUtility.singleLineHeight);
                var ascRect   = new Rect(rect.xMax - ascW, rect.y, ascW,
                    EditorGUIUtility.singleLineHeight);

                var displays = st.Displays ?? new[] { "道具 ID" };
                var values   = st.Values   ?? new[] { "__id__" };
                int curIdx   = FindOptionIndex(values, sp.field);

                EditorGUI.BeginChangeCheck();
                int  picked = EditorGUI.Popup(fieldRect, curIdx, displays);
                bool newAsc = EditorGUI.Toggle(ascRect,
                    new GUIContent(sp.ascending ? "升序" : "降序"), sp.ascending);
                if (EditorGUI.EndChangeCheck())
                {
                    st.Ctx.RecordUndo("修改" + undoLabel);
                    sp.field     = values[picked];
                    sp.ascending = newAsc;
                    st.Ctx.MarkDirty();
                }
            };

            rl.onAddCallback = _ =>
            {
                st.Ctx.RecordUndo("添加" + undoLabel + "项");
                list.Add(new SortPriority("__id__"));
                st.Ctx.MarkDirty();
            };

            rl.onRemoveCallback = l =>
            {
                st.Ctx.RecordUndo("删除" + undoLabel + "项");
                list.RemoveAt(l.index);
                st.Ctx.MarkDirty();
            };

            rl.onReorderCallback = _ =>
            {
                st.Ctx.RecordUndo("调整" + undoLabel + "顺序");
                st.Ctx.MarkDirty();
            };

            return rl;
        }

        /// <summary>
        /// 构建整理条件选项（与 <see cref="InventoryInspectorPanel"/> 一致）：<br/>
        ///   "__id__" = 道具 ID；属性 ID = 按该属性排序；"__tagOrder__" = 按功能标签全局顺序。
        /// </summary>
        public static void BuildFieldOptions(InventoryDatabase db,
            out string[] displays, out string[] values)
        {
            var dList = new List<string> { "道具 ID" };
            var vList = new List<string> { "__id__" };
            var seen  = new HashSet<string>();

            foreach (var tmpl in db.ItemTemplates)
                foreach (var def in tmpl.attributes)
                    if (!string.IsNullOrEmpty(def.id) && seen.Add(def.id))
                    { dList.Add("属性/" + def.id); vList.Add(def.id); }

            foreach (var tag in db.FunctionTags)
                foreach (var def in tag.attributes)
                    if (!string.IsNullOrEmpty(def.id) && seen.Add(def.id))
                    { dList.Add("属性/" + def.id); vList.Add(def.id); }

            if (db.FunctionTags.Count > 0)
            { dList.Add("功能标签"); vList.Add("__tagOrder__"); }

            displays = dList.ToArray();
            values   = vList.ToArray();
        }

        private static int FindOptionIndex(string[] values, string field)
        {
            if (string.IsNullOrEmpty(field) || field == "__id__") return 0;
            for (int i = 1; i < values.Length; i++)
                if (values[i] == field) return i;
            return 0;
        }
    }
}

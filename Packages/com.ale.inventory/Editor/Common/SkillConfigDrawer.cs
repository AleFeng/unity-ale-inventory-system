using System.Collections.Generic;
using InventorySystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 技能共享配置项的 IMGUI 绘制：技能默认信息（名称 / 描述 均为 Text：纯文本 + 本地化引用 / 图标）与分组标签（主 + 副）。
    /// 按 <see cref="ISkillConfig"/> 工作，供 <see cref="SkillInspectorPanel"/>（技能实例）与
    /// <see cref="SkillTemplatePanel"/>（技能模板）共用；技能模板据此配置默认值，创建技能时复制。
    /// </summary>
    public static class SkillConfigDrawer
    {
        /// <summary>副分组标签列表拖拽重排状态机（每帧仅绘制一份技能配置 Inspector，故静态共享安全）。</summary>
        private static readonly EditorReorderableDrag SecondaryGroupDrag
            = new EditorReorderableDrag("SkillCfgSecondaryGroupDrag");

        // ── 技能默认信息（名称 / 本地化 / 描述 / 图标）────────────────────────────────

        public static void DrawDisplayFields(IInventoryEditorContext ctx, ISkillConfig cfg)
        {
            // 名称 / 描述：Text（纯文本 fallback + 原生可搜索本地化选择器），由 AttributeFieldDrawer 统一绘制。
            AttributeFieldDrawer.Draw(ctx, "名称", cfg.DisplayName, null);
            AttributeFieldDrawer.Draw(ctx, "描述", cfg.Description, null);

            // 图标：直接模式 ObjectField / 授权模式原生 AssetReference 选择器
            if (InventoryAssetRefField.DrawSprite("图标", cfg, "skillIcon", cfg.Icon, cfg.IconAddress,
                    out var newIcon, out var newIconAddr))
            {
                ctx.RecordUndo("修改技能图标");
                cfg.Icon        = newIcon;
                cfg.IconAddress = newIconAddr;
                ctx.MarkDirty();
            }
        }

        // ── 分组标签（主 + 副）──────────────────────────────────────────────────────

        public static void DrawGroupTags(IInventoryEditorContext ctx, ISkillConfig cfg)
        {
            var db = ctx.Database;
            EditorGUILayout.LabelField("分组标签", InventoryEditorStyles.Header);

            if (db.SkillGroupTags.Count == 0)
            {
                EditorGUILayout.LabelField("（暂无分组标签；请在左侧「分组标签」中添加）", EditorStyles.miniLabel);
                return;
            }

            // 主分组（单选 Popup；index 0 = 无）
            var ids      = new List<string>();
            var displays = new List<string> { "（无）" };
            foreach (var g in db.SkillGroupTags)
            {
                ids.Add(g.id);
                displays.Add(g.PlainName());
            }
            int curIdx = 0;
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] == cfg.PrimaryGroupTag) { curIdx = i + 1; break; }

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup("主分组标签", curIdx, displays.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改主分组标签");
                cfg.PrimaryGroupTag = picked <= 0 ? string.Empty : ids[picked - 1];
                ctx.MarkDirty();
            }

            // 副分组（列表 + 「+」下拉添加，已添加的不在下拉中显示）
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("副分组标签", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(24)))
                ShowAddSecondaryGroupMenu(ctx, cfg);
            EditorGUILayout.EndHorizontal();

            var secs = cfg.SecondaryGroupTags;
            if (secs.Count == 0)
            {
                EditorGUILayout.LabelField("（未添加）", EditorStyles.miniLabel);
                return;
            }

            SecondaryGroupDrag.BeginFrame();
            int removeIndex = -1;
            for (int i = 0; i < secs.Count; i++)
            {
                var g = db.GetSkillGroupTag(secs[i]);
                bool exists = g != null;
                string label = exists ? g.PlainName() : secs[i] + "（已删除）";

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                SecondaryGroupDrag.RecordRow(i, rowRect);
                GUILayout.Space(EditorReorderableDrag.HandleWidth);
                EditorGUILayout.LabelField(label,
                    exists ? EditorStyles.label : InventoryEditorStyles.StatusError);
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();

                var handleRect = new Rect(rowRect.x,
                    rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
                    EditorReorderableDrag.HandleWidth, EditorGUIUtility.singleLineHeight);
                SecondaryGroupDrag.DrawHandle(handleRect, i);
            }
            SecondaryGroupDrag.EndFrame(ctx, secs, "调整副分组标签顺序");

            if (removeIndex >= 0)
            {
                ctx.RecordUndo("移除副分组标签");
                secs.RemoveAt(removeIndex);
                ctx.MarkDirty();
            }
        }

        private static void ShowAddSecondaryGroupMenu(IInventoryEditorContext ctx, ISkillConfig cfg)
        {
            var db   = ctx.Database;
            var menu = new GenericMenu();
            bool any = false;
            foreach (var g in db.SkillGroupTags)
            {
                if (string.IsNullOrEmpty(g.id) || cfg.SecondaryGroupTags.Contains(g.id)) continue;
                any = true;
                string id    = g.id;
                string label = g.PlainName();
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    ctx.RecordUndo("添加副分组标签");
                    cfg.SecondaryGroupTags.Add(id);
                    ctx.MarkDirty();
                    ctx.Repaint();
                });
            }
            if (!any)
                menu.AddDisabledItem(new GUIContent("（无可添加的分组标签）"));
            menu.ShowAsContext();
        }
    }
}

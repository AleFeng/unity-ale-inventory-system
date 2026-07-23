using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 蓝图可配置项的共享 IMGUI 绘制：制作参数（时间 / 连续次数）、制作仓库（有序，按 Index 优先级）、
    /// 整理设置、UI 配置（数字格式 + 属性字段显示）。按 <see cref="ICraftingConfig"/> 工作，
    /// 供 <see cref="CraftingInspectorPanel"/>（蓝图实例）与 <see cref="CraftingTemplatePanel"/>（蓝图模板）共用。
    /// </summary>
    public static class CraftingConfigDrawer
    {
        /// <summary>制作仓库列表拖拽重排状态机（每帧仅绘制一份蓝图配置，故静态共享安全）。</summary>
        private static readonly EditorReorderableDrag CraftInventoriesDrag
            = new EditorReorderableDrag("CraftInventoriesDrag");

        /// <summary>属性字段显示列表拖拽重排状态机。</summary>
        private static readonly EditorReorderableDrag AttributeDisplaysDrag
            = new EditorReorderableDrag("CraftingAttrDisplaysDrag");

        /// <summary>
        /// 按顺序绘制全部共享配置区块（全部可编辑）。供「蓝图模板」使用——
        /// 制作仓库与 UI 配置为模板级配置，只在模板中编辑。
        /// </summary>
        public static void DrawAll(IInventoryEditorContext ctx, ICraftingConfig cfg)
        {
            DrawCraftParams(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawCraftInventories(ctx, cfg);
            EditorGUILayout.Space(6);
            DrawUIConfig(ctx, cfg);
        }

        /// <summary>
        /// 绘制「蓝图实例」的可配置项：制作参数可编辑；制作仓库与 UI 配置为模板级配置，
        /// 此处只读展示（镜像来源模板，由 <see cref="CraftingBlueprint.RebuildAttributes"/> 同步），
        /// 仅可在「蓝图模板」中修改。
        /// </summary>
        public static void DrawBlueprintConfig(IInventoryEditorContext ctx, CraftingBlueprint bp)
        {
            DrawCraftParams(ctx, bp);
            EditorGUILayout.Space(6);
            DrawInheritedConfigReadonly(ctx, bp);
        }

        // ── 制作参数 ──────────────────────────────────────────────────────────────

        private static void DrawCraftParams(IInventoryEditorContext ctx, ICraftingConfig cfg)
        {
            EditorGUILayout.LabelField("制作参数", InventoryEditorStyles.Header);

            EditorGUI.BeginChangeCheck();
            float newTime = EditorGUILayout.FloatField(
                new GUIContent("制作时间(秒)", "制作一次需要的时间，进度条按此推进。"), cfg.CraftTime);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改制作时间");
                cfg.CraftTime = Mathf.Max(0f, newTime);
                ctx.MarkDirty();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newMax = EditorGUILayout.IntField(
                new GUIContent("连续制作次数", "单次「制作」动作可连续重复的次数上限；与材料决定的可制作次数取小。-1 = 无限。"),
                cfg.MaxCraftCount);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改连续制作次数");
                cfg.MaxCraftCount = newMax < 0 ? -1 : newMax;
                ctx.MarkDirty();
            }
            EditorGUILayout.LabelField("（-1 = 无限）", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        // ── 制作仓库（有序，按 Index 优先级）────────────────────────────────────────

        private static void DrawCraftInventories(IInventoryEditorContext ctx, ICraftingConfig cfg)
        {
            InventoryRefListDrawer.Draw(ctx, cfg.CraftInventoryRefs, CraftInventoriesDrag,
                "制作仓库", "制作仓库",
                hint: "按上下顺序作为优先级：先从第一个仓库消耗材料 / 放置产出，不足时顺延。",
                emptyHint: "（未配置；消耗材料的来源与产出落点仓库）");
        }

        // ── UI 配置（数字格式 + 属性字段显示）────────────────────────────────────────

        private static void DrawUIConfig(IInventoryEditorContext ctx, ICraftingConfig cfg)
        {
            EditorGUILayout.LabelField("UI 配置", InventoryEditorStyles.Header);

            NumberFormatConfigDrawer.DrawRefPopup(ctx, "数字格式",
                cfg.NumberFormatRef, v => cfg.NumberFormatRef = v);

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("属性字段显示", EditorStyles.boldLabel);
            if (GUILayout.Button("+ 添加", EditorStyles.miniButton, GUILayout.Width(64)))
            {
                ctx.RecordUndo("添加属性字段显示");
                cfg.AttributeDisplays.Add(new CraftingAttributeDisplay());
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("在蓝图条目 / 详情上显示主产出道具的属性值（形如「Label 值」）。",
                EditorStyles.miniLabel);

            var attrIds  = BuildAttrIdOptions(ctx.Database);
            var displays = new string[attrIds.Count + 1];
            displays[0]  = "（无）";
            for (int i = 0; i < attrIds.Count; i++) displays[i + 1] = attrIds[i];

            var list = cfg.AttributeDisplays;

            // 本处沿用两点与其它列表不同的既有行为：插入指示线右侧内缩一个「✕」宽度、删除文案用「删除」而非「移除」。
            EditorDraggableRowList.Draw(ctx, list, AttributeDisplaysDrag, "属性字段显示",
                (_, ad) =>
                {
                    EditorGUI.BeginChangeCheck();
                    string newLabel = EditorGUILayout.TextField(ad.label, GUILayout.Width(120));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ctx.RecordUndo("修改属性显示标签");
                        ad.label = newLabel;
                        ctx.MarkDirty();
                    }

                    int curIdx = 0;
                    for (int k = 0; k < attrIds.Count; k++)
                        if (attrIds[k] == ad.attrId) { curIdx = k + 1; break; }
                    EditorGUI.BeginChangeCheck();
                    int picked = EditorGUILayout.Popup(curIdx, displays);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ctx.RecordUndo("修改属性显示字段");
                        ad.attrId = picked <= 0 ? string.Empty : attrIds[picked - 1];
                        ctx.MarkDirty();
                    }
                },
                removeUndoLabel: "删除属性字段显示",
                lineRightInset:  EditorDraggableRowList.RemoveButtonWidth);
        }

        // ── 蓝图实例：制作仓库 + UI 配置 只读展示（模板级配置）────────────────────────

        private static void DrawInheritedConfigReadonly(IInventoryEditorContext ctx, CraftingBlueprint bp)
        {
            var db   = ctx.Database;
            var tmpl = db.GetCraftingBlueprintTemplate(bp.templateRef);

            // 制作仓库（只读）
            EditorGUILayout.LabelField("制作仓库", InventoryEditorStyles.Header);
            EditorGUILayout.LabelField("由蓝图模板配置，蓝图条目不可修改（仅展示）。", EditorStyles.miniLabel);
            DrawInventoryRefsReadonly(db, bp.craftInventoryRefs);

            EditorGUILayout.Space(6);

            // UI 配置（只读）
            EditorGUILayout.LabelField("UI 配置", InventoryEditorStyles.Header);
            EditorGUILayout.LabelField("由蓝图模板配置，蓝图条目不可修改（仅展示）。", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("数字格式",
                    string.IsNullOrEmpty(bp.numberFormatRef) ? "（无）" : bp.numberFormatRef);

            EditorGUILayout.LabelField("属性字段显示", EditorStyles.boldLabel);
            if (bp.attributeDisplays == null || bp.attributeDisplays.Count == 0)
            {
                EditorGUILayout.LabelField("（无）", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var ad in bp.attributeDisplays)
                {
                    string label  = string.IsNullOrEmpty(ad.label)  ? "（无标签）"   : ad.label;
                    string attrId = string.IsNullOrEmpty(ad.attrId) ? "（未选属性）" : ad.attrId;
                    EditorGUILayout.LabelField($"• {label}　{attrId}", EditorStyles.miniLabel);
                }
            }

            // 来源模板提示
            if (string.IsNullOrEmpty(bp.templateRef))
                EditorGUILayout.LabelField("（无来源模板：为蓝图指定模板后，可在该模板中配置以上项）",
                    EditorStyles.miniLabel);
            else if (tmpl == null)
                EditorGUILayout.LabelField($"⚠ 来源模板「{bp.templateRef}」不存在",
                    InventoryEditorStyles.StatusError);
        }

        private static void DrawInventoryRefsReadonly(InventoryDatabase db, List<string> refs)
        {
            if (refs == null || refs.Count == 0)
            {
                EditorGUILayout.LabelField("（未配置）", EditorStyles.miniLabel);
                return;
            }
            for (int i = 0; i < refs.Count; i++)
            {
                bool exists = db.GetInventory(refs[i]) != null;
                EditorGUILayout.LabelField($"{i + 1}. " + (exists ? refs[i] : refs[i] + "（已删除）"),
                    exists ? EditorStyles.miniLabel : InventoryEditorStyles.StatusError);
            }
        }

        /// <summary>收集数据库中所有属性字段 id（来自道具模板与功能标签，去重保序）。</summary>
        private static List<string> BuildAttrIdOptions(InventoryDatabase db)
        {
            var ids  = new List<string>();
            var seen = new HashSet<string>();
            void Collect(List<AttributeDefinition> defs)
            {
                if (defs == null) return;
                foreach (var def in defs)
                    if (!string.IsNullOrEmpty(def.id) && seen.Add(def.id))
                        ids.Add(def.id);
            }
            foreach (var tmpl in db.ItemTemplates) Collect(tmpl.attributes);
            foreach (var tag in db.FunctionTags)   Collect(tag.attributes);
            return ids;
        }
    }
}

using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 实体 Inspector 的共用绘制片段：ID 行（含重复高亮）、只读「来源模板」行、自定义属性列表。
    /// 六个实体 Inspector（道具 / 仓库 / 商店 / 蓝图 / 装备组 / 技能）此前各写了一遍，
    /// 只差实体类型、重复 ID 集合与 Undo 文案。
    /// </summary>
    public static class EditorEntityHeader
    {
        /// <summary>「⚠ ID 重复或为空」的默认提示文案。</summary>
        public const string DefaultDuplicateHint = "⚠ ID 重复或为空";

        /// <summary>
        /// ID 输入行：重复 / 空 ID 以红色输入框高亮，并在下方给出提示。
        /// 改动经 <paramref name="setId"/> 写回（内部已做 <c>RecordUndo</c> + <c>MarkDirty</c>）。
        /// </summary>
        /// <param name="noun">实体名词，用于 Undo 文案「修改{noun} ID」。</param>
        /// <param name="duplicateIds">该系统的重复 ID 集合（空 ID 以 <see cref="string.Empty"/> 参与判定）。</param>
        /// <param name="dupHint">重复时的提示文案；默认 <see cref="DefaultDuplicateHint"/>。</param>
        public static void DrawIdField(IInventoryEditorContext ctx, string noun,
            string currentId, ICollection<string> duplicateIds, Action<string> setId,
            string dupHint = DefaultDuplicateHint)
        {
            bool isDup = duplicateIds != null && duplicateIds.Contains(
                string.IsNullOrWhiteSpace(currentId) ? string.Empty : currentId);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("ID");
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField(
                currentId, isDup ? InventoryEditorStyles.RedField : EditorStyles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo($"修改{noun} ID");
                setId(newId);
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            if (isDup)
                EditorGUILayout.LabelField(dupHint, InventoryEditorStyles.StatusError);
        }

        /// <summary>只读「来源模板」行（创建后不可更改；为空显示「（无）」）。</summary>
        public static void DrawTemplateRefReadonly(string templateRef)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("来源模板",
                    string.IsNullOrEmpty(templateRef) ? "（无）" : templateRef);
        }

        /// <summary>
        /// 「自定义属性」区：标题 + 逐条绘制实体属性值（枚举字段自动解析其枚举类型）。
        /// <para>模板属性定义先建一次字典再查 —— 此前五处都是 <c>values × attributes</c> 的嵌套线性扫描，
        /// 且发生在**每帧** OnGUI 上。</para>
        /// </summary>
        /// <param name="values">实体自身的属性值列表。</param>
        /// <param name="templateAttrs">来源模板的属性定义列表；可为 null（此时枚举类型解析不到，按无定义绘制）。</param>
        /// <param name="emptyHint">列表为空时的提示文案。</param>
        /// <param name="emptyStyle">空提示样式；null = <see cref="EditorStyles.miniLabel"/>。</param>
        public static void DrawCustomAttributes(IInventoryEditorContext ctx,
            List<AttributeEntry> values, List<AttributeDefinition> templateAttrs,
            string emptyHint, GUIStyle emptyStyle = null)
        {
            EditorGUILayout.LabelField("自定义属性", InventoryEditorStyles.Header);

            if (values == null || values.Count == 0)
            {
                EditorGUILayout.LabelField(emptyHint, emptyStyle ?? EditorStyles.miniLabel);
                return;
            }

            Dictionary<string, AttributeDefinition> defs = null;
            if (templateAttrs != null && templateAttrs.Count > 0)
            {
                defs = new Dictionary<string, AttributeDefinition>(templateAttrs.Count);
                foreach (var d in templateAttrs)
                    if (d != null && !string.IsNullOrEmpty(d.id)) defs[d.id] = d;
            }

            var db = ctx.Database;
            foreach (var entry in values)
            {
                AttributeDefinition def = null;
                if (defs != null && !string.IsNullOrEmpty(entry.id))
                    defs.TryGetValue(entry.id, out def);

                var enumType = def != null && def.type == EFieldType.Enum
                    ? db.GetEnumType(def.enumTypeRef) : null;
                AttributeFieldDrawer.Draw(ctx, entry.id, entry.value, enumType);
            }
        }
    }
}

using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 绘制并编辑单个 <see cref="AttributeDefinition"/>：显示名、key、类型、是否数组、枚举类型引用、默认值。
    /// </summary>
    public static class AttributeDefinitionDrawer
    {
        #region Layout 绘制

        public static void Draw(IInventoryEditorContext ctx, AttributeDefinition def)
        {
            if (def == null) return;
            var db = ctx.Database;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();
            string attrId = EditorGUILayout.TextField("ID", def.id);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改属性定义");
                def.id = attrId;
                ctx.MarkDirty();
            }

            // 类型 + 是否数组（LocalizedString 仅在 IS_LOCALIZATION 启用时可选）
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var type = DrawFieldTypePopup(Tr("类型"), def.type);
            bool isArray = GUILayout.Toggle(def.isArray, Tr("数组"), GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改属性类型");
                bool typeChanged = type != def.type;
                bool arrayChanged = isArray != def.isArray;
                def.type = type;
                def.isArray = isArray;
                if (typeChanged)
                    def.defaultValue.ChangeType(type, isArray, def.enumTypeRef);
                else if (arrayChanged)
                    def.defaultValue.SetIsArray(isArray);
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // 枚举类型引用（Enum / EnumIntPair）
            if (def.type == EFieldType.Enum || def.type == EFieldType.EnumIntPair)
                DrawEnumTypeRef(ctx, def, db);

            // 默认值
            EditorGUILayout.LabelField(Tr("默认值"), EditorStyles.miniBoldLabel);
            var enumType = (def.type == EFieldType.Enum || def.type == EFieldType.EnumIntPair)
                ? db?.GetEnumType(def.enumTypeRef) : null;
            AttributeFieldDrawer.Draw(ctx, Tr("默认"), def.defaultValue, enumType);

            EditorGUILayout.EndVertical();
        }
        
        // ─────────────────────────────────────────────────────────────────────────
        // Rect-based drawing（供 ReorderableList.drawElementCallback 使用）
        // 纯 EditorGUI.* 实现，不调用任何 GUILayout API，不向父 GUILayoutGroup 注册槽位。
        // ─────────────────────────────────────────────────────────────────────────

        #endregion

        #region Rect 绘制

        /// <summary>
        /// 在指定 <paramref name="rect"/> 内绘制单个 <see cref="AttributeDefinition"/>。
        /// 不调用任何 GUILayout / BeginArea API，可安全用于 <c>drawElementCallback</c>。
        /// </summary>
        public static void DrawRect(IInventoryEditorContext ctx, AttributeDefinition def, Rect rect)
        {
            if (def == null) return;
            var db = ctx.Database;

            // 暂时缩小 labelWidth，让字段列更宽
            float prevLw = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 56f;

            float lh  = EditorGUIUtility.singleLineHeight;
            float sp  = EditorGUIUtility.standardVerticalSpacing;
            float row = lh + sp;
            const float pad = 4f;

            // 外框背景
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            float x = rect.x + pad;
            float y = rect.y + pad;
            float w = rect.width - pad * 2f;

            // ── ID ────────────────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string newId = EditorGUI.TextField(new Rect(x, y, w, lh), "ID", def.id);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改属性定义");
                def.id = newId;
                ctx.MarkDirty();
            }
            y += row;

            // ── 类型 + 数组 Toggle ────────────────────────────────────────────
            float typeW = w - 60f;
            EditorGUI.BeginChangeCheck();
            EFieldType newType  = DrawFieldTypePopupRect(new Rect(x, y, typeW, lh), Tr("类型"), def.type);
            bool       newArray = EditorGUI.ToggleLeft(
                new Rect(x + typeW + 2f, y, 58f, lh), Tr("数组"), def.isArray);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改属性类型");
                bool tc = newType  != def.type;
                bool ac = newArray != def.isArray;
                def.type    = newType;
                def.isArray = newArray;
                if (tc)      def.defaultValue.ChangeType(newType, newArray, def.enumTypeRef);
                else if (ac) def.defaultValue.SetIsArray(newArray);
                ctx.MarkDirty();
            }
            y += row;

            // ── 枚举类型引用（Enum / EnumIntPair）─────────────────────────────
            if (def.type == EFieldType.Enum || def.type == EFieldType.EnumIntPair)
            {
                DrawEnumTypeRefRect(ctx, def, db, new Rect(x, y, w, lh));
                y += row;
            }

            // ── "默认值" 标签 ─────────────────────────────────────────────────
            EditorGUI.LabelField(new Rect(x, y, w, lh), Tr("默认值"), EditorStyles.miniBoldLabel);
            y += row;

            // ── 默认值字段 ────────────────────────────────────────────────────
            float dvH = rect.yMax - y - pad;
            if (dvH > 0f)
            {
                var enumType = (def.type == EFieldType.Enum || def.type == EFieldType.EnumIntPair)
                    ? db?.GetEnumType(def.enumTypeRef) : null;
                AttributeFieldDrawer.DrawRect(ctx, Tr("默认"), def.defaultValue, enumType,
                    new Rect(x, y, w, dvH));
            }

            EditorGUIUtility.labelWidth = prevLw;
        }
        
        /// <summary>
        /// Rect-based 字段类型弹窗。Text 始终可选：纯文本 fallback 恒在，
        /// 启用 IS_LOCALIZATION 宏时额外提供本地化（表 / 条目）选择器。
        /// </summary>
        private static EFieldType DrawFieldTypePopupRect(Rect rect, string label, EFieldType current)
        {
            var types = new List<EFieldType>
            {
                EFieldType.Bool, EFieldType.Int, EFieldType.Float, EFieldType.String, EFieldType.Text,
                EFieldType.Enum,
                EFieldType.Vector2, EFieldType.Vector3, EFieldType.Vector4,
                EFieldType.VectorInt2, EFieldType.VectorInt3, EFieldType.VectorInt4,
                EFieldType.StringIntPair, EFieldType.EnumIntPair,
                EFieldType.Color, EFieldType.Prefab, EFieldType.Sprite, EFieldType.Texture, EFieldType.Material,
                EFieldType.AudioClip, EFieldType.AnimationClip, EFieldType.AnimationCurve,
                EFieldType.PhysicsMaterial, EFieldType.PhysicsMaterial2D,
            };
            var names = new string[types.Count];
            for (int i = 0; i < types.Count; i++) names[i] = TrEnum(types[i]);

            int idx = types.IndexOf(current);
            if (idx < 0)
            {
                EditorGUI.LabelField(rect, label, Fmt("{0}（未知类型）", TrEnum(current)));
                return current;
            }
            int picked = EditorGUI.Popup(rect, label, idx, names);
            
            return (picked >= 0 && picked < types.Count) ? types[picked] : current;
        }

        /// <summary>Rect-based 枚举类型引用弹窗。</summary>
        private static void DrawEnumTypeRefRect(
            IInventoryEditorContext ctx, AttributeDefinition def, InventoryDatabase db, Rect rect)
        {
            var names = new List<string>();
            if (db)
                foreach (var e in db.EnumTypes)
                    names.Add(e.name);

            int current = names.IndexOf(def.enumTypeRef);
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(rect, Tr("枚举类型"), current, names.ToArray());
            if (EditorGUI.EndChangeCheck() && picked >= 0 && picked < names.Count)
            {
                ctx.RecordUndo("修改枚举类型引用");
                def.enumTypeRef = names[picked];
                def.defaultValue.EnumTypeRef = names[picked];
                ctx.MarkDirty();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 原有 GUILayout-based 私有辅助（供 Draw 使用，保持不变）
        // ─────────────────────────────────────────────────────────────────────────

        private static EFieldType DrawFieldTypePopup(string label, EFieldType current)
        {
            // GUILayout 版字段类型弹窗。
            var types = new List<EFieldType>
            {
                EFieldType.Bool, EFieldType.Int, EFieldType.Float, EFieldType.String, EFieldType.Text,
                EFieldType.Enum,
                EFieldType.Vector2, EFieldType.Vector3, EFieldType.Vector4,
                EFieldType.VectorInt2, EFieldType.VectorInt3, EFieldType.VectorInt4,
                EFieldType.StringIntPair, EFieldType.EnumIntPair,
                EFieldType.Color, EFieldType.Prefab, EFieldType.Sprite, EFieldType.Texture, EFieldType.Material,
                EFieldType.AudioClip, EFieldType.AnimationClip, EFieldType.AnimationCurve,
                EFieldType.PhysicsMaterial, EFieldType.PhysicsMaterial2D,
            };
            var names = new string[types.Count];
            for (int i = 0; i < types.Count; i++) names[i] = TrEnum(types[i]);

            int idx = types.IndexOf(current);
            if (idx < 0)
            {
                // 当前值为 LocalizedString 但宏未启用：灰色只读显示
                EditorGUILayout.LabelField(label, Fmt("{0}（未知类型）", TrEnum(current)));
                return current;
            }

            int picked = EditorGUILayout.Popup(label, idx, names);
            
            return (picked >= 0 && picked < types.Count) ? types[picked] : current;
        }

        private static void DrawEnumTypeRef(IInventoryEditorContext ctx, AttributeDefinition def, InventoryDatabase db)
        {
            var names = new List<string>();
            if (db)
                foreach (var e in db.EnumTypes)
                    names.Add(e.name);

            int current = names.IndexOf(def.enumTypeRef);
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup(Tr("枚举类型"), current, names.ToArray());
            if (EditorGUI.EndChangeCheck() && picked >= 0 && picked < names.Count)
            {
                ctx.RecordUndo("修改枚举类型引用");
                def.enumTypeRef = names[picked];
                def.defaultValue.EnumTypeRef = names[picked];
                ctx.MarkDirty();
            }
        }
        #endregion

    }
}

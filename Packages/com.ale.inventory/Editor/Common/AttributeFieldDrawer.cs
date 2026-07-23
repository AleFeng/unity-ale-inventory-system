using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 按 <see cref="EFieldType"/> 绘制单个 <see cref="AttributeValue"/>（支持标量与数组形态）。
    /// 修改时通过 <see cref="IInventoryEditorContext"/> 记录 Undo 并标记脏。
    ///
    /// Text 类型需独立垂直绘制：纯文本 fallback 一行 + 原生本地化选择器（复合子控件，
    /// 放入 BeginHorizontal 会导致布局错乱、后续控件消失）。
    /// </summary>
    public static class AttributeFieldDrawer
    {
        // ─────────────────────────────────────────────────────────────────────
        // AssetReference 授权注入点（IS_ADDRESSABLE）
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// AssetReference 授权字段绘制器。由受 IS_ADDRESSABLE 约束的 Addressable 编辑器程序集在
        /// <c>[InitializeOnLoad]</c> 时注入；为 null（宏未启用 / 包未装）时对象字段回退为普通 ObjectField。
        /// core 编辑器程序集对 Addressables 零依赖——此处仅持有一个纯接口引用，与
        /// <see cref="InventoryExportResolver.AddressableProvider"/> 同构。
        /// </summary>
        public static IAddressableAssetFieldDrawer AddressableFieldDrawer;

        /// <summary>是否已注入 AssetReference 授权绘制器（IS_ADDRESSABLE 启用且包可用）。</summary>
        public static bool HasAddressableDrawer => AddressableFieldDrawer != null;

        /// <summary>
        /// 对象引用字段在 rect 模式下的行高：授权模式据原生 AssetReference 绘制器求得（可能多行），
        /// 否则单行。供列表 / 数组高度回调与 <see cref="AttributeDefinitionListDrawer"/> 保持一致。
        /// </summary>
        public static float GetObjectRowHeight(AttributeValue value, int index)
            => AddressableFieldDrawer != null
                ? AddressableFieldDrawer.GetHeight(value, index)
                : EditorGUIUtility.singleLineHeight;

        /// <summary>
        /// 单个逻辑元素在<b>数组行</b>中占用的高度：Text 走 <see cref="GetTextRowHeight"/>（含本地化选择器），
        /// 对象类走 <see cref="GetObjectRowHeight"/>（授权模式随选择器变高），其余为单行。
        /// <para>Rect 路径（<see cref="DrawRect"/>）与 Layout 路径（<see cref="DrawElementField"/>）此前各算一遍，现收口于此。</para>
        /// </summary>
        private static float RowHeight(AttributeValue value, int index)
            => value.Type == EFieldType.Text ? GetTextRowHeight(value, index)
             : value.Type.IsObjectBacked()   ? GetObjectRowHeight(value, index)
             : EditorGUIUtility.singleLineHeight;

        /// <summary><see cref="EFieldType"/> 对象类型 → 对应 Unity 资源 <see cref="System.Type"/>。</summary>
        private static System.Type ObjectTypeFor(EFieldType type)
        {
            switch (type)
            {
                case EFieldType.Sprite:            return typeof(Sprite);
                case EFieldType.Prefab:            return typeof(GameObject);
                case EFieldType.Texture:           return typeof(Texture);
                case EFieldType.Material:          return typeof(Material);
                case EFieldType.AudioClip:         return typeof(AudioClip);
                case EFieldType.AnimationClip:     return typeof(AnimationClip);
#if UNITY_6000_0_OR_NEWER
                case EFieldType.PhysicsMaterial:   return typeof(PhysicsMaterial);
#else
                case EFieldType.PhysicsMaterial:   return typeof(PhysicMaterial);
#endif
                case EFieldType.PhysicsMaterial2D: return typeof(PhysicsMaterial2D);
                default:                           return typeof(Object);
            }
        }

        /// <summary>
        /// 绘制对象引用字段（rect 版）。授权模式（已注入）走原生 AssetReference 选择器——
        /// GUID 存入授权地址、objRefs 对应槽置空（不硬引用资源）；否则普通 <c>ObjectField</c>（直接存 objRefs）。
        /// </summary>
        private static void DrawObjectFieldRect(IInventoryEditorContext ctx, AttributeValue value,
            int index, System.Type objType, Rect rect)
        {
            if (AddressableFieldDrawer != null)
            {
                if (AddressableFieldDrawer.Draw(rect, value, index, objType, null, out string guid))
                    Apply(ctx, () => { value.SetObjAddress(index, guid); value.SetObject(index, null); });
                return;
            }
            EditorGUI.BeginChangeCheck();
            var v = EditorGUI.ObjectField(rect, value.GetObject(index), objType, false);
            if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetObject(index, v));
        }

        /// <summary>
        /// 授权地址（<c>GUID</c>；子资源为 <c>GUID[子名]</c>）→ 资源对象，仅供编辑器内预览用。
        /// 纯 <see cref="AssetDatabase"/> 实现（core 对 Addressables 零依赖，无需真正走 Addressables 加载）；
        /// 地址为空或解析失败返回 null。
        /// </summary>
        private static Object ResolveAssetFromAddress(string address, System.Type type)
        {
            if (string.IsNullOrEmpty(address)) return null;

            string guidPart = address;
            string subName  = null;
            int lb = address.IndexOf('[');
            if (lb >= 0 && address.EndsWith("]"))
            {
                guidPart = address.Substring(0, lb);
                subName  = address.Substring(lb + 1, address.Length - lb - 2);
            }

            string path = AssetDatabase.GUIDToAssetPath(guidPart);
            if (string.IsNullOrEmpty(path)) return null;

            // 子资源（如图集中的子 Sprite）：在该路径下按名字匹配。
            if (!string.IsNullOrEmpty(subName))
            {
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (a && a.name == subName && type.IsInstanceOfType(a)) return a;
                return null;
            }
            return AssetDatabase.LoadAssetAtPath(path, type);
        }

        /// <summary>
        /// 绘制一个属性值。<paramref name="enumType"/> 仅在类型为 Enum 时使用（可为 null）。
        /// </summary>
        public static void Draw(IInventoryEditorContext ctx, string label, AttributeValue value, EnumType enumType)
        {
            if (value == null) return;

            enumType = ResolveEnumType(ctx, value, enumType);

            // 包一层垂直组以取得整条属性的矩形，用于悬停 Ctrl+C / Ctrl+V 与右键复制 / 粘贴。
            Rect rowRect = EditorGUILayout.BeginVertical();
            DrawBody(ctx, label, value, enumType);
            EditorGUILayout.EndVertical();

            HandleEntryCopyPaste(ctx, value, rowRect);
        }

        /// <summary>实际绘制属性值内容（不含复制 / 粘贴交互）。</summary>
        private static void DrawBody(IInventoryEditorContext ctx, string label, AttributeValue value, EnumType enumType)
        {
            if (value == null) return;

            if (!value.IsArray)
            {
                EnsureCount(value, 1);

                // Text：纯文本 fallback + 原生本地化选择器，必须独立垂直绘制
                if (value.Type == EFieldType.Text)
                {
                    EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    DrawTextFieldLayout(ctx, value, 0);
                    EditorGUI.indentLevel--;
                    return;
                }
                // Sprite 非数组：3 行高度 + 左对齐正方形预览（与模板 Inspector 保持一致）
                if (value.Type == EFieldType.Sprite)
                {
                    // 授权模式：上 = 标签 + 原生 AssetReference 选择器（整行宽，便于搜索）；
                    //           下 = 左对齐正方形预览（与非授权模式一致，可直接拖入 Sprite 差替 → 自动转 GUID 授权）。
                    if (AddressableFieldDrawer != null)
                    {
                        float lw = EditorGUIUtility.labelWidth;

                        // 上：标签 + 选择器
                        float hh = AddressableFieldDrawer.GetHeight(value, 0);
                        var rr   = EditorGUILayout.GetControlRect(true, hh);
                        EditorGUI.LabelField(new Rect(rr.x, rr.y, lw, EditorGUIUtility.singleLineHeight), label);
                        DrawObjectFieldRect(ctx, value, 0, typeof(Sprite),
                            new Rect(rr.x + lw, rr.y, rr.width - lw, hh));

                        // 下：左对齐正方形预览（3 行高）。授权模式无实引用，从当前授权地址解析；
                        // 尚未迁移的残留实引用优先显示。拖入新 Sprite → 经注入实现转 GUID（+登记进分组）后授权、objRefs 置空。
                        float lhP  = EditorGUIUtility.singleLineHeight;
                        float sqH  = lhP * 3f;
                        var preRect = EditorGUILayout.GetControlRect(true, sqH);
                        var current = value.GetObject(0) as Sprite
                                      ?? ResolveAssetFromAddress(value.GetObjAddress(0), typeof(Sprite)) as Sprite;
                        float sqAddr   = Mathf.Min(sqH, Mathf.Max(preRect.width - lw, 0f));
                        // 与非授权模式同：预先反向补偿 ObjectField 内部 IndentedRect 的缩进偏移。
                        float indentP = EditorGUI.indentLevel * 15f;
                        var sqRect = new Rect(preRect.x + lw, preRect.y, sqAddr + indentP, sqAddr);
                        EditorGUI.BeginChangeCheck();
                        var dropped = EditorGUI.ObjectField(sqRect, current, typeof(Sprite), false) as Sprite;
                        if (EditorGUI.EndChangeCheck())
                        {
                            string key = dropped ? (AddressableFieldDrawer.ObjectToKey(dropped) ?? string.Empty) : string.Empty;
                            Apply(ctx, () => { value.SetObjAddress(0, key); value.SetObject(0, null); });
                        }
                        return;
                    }
                    float lhS   = EditorGUIUtility.singleLineHeight;
                    float totalH = lhS * 3f;
                    var fullRect = EditorGUILayout.GetControlRect(true, totalH);
                    float lW     = EditorGUIUtility.labelWidth;
                    EditorGUI.LabelField(new Rect(fullRect.x, fullRect.y, lW, lhS), label);
                    float sq = Mathf.Min(totalH, Mathf.Max(fullRect.width - lW, 0f));

                    // EditorGUI.ObjectField 内部调用 IndentedRect(position)，
                    // 会将传入 rect 向右偏移 EditorGUIUtility.indent（= indentLevel × 15px）并削减等量宽度。
                    // 在此预先反向补偿：x 左移 indent，width 加 indent，
                    // 经内部 IndentedRect 后恰好还原为目标正方形 rect。
                    float indent = EditorGUI.indentLevel * 15f;
                    var objRect = new Rect(fullRect.x + lW, fullRect.y, sq + indent, sq);
                    EditorGUI.BeginChangeCheck();
                    var sv = EditorGUI.ObjectField(objRect, value.GetObject(0), typeof(Sprite), false);
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetObject(0, sv));
                    return;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(label);
                DrawElementField(ctx, value, enumType, 0);
                EditorGUILayout.EndHorizontal();
                return;
            }

            // 数组形态。
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{label}  [{value.Count}]", EditorStyles.boldLabel);
            if (GUILayout.Button("添加", GUILayout.Width(48)))
            {
                ctx.RecordUndo("添加数组元素");
                value.AddElement();
                ctx.MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            // 左侧 ≡ 句柄拖拽重排：每个数组值一份拖拽状态（弱引用缓存，随值回收）。
            var drag = GetArrayDrag(value);
            drag.BeginFrame();
            int removeAt = -1;

            for (int i = 0; i < value.Count; i++)
            {
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                drag.DrawHandleColumn(i);            // 左侧 ≡ 拖拽句柄
                EditorGUILayout.BeginVertical();

                if (value.Type == EFieldType.Text)
                {
                    // 索引标签 + 删除按钮在同一行，Text 的文本框 / 本地化选择器另起一行
                    EditorGUILayout.BeginHorizontal();
                    DrawArrayIndex(i);
                    GUILayout.FlexibleSpace();
                    bool delTx = GUILayout.Button("✕", GUILayout.Width(22));
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel++;
                    DrawTextFieldLayout(ctx, value, i);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(2);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    drag.RecordRow(i, rowRect);
                    if (delTx) { removeAt = i; break; }
                    continue;
                }
                EditorGUILayout.BeginHorizontal();
                DrawArrayIndex(i);
                DrawElementField(ctx, value, enumType, i);
                bool del = GUILayout.Button("✕", GUILayout.Width(22));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                drag.RecordRow(i, rowRect);
                if (del) { removeAt = i; break; }
            }

            if (removeAt >= 0)
            {
                ctx.RecordUndo("移除数组元素");
                value.RemoveElement(removeAt);
                ctx.MarkDirty();
            }
            else
            {
                // 无结构变更时处理拖拽落点：把新顺序应用到属性值（EndFrame 已记录 Undo）。
                _reorderProxy.Clear();
                for (int i = 0; i < value.Count; i++) _reorderProxy.Add(i);
                if (drag.EndFrame(ctx, _reorderProxy, "移动数组元素",
                        EditorReorderableDrag.HandleWidth, 24f))
                    value.ReorderElements(_reorderProxy);
            }

            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Rect-based drawing（供 ReorderableList.drawElementCallback 使用）
        // 不调用任何 GUILayout API。
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 在 <paramref name="rect"/> 内绘制属性值（非数组=单行；数组=内嵌 helpBox）。
        /// 纯 <c>EditorGUI.*</c> 实现，不调用任何 GUILayout API。
        /// </summary>
        public static void DrawRect(IInventoryEditorContext ctx, string label,
            AttributeValue value, EnumType enumType, Rect rect)
        {
            if (value == null) return;

            enumType = ResolveEnumType(ctx, value, enumType);

            float lh  = EditorGUIUtility.singleLineHeight;
            float sp  = EditorGUIUtility.standardVerticalSpacing;
            float row = lh + sp;

            if (!value.IsArray)
            {
                EnsureCount(value, 1);
                if (value.Type == EFieldType.Text)
                {
                    DrawTextRect(ctx, value, 0, label, rect);
                    return;
                }
                DrawLabeledFieldControlRect(ctx, value, enumType, 0, rect, label);
                return;
            }

            // ── 数组形态 ─────────────────────────────────────────────────────
            const float pad = 4f;
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            float x = rect.x + pad;
            float y = rect.y + pad;
            float w = rect.width - pad * 2f;

            // 标题行：label [N] + 添加
            EditorGUI.LabelField(
                new Rect(x, y, w - 54f, lh),
                $"{label}  [{value.Count}]",
                EditorStyles.boldLabel);
            if (GUI.Button(new Rect(x + w - 52f, y, 50f, lh), "添加", EditorStyles.miniButton))
            {
                ctx.RecordUndo("添加数组元素");
                value.AddElement();
                ctx.MarkDirty();
            }
            y += row;

            for (int i = 0; i < value.Count; i++)
            {
                const float idxW = 30f, delW = 22f, gap = 2f;
                float fieldW = w - idxW - delW - gap * 2f;
                float elemH  = RowHeight(value, i);

                var idxR   = new Rect(x,              y, idxW,   lh);
                var fieldR = new Rect(x + idxW + gap, y, fieldW, elemH);
                var delR   = new Rect(x + w - delW,   y, delW,   lh);

                EditorGUI.LabelField(idxR, $"[{i}]");
                if (value.Type == EFieldType.Text)
                    DrawTextRect(ctx, value, i, null, fieldR);
                else
                    DrawFieldControlRect(ctx, value, enumType, i, fieldR);

                if (GUI.Button(delR, "✕", EditorStyles.miniButtonRight))
                {
                    ctx.RecordUndo("移除数组元素");
                    value.RemoveElement(i);
                    ctx.MarkDirty();
                    break;
                }
                y += elemH + sp;
            }
        }

        /// <summary>
        /// 绘制带标签的单行字段（非数组场景）。
        /// <para>
        /// 采用<b>手动分割</b>方式：先用 <see cref="EditorGUI.LabelField"/> 绘制标签，
        /// 再把剩余宽度传给 <see cref="DrawFieldControlRect"/>（无标签版）。<br/>
        /// 这样避免了 <see cref="EditorGUI.Vector2Field"/>/<see cref="EditorGUI.ObjectField"/> 等
        /// 控件内部依赖 <c>EditorGUIUtility.labelWidth</c> 进行折行或布局分割时产生的
        /// "挤到右侧" / "溢出到下方" 问题。
        /// </para>
        /// </summary>
        private static void DrawLabeledFieldControlRect(
            IInventoryEditorContext ctx, AttributeValue value, EnumType enumType,
            int index, Rect rect, string label)
        {
            float lh = EditorGUIUtility.singleLineHeight;

            // ── 手动计算标签宽度并绘制标签 ──────────────────────────────────────
            // 使用当前 labelWidth（由调用链中的 DrawRect 已设为 56f），
            // 但限制为 rect 宽度的 40%，保证字段区始终有足够空间。
            float lW = Mathf.Min(EditorGUIUtility.labelWidth, rect.width * 0.4f);
            lW = Mathf.Max(lW, 0f);

            if (!string.IsNullOrEmpty(label))
                EditorGUI.LabelField(new Rect(rect.x, rect.y, lW, lh), label);
            else
                lW = 0f;

            // ── 字段区（无标签版，宽度 = 剩余空间）────────────────────────────────
            // DrawFieldControlRect 内部所有控件均使用无标签版（Vector2Field(GUIContent.none)、
            // ObjectField 无标签重载等），不受 labelWidth 影响，布局完全由给定 rect 决定。
            DrawFieldControlRect(ctx, value, enumType, index,
                new Rect(rect.x + lW, rect.y, rect.width - lW, rect.height));
        }

        /// <summary>绘制无标签的字段 control（数组元素，标签由调用方绘制）。</summary>
        private static void DrawFieldControlRect(
            IInventoryEditorContext ctx, AttributeValue value, EnumType enumType,
            int index, Rect rect)
        {
            switch (value.Type)
            {
                case EFieldType.Int:
                {
                    EditorGUI.BeginChangeCheck();
                    int v = EditorGUI.IntField(rect, value.GetInt(index));
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetInt(index, v));
                    break;
                }
                case EFieldType.Float:
                {
                    EditorGUI.BeginChangeCheck();
                    float v = EditorGUI.FloatField(rect, value.GetFloat(index));
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetFloat(index, v));
                    break;
                }
                case EFieldType.String:
                {
                    EditorGUI.BeginChangeCheck();
                    string v = EditorGUI.TextField(rect, value.GetString(index));
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetString(index, v));
                    break;
                }
                case EFieldType.Bool:
                {
                    EditorGUI.BeginChangeCheck();
                    bool v = EditorGUI.Toggle(rect, value.GetInt(index) != 0);
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetInt(index, v ? 1 : 0));
                    break;
                }
                case EFieldType.Enum:
                    DrawEnumPopupRect(ctx, value, enumType, index, rect, null);
                    break;
                case EFieldType.Vector2:
                {
                    EditorGUI.BeginChangeCheck();
                    Vector2 v = EditorGUI.Vector2Field(rect, GUIContent.none, value.GetVector2(index));
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetVector2(index, v));
                    break;
                }
                case EFieldType.Vector3:
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 v = EditorGUI.Vector3Field(rect, GUIContent.none, value.GetVector3(index));
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetVector3(index, v));
                    break;
                }
                case EFieldType.Vector4:
                {
                    EditorGUI.BeginChangeCheck();
                    Vector4 v = EditorGUI.Vector4Field(rect, GUIContent.none, value.GetVector4(index));
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetVector4(index, v));
                    break;
                }
                case EFieldType.Color:
                {
                    EditorGUI.BeginChangeCheck();
                    Color v = EditorGUI.ColorField(rect, value.GetColor(index));
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetColor(index, v));
                    break;
                }
                case EFieldType.Sprite:
                case EFieldType.Prefab:
                case EFieldType.Texture:
                case EFieldType.Material:
                case EFieldType.AudioClip:
                case EFieldType.AnimationClip:
                case EFieldType.PhysicsMaterial:
                case EFieldType.PhysicsMaterial2D:
                {
                    // 直接模式下 Sprite 保留左对齐正方形预览（边长 = rect.height，外部已分配 3×lh）；
                    // 其余对象类型 / 授权模式统一走 DrawObjectFieldRect（原生 AssetReference 或 ObjectField）。
                    Rect r = (value.Type == EFieldType.Sprite && AddressableFieldDrawer == null)
                        ? new Rect(rect.x, rect.y, rect.height, rect.height)
                        : rect;
                    DrawObjectFieldRect(ctx, value, index, ObjectTypeFor(value.Type), r);
                    break;
                }
                case EFieldType.AnimationCurve:
                {
                    var curve = value.GetAnimationCurve(index) ?? DefaultCurve();
                    EditorGUI.BeginChangeCheck();
                    var v = EditorGUI.CurveField(rect, curve);
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetAnimationCurve(index, v));
                    break;
                }
                case EFieldType.VectorInt2:
                {
                    EditorGUI.BeginChangeCheck();
                    Vector2Int v = EditorGUI.Vector2IntField(rect, GUIContent.none, value.GetVector2Int(index));
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetVector2Int(index, v));
                    break;
                }
                case EFieldType.VectorInt3:
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3Int v = EditorGUI.Vector3IntField(rect, GUIContent.none, value.GetVector3Int(index));
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetVector3Int(index, v));
                    break;
                }
                case EFieldType.VectorInt4:
                {
                    // Unity 无原生 Vector4IntField，手动排布 4 个 IntField
                    var (x, y, z, w) = value.GetVector4Int(index);
                    float fw = (rect.width + 1f) / 4f;
                    float fh = rect.height;
                    EditorGUI.BeginChangeCheck();
                    int nx = EditorGUI.IntField(new Rect(rect.x,           rect.y, fw - 1f, fh), x);
                    int ny = EditorGUI.IntField(new Rect(rect.x + fw,       rect.y, fw - 1f, fh), y);
                    int nz = EditorGUI.IntField(new Rect(rect.x + fw * 2f,  rect.y, fw - 1f, fh), z);
                    int nw = EditorGUI.IntField(new Rect(rect.x + fw * 3f,  rect.y, fw - 1f, fh), w);
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetVector4Int(index, nx, ny, nz, nw));
                    break;
                }
                case EFieldType.StringIntPair:
                {
                    // 左 60% 为字符串键，右 40% 为整数值
                    var (key, val) = value.GetStringIntPair(index);
                    float strW = (rect.width - 2f) * 0.6f;
                    float intW = rect.width - strW - 2f;
                    EditorGUI.BeginChangeCheck();
                    string newKey = EditorGUI.TextField(new Rect(rect.x,              rect.y, strW, rect.height), key);
                    int    newVal = EditorGUI.IntField( new Rect(rect.x + strW + 2f,  rect.y, intW, rect.height), val);
                    if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetStringIntPair(index, newKey, newVal));
                    break;
                }
                case EFieldType.EnumIntPair:
                {
                    // 左 60% 为枚举键弹窗，右 40% 为整数值
                    var (_, val) = value.GetEnumIntPair(index);
                    float enumW = (rect.width - 2f) * 0.6f;
                    float intW  = rect.width - enumW - 2f;
                    DrawEnumIntPairPopupRect(ctx, value, enumType, index,
                        new Rect(rect.x, rect.y, enumW, rect.height));
                    EditorGUI.BeginChangeCheck();
                    int newVal = EditorGUI.IntField(new Rect(rect.x + enumW + 2f, rect.y, intW, rect.height), val);
                    if (EditorGUI.EndChangeCheck())
                    {
                        int keepEnum = value.GetEnumIntPair(index).enumValue;
                        Apply(ctx, () => value.SetEnumIntPair(index, keepEnum, newVal));
                    }
                    break;
                }
                default:
                    EditorGUI.LabelField(rect, value.Type.ToString());
                    break;
            }
        }

        /// <summary>Rect-based 枚举弹窗（<paramref name="label"/> 为 null 时不绘制标签列）。</summary>
        private static void DrawEnumPopupRect(
            IInventoryEditorContext ctx, AttributeValue value, EnumType enumType,
            int index, Rect rect, string label)
        {
            if (enumType == null || enumType.items.Count == 0)
            {
                string msg = $"<未找到枚举类型 \"{value.EnumTypeRef}\">";
                if (string.IsNullOrEmpty(label))
                    EditorGUI.LabelField(rect, msg, InventoryEditorStyles.StatusError);
                else
                    EditorGUI.LabelField(rect, label, msg, InventoryEditorStyles.StatusError);
                return;
            }

            var names   = new string[enumType.items.Count];
            int current = -1;
            int stored  = value.GetInt(index);
            for (int i = 0; i < enumType.items.Count; i++)
            {
                names[i] = enumType.items[i].name;
                if (enumType.items[i].value == stored) current = i;
            }

            EditorGUI.BeginChangeCheck();
            int picked = string.IsNullOrEmpty(label)
                ? EditorGUI.Popup(rect, current, names)
                : EditorGUI.Popup(rect, label, current, names);
            if (EditorGUI.EndChangeCheck() && picked >= 0 && picked < enumType.items.Count)
            {
                int newVal = enumType.items[picked].value;
                Apply(ctx, () => value.SetInt(index, newVal));
            }
        }

        /// <summary>Rect-based：EnumIntPair 的枚举键弹窗（仅改写 pair 的枚举分量，保留整数分量）。</summary>
        private static void DrawEnumIntPairPopupRect(
            IInventoryEditorContext ctx, AttributeValue value, EnumType enumType, int index, Rect rect)
        {
            if (enumType == null || enumType.items.Count == 0)
            {
                EditorGUI.LabelField(rect, $"<未找到枚举类型 \"{value.EnumTypeRef}\">", InventoryEditorStyles.StatusError);
                return;
            }

            var names   = new string[enumType.items.Count];
            int current = -1;
            int stored  = value.GetEnumIntPair(index).enumValue;
            for (int i = 0; i < enumType.items.Count; i++)
            {
                names[i] = enumType.items[i].name;
                if (enumType.items[i].value == stored) current = i;
            }

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(rect, current, names);
            if (EditorGUI.EndChangeCheck() && picked >= 0 && picked < enumType.items.Count)
            {
                int newEnum = enumType.items[picked].value;
                int keepVal = value.GetEnumIntPair(index).value;
                Apply(ctx, () => value.SetEnumIntPair(index, newEnum, keepVal));
            }
        }

#if IS_LOCALIZATION
        /// <summary>
        /// Text 本地化引用在 rect 模式下的降级实现（仅单行 Entry Key 文本），
        /// 仅当 Unity.Localization 原生属性路径与当前包版本不匹配时使用。
        /// </summary>
        private static void DrawLocalizedStringFallbackRect(
            IInventoryEditorContext ctx, AttributeValue value, int index, string label, Rect rect)
        {
            float lh = EditorGUIUtility.singleLineHeight;
            var (tableRef, entryKey) = value.GetLocalizedStringRef(index);

            // 手动分割标签与输入框
            float lW = 0f;
            if (!string.IsNullOrEmpty(label))
            {
                lW = Mathf.Max(Mathf.Min(EditorGUIUtility.labelWidth, rect.width * 0.4f), 0f);
                EditorGUI.LabelField(new Rect(rect.x, rect.y, lW, lh), label);
            }

            EditorGUI.BeginChangeCheck();
            string newEntry = EditorGUI.TextField(
                new Rect(rect.x + lW, rect.y, rect.width - lW, lh), entryKey);
            if (EditorGUI.EndChangeCheck())
                Apply(ctx, () => value.SetLocalizedStringRef(index, tableRef, newEntry));
        }
#endif

        // ─────────────────────────────────────────────────────────────────────
        // 原有 GUILayout-based 私有辅助（供 Draw 使用，保持不变）
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>绘制指定元素索引的字段（无标签）。</summary>
        /// <summary>
        /// Layout 路径的单元素绘制：取一个 <see cref="EditorGUILayout.GetControlRect(bool,float)"/> 矩形后
        /// 转调 Rect 路径的 <see cref="DrawFieldControlRect"/>，使两条路径的字段外观完全一致。
        /// <para><b>1.6.0 起的显示行为对齐</b>（此前 Layout 版与 Rect 版分歧）：
        /// Vector4 不再多出一个空标签列；数组内 Sprite 采用与 Rect 版一致的正方形预览；
        /// VectorInt4 / StringIntPair 由纵向堆叠改为与 Rect 版一致的横向分栏；未知类型显示类型名。</para>
        /// </summary>
        private static void DrawElementField(IInventoryEditorContext ctx, AttributeValue value, EnumType enumType, int index)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, RowHeight(value, index));
            DrawFieldControlRect(ctx, value, enumType, index, rect);
        }

        // ─── Text：纯文本 fallback + 本地化选择器 ─────────────────────────────────

        /// <summary>GUILayout：绘制一个 Text 元素（纯文本框 + 原生可搜索本地化选择器）。</summary>
        private static void DrawTextFieldLayout(IInventoryEditorContext ctx, AttributeValue value, int index)
        {
            EditorGUI.BeginChangeCheck();
            string plain = EditorGUILayout.TextField("文本", value.GetTextValue(index));
            if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetTextValue(index, plain));
#if IS_LOCALIZATION
            DrawLocalizedStringField(ctx, value, index);
#endif
        }

        /// <summary>rect：绘制一个 Text 元素（纯文本框 + 原生可搜索本地化选择器）。高度须由调用方按 <see cref="GetTextRowHeight"/> 预留。</summary>
        private static void DrawTextRect(IInventoryEditorContext ctx, AttributeValue value, int index, string label, Rect rect)
        {
            float lh = EditorGUIUtility.singleLineHeight;
            float sp = EditorGUIUtility.standardVerticalSpacing;
            float y  = rect.y;

            // 纯文本 fallback 行（手动分割 label / 输入框，避免窄宽下挤压）
            float lW = 0f;
            if (!string.IsNullOrEmpty(label))
            {
                lW = Mathf.Max(Mathf.Min(EditorGUIUtility.labelWidth, rect.width * 0.4f), 0f);
                EditorGUI.LabelField(new Rect(rect.x, y, lW, lh), label);
            }
            EditorGUI.BeginChangeCheck();
            string plain = EditorGUI.TextField(new Rect(rect.x + lW, y, rect.width - lW, lh), value.GetTextValue(index));
            if (EditorGUI.EndChangeCheck()) Apply(ctx, () => value.SetTextValue(index, plain));
            y += lh + sp;

#if IS_LOCALIZATION
            float ph = GetLocalizedPropHeight(value, index);
            DrawLocalizedStringFieldRect(ctx, value, index, new Rect(rect.x, y, rect.width, ph));
#endif
        }

        /// <summary>Text 元素在 rect 模式下的总高度（纯文本行 + 本地化选择器高度）。</summary>
        public static float GetTextRowHeight(AttributeValue value, int index)
        {
            float h = EditorGUIUtility.singleLineHeight;
#if IS_LOCALIZATION
            h += EditorGUIUtility.standardVerticalSpacing + GetLocalizedPropHeight(value, index);
#endif
            return h;
        }

#if IS_LOCALIZATION
        // 每个 (AttributeValue 实例, 元素索引) 对应一套独立的 SO，避免多字段共享时状态互相污染。
        // key = (RuntimeHelpers.GetHashCode(value) << 16) | (uint)index，在会话内稳定唯一。
        private static readonly Dictionary<long, LocalizedStringHolder> _lsHolders =
            new Dictionary<long, LocalizedStringHolder>();
        private static readonly Dictionary<long, SerializedObject> _lsSOs =
            new Dictionary<long, SerializedObject>();

        private static long LSKey(AttributeValue v, int idx) =>
            ((long)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(v) << 16) | (uint)idx;

        /// <summary>懒建 (holder, SerializedObject)。</summary>
        private static (LocalizedStringHolder holder, SerializedObject so) EnsureLsHolder(AttributeValue value, int index)
        {
            long key = LSKey(value, index);
            if (!_lsHolders.TryGetValue(key, out var holder) || holder == null)
            {
                holder = ScriptableObject.CreateInstance<LocalizedStringHolder>();
                holder.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                _lsHolders[key] = holder;
                _lsSOs[key]     = new SerializedObject(holder);
            }
            var so = _lsSOs.TryGetValue(key, out var cached) && cached != null && cached.targetObject != null
                ? cached
                : (_lsSOs[key] = new SerializedObject(holder));
            return (holder, so);
        }

        /// <summary>本地化选择器控件的高度（据 holder 的原生属性绘制器求得，随展开状态变化）。</summary>
        private static float GetLocalizedPropHeight(AttributeValue value, int index)
        {
            var (_, so) = EnsureLsHolder(value, index);
            var valueProp = so.FindProperty("value");
            return valueProp != null
                ? EditorGUI.GetPropertyHeight(valueProp, GUIContent.none, true)
                : EditorGUIUtility.singleLineHeight;
        }

        /// <summary>
        /// 把 value 元素的 (tableRef, entryKey) 同步进 holder，返回 valueProp 及其表 / 条目子属性；
        /// 路径不匹配（包版本）时子属性返回 null，调用方降级为文本输入。
        /// </summary>
        private static SerializedProperty SyncLsHolder(AttributeValue value, int index,
            out SerializedObject so, out SerializedProperty tableProp, out SerializedProperty keyProp)
        {
            var (tableRef, entryKey) = value.GetLocalizedStringRef(index);
            (_, so) = EnsureLsHolder(value, index);

            var valueProp = so.FindProperty("value");
            tableProp = valueProp?.FindPropertyRelative("m_TableReference")?.FindPropertyRelative("m_TableCollectionName");
            keyProp   = valueProp?.FindPropertyRelative("m_TableEntryReference")?.FindPropertyRelative("m_Key");
            if (valueProp == null || tableProp == null || keyProp == null) return valueProp;

            // 仅在值实际不同时才写入，避免覆盖进行中的编辑
            so.Update();
            if (tableProp.stringValue != tableRef || keyProp.stringValue != entryKey)
            {
                tableProp.stringValue = tableRef;
                keyProp.stringValue   = entryKey;
                so.ApplyModifiedPropertiesWithoutUndo();
                so.Update();
            }
            return valueProp;
        }

        /// <summary>把 holder 中（用户经原生控件改动后的）表 / 条目回写到 value 元素。</summary>
        private static void ApplyLsReadback(IInventoryEditorContext ctx, AttributeValue value, int index,
            SerializedProperty tableProp, SerializedProperty keyProp)
        {
            string newTable = tableProp.stringValue ?? string.Empty;
            string newKey   = keyProp.stringValue   ?? string.Empty;
            Apply(ctx, () => value.SetLocalizedStringRef(index, newTable, newKey));
        }

        /// <summary>GUILayout：原生本地化选择器（表 / 条目，弹窗可搜索快速指定）。</summary>
        private static void DrawLocalizedStringField(IInventoryEditorContext ctx, AttributeValue value, int index)
        {
            var valueProp = SyncLsHolder(value, index, out var so, out var tableProp, out var keyProp);
            if (valueProp == null || tableProp == null || keyProp == null)
            {
                DrawLocalizedStringFallback(ctx, value, index);
                return;
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(valueProp, new GUIContent("本地化"), true);
            bool soChanged = so.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck() || soChanged)
                ApplyLsReadback(ctx, value, index, tableProp, keyProp);
        }

        /// <summary>rect：原生本地化选择器（表 / 条目，弹窗可搜索快速指定）。</summary>
        private static void DrawLocalizedStringFieldRect(IInventoryEditorContext ctx, AttributeValue value, int index, Rect rect)
        {
            var valueProp = SyncLsHolder(value, index, out var so, out var tableProp, out var keyProp);
            if (valueProp == null || tableProp == null || keyProp == null)
            {
                DrawLocalizedStringFallbackRect(ctx, value, index, "本地化", rect);
                return;
            }
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, valueProp, new GUIContent("本地化"), true);
            bool soChanged = so.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck() || soChanged)
                ApplyLsReadback(ctx, value, index, tableProp, keyProp);
        }

        /// <summary>GUILayout 降级文本输入：当 Unity.Localization 内部属性路径与当前包版本不匹配时使用。</summary>
        private static void DrawLocalizedStringFallback(IInventoryEditorContext ctx, AttributeValue value, int index)
        {
            var (tableRef, entryKey) = value.GetLocalizedStringRef(index);
            EditorGUI.BeginChangeCheck();
            string newTable = EditorGUILayout.TextField("Table", tableRef);
            string newEntry = EditorGUILayout.TextField("Entry Key", entryKey);
            if (EditorGUI.EndChangeCheck())
                Apply(ctx, () => value.SetLocalizedStringRef(index, newTable, newEntry));
        }
#endif

        // ─── 辅助 ────────────────────────────────────────────────────────────────

        /// <summary>AnimationCurve 默认值：(time=0,value=0) → (time=1,value=1) 线性直线。</summary>
        private static AnimationCurve DefaultCurve() => AnimationCurve.Linear(0f, 0f, 1f, 1f);

        /// <summary>数组条目序号样式：左对齐、去内边距，紧贴左侧 ≡ 拖拽句柄。</summary>
        private static GUIStyle _arrayIndexStyle;
        private static GUIStyle ArrayIndexStyle => _arrayIndexStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(1, 0, 0, 0),
        };

        /// <summary>绘制数组条目序号：清零缩进并左对齐，使其紧贴左侧 ≡ 拖拽句柄（不随外层缩进右移）。</summary>
        private static void DrawArrayIndex(int i)
        {
            int prev = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUILayout.LabelField($"[{i}]", ArrayIndexStyle, GUILayout.Width(26));
            EditorGUI.indentLevel = prev;
        }

        // ─── 数组拖拽重排 ────────────────────────────────────────────────────────

        /// <summary>每个数组型 AttributeValue 各持一份拖拽状态（弱引用，值被回收时自动清理）。</summary>
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<AttributeValue, EditorReorderableDrag> _arrayDrags
            = new System.Runtime.CompilerServices.ConditionalWeakTable<AttributeValue, EditorReorderableDrag>();

        /// <summary>拖拽落点计算用的临时索引序列（每帧重建，复用以避免分配）。</summary>
        private static readonly List<int> _reorderProxy = new List<int>();

        private static EditorReorderableDrag GetArrayDrag(AttributeValue value)
        {
            if (!_arrayDrags.TryGetValue(value, out var drag))
            {
                drag = new EditorReorderableDrag(
                    "attrArray:" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value));
                _arrayDrags.Add(value, drag);
            }
            return drag;
        }

        /// <summary>
        /// 兜底解析枚举类型：Enum / EnumIntPair 需要枚举类型才能绘制弹窗，
        /// 若调用方未传入（多数值级面板对非 Enum 类型传 null），则按值携带的 <see cref="AttributeValue.EnumTypeRef"/>
        /// 从编辑器数据库解析，使 EnumIntPair 无需逐一改动各面板即可在任意处正确编辑。
        /// </summary>
        private static EnumType ResolveEnumType(IInventoryEditorContext ctx, AttributeValue value, EnumType enumType)
        {
            if (enumType != null || value == null) return enumType;
            if (value.Type != EFieldType.Enum && value.Type != EFieldType.EnumIntPair) return enumType;
            if (string.IsNullOrEmpty(value.EnumTypeRef)) return enumType;
            return ctx?.Database?.GetEnumType(value.EnumTypeRef);
        }

        private static void Apply(IInventoryEditorContext ctx, System.Action mutate)
        {
            ctx.RecordUndo("修改属性值");
            mutate();
            ctx.MarkDirty();
        }

        private static void EnsureCount(AttributeValue value, int count)
        {
            while (value.Count < count)
                value.AddElement();
        }

        // ─── 复制 / 粘贴（Ctrl+C / Ctrl+V，便于快速复制配置数据）────────────────────

        // 系统剪贴板载荷前缀：标识这是一条属性值 JSON，避免把任意文本当作属性粘贴。
        private const string ClipboardHeader = "InventoryAttrValue:v1\n";

        /// <summary>
        /// 处理一条属性的复制 / 粘贴交互：
        /// <list type="bullet">
        ///   <item>鼠标悬停于该条目矩形内且未在编辑文本框时，Ctrl+C 复制、Ctrl+V 粘贴；</item>
        ///   <item>右键弹出「复制属性值 / 粘贴属性值」菜单（剪贴板不兼容时粘贴项灰显）。</item>
        /// </list>
        /// 仅当剪贴板属性与目标的类型 / 数组形态（枚举还需同枚举类型）一致时才允许粘贴，确保数据有效。
        /// </summary>
        private static void HandleEntryCopyPaste(IInventoryEditorContext ctx, AttributeValue value, Rect rowRect)
        {
            var e = Event.current;
            if (e == null || value == null) return;
            if (!rowRect.Contains(e.mousePosition)) return;

            // 键盘快捷键：编辑文本框时让位给文本本身的复制 / 粘贴（此时不抢占）
            if (e.type == EventType.KeyDown && (e.control || e.command) && !EditorGUIUtility.editingTextField)
            {
                if (e.keyCode == KeyCode.C)
                {
                    CopyValue(value);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.V)
                {
                    if (TryPasteValue(ctx, value)) e.Use();
                }
            }

            // 右键上下文菜单（更易发现，且不与文本编辑冲突）
            if (e.type == EventType.ContextClick)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("复制属性值  Ctrl+C"), false, () => CopyValue(value));
                if (ReadClipboardCompatible(value) != null)
                    menu.AddItem(new GUIContent("粘贴属性值  Ctrl+V"), false, () => TryPasteValue(ctx, value));
                else
                    menu.AddDisabledItem(new GUIContent("粘贴属性值  Ctrl+V"));
                menu.ShowAsContext();
                e.Use();
            }
        }

        /// <summary>将属性值序列化为 JSON 写入系统剪贴板（带前缀标识）。</summary>
        private static void CopyValue(AttributeValue value)
        {
            if (value == null) return;
            EditorGUIUtility.systemCopyBuffer = ClipboardHeader + EditorJsonUtility.ToJson(value);
        }

        /// <summary>尝试把剪贴板中的属性值粘贴到 <paramref name="target"/>（类型兼容时）。返回是否成功。</summary>
        private static bool TryPasteValue(IInventoryEditorContext ctx, AttributeValue target)
        {
            var clip = ReadClipboardCompatible(target);
            if (clip == null) return false;

            ctx.RecordUndo("粘贴属性值");
            target.SetRaw(clip.Type, clip.IsArray, clip.EnumTypeRef,
                clip.RawInts, clip.RawFloats, clip.RawStrings, clip.RawObjects, clip.RawCurves,
                clip.RawObjAddresses);
            ctx.MarkDirty();
            return true;
        }

        /// <summary>
        /// 读取剪贴板中的属性值：为本系统的属性载荷且与 <paramref name="target"/> 类型兼容时返回解析结果，否则 null。
        /// </summary>
        private static AttributeValue ReadClipboardCompatible(AttributeValue target)
        {
            string buf = EditorGUIUtility.systemCopyBuffer;
            if (target == null || string.IsNullOrEmpty(buf) || !buf.StartsWith(ClipboardHeader))
                return null;

            var clip = new AttributeValue();
            try { EditorJsonUtility.FromJsonOverwrite(buf.Substring(ClipboardHeader.Length), clip); }
            catch { return null; }

            if (clip.Type != target.Type || clip.IsArray != target.IsArray) return null;
            if ((clip.Type == EFieldType.Enum || clip.Type == EFieldType.EnumIntPair)
                && clip.EnumTypeRef != target.EnumTypeRef) return null;
            return clip;
        }
    }
}

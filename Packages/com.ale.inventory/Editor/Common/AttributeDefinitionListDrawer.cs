using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 绘制一组 <see cref="AttributeDefinition"/>（功能标签 / 道具模板共用）：
    /// 标题 + 添加按钮 + 可拖拽排序的 <see cref="ReorderableList"/>（每行左侧有拖拽句柄，右侧有删除按钮）。
    ///
    /// <para><b>重要设计说明</b><br/>
    /// <see cref="ReorderableList.drawElementCallback"/> 中 <b>必须</b> 使用纯 rect-based
    /// <c>EditorGUI.*</c> 绘制，<b>严禁</b> 调用 <c>GUILayout.BeginArea</c>/<c>GUI.BeginGroup</c>。<br/>
    /// 原因：<c>GUILayout.BeginArea</c> 会向父级 <see cref="GUILayoutGroup"/> 注册槽位，
    /// Unity IMGUI 要求 Layout 和 Repaint 两次事件的槽位数完全一致；
    /// 一旦在两次事件之间修改了元素数量，就会抛出
    /// "Getting control X's position in a group with only Y controls" 异常。<br/>
    /// （与元素数量无关），根本上消除了该异常。</para>
    /// </summary>
    public class AttributeDefinitionListDrawer
    {
        // ── ReorderableList 状态 ──────────────────────────────────────────────────
        private ReorderableList              _list;
        private List<AttributeDefinition>    _bound;
        private IInventoryEditorContext      _ctx;

        // ── Pending 操作（在 DoLayoutList 之后执行，避免在回调中修改集合）────────────
        private bool _pendingAdd;
        private int  _pendingDeleteIndex = -1;

        // ── 高度常量（与 AttributeDefinitionDrawer.DrawRect / AttributeFieldDrawer.DrawRect 保持一致）
        private const float LineHeight  = 18f; // EditorGUIUtility.singleLineHeight
        private const float VerticalSpacing  =  2f; // standardVerticalSpacing
        private const float Row = 20f; // _lh + _sp
        private const float Padding =  4f; // helpBox 内边距（单侧）

        // ── 公开接口 ──────────────────────────────────────────────────────────────

        /// <summary>数据库切换或面板重置时调用，清空列表缓存。</summary>
        public void Invalidate()
        {
            _list  = null;
            _bound = null;
        }

        /// <summary>
        /// 绘制属性字段定义列表：标题行（含「添加字段」按钮）+ 可拖拽排序列表。
        /// </summary>
        public void Draw(IInventoryEditorContext ctx, List<AttributeDefinition> defs, string title)
        {
            _ctx = ctx;

            // 初始化或 defs 引用变更时重建列表
            if (_list == null || !ReferenceEquals(_bound, defs))
                BuildList(defs);

            // ── 标题 + 添加字段 按钮 ─────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, InventoryEditorStyles.Header);
            if (GUILayout.Button("添加字段", GUILayout.Width(72)))
                _pendingAdd = true; // 延迟到 DoLayoutList 之后执行
            EditorGUILayout.EndHorizontal();

            if (_list != null) _list.DoLayoutList();

            // ── 延迟修改（在 DoLayoutList 完成后执行）──────────────────────────────
            if (_pendingAdd)
            {
                _pendingAdd = false;
                ctx.RecordUndo("添加属性字段");
                defs.Add(new AttributeDefinition(GenerateId(defs), EFieldType.Int));
                ctx.MarkDirty();
                // 重建列表（_list 绑定的是 defs 本身，只需刷新 index 等状态）
                BuildList(defs);
            }

            if (_pendingDeleteIndex >= 0)
            {
                int di = _pendingDeleteIndex;
                _pendingDeleteIndex = -1;
                if (di >= 0 && di < defs.Count)
                {
                    ctx.RecordUndo("删除属性字段");
                    defs.RemoveAt(di);
                    ctx.MarkDirty();
                    BuildList(defs);
                }
            }
        }

        // ── 构建 ReorderableList（直接绑定 defs）─────────────────────────────────

        private void BuildList(List<AttributeDefinition> defs)
        {
            _bound = defs;

            // 直接绑定 defs（无需 snapshot），因为 drawElementCallback 不使用 GUILayout
            _list = new ReorderableList(defs, typeof(AttributeDefinition),
                draggable:           true,
                displayHeader:       false,
                displayAddButton:    false,
                displayRemoveButton: false);

            // ── 元素高度 ─────────────────────────────────────────────────────────
            _list.elementHeightCallback = index =>
            {
                if (index < 0 || index >= defs.Count) return Row + Padding * 2f + 2f;
                return CalcElementHeight(defs[index]);
            };

            // ── 绘制每个元素（纯 rect-based，零 GUILayout API 调用）─────────────────
            _list.drawElementCallback = (rect, index, _, _) =>
            {
                if (index < 0 || index >= defs.Count) return;

                float lh  = EditorGUIUtility.singleLineHeight;
                float sp  = EditorGUIUtility.standardVerticalSpacing;
                float row = lh + sp;
                float y   = rect.y + 1f;
                float x   = rect.x;
                float w   = rect.width;

                // ── 序号 + 删除按钮（纯 EditorGUI / GUI，无 GUILayout 调用）─────────
                EditorGUI.LabelField(new Rect(x, y, 28f, lh), $"#{index}");
                if (GUI.Button(new Rect(x + w - 24f, y, 22f, lh), "✕", EditorStyles.miniButton))
                    _pendingDeleteIndex = index;
                y += row;

                // ── 字段详情（AttributeDefinitionDrawer.DrawRect，同样无 GUILayout）────
                float contentH = rect.yMax - y - 1f;
                if (contentH > 0f)
                    AttributeDefinitionDrawer.DrawRect(_ctx, defs[index],
                        new Rect(x, y, w, contentH));
            };

            // 拖拽排序完成后记录 Undo（ReorderableList 直接修改 defs，无需手动同步）
            _list.onReorderCallback = _ =>
            {
                _ctx?.RecordUndo("调整属性字段顺序");
                _ctx?.MarkDirty();
            };
        }

        // ── 元素高度计算（需与 DrawRect / DrawFieldDrawer.DrawRect 的绘制行数严格匹配）────

        /// <summary>
        /// 计算单个属性字段定义在列表中的显示高度。<br/>
        /// 结构：1px margin + header row + helpBox(pad×2 + ID + type + [enum] + label + value) + 2px buffer
        /// </summary>
        private static float CalcElementHeight(AttributeDefinition def)
        {
            if (def == null) return Row * 5f + Padding * 2f + 2f;

            float h = 1f         // drawElementCallback 顶部 margin
                    + Row       // 序号 + 删除按钮行
                    + Padding * 2f  // helpBox 上下 padding
                    + Row       // ID 行
                    + Row       // 类型 + 数组行
                    + Row       // "默认值" 标签行
                    ;

            if (def.type == EFieldType.Enum || def.type == EFieldType.EnumIntPair) h += Row; // 枚举类型引用行

            if (!def.isArray)
            {
                // Text：纯文本行 + 本地化选择器（动态）；对象类型：授权模式据 AssetReference 高度、
                // 否则 Sprite 3 行 / 其余单行；其余类型单行。
                if (def.type == EFieldType.Text)
                    h += AttributeFieldDrawer.GetTextRowHeight(def.defaultValue, 0);
                else if (def.type.IsObjectBacked())
                    h += AttributeFieldDrawer.HasAddressableDrawer
                        ? AttributeFieldDrawer.GetObjectRowHeight(def.defaultValue, 0)
                        : (def.type == EFieldType.Sprite ? LineHeight * 3f : LineHeight);
                else
                    h += LineHeight;
            }
            else
            {
                // 数组：内嵌 helpBox（pad×2 + 标题行 + N×元素行；Text / 授权对象类型按控件动态高度）
                int n = def.defaultValue?.Count ?? 0;
                h += Padding * 2f + Row;
                if (def.type == EFieldType.Text)
                    for (int i = 0; i < n; i++) h += AttributeFieldDrawer.GetTextRowHeight(def.defaultValue, i) + VerticalSpacing;
                else if (def.type.IsObjectBacked() && AttributeFieldDrawer.HasAddressableDrawer)
                    for (int i = 0; i < n; i++) h += AttributeFieldDrawer.GetObjectRowHeight(def.defaultValue, i) + VerticalSpacing;
                else
                    h += n * Row;
            }

            h += 2f; // 底部缓冲
            return h;
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────────

        private static string GenerateId(List<AttributeDefinition> defs)
        {
            int n = defs.Count;
            string attrId;
            do { attrId = "field_" + n; n++; }
            while (Exists(defs, attrId));
            return attrId;
        }

        private static bool Exists(List<AttributeDefinition> defs, string attrId)
        {
            foreach (var d in defs)
                if (d.id == attrId) return true;
            return false;
        }
    }
}

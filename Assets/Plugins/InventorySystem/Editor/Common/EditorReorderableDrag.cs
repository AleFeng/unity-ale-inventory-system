using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 通用「拖拽重排」状态机：在以「行 Rect」布局的列表里，左侧 ≡ 句柄长按拖动调整顺序，
    /// 拖动时绘制蓝色插入指示线，松开时对目标 <see cref="IList{T}"/> 执行带 Undo 的移动。
    ///
    /// <para>一个实例持有一条列表的拖拽状态；每帧按
    /// <see cref="BeginFrame"/> → （每行）<see cref="DrawHandle"/> / <see cref="RecordRow"/> → <see cref="EndFrame{T}"/>
    /// 的顺序调用。</para>
    ///
    /// <para>同时适用于：</para>
    /// <list type="bullet">
    /// <item>固定行高的滚动列表（道具 / 仓库 / 商店列表面板）——调用方自行计算句柄与整行 Rect；</item>
    /// <item>自动布局的可变高度条目（商品组 / 商品列表）——用 <see cref="DrawHandleColumn"/> 预留左侧句柄列，
    /// 用 <c>EditorGUILayout.BeginHorizontal()</c> 的返回 Rect 作为整行 Rect。</item>
    /// </list>
    /// </summary>
    public class EditorReorderableDrag
    {
        /// <summary>左侧拖拽句柄列建议宽度。</summary>
        public const float HandleWidth = 16f;

        private static readonly Color InsertLineColor = new Color(0.25f, 0.6f, 1f, 0.9f);

        /// <summary>拖拽源行的轻微高亮色（调用方可在 <see cref="IsDragSource"/> 为真时用它绘制背景）。</summary>
        public static readonly Color DragSourceTint = new Color(1f, 1f, 1f, 0.07f);

        private readonly int _ctrlHash;
        private int   _srcIndex = -1; // 正在拖拽的条目索引（调用方坐标系，通常为数据列表下标），-1 = 未拖拽
        private float _mouseY;        // 当前拖拽鼠标 Y（与行 Rect 同坐标系）
        private int   _ctrlId;        // 本帧拖拽控件 ID

        private struct Row { public int Index; public Rect Rect; }
        private readonly List<Row> _rows = new List<Row>();

        private static GUIStyle _handleStyle;
        private static GUIStyle HandleStyle => _handleStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.5f, 0.5f, 0.5f) },
        };

        /// <param name="id">用于生成稳定拖拽控件 ID 的标识串（每条列表用各自的串）。</param>
        public EditorReorderableDrag(string id) => _ctrlHash = id.GetHashCode();

        /// <summary>该索引的条目是否为当前拖拽源（调用方可据此给源行加轻微高亮）。</summary>
        public bool IsDragSource(int index) => _srcIndex == index;

        /// <summary>每帧开始：注册稳定的拖拽控件 ID，并清空上一帧的行信息。须在列表内容绘制前调用一次。</summary>
        public void BeginFrame()
        {
            _ctrlId = GUIUtility.GetControlID(_ctrlHash, FocusType.Passive);
            _rows.Clear();
        }

        /// <summary>记录某条目的整行 Rect（用于计算插入槽位与指示线）。</summary>
        public void RecordRow(int index, Rect fullRect) => _rows.Add(new Row { Index = index, Rect = fullRect });

        /// <summary>绘制某条目的 ≡ 拖拽句柄，并在其上按下时开始拖拽。</summary>
        public void DrawHandle(Rect handleRect, int index)
        {
            GUI.Label(handleRect, "≡", HandleStyle);
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.Pan);

            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && handleRect.Contains(Event.current.mousePosition)
                && GUIUtility.hotControl == 0)
            {
                _srcIndex             = index;
                _mouseY               = Event.current.mousePosition.y;
                GUIUtility.hotControl = _ctrlId;
                Event.current.Use();
            }
        }

        /// <summary>
        /// 自动布局场景便捷方法：在 <c>BeginHorizontal()</c> 之后、条目内容之前调用，
        /// 预留并绘制一列纵向拉伸到整条目高度的拖拽句柄。
        /// </summary>
        public void DrawHandleColumn(int index)
        {
            Rect r = GUILayoutUtility.GetRect(HandleWidth, EditorGUIUtility.singleLineHeight,
                GUILayout.Width(HandleWidth), GUILayout.ExpandHeight(true));
            DrawHandle(r, index);
        }

        /// <summary>
        /// 每帧结束：处理拖拽 / 落点、绘制蓝色插入指示线；落点有效时对 <paramref name="list"/>
        /// 执行带 Undo 的移动。须在与各行 Rect 相同的坐标系（同一滚动区）内、行循环之后调用。
        /// </summary>
        /// <param name="lineInset">插入线相对整行左边缘的内缩（一般等于句柄列宽，让线从内容区起画）。</param>
        /// <param name="lineRightInset">插入线相对整行右边缘的内缩（一般等于右侧删除按钮宽度）。</param>
        /// <returns>本帧是否发生了重排。</returns>
        public bool EndFrame<T>(IInventoryEditorContext ctx, IList<T> list, string undoLabel,
            float lineInset = HandleWidth, float lineRightInset = 0f)
        {
            if (_srcIndex < 0) return false;
            bool reordered = false;

            if (Event.current.type == EventType.MouseDrag && GUIUtility.hotControl == _ctrlId)
            {
                _mouseY = Event.current.mousePosition.y;
                ctx.Repaint();
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseUp && GUIUtility.hotControl == _ctrlId)
            {
                int srcIdx = _srcIndex;
                int slot   = ComputeInsertSlot(_mouseY);
                int tgtIdx = slot >= _rows.Count ? list.Count : _rows[slot].Index;

                if (tgtIdx != srcIdx && tgtIdx != srcIdx + 1
                    && srcIdx >= 0 && srcIdx < list.Count)
                {
                    ctx.RecordUndo(undoLabel);
                    var dragged = list[srcIdx];
                    list.RemoveAt(srcIdx);
                    if (tgtIdx > srcIdx) tgtIdx--;
                    list.Insert(Mathf.Clamp(tgtIdx, 0, list.Count), dragged);
                    ctx.MarkDirty();
                    reordered = true;
                }

                _srcIndex             = -1;
                GUIUtility.hotControl = 0;
                Event.current.Use();
            }

            // 绘制蓝色插入指示线（跳过不产生实际移动的插槽）。
            if (Event.current.type == EventType.Repaint && _rows.Count > 0)
            {
                int slot   = ComputeInsertSlot(_mouseY);
                int srcVis = -1;
                for (int k = 0; k < _rows.Count; k++)
                    if (_rows[k].Index == _srcIndex) { srcVis = k; break; }

                bool noOp = srcVis >= 0 && (slot == srcVis || slot == srcVis + 1);
                if (!noOp)
                {
                    float lineY;
                    if (slot <= 0)
                        lineY = _rows[0].Rect.yMin;
                    else if (slot >= _rows.Count)
                        lineY = _rows[_rows.Count - 1].Rect.yMax - 1f;
                    else
                        lineY = (_rows[slot - 1].Rect.yMax + _rows[slot].Rect.yMin) * 0.5f;

                    float lx = _rows[0].Rect.xMin + lineInset;
                    float lw = _rows[0].Rect.width - lineInset - lineRightInset;
                    EditorGUI.DrawRect(new Rect(lx, lineY - 1f, lw, 2f), InsertLineColor);
                }
            }

            return reordered;
        }

        /// <summary>据鼠标 Y 计算插入槽位：0 = 第一行之前，Count = 最后一行之后。</summary>
        private int ComputeInsertSlot(float mouseY)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                float midY = (_rows[i].Rect.yMin + _rows[i].Rect.yMax) * 0.5f;
                if (mouseY < midY) return i;
            }
            return _rows.Count;
        }
    }
}

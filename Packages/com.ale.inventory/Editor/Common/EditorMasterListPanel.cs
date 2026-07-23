using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 编辑器「左栏主列表」泛型基类：标题栏（+「+」添加）→ 可拖拽重排的 <see cref="ReorderableList"/>
    /// （每行：可选色点 + 标签 +「✕」）→ 延迟删除，并与调用方（各 SystemTab）双向同步选中索引。
    ///
    /// <para>此前该四段骨架在 13 处各写了一遍，四个 <see cref="ReorderableList"/> 回调体逐字相同、
    /// 只有标签表达式不同，且已开始漂移（部分拷贝多了无用的 <c>_masterList != null</c> 空守卫、
    /// 部分用 C# 9 弃元参数）。现统一到本类，子类只提供「取哪个列表 / 叫什么 / 每行显示什么」。</para>
    ///
    /// <para>Undo 文案由 <see cref="Noun"/> 机械推导为「添加X」「删除X」「调整X顺序」，
    /// 与 13 处原有字面量逐字一致；标题同样取 <see cref="Noun"/>。</para>
    /// </summary>
    /// <typeparam name="T">列表元素类型。</typeparam>
    public abstract class EditorMasterListPanel<T> where T : class
    {
        private ReorderableList         _masterList;
        private List<T>                 _boundList;
        private int                     _selectedIndex      = -1;
        private int                     _pendingDeleteIndex = -1;
        private IInventoryEditorContext _masterCtx;

        /// <summary>行高（与 13 处原实现一致）。</summary>
        private const float RowHeight = 22f;
        /// <summary>行尾「✕」按钮的占位宽度 / 按钮宽度。</summary>
        private const float DelSlotWidth = 22f, DelButtonWidth = 20f;
        /// <summary>色点宽度。</summary>
        private const float DotWidth = 14f;

        #region 子类契约

        /// <summary>取本面板绑定的列表（如 <c>db.ItemTemplates</c>）。</summary>
        protected abstract List<T> GetList(InventoryDatabase db);

        /// <summary>名词：既作标题，也作 Undo 文案词根（「添加X」「删除X」「调整X顺序」）。</summary>
        protected abstract string Noun { get; }

        /// <summary>该行显示的文本。</summary>
        protected abstract string RowLabel(T item);

        /// <summary>新建一个条目（仅当 <see cref="CanAdd"/> 为真时调用）。</summary>
        protected virtual T CreateNew(InventoryDatabase db, List<T> list) => null;

        /// <summary>是否在行首绘制圆形色点。</summary>
        protected virtual bool HasColorDot => false;

        /// <summary>色点颜色（<see cref="HasColorDot"/> 为真时使用）。</summary>
        protected virtual Color RowColor(T item) => Color.gray;

        /// <summary>是否允许拖拽重排。</summary>
        protected virtual bool Draggable => true;

        /// <summary>是否显示标题栏的「+」添加按钮。</summary>
        protected virtual bool CanAdd => true;

        /// <summary>是否显示行尾的「✕」删除按钮。</summary>
        protected virtual bool CanDelete => true;

        /// <summary>标题下方的 HelpBox 文本；null = 不显示。</summary>
        protected virtual string HeaderHelp => null;

        /// <summary>列表为空时替代列表显示的占位文本；null = 仍照常绘制空列表。</summary>
        protected virtual string EmptyPlaceholder => null;

        /// <summary>绘制列表之前的钩子（如按需重建数据源）。默认空。</summary>
        protected virtual void BeforeDrawList(IInventoryEditorContext ctx) { }

        /// <summary><see cref="Invalidate"/> 时的附加清理（如内嵌绘制器的缓存）。默认空。</summary>
        protected virtual void OnInvalidate() { }

        #endregion

        #region 主列表

        /// <summary>绘制主列表，返回当前选中索引（-1 = 未选中）。</summary>
        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            _masterCtx = ctx;
            BeforeDrawList(ctx);

            var db   = ctx.Database;
            var list = GetList(db);

            if (_masterList == null || !ReferenceEquals(_boundList, list))
            {
                _selectedIndex = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                BuildMasterList(list);
            }
            else
            {
                // 外部同步：调用方（Tab）重置索引时以调用方为准，
                // 避免面板返回旧索引触发错误的选中切换。
                int clamped = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                if (_selectedIndex != clamped)
                {
                    _selectedIndex    = clamped;
                    _masterList.index = clamped;
                }
            }

            // ── 标题栏（+ 添加按钮）──────────────────────────────────────────────
            if (CanAdd)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Noun, InventoryEditorStyles.Header);
                if (GUILayout.Button("+", GUILayout.Width(24)))
                {
                    var created = CreateNew(db, list);
                    if (created != null)
                    {
                        ctx.RecordUndo($"添加{Noun}");
                        list.Add(created);
                        ctx.MarkDirty();
                        _selectedIndex    = list.Count - 1;
                        _masterList.index = _selectedIndex;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField(Noun, InventoryEditorStyles.Header);
            }

            if (!string.IsNullOrEmpty(HeaderHelp))
                EditorGUILayout.HelpBox(HeaderHelp, MessageType.None);

            if (list.Count == 0 && !string.IsNullOrEmpty(EmptyPlaceholder))
            {
                EditorGUILayout.LabelField(EmptyPlaceholder, InventoryEditorStyles.Placeholder);
                return _selectedIndex;
            }

            _masterList.DoLayoutList();

            // 延迟删除：在 DoLayoutList 完成之后处理，避免在回调中修改其绑定的集合。
            if (_pendingDeleteIndex >= 0)
            {
                int di = _pendingDeleteIndex;
                _pendingDeleteIndex = -1;
                if (di < list.Count)
                {
                    ctx.RecordUndo($"删除{Noun}");
                    list.RemoveAt(di);
                    ctx.MarkDirty();
                    _selectedIndex    = Mathf.Clamp(_selectedIndex, -1, list.Count - 1);
                    _masterList.index = _selectedIndex;
                }
            }

            return _selectedIndex;
        }

        private void BuildMasterList(List<T> list)
        {
            _boundList  = list;
            _masterList = new ReorderableList(list, typeof(T),
                draggable: Draggable, displayHeader: false,
                displayAddButton: false, displayRemoveButton: false);

            _masterList.elementHeight = RowHeight;
            _masterList.index         = _selectedIndex;

            _masterList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (active)
                    InventoryEditorStyles.DrawRowBackground(rect, InventoryEditorStyles.SelectedColor);
            };

            _masterList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= list.Count) return;
                var   item = list[index];
                float cy   = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;
                float lh   = EditorGUIUtility.singleLineHeight;

                float labelX     = rect.x;
                float labelRight = rect.xMax;

                if (HasColorDot)
                {
                    var dotRect = new Rect(rect.x, cy, DotWidth, lh);
                    InventoryEditorStyles.DrawColorDot(dotRect, RowColor(item));
                    labelX = dotRect.xMax + 3f;
                }

                if (CanDelete)
                {
                    var delRect = new Rect(rect.xMax - DelSlotWidth, cy, DelButtonWidth, lh);
                    // 标签右侧留白：带色点的行沿用 7px，不带色点的沿用 4px（与各原实现一致）。
                    labelRight = rect.xMax - DelSlotWidth - (HasColorDot ? 7f : 4f);
                    if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                        _pendingDeleteIndex = index;
                }
                else
                {
                    // 只读列表（无删除按钮）：整行留给标签，左侧小幅内缩。
                    labelX = rect.x + 4f;
                }

                GUI.Label(new Rect(labelX, cy, Mathf.Max(0f, labelRight - labelX), lh), RowLabel(item));
            };

            _masterList.onSelectCallback = rl => _selectedIndex = rl.index;

            if (Draggable)
            {
                _masterList.onReorderCallback = _ =>
                {
                    _masterCtx.RecordUndo($"调整{Noun}顺序");
                    _masterCtx.MarkDirty();
                };
            }
        }

        /// <summary>数据库切换或外部重置时调用，清空主列表缓存。</summary>
        public void Invalidate()
        {
            _masterList         = null;
            _boundList          = null;
            _selectedIndex      = -1;
            _pendingDeleteIndex = -1;
            OnInvalidate();
        }

        #endregion
    }
}

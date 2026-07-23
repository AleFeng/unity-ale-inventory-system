using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 编辑器「系统页签」三列布局泛型基类：
    /// <list type="bullet">
    ///   <item><b>左列</b> — 可选的子页签工具栏 + 当前子页签对应的主列表（<see cref="IEditorMasterListPanel"/>）；</item>
    ///   <item><b>中列</b> — 实体列表（道具 / 仓库 / 商店 / 蓝图 / 装备组 / 技能）；</item>
    ///   <item><b>右列</b> — 上下文 Inspector：中列有选中则画实体（带「删除X」按钮），否则画左列当前子面板的 Inspector。</item>
    /// </list>
    ///
    /// <para>选中互斥：左列选中时清空中列选中，中列选中时清空左列全部索引 ——
    /// 这样两侧都能被「再次点击选中」，不会因引用 / 索引相同而跳过变化检测。</para>
    ///
    /// <para>此前六个系统页签是同一份骨架的六份拷贝（其中制作 / 装备 / 技能三份逐行对齐），
    /// 并已出现漂移：右列滚动视图两种重载混用、删除按钮宽度 68/64/64/64/72/64 各不相同、
    /// 切子页签时清哪些选中状态各不一致、每帧新建删除按钮 <see cref="GUIStyle"/>。现统一到本类。</para>
    /// </summary>
    /// <typeparam name="TEntity">中列实体类型。</typeparam>
    public abstract class EditorThreeColumnTab<TEntity> where TEntity : class
    {
        // 三列宽度（此前在六处各声明一遍，值完全相同）。
        private const float LeftWidth = 260f, MiddleWidthMin = 320f, RightWidth = 380f, Padding = 4f;

        private int     _leftSubTab;
        private int[]   _leftSelected;      // 每个左侧子面板各自的选中索引
        private TEntity _selectedEntity;
        private bool    _entityMode;        // true = 右列画中列实体；false = 画左列子面板
        private Vector2 _leftScroll, _rightScroll;
        private bool    _pendingDeleteEntity;

        #region 子类契约

        /// <summary>左列子页签名称；返回 null 或长度 &lt; 2 时不绘制工具栏（如商店系统只有「商店模板」一项）。</summary>
        protected virtual string[] LeftSubTabs => null;

        /// <summary>左列各子页签对应的主列表面板，顺序与 <see cref="LeftSubTabs"/> 一致。</summary>
        protected abstract IEditorMasterListPanel[] LeftPanels { get; }

        /// <summary>实体名词（用于「删除X」按钮与「X Inspector」标题）。</summary>
        protected abstract string EntityNoun { get; }

        /// <summary>数据库中的实体列表（用于删除与「选中项是否仍存在」判定）。</summary>
        protected abstract List<TEntity> EntityList(InventoryDatabase db);

        /// <summary>绘制中列实体列表，返回其当前选中项。</summary>
        protected abstract TEntity DrawEntityList(IInventoryEditorContext ctx, TEntity displaySelected);

        /// <summary>取走中列面板挂起的「请求选中」（如从模板添加后自动选中新条目）；无则返回 null。</summary>
        protected abstract TEntity ConsumePendingSelect();

        /// <summary>绘制右列的实体 Inspector。</summary>
        protected abstract void DrawEntityInspector(IInventoryEditorContext ctx, TEntity entity);

        /// <summary>「删除X」按钮宽度。</summary>
        protected virtual float DeleteButtonWidth => 64f;

        #endregion

        #region 选中状态

        private IEditorMasterListPanel[] Panels => LeftPanels;

        private int[] Selected
        {
            get
            {
                if (_leftSelected == null || _leftSelected.Length != Panels.Length)
                {
                    _leftSelected = new int[Panels.Length];
                    for (int i = 0; i < _leftSelected.Length; i++) _leftSelected[i] = -1;
                }
                return _leftSelected;
            }
        }

        /// <summary>激活左列 Inspector：清空中列选中。</summary>
        private void ActivateLeft()
        {
            _selectedEntity = null;
            _entityMode     = false;
        }

        /// <summary>激活中列 Inspector：清空左列全部索引。</summary>
        private void ActivateEntity(TEntity entity)
        {
            var sel = Selected;
            for (int i = 0; i < sel.Length; i++) sel[i] = -1;
            _selectedEntity = entity;
            _entityMode     = entity != null;
        }

        public virtual void OnDatabaseChanged(IInventoryEditorContext ctx)
        {
            ActivateEntity(null);
            foreach (var p in Panels) p.Invalidate();
        }

        public virtual void OnUndoRedo()
        {
            foreach (var p in Panels) p.Invalidate();
        }

        #endregion

        #region 绘制

        public void OnGUI(Rect rect, IInventoryEditorContext ctx)
        {
            // 集合改动与「挂起选中」一律推迟到 Layout 事件处理，避免在 Repaint 中改动布局。
            if (Event.current.type == EventType.Layout)
            {
                if (_pendingDeleteEntity)
                {
                    _pendingDeleteEntity = false;
                    var list = EntityList(ctx.Database);
                    if (_selectedEntity != null && list.Contains(_selectedEntity))
                    {
                        ctx.RecordUndo($"删除{EntityNoun}");
                        list.Remove(_selectedEntity);
                        ctx.MarkDirty();
                    }
                    ActivateLeft();
                }

                var pending = ConsumePendingSelect();
                if (pending != null) ActivateEntity(pending);
            }

            float middleWidth = Mathf.Max(MiddleWidthMin,
                rect.width - LeftWidth - RightWidth - Padding * 4);

            var leftRect   = new Rect(rect.x + Padding,          rect.y + Padding, LeftWidth,   rect.height - Padding * 2);
            var middleRect = new Rect(leftRect.xMax + Padding,   rect.y + Padding, middleWidth, rect.height - Padding * 2);
            var rightRect  = new Rect(middleRect.xMax + Padding, rect.y + Padding,
                rect.width - middleRect.xMax - Padding * 2, rect.height - Padding * 2);

            DrawLeft(leftRect, ctx);
            DrawMiddle(middleRect, ctx);
            DrawRight(rightRect, ctx);
        }

        private void DrawLeft(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            var tabs = LeftSubTabs;
            if (tabs != null && tabs.Length > 1)
            {
                int prev = _leftSubTab;
                _leftSubTab = GUILayout.Toolbar(_leftSubTab, tabs);
                // 切子页签：清空左列全部索引与中列选中（此前六处对「清哪些」各行其是）。
                if (_leftSubTab != prev) ActivateEntity(null);
            }
            _leftSubTab = Mathf.Clamp(_leftSubTab, 0, Panels.Length - 1);

            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            var sel = Selected;
            int picked = Panels[_leftSubTab].DrawMasterList(ctx, sel[_leftSubTab]);
            if (picked != sel[_leftSubTab])
            {
                sel[_leftSubTab] = picked;
                ActivateLeft();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMiddle(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            TEntity display = _entityMode ? _selectedEntity : null;
            TEntity picked  = DrawEntityList(ctx, display);
            if (!ReferenceEquals(picked, display)) ActivateEntity(picked);

            GUILayout.EndArea();
        }

        private void DrawRight(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            bool drawEntity = _entityMode && _selectedEntity != null
                              && EntityList(ctx.Database).Contains(_selectedEntity);

            // 选中项已被删除（如经其它路径移除）→ 退回左列 Inspector。
            if (!drawEntity && _entityMode) ActivateLeft();

            if (drawEntity)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{EntityNoun} Inspector", InventoryEditorStyles.Header);
                if (GUILayout.Button($"删除{EntityNoun}", InventoryEditorStyles.DangerMiniButton,
                        GUILayout.Width(DeleteButtonWidth)))
                    _pendingDeleteEntity = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            // 隐藏横向滚动条：内容自适应填满 Inspector 宽度。
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, false, false,
                GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);

            if (drawEntity) DrawEntityInspector(ctx, _selectedEntity);
            else            Panels[_leftSubTab].DrawInspectorAt(ctx, Selected[_leftSubTab]);

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 编辑器「中栏实体列表」泛型基类：模板过滤页签 + 搜索栏 +「从模板添加」/「快速添加」+ 双行条目列表
    /// （拖拽句柄 / 模板色点 / 若干列 / 删除按钮）+ 拖拽重排 + 延迟删除 + 上下键导航。
    ///
    /// <para>六个系统的中栏列表此前是同一份骨架的六份拷贝。各面板真正不同的只有
    /// <see cref="DrawRowColumns"/>（列布局）与新增 / 搜索规则，其余全部收口于此。</para>
    ///
    /// <para>顺带统一了三处既有漂移：<c>SubStyle</c> 灰度（0.6f vs 0.62f）、
    /// 值行垂直居中算法（<c>+3/-6</c> vs 按行高居中）、以及把 <c>_pendingSelect</c>
    /// 由 <c>static</c> 改为实例字段（原写法在同时存在两个编辑器窗口时会串台）。</para>
    /// </summary>
    /// <typeparam name="TEntity">实体类型（道具 / 仓库 / 商店 / 蓝图 / 装备组 / 技能）。</typeparam>
    /// <typeparam name="TTemplate">其模板类型。</typeparam>
    public abstract class EditorEntityListPanel<TEntity, TTemplate>
        where TEntity : class where TTemplate : class
    {
        // ── 布局常量（此前六处各声明一遍，值相同）──────────────────────────────────
        protected const float KeyRowH     = 13f;   // 列名行高
        protected const float ValRowH     = 22f;   // 值行高
        protected const float DragHandleW = 16f;   // 拖拽句柄列宽
        protected const float DotW        = 14f;   // 模板色点宽
        protected const float DelBtnW     = 20f;   // 删除按钮宽
        protected const float Pad         = 4f;    // 列间距

        // ── 共享样式 ────────────────────────────────────────────────────────────────
        private static GUIStyle _keyStyle, _idStyle, _subStyle;

        /// <summary>列名（表头）样式：淡蓝小字。</summary>
        protected static GUIStyle KeyStyle => _keyStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.6f, 0.85f, 1.0f) }, wordWrap = false };

        /// <summary>ID 列样式：粗体、裁切。</summary>
        protected static GUIStyle IdStyle => _idStyle ??= new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 11, wordWrap = false, clipping = TextClipping.Clip };

        /// <summary>次要列样式：灰色小字、裁切。</summary>
        protected static GUIStyle SubStyle => _subStyle ??= new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.62f, 0.62f, 0.62f) }, wordWrap = false, clipping = TextClipping.Clip };

        private Vector2 _scroll;
        private string  _search         = string.Empty;
        private string  _templateFilter;          // null = 「全部」
        private TEntity _pendingSelect;           // 实例字段：两个窗口各自独立

        private readonly EditorReorderableDrag _drag;

        /// <param name="dragId">拖拽状态机的稳定标识串（每个面板用各自的串）。</param>
        protected EditorEntityListPanel(string dragId)
            => _drag = new EditorReorderableDrag(dragId);

        #region 子类契约

        /// <summary>数据库中的实体列表。</summary>
        protected abstract List<TEntity> Entities(InventoryDatabase db);

        /// <summary>数据库中的模板列表（用于顶部过滤页签与「从模板添加」菜单）。</summary>
        protected abstract List<TTemplate> Templates(InventoryDatabase db);

        /// <summary>模板显示名 / 键。</summary>
        protected abstract string TemplateName(TTemplate t);

        /// <summary>实体引用的模板名（用于按模板过滤）。</summary>
        protected abstract string TemplateRefOf(TEntity e);

        /// <summary>实体 ID（用于重复高亮）。</summary>
        protected abstract string IdOf(TEntity e);

        /// <summary>实体种类（用于取重复 ID 集合）。</summary>
        protected abstract EInventoryEntityKind Kind { get; }

        /// <summary>实体名词（Undo 文案：删除X / 调整X顺序 / …）。</summary>
        protected abstract string Noun { get; }

        /// <summary>行首色点颜色（一般取来源模板的标识色）。</summary>
        protected abstract Color RowDotColor(InventoryDatabase db, TEntity e);

        /// <summary>搜索匹配规则（道具面板需要 <paramref name="db"/> 以按属性值匹配）。</summary>
        protected abstract bool Matches(InventoryDatabase db, TEntity e, string term);

        /// <summary>从模板新建一个实体并加入数据库，返回之（含 RecordUndo / MarkDirty）。</summary>
        protected abstract TEntity AddFromTemplate(IInventoryEditorContext ctx, string templateName);

        /// <summary>复制末尾条目新建一个实体并加入数据库，返回之（含 RecordUndo / MarkDirty）。</summary>
        protected abstract TEntity QuickAdd(IInventoryEditorContext ctx);

        /// <summary>
        /// 绘制该行的列（表头行 + 值行）。基类已画好背景、句柄、色点与删除按钮，
        /// 子类只需从 <paramref name="contentX"/> 起向右排布自己的列。
        /// </summary>
        /// <param name="contentX">内容区起始 X（已跳过句柄列与色点）。</param>
        /// <param name="contentRight">内容区右边界（已扣掉删除按钮列）；动态列布局据此决定放得下几列。</param>
        /// <param name="valY">值行中已垂直居中的 Y。</param>
        /// <param name="valH">值行控件高（单行高）。</param>
        protected abstract void DrawRowColumns(InventoryDatabase db, TEntity e,
            Rect keyRow, float contentX, float contentRight, float valY, float valH);

        /// <summary>「从模板添加」菜单在无可用模板时的提示。</summary>
        protected virtual string NoTemplateHint => Fmt("（无可用{0}模板）", Tr(Noun));

        #endregion

        #region 绘制

        /// <summary>绘制列表，返回当前选中的实体引用。</summary>
        public TEntity DrawList(IInventoryEditorContext ctx, TEntity selected)
        {
            var db        = ctx.Database;
            var entities  = Entities(db);
            var templates = Templates(db);

            // 过滤页签绑定的模板被删除后回落到「全部」
            if (_templateFilter != null && !TemplateExists(templates, _templateFilter))
                _templateFilter = null;

            _templateFilter = EditorFilterTabs.Draw(_templateFilter, templates, TemplateName);

            // ── 工具栏 ──────────────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(Tr("从模板添加"), EditorStyles.toolbarDropDown, GUILayout.Width(84)))
                ShowAddFromTemplateMenu(ctx);
            using (new EditorGUI.DisabledScope(entities.Count == 0))
            {
                if (GUILayout.Button(Tr("快速添加"), EditorStyles.toolbarButton, GUILayout.Width(72)))
                    selected = QuickAdd(ctx);
            }
            EditorGUILayout.EndHorizontal();

            _drag.BeginFrame();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int deleteIndex = -1;
            var visible     = new List<TEntity>();   // 本帧可见（已过滤）条目，供键盘上下键导航
            var dupIds      = ctx.DuplicateIdsOf(Kind);

            for (int i = 0; i < entities.Count; i++)
            {
                var e = entities[i];

                if (_templateFilter != null && TemplateRefOf(e) != _templateFilter) continue;
                if (!Matches(db, e, _search)) continue;

                visible.Add(e);

                string id     = IdOf(e);
                bool isDup    = dupIds.Contains(string.IsNullOrWhiteSpace(id) ? string.Empty : id);
                bool isSel    = ReferenceEquals(e, selected);

                Rect keyRow   = EditorGUILayout.GetControlRect(false, KeyRowH);
                Rect valRow   = EditorGUILayout.GetControlRect(false, ValRowH);
                Rect fullRect = Rect.MinMaxRect(keyRow.xMin, keyRow.yMin, valRow.xMax, valRow.yMax);

                _drag.RecordRow(i, fullRect);

                // 三层行背景：选中 → 重复 ID → 拖拽源
                if (isSel)
                    InventoryEditorStyles.DrawRowBackground(fullRect, InventoryEditorStyles.SelectedColor);
                if (isDup)
                    InventoryEditorStyles.DrawRowBackground(fullRect,
                        new Color(InventoryEditorStyles.ErrorColor.r,
                                  InventoryEditorStyles.ErrorColor.g,
                                  InventoryEditorStyles.ErrorColor.b, 0.25f));
                if (_drag.IsDragSource(i))
                    InventoryEditorStyles.DrawRowBackground(fullRect, EditorReorderableDrag.DragSourceTint);

                var delRect = new Rect(fullRect.xMax - DelBtnW, valRow.y + 2, DelBtnW - 2, ValRowH - 4);
                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    deleteIndex = i;

                var dragRect = new Rect(fullRect.xMin, fullRect.yMin, DragHandleW - 2, fullRect.height);
                _drag.DrawHandle(dragRect, i);

                // 模板色点
                float cx = fullRect.x + DragHandleW;
                InventoryEditorStyles.DrawColorDot(
                    new Rect(cx, fullRect.y + (fullRect.height - DotW) * 0.5f, DotW, DotW),
                    RowDotColor(db, e));
                cx += DotW + Pad;

                float vy           = valRow.y + (ValRowH - EditorGUIUtility.singleLineHeight) * 0.5f;
                float vh           = EditorGUIUtility.singleLineHeight;
                float contentRight = fullRect.xMax - DelBtnW - Pad;
                DrawRowColumns(db, e, keyRow, cx, contentRight, vy, vh);

                // 行点击选中（排除删除按钮与拖拽句柄）
                if (Event.current.type == EventType.MouseDown
                    && fullRect.Contains(Event.current.mousePosition)
                    && !delRect.Contains(Event.current.mousePosition)
                    && !dragRect.Contains(Event.current.mousePosition))
                {
                    selected = e;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            _drag.EndFrame(ctx, entities, $"调整{Noun}顺序", DragHandleW, DelBtnW);

            EditorGUILayout.EndScrollView();
            float viewportHeight = GUILayoutUtility.GetLastRect().height;

            // 延迟删除：在列表绘制之后处理，避免遍历中改动集合
            if (deleteIndex >= 0 && deleteIndex < entities.Count)
            {
                if (ReferenceEquals(entities[deleteIndex], selected)) selected = null;
                ctx.RecordUndo($"删除{Noun}");
                entities.RemoveAt(deleteIndex);
                ctx.MarkDirty();
            }

            // 键盘 上/下 方向键：在可见条目间切换选中，并在越界时自动滚动一行
            float rowPitch = KeyRowH + ValRowH + 2f * EditorGUIUtility.standardVerticalSpacing;
            if (EditorListKeyboardNav.HandleUpDown(visible, selected, out var nav,
                    ref _scroll, rowPitch, viewportHeight))
            {
                selected = nav;
                GUI.FocusControl(null);
                ctx.Repaint();
            }

            return selected;
        }

        /// <summary>取出并清空「待选中」条目（由所属 SystemTab 在每帧 Layout 前调用）。</summary>
        public TEntity ConsumePendingSelect()
        {
            var e = _pendingSelect;
            _pendingSelect = null;
            return e;
        }

        private void ShowAddFromTemplateMenu(IInventoryEditorContext ctx)
        {
            var templates = Templates(ctx.Database);
            var menu      = new GenericMenu();

            if (templates.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent(NoTemplateHint));
            }
            else
            {
                foreach (var t in templates)
                {
                    string name = TemplateName(t);
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        _pendingSelect = AddFromTemplate(ctx, name);
                        ctx.Repaint();
                    });
                }
            }
            menu.ShowAsContext();
        }

        private bool TemplateExists(List<TTemplate> templates, string name)
        {
            foreach (var t in templates)
                if (TemplateName(t) == name) return true;
            return false;
        }

        #endregion

        #region 辅助

        /// <summary>生成唯一 ID：<c>{prefix}{n}</c>，n 自「当前条目数 + 1」起递增直到 <paramref name="exists"/> 返回 false。</summary>
        protected string GenerateId(InventoryDatabase db, string prefix, Func<string, bool> exists)
        {
            int n = Entities(db).Count + 1;
            string id;
            do { id = prefix + n; n++; }
            while (exists(id));
            return id;
        }

        #endregion
    }
}

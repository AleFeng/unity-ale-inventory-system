using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「道具系统」页签：三列布局。
    /// 左列 = 子页签 + 对应主列表；中列 = 道具列表；右列 = 上下文 Inspector（共用）。
    ///
    /// 选中互斥规则：左侧选中时清空中列选中，中列选中时清空左侧选中索引。
    /// 这样保证两侧各自都能被「再次点击选中」，不会因引用/索引相同而跳过变化检测。
    /// </summary>
    public class ItemSystemTab
    {
        private const float LeftWidth      = 260f; // 左侧面板 宽度
        private const float MiddleWidthMin = 320f; // 中间面板宽度 最小值
        private const float RightWidth     = 380f; // 右侧面板 宽度
        private const float Padding        = 4f;   // 面板间距

        private static readonly string[] LeftSubTabs = { "枚举类型", "功能标签", "道具模板" };

        private enum RightMode { Entity, Item }

        private int       _leftSubTab;
        private int       _selectedEnumIndex     = -1;
        private int       _selectedTagIndex      = -1;
        private int       _selectedTemplateIndex = -1;
        private Item      _selectedItem          = null;
        private RightMode _rightMode             = RightMode.Entity;

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        private bool _pendingDeleteItem;

        private readonly EnumTypePanel        _enumPanel          = new EnumTypePanel();
        private readonly FunctionTagPanel     _tagPanel           = new FunctionTagPanel();
        private readonly ItemTemplatePanel    _templatePanel      = new ItemTemplatePanel();
        private readonly ItemListPanel        _itemListPanel      = new ItemListPanel();
        private readonly ItemInspectorPanel   _itemInspectorPanel = new ItemInspectorPanel();

        // ── 互斥选中辅助 ─────────────────────────────────────────────────────────────

        /// <summary>激活左侧 Inspector：清空道具列选中，切换到 Entity 模式。</summary>
        private void ActivateEntity()
        {
            _selectedItem = null;
            _rightMode    = RightMode.Entity;
        }

        /// <summary>激活中列 Inspector：清空左侧三个索引，切换到 Item 模式。</summary>
        private void ActivateItem(Item item)
        {
            _selectedEnumIndex     = -1;
            _selectedTagIndex      = -1;
            _selectedTemplateIndex = -1;
            _selectedItem          = item;
            _rightMode             = item == null ? RightMode.Entity : RightMode.Item;
        }

        // ── 外部回调 ─────────────────────────────────────────────────────────────────

        public void OnDatabaseChanged(IInventoryEditorContext ctx)
        {
            _selectedEnumIndex     = -1;
            _selectedTagIndex      = -1;
            _selectedTemplateIndex = -1;
            _selectedItem          = null;
            _rightMode             = RightMode.Entity;
            _enumPanel.Invalidate();
            _tagPanel.Invalidate();
            _templatePanel.Invalidate();
        }

        public void OnUndoRedo()
        {
            _enumPanel.Invalidate();
            _tagPanel.Invalidate();
            _templatePanel.Invalidate();
        }

        // ── 主绘制 ───────────────────────────────────────────────────────────────────

        public void OnGUI(Rect rect, IInventoryEditorContext ctx)
        {
            // ── Layout 事件前：执行所有延迟动作 ─────────────────────────────────────
            if (Event.current.type == EventType.Layout)
            {
                // 来自 Inspector 删除按钮的延迟删除
                if (_pendingDeleteItem)
                {
                    _pendingDeleteItem = false;
                    if (_selectedItem != null && ctx.Database.Items.Contains(_selectedItem))
                    {
                        ctx.RecordUndo("删除道具");
                        ctx.Database.Items.Remove(_selectedItem);
                        ctx.MarkDirty();
                    }
                    ActivateEntity();
                }

                // 来自「从模板添加」菜单的延迟选中
                var pending = _itemListPanel.ConsumePendingSelect();
                if (pending != null)
                    ActivateItem(pending);
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

        // ── 左列 ─────────────────────────────────────────────────────────────────────

        private void DrawLeft(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            int prev = _leftSubTab;
            _leftSubTab = GUILayout.Toolbar(_leftSubTab, LeftSubTabs);
            if (_leftSubTab != prev)
            {
                // 子页签切换：保持 Entity 模式，并清空道具列选中
                _selectedItem = null;
                _rightMode    = RightMode.Entity;
            }

            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
            switch (_leftSubTab)
            {
                case 0:
                {
                    int sel = _enumPanel.DrawMasterList(ctx, _selectedEnumIndex);
                    if (sel != _selectedEnumIndex)
                    {
                        _selectedEnumIndex = sel;
                        ActivateEntity();
                        _enumPanel.Invalidate();
                    }
                    break;
                }
                case 1:
                {
                    int sel = _tagPanel.DrawMasterList(ctx, _selectedTagIndex);
                    if (sel != _selectedTagIndex)
                    {
                        _selectedTagIndex = sel;
                        ActivateEntity();
                    }
                    break;
                }
                case 2:
                {
                    int sel = _templatePanel.DrawMasterList(ctx, _selectedTemplateIndex);
                    if (sel != _selectedTemplateIndex)
                    {
                        _selectedTemplateIndex = sel;
                        ActivateEntity();
                    }
                    break;
                }
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ── 中列 ─────────────────────────────────────────────────────────────────────

        private void DrawMiddle(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            // 关键：当前 Inspector 不在 Item 模式时，传 null 给列表，
            // 使列表不显示任何高亮，同时让下一次点击任意道具都能触发变化检测。
            Item displaySelected = _rightMode == RightMode.Item ? _selectedItem : null;
            Item sel = _itemListPanel.DrawList(ctx, displaySelected);

            if (sel != displaySelected)
            {
                ActivateItem(sel);
            }

            GUILayout.EndArea();
        }

        // ── 右列 ─────────────────────────────────────────────────────────────────────

        private void DrawRight(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            bool drawItem = _rightMode == RightMode.Item
                && _selectedItem != null
                && ctx.Database.Items.Contains(_selectedItem);

            if (!drawItem && _rightMode == RightMode.Item)
            {
                // 选中项已不在数据库（极端情况保护）
                ActivateEntity();
            }

            if (drawItem)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("道具 Inspector", InventoryEditorStyles.Header);
                var delStyle = new GUIStyle(EditorStyles.miniButton)
                    { normal = { textColor = new Color(0.9f, 0.45f, 0.45f) } };
                if (GUILayout.Button("删除道具", delStyle, GUILayout.Width(68)))
                    _pendingDeleteItem = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            if (drawItem)
            {
                _itemInspectorPanel.DrawInspector(ctx, _selectedItem);
            }
            else
            {
                switch (_leftSubTab)
                {
                    case 0: _enumPanel.DrawInspector(ctx,
                        GetSelected(ctx.Database.EnumTypes, _selectedEnumIndex)); break;
                    case 1: _tagPanel.DrawInspector(ctx,
                        GetSelected(ctx.Database.FunctionTags, _selectedTagIndex)); break;
                    case 2: _templatePanel.DrawInspector(ctx,
                        GetSelected(ctx.Database.ItemTemplates, _selectedTemplateIndex)); break;
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static T GetSelected<T>(System.Collections.Generic.List<T> list, int index)
            where T : class
        {
            if (index < 0 || index >= list.Count) return null;
            return list[index];
        }
    }
}

using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;
    /// <summary>
    /// 「仓库系统」页签：三列布局，与道具系统页签对称。
    /// 左列 = 子页签 + 仓库模板列表；中列 = 仓库列表；右列 = 上下文 Inspector（共用）。
    /// </summary>
    public class InventorySystemTab
    {
        private const float LeftWidth      = 260f; // 左侧面板 宽度
        private const float MiddleWidthMin = 320f; // 中间面板宽度 最小值
        private const float RightWidth     = 380f; // 右侧面板 宽度
        private const float Padding        = 4f;   // 面板间距

        private static readonly string[] LeftSubTabs = { "整理选项", "数字格式", "仓库模板" };

        private enum RightMode { Entity, Inventory, SortOption, NumberFormat }

        private int       _leftSubTab;
        private int       _selectedTemplateIndex     = -1;
        private Inventory _selectedInventory;
        private RightMode _rightMode                 = RightMode.Entity;
        private int       _selectedSortOptionIndex   = -1;
        private int       _selectedNumberFormatIndex = -1;

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        private bool _pendingDeleteInventory;

        private readonly InventoryTemplatePanel    _templatePanel     = new InventoryTemplatePanel();
        private readonly InventoryListPanel        _listPanel         = new InventoryListPanel();
        private readonly InventoryInspectorPanel   _inspectorPanel    = new InventoryInspectorPanel();
        private readonly SortOptionPanel           _sortOptionPanel   = new SortOptionPanel();
        private readonly NumberFormatConfigPanel   _numberFormatPanel = new NumberFormatConfigPanel();

        private void ActivateEntity()
        {
            _selectedInventory = null;
            _rightMode         = RightMode.Entity;
        }

        private void ActivateInventory(Inventory inv)
        {
            _selectedTemplateIndex = -1;
            _selectedInventory     = inv;
            _rightMode             = inv == null ? RightMode.Entity : RightMode.Inventory;
        }

        public void OnDatabaseChanged(IInventoryEditorContext ctx)
        {
            _selectedTemplateIndex     = -1;
            _selectedInventory         = null;
            _selectedSortOptionIndex   = -1;
            _selectedNumberFormatIndex = -1;
            _rightMode                 = RightMode.Entity;
            _templatePanel.Invalidate();
            _sortOptionPanel.Invalidate();
            _numberFormatPanel.Invalidate();
        }

        public void OnUndoRedo()
        {
            _templatePanel.Invalidate();
            _sortOptionPanel.Invalidate();
            _numberFormatPanel.Invalidate();
        }

        public void OnGUI(Rect rect, IInventoryEditorContext ctx)
        {
            if (Event.current.type == EventType.Layout)
            {
                if (_pendingDeleteInventory)
                {
                    _pendingDeleteInventory = false;
                    if (_selectedInventory != null
                        && ctx.Database.Inventories.Contains(_selectedInventory))
                    {
                        ctx.RecordUndo("删除仓库");
                        ctx.Database.Inventories.Remove(_selectedInventory);
                        ctx.MarkDirty();
                    }
                    ActivateEntity();
                }

                var pending = _listPanel.ConsumePendingSelect();
                if (pending != null)
                    ActivateInventory(pending);
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

            int prev = _leftSubTab;
            _leftSubTab = GUILayout.Toolbar(_leftSubTab, LeftSubTabs);
            if (_leftSubTab != prev)
            {
                _selectedInventory         = null;
                _selectedTemplateIndex     = -1;
                _selectedSortOptionIndex   = -1;
                _selectedNumberFormatIndex = -1;
                _rightMode                 = RightMode.Entity;
            }

            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            if (_leftSubTab == 0)
            {
                // 整理选项列表
                int sel = _sortOptionPanel.DrawMasterList(ctx, _selectedSortOptionIndex);
                if (sel != _selectedSortOptionIndex)
                {
                    _selectedSortOptionIndex = sel;
                    _selectedInventory       = null;
                    _rightMode               = RightMode.SortOption;
                }
            }
            else if (_leftSubTab == 1)
            {
                // 数字格式配置列表
                int sel = _numberFormatPanel.DrawMasterList(ctx, _selectedNumberFormatIndex);
                if (sel != _selectedNumberFormatIndex)
                {
                    _selectedNumberFormatIndex = sel;
                    _selectedInventory         = null;
                    _rightMode                 = RightMode.NumberFormat;
                }
            }
            else
            {
                // 仓库模板列表
                int sel = _templatePanel.DrawMasterList(ctx, _selectedTemplateIndex);
                if (sel != _selectedTemplateIndex)
                {
                    _selectedTemplateIndex = sel;
                    ActivateEntity();
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMiddle(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            Inventory displaySelected = _rightMode == RightMode.Inventory ? _selectedInventory : null;
            Inventory sel = _listPanel.DrawList(ctx, displaySelected);

            if (sel != displaySelected)
                ActivateInventory(sel);

            GUILayout.EndArea();
        }

        private void DrawRight(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            bool drawInventory = _rightMode == RightMode.Inventory
                && _selectedInventory != null
                && ctx.Database.Inventories.Contains(_selectedInventory);

            if (!drawInventory && _rightMode == RightMode.Inventory)
                ActivateEntity();

            if (drawInventory)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("仓库 Inspector", InventoryEditorStyles.Header);
                var delStyle = new GUIStyle(EditorStyles.miniButton)
                    { normal = { textColor = new Color(0.9f, 0.45f, 0.45f) } };
                if (GUILayout.Button("删除仓库", delStyle, GUILayout.Width(64)))
                    _pendingDeleteInventory = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            if (drawInventory)
            {
                _inspectorPanel.DrawInspector(ctx, _selectedInventory);
            }
            else if (_rightMode == RightMode.SortOption)
            {
                var db = ctx.Database;
                SortOption selected = null;
                if (_selectedSortOptionIndex >= 0 && _selectedSortOptionIndex < db.SortOptions.Count)
                    selected = db.SortOptions[_selectedSortOptionIndex];
                _sortOptionPanel.DrawInspector(ctx, selected);
            }
            else if (_rightMode == RightMode.NumberFormat)
            {
                var db = ctx.Database;
                NumberFormatConfig selected = null;
                if (_selectedNumberFormatIndex >= 0 && _selectedNumberFormatIndex < db.NumberFormatConfigs.Count)
                    selected = db.NumberFormatConfigs[_selectedNumberFormatIndex];
                _numberFormatPanel.DrawInspector(ctx, selected);
            }
            else
            {
                InventoryTemplate selected = null;
                var db = ctx.Database;
                if (_selectedTemplateIndex >= 0 && _selectedTemplateIndex < db.InventoryTemplates.Count)
                    selected = db.InventoryTemplates[_selectedTemplateIndex];
                _templatePanel.DrawInspector(ctx, selected);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

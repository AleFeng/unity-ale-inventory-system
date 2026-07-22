using InventorySystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 「商店系统」页签：三列布局，与仓库系统页签对称。
    /// 左列 = 商店模板列表；中列 = 商店列表；右列 = 上下文 Inspector（模板 / 商店）。
    /// </summary>
    public class ShopSystemTab
    {
        // 与「仓库系统」页签保持一致的三列宽度。
        private const float LeftWidth      = 260f;
        private const float MiddleWidthMin = 320f;
        private const float RightWidth     = 380f;
        private const float Padding        = 4f;

        private enum RightMode { Entity, Shop }

        private int       _selectedTemplateIndex = -1;
        private Shop      _selectedShop          = null;
        private RightMode _rightMode             = RightMode.Entity;

        private Vector2 _leftScroll;
        private Vector2 _rightScroll;

        private bool _pendingDeleteShop;

        private readonly ShopTemplatePanel  _templatePanel  = new ShopTemplatePanel();
        private readonly ShopListPanel      _listPanel      = new ShopListPanel();
        private readonly ShopInspectorPanel _inspectorPanel = new ShopInspectorPanel();

        private void ActivateEntity()
        {
            _selectedShop = null;
            _rightMode    = RightMode.Entity;
        }

        private void ActivateShop(Shop shop)
        {
            _selectedTemplateIndex = -1;
            _selectedShop          = shop;
            _rightMode             = shop == null ? RightMode.Entity : RightMode.Shop;
        }

        public void OnDatabaseChanged(IInventoryEditorContext ctx)
        {
            _selectedTemplateIndex = -1;
            _selectedShop          = null;
            _rightMode             = RightMode.Entity;
            _templatePanel.Invalidate();
        }

        public void OnUndoRedo()
        {
            _templatePanel.Invalidate();
        }

        public void OnGUI(Rect rect, IInventoryEditorContext ctx)
        {
            if (Event.current.type == EventType.Layout)
            {
                if (_pendingDeleteShop)
                {
                    _pendingDeleteShop = false;
                    if (_selectedShop != null && ctx.Database.Shops.Contains(_selectedShop))
                    {
                        ctx.RecordUndo("删除商店");
                        ctx.Database.Shops.Remove(_selectedShop);
                        ctx.MarkDirty();
                    }
                    ActivateEntity();
                }

                var pending = _listPanel.ConsumePendingSelect();
                if (pending != null)
                    ActivateShop(pending);
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
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            int sel = _templatePanel.DrawMasterList(ctx, _selectedTemplateIndex);
            if (sel != _selectedTemplateIndex)
            {
                _selectedTemplateIndex = sel;
                ActivateEntity();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMiddle(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            Shop displaySelected = _rightMode == RightMode.Shop ? _selectedShop : null;
            Shop sel = _listPanel.DrawList(ctx, displaySelected);

            if (sel != displaySelected)
                ActivateShop(sel);

            GUILayout.EndArea();
        }

        private void DrawRight(Rect rect, IInventoryEditorContext ctx)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);

            bool drawShop = _rightMode == RightMode.Shop
                && _selectedShop != null
                && ctx.Database.Shops.Contains(_selectedShop);

            if (!drawShop && _rightMode == RightMode.Shop)
                ActivateEntity();

            if (drawShop)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("商店 Inspector", InventoryEditorStyles.Header);
                var delStyle = new GUIStyle(EditorStyles.miniButton)
                    { normal = { textColor = new Color(0.9f, 0.45f, 0.45f) } };
                if (GUILayout.Button("删除商店", delStyle, GUILayout.Width(64)))
                    _pendingDeleteShop = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            // 隐藏横向滚动条：强制内容自适应填满 Inspector 宽度，避免条目超出可视范围被裁切
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, false, false,
                GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);

            if (drawShop)
            {
                _inspectorPanel.DrawInspector(ctx, _selectedShop);
            }
            else
            {
                ShopTemplate selected = null;
                var db = ctx.Database;
                if (_selectedTemplateIndex >= 0 && _selectedTemplateIndex < db.ShopTemplates.Count)
                    selected = db.ShopTemplates[_selectedTemplateIndex];
                _templatePanel.DrawInspector(ctx, selected);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}

using System.Collections.Generic;
using InventorySystem.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 枚举类型面板：
    ///   左侧 — 枚举类型主列表（名称 + 项数，可拖拽排序）
    ///   右侧 Inspector —
    ///     1. 枚举名称
    ///     2. 属性字段定义（该类型下所有枚举项共享的 schema）
    ///     3. 枚举项列表（可重排，名称可编辑，值只读）
    ///     4. 选中枚举项的属性值编辑区
    /// </summary>
    public class EnumTypePanel
    {
        // ── 主列表 ReorderableList 状态 ────────────────────────────────────────────
        private ReorderableList         _masterList;
        private List<EnumType>          _boundMasterList;
        private int                     _selectedIndex      = -1;
        private int                     _pendingDeleteIndex = -1;
        private IInventoryEditorContext _masterCtx;

        // ── 枚举项列表 ReorderableList 状态 ───────────────────────────────────────
        private ReorderableList         _itemList;
        private EnumType                _boundEnum;
        private IInventoryEditorContext _ctx;
        private int                     _selectedItemIndex = -1;

        // ── 属性字段定义列表绘制器（实例持有，保持拖拽排序缓存）──────────────────────
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        // 样式缓存
        private GUIStyle _itemHeaderStyle;
        private GUIStyle ItemHeaderStyle => _itemHeaderStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            normal   = { textColor = new Color(0.55f, 0.85f, 1.0f) },
            fontSize = 11
        };

        // ── 主列表 ────────────────────────────────────────────────────────────────

        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            var db   = ctx.Database;
            var list = db.EnumTypes;
            _masterCtx = ctx;

            if (_masterList == null || !ReferenceEquals(_boundMasterList, list))
            {
                _selectedIndex = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                BuildMasterList(list);
            }
            else
            {
                int clamped = Mathf.Clamp(selectedIndex, -1, list.Count - 1);
                if (_selectedIndex != clamped)
                {
                    _selectedIndex    = clamped;
                    _masterList.index = clamped;
                }
            }

            // ── 标题栏 + 添加按钮 ──────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("枚举类型", InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加枚举类型");
                list.Add(new EnumType("新枚举"));
                ctx.MarkDirty();
                _selectedIndex    = list.Count - 1;
                _masterList.index = _selectedIndex;
            }
            EditorGUILayout.EndHorizontal();

            _masterList.DoLayoutList();

            // ── 延迟删除 ──────────────────────────────────────────────────────
            if (_pendingDeleteIndex >= 0)
            {
                int di = _pendingDeleteIndex;
                _pendingDeleteIndex = -1;
                if (di < list.Count)
                {
                    ctx.RecordUndo("删除枚举类型");
                    list.RemoveAt(di);
                    ctx.MarkDirty();
                    _selectedIndex    = Mathf.Clamp(_selectedIndex, -1, list.Count - 1);
                    _masterList.index = _selectedIndex;
                }
            }

            return _selectedIndex;
        }

        private void BuildMasterList(List<EnumType> list)
        {
            _boundMasterList = list;
            _masterList = new ReorderableList(list, typeof(EnumType),
                draggable: true, displayHeader: false,
                displayAddButton: false, displayRemoveButton: false);

            _masterList.elementHeight = 22f;
            _masterList.index         = _selectedIndex;

            _masterList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (active)
                    InventoryEditorStyles.DrawRowBackground(rect, InventoryEditorStyles.SelectedColor);
            };

            _masterList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= list.Count) return;
                var e    = list[index];
                float cy = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;

                var delRect   = new Rect(rect.xMax - 22, cy, 20f, EditorGUIUtility.singleLineHeight);
                var labelRect = new Rect(rect.x, cy,
                    rect.xMax - 22 - rect.x - 4, EditorGUIUtility.singleLineHeight);
                GUI.Label(labelRect, $"{e.name}  ({e.items.Count})");

                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    _pendingDeleteIndex = index;
            };

            _masterList.onSelectCallback = rl => _selectedIndex = rl.index;

            _masterList.onReorderCallback = _ =>
            {
                _masterCtx.RecordUndo("调整枚举类型顺序");
                _masterCtx.MarkDirty();
            };
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        public void DrawInspector(IInventoryEditorContext ctx, EnumType e)
        {
            if (e == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个枚举类型。");
                return;
            }

            _ctx = ctx;

            // 每帧同步枚举项属性（幂等操作，保证与 schema 一致）
            e.RebuildItemAttributes();

            // ── 1. 枚举名称 ───────────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("枚举名称", e.name);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改枚举名称");
                e.name = newName;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 2. 属性字段定义（schema）────────────────────────────────────
            _attrDefsDrawer.Draw(ctx, e.attributes, "枚举项属性字段");
            // 定义变更后立刻同步各枚举项
            e.RebuildItemAttributes();

            EditorGUILayout.Space(3);
            // 分隔线
            var sepRect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(sepRect, new Color(0.28f, 0.28f, 0.28f, 1f));
            EditorGUILayout.Space(3);

            // ── 3. 枚举项列表 ────────────────────────────────────────────────
            if (_itemList == null || _boundEnum != e)
                BuildItemList(e);

            _itemList.DoLayoutList();

            // 同步选中索引
            if (_itemList.index >= 0 && _itemList.index < e.items.Count)
                _selectedItemIndex = _itemList.index;
            else
                _selectedItemIndex = -1;

            // ── 4. 选中枚举项的属性值编辑区 ─────────────────────────────────
            if (_selectedItemIndex >= 0 && _selectedItemIndex < e.items.Count
                && e.attributes.Count > 0)
            {
                var enumItem = e.items[_selectedItemIndex];

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"▸ {enumItem.name}  的属性值", ItemHeaderStyle);
                EditorGUI.indentLevel++;

                foreach (var def in e.attributes)
                {
                    AttributeEntry entry = null;
                    foreach (var av in enumItem.attributeValues)
                        if (av.id == def.id) { entry = av; break; }

                    if (entry == null) continue;

                    var attrEnumType = def.type == EFieldType.Enum
                        ? ctx.Database.GetEnumType(def.enumTypeRef) : null;
                    AttributeFieldDrawer.Draw(ctx, def.id, entry.value, attrEnumType);
                }

                EditorGUI.indentLevel--;
            }
        }

        // ── 枚举项 ReorderableList 构建 ───────────────────────────────────────────

        private void BuildItemList(EnumType e)
        {
            _boundEnum = e;
            _itemList  = new ReorderableList(e.items, typeof(EnumItem), true, true, true, true);

            _itemList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "枚举项（值由系统分配、只读；点击行选中以编辑属性值）");

            _itemList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= e.items.Count) return;
                var item      = e.items[index];
                rect.y       += 2;
                float valueW  = 56f;
                var nameRect  = new Rect(rect.x, rect.y,
                    rect.width - valueW - 6, EditorGUIUtility.singleLineHeight);
                var valueRect = new Rect(rect.xMax - valueW, rect.y,
                    valueW, EditorGUIUtility.singleLineHeight);

                EditorGUI.BeginChangeCheck();
                string newItemName = EditorGUI.TextField(nameRect, item.name);
                if (EditorGUI.EndChangeCheck())
                {
                    _ctx.RecordUndo("修改枚举项名称");
                    item.name = newItemName;
                    _ctx.MarkDirty();
                }

                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.IntField(valueRect, item.value);
            };

            _itemList.onAddCallback = list =>
            {
                _ctx.RecordUndo("添加枚举项");
                e.AddItem("新项");
                _ctx.MarkDirty();
            };

            _itemList.onRemoveCallback = list =>
            {
                _ctx.RecordUndo("删除枚举项");
                e.RemoveItemAt(list.index);
                _ctx.MarkDirty();
            };

            _itemList.onReorderCallback = list =>
            {
                _ctx.RecordUndo("调整枚举项顺序");
                _ctx.MarkDirty();
            };

            _itemList.onSelectCallback = list =>
            {
                _selectedItemIndex = list.index;
            };
        }

        /// <summary>数据库切换、外部重置或 Undo/Redo 时调用，清空所有缓存列表。</summary>
        public void Invalidate()
        {
            // 主列表缓存
            _masterList         = null;
            _boundMasterList    = null;
            _selectedIndex      = -1;
            _pendingDeleteIndex = -1;
            // 枚举项列表缓存
            _itemList           = null;
            _boundEnum          = null;
            _selectedItemIndex  = -1;
            // 属性字段定义列表缓存
            _attrDefsDrawer.Invalidate();
        }
    }
}

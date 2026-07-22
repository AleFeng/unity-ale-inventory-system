using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 道具模板面板：左侧主列表（模板行，可拖拽排序）+ 右侧 Inspector（名称、颜色、属性字段定义列表）。
    /// </summary>
    public class ItemTemplatePanel
    {
        // ── 主列表 ReorderableList 状态 ────────────────────────────────────────────
        private ReorderableList         _masterList;
        private List<ItemTemplate>      _boundList;
        private int                     _selectedIndex      = -1;
        private int                     _pendingDeleteIndex = -1;
        private IInventoryEditorContext _masterCtx;

        // ── 属性字段定义列表绘制器（实例持有，保持拖拽排序缓存）──────────────────────
        private readonly AttributeDefinitionListDrawer _attrDefsDrawer = new AttributeDefinitionListDrawer();

        // ── 主列表 ────────────────────────────────────────────────────────────────

        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            var db   = ctx.Database;
            var list = db.ItemTemplates;
            _masterCtx = ctx;

            if (_masterList == null || !ReferenceEquals(_boundList, list))
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
            EditorGUILayout.LabelField("道具模板", InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加道具模板");
                list.Add(new ItemTemplate("新模板"));
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
                    ctx.RecordUndo("删除道具模板");
                    list.RemoveAt(di);
                    ctx.MarkDirty();
                    _selectedIndex    = Mathf.Clamp(_selectedIndex, -1, list.Count - 1);
                    _masterList.index = _selectedIndex;
                }
            }

            return _selectedIndex;
        }

        private void BuildMasterList(List<ItemTemplate> list)
        {
            _boundList  = list;
            _masterList = new ReorderableList(list, typeof(ItemTemplate),
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
                var t    = list[index];
                float cy = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) * 0.5f;

                // 圆形色点
                var dotRect = new Rect(rect.x, cy, 14f, EditorGUIUtility.singleLineHeight);
                InventoryEditorStyles.DrawColorDot(dotRect, t.color);

                var delRect   = new Rect(rect.xMax - 22, cy, 20f, EditorGUIUtility.singleLineHeight);
                var labelRect = new Rect(dotRect.xMax + 3, cy,
                    rect.xMax - 22 - dotRect.xMax - 7, EditorGUIUtility.singleLineHeight);
                GUI.Label(labelRect, t.name);

                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    _pendingDeleteIndex = index;
            };

            _masterList.onSelectCallback = rl => _selectedIndex = rl.index;

            _masterList.onReorderCallback = _ =>
            {
                _masterCtx.RecordUndo("调整道具模板顺序");
                _masterCtx.MarkDirty();
            };
        }

        /// <summary>数据库切换或外部重置时调用，清空主列表缓存。</summary>
        public void Invalidate()
        {
            _masterList         = null;
            _boundList          = null;
            _selectedIndex      = -1;
            _pendingDeleteIndex = -1;
            _attrDefsDrawer.Invalidate();
        }

        // ── Inspector ────────────────────────────────────────────────────────────

        public void DrawInspector(IInventoryEditorContext ctx, ItemTemplate template)
        {
            if (template == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个道具模板。");
                return;
            }

            var db = ctx.Database;

            // ── 模板名称 + 颜色 ──────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            string newName  = EditorGUILayout.TextField("模板名称", template.name);
            Color  newColor = EditorGUILayout.ColorField("标识颜色", template.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改模板基本信息");
                template.name  = newName;
                template.color = newColor;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 默认功能标签 ─────────────────────────────────────────────────
            EditorGUILayout.LabelField("默认功能标签", InventoryEditorStyles.Header);
            if (db.FunctionTags.Count == 0)
            {
                EditorGUILayout.LabelField("（暂无功能标签，请先在左侧「功能标签」中创建）",
                    EditorStyles.miniLabel);
            }
            else
            {
                foreach (var tag in db.FunctionTags)
                {
                    bool has = template.tagRefs.Contains(tag.name);
                    bool now = EditorGUILayout.ToggleLeft(tag.name, has);
                    if (now != has)
                    {
                        ctx.RecordUndo(now ? "模板添加功能标签" : "模板移除功能标签");
                        if (now) template.tagRefs.Add(tag.name);
                        else     template.tagRefs.Remove(tag.name);
                        ctx.MarkDirty();
                    }
                }
            }

            EditorGUILayout.Space(6);

            // ── 仓库属性 ─────────────────────────────────────────────────────
            EditorGUILayout.LabelField("仓库属性", InventoryEditorStyles.Header);
            EditorGUI.BeginChangeCheck();
            float newWeight     = EditorGUILayout.FloatField("重量", template.weight);
            int   newStackLimit = EditorGUILayout.IntField("堆叠上限", template.stackLimit);
            bool  newHideInInventory = EditorGUILayout.Toggle("仓库中隐藏", template.hideInInventory);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改模板仓库属性");
                template.weight     = Mathf.Max(0f, newWeight);
                template.stackLimit = Mathf.Max(0, newStackLimit);
                template.hideInInventory = newHideInInventory;
                ctx.MarkDirty();
            }

            EditorGUILayout.Space(6);

            // ── 属性字段定义 ─────────────────────────────────────────────────
            _attrDefsDrawer.Draw(ctx, template.attributes, "属性字段");
        }
    }
}

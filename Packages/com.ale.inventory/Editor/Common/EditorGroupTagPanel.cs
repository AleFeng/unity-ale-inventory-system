using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「分组标签」面板泛型基类：左侧主列表（标签行，色点 + ID，可拖拽排序）+ 右侧 Inspector
    /// （ID / 标识颜色 / 名称 / 描述）。制作、装备、技能三个系统的分组标签面板共用本实现。
    ///
    /// <para>三者的数据类都继承 <see cref="GroupTag"/>（同一套 id / displayName / description / color
    /// 与 <see cref="GroupTag.NormalizeTextFields"/>），面板此前是三份逐字符相同的拷贝，
    /// 仅「取哪个列表」与「新 ID 前缀」不同 —— 现由两个子类契约成员表达。</para>
    /// </summary>
    /// <typeparam name="T">分组标签类型（<c>CraftingGroupTag</c> / <c>EquipmentGroupTag</c> / <c>SkillGroupTag</c>）。</typeparam>
    public abstract class EditorGroupTagPanel<T> where T : GroupTag, new()
    {
        private ReorderableList         _masterList;
        private List<T>                 _boundList;
        private int                     _selectedIndex      = -1;
        private int                     _pendingDeleteIndex = -1;
        private IInventoryEditorContext _masterCtx;

        #region 子类契约

        /// <summary>取本系统的分组标签列表（如 <c>db.CraftingGroupTags</c>）。</summary>
        protected abstract List<T> GetList(InventoryDatabase db);

        /// <summary>自动生成 ID 的前缀（如 <c>"group_"</c>）。</summary>
        protected abstract string IdPrefix { get; }

        #endregion

        #region 主列表

        public int DrawMasterList(IInventoryEditorContext ctx, int selectedIndex)
        {
            var db   = ctx.Database;
            var list = GetList(db);
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

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("分组标签", InventoryEditorStyles.Header);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                ctx.RecordUndo("添加分组标签");
                list.Add(NewTag(db));
                ctx.MarkDirty();
                _selectedIndex    = list.Count - 1;
                _masterList.index = _selectedIndex;
            }
            EditorGUILayout.EndHorizontal();

            _masterList.DoLayoutList();

            // 删除延迟到列表绘制之后，避免在 ReorderableList 回调中改动其绑定的列表。
            if (_pendingDeleteIndex >= 0)
            {
                int di = _pendingDeleteIndex;
                _pendingDeleteIndex = -1;
                if (di < list.Count)
                {
                    ctx.RecordUndo("删除分组标签");
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

                var dotRect = new Rect(rect.x, cy, 14f, EditorGUIUtility.singleLineHeight);
                InventoryEditorStyles.DrawColorDot(dotRect, t.color);

                var delRect   = new Rect(rect.xMax - 22, cy, 20f, EditorGUIUtility.singleLineHeight);
                var labelRect = new Rect(dotRect.xMax + 3, cy,
                    rect.xMax - 22 - dotRect.xMax - 7, EditorGUIUtility.singleLineHeight);
                GUI.Label(labelRect, string.IsNullOrEmpty(t.id) ? "(空 ID)" : t.id);

                if (GUI.Button(delRect, "✕", EditorStyles.miniButton))
                    _pendingDeleteIndex = index;
            };

            _masterList.onSelectCallback = rl => _selectedIndex = rl.index;

            _masterList.onReorderCallback = _ =>
            {
                _masterCtx.RecordUndo("调整分组标签顺序");
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
        }

        #endregion

        #region Inspector

        public void DrawInspector(IInventoryEditorContext ctx, T tag)
        {
            if (tag == null)
            {
                EditorGUILayout.LabelField("请选择或新建一个分组标签。");
                return;
            }

            EditorGUILayout.LabelField("基础信息", InventoryEditorStyles.Header);

            EditorGUI.BeginChangeCheck();
            string id    = EditorGUILayout.TextField("ID", tag.id);
            Color  color = EditorGUILayout.ColorField("标识颜色", tag.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改分组标签");
                tag.id    = id;
                tag.color = color;
                ctx.MarkDirty();
            }

            // 名称 / 描述为 Text 类型属性值（纯文本 fallback + 可选本地化引用），复用统一属性绘制器。
            tag.NormalizeTextFields();
            AttributeFieldDrawer.Draw(ctx, "名称", tag.displayName, null);
            AttributeFieldDrawer.Draw(ctx, "描述", tag.description, null);
        }

        #endregion

        #region 辅助

        /// <summary>新建一个标签：自动分配唯一 ID，显示名默认「新分组」（等价于各子类原先的 (id, "新分组") 构造）。</summary>
        private T NewTag(InventoryDatabase db)
        {
            var tag = new T { id = GenerateId(db) };
            tag.displayName.SetTextValue(0, "新分组");
            return tag;
        }

        /// <summary>生成本系统内唯一的新 ID：<c>{IdPrefix}{n}</c>，n 自「当前条目数 + 1」起递增直到不重复。</summary>
        private string GenerateId(InventoryDatabase db)
        {
            var list = GetList(db);
            int n = list.Count + 1;
            string id;
            do { id = IdPrefix + n; n++; }
            while (ContainsId(list, id));
            return id;
        }

        private static bool ContainsId(List<T> list, string id)
        {
            foreach (var t in list)
                if (t != null && t.id == id) return true;
            return false;
        }

        #endregion
    }
}

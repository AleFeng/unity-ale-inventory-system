using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 「分组标签」面板泛型基类：主列表骨架来自 <see cref="EditorMasterListPanel{T}"/>，
    /// 本类补上分组标签专有的行显示（色点 + ID）、新建规则（自动 ID + 默认名）与
    /// Inspector（ID / 标识颜色 / 名称 / 描述）。制作、装备、技能三个系统共用。
    ///
    /// <para>三者的数据类都继承 <see cref="GroupTag"/>，面板此前是三份逐字符相同的拷贝，
    /// 仅「取哪个列表」与「新 ID 前缀」不同 —— 现由两个子类契约成员表达。</para>
    /// </summary>
    /// <typeparam name="T">分组标签类型（<c>CraftingGroupTag</c> / <c>EquipmentGroupTag</c> / <c>SkillGroupTag</c>）。</typeparam>
    public abstract class EditorGroupTagPanel<T> : EditorMasterListPanel<T> where T : GroupTag, new()
    {
        /// <summary>自动生成 ID 的前缀（如 <c>"group_"</c>）。</summary>
        protected abstract string IdPrefix { get; }

        #region 主列表配置

        protected override string Noun        => "分组标签";
        protected override bool   HasColorDot => true;
        protected override Color  RowColor(T item) => item.color;

        protected override string RowLabel(T item)
            => string.IsNullOrEmpty(item.id) ? Tr("(空 ID)") : item.id;

        /// <summary>新建一个标签：自动分配唯一 ID，显示名默认「新分组」（等价于各子类原先的 (id, "新分组") 构造）。</summary>
        protected override T CreateNew(InventoryDatabase db, List<T> list)
        {
            var tag = new T { id = GenerateId(list) };
            tag.displayName.SetTextValue(0, Tr("新分组"));
            return tag;
        }

        /// <summary>生成本系统内唯一的新 ID：<c>{IdPrefix}{n}</c>，n 自「当前条目数 + 1」起递增直到不重复。</summary>
        private string GenerateId(List<T> list)
        {
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

        #region Inspector

        public override void DrawInspector(IInventoryEditorContext ctx, T tag)
        {
            if (tag == null)
            {
                EditorGUILayout.LabelField(Tr("请选择或新建一个分组标签。"));
                return;
            }

            EditorGUILayout.LabelField(Tr("基础信息"), InventoryEditorStyles.Header);

            EditorGUI.BeginChangeCheck();
            string id    = EditorGUILayout.TextField("ID", tag.id);
            Color  color = EditorGUILayout.ColorField(Tr("标识颜色"), tag.color);
            if (EditorGUI.EndChangeCheck())
            {
                ctx.RecordUndo("修改分组标签");
                tag.id    = id;
                tag.color = color;
                ctx.MarkDirty();
            }

            // 名称 / 描述为 Text 类型属性值（纯文本 fallback + 可选本地化引用），复用统一属性绘制器。
            tag.NormalizeTextFields();
            AttributeFieldDrawer.Draw(ctx, Tr("名称"), tag.displayName, null);
            AttributeFieldDrawer.Draw(ctx, Tr("描述"), tag.description, null);
        }

        #endregion
    }
}

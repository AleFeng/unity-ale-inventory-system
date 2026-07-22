using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.Serialization;
using UnityEditor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 编辑器面板与主窗口之间的交互契约。面板通过它访问数据库、记录 Undo、标记脏、查询重复 ID 等，
    /// 而无需直接耦合到窗口类型。
    /// </summary>
    public interface IInventoryEditorContext
    {
        /// <summary>当前编辑的数据库（可能为 null）。</summary>
        InventoryDatabase Database { get; }

        /// <summary>数据库对应的 SerializedObject（用于属性绘制路径）。</summary>
        SerializedObject Serialized { get; }

        /// <summary>导出/资源引用解析器（编辑器实现）。</summary>
        IAssetRefResolver Resolver { get; }

        /// <summary>当前重复（或空）道具 ID 集合，用于红色高亮。</summary>
        HashSet<string> DuplicateIds { get; }

        /// <summary>当前重复（或空）仓库 ID 集合，用于仓库列表与 Inspector 红色高亮。</summary>
        HashSet<string> InventoryDuplicateIds { get; }

        /// <summary>当前重复（或空）商店 ID 集合，用于商店列表与 Inspector 红色高亮。</summary>
        HashSet<string> ShopDuplicateIds { get; }

        /// <summary>当前重复（或空）蓝图 ID 集合，用于蓝图列表与 Inspector 红色高亮。</summary>
        HashSet<string> CraftingDuplicateIds { get; }

        /// <summary>当前重复（或空）装备组 ID 集合，用于装备组列表与 Inspector 红色高亮。</summary>
        HashSet<string> EquipmentDuplicateIds { get; }

        /// <summary>当前重复（或空）技能 ID 集合，用于技能列表与 Inspector 红色高亮。</summary>
        HashSet<string> SkillDuplicateIds { get; }

        /// <summary>在修改前调用，记录 Undo。</summary>
        void RecordUndo(string actionName);

        /// <summary>在修改后调用，标记数据库为脏并触发重复 ID 重算。</summary>
        void MarkDirty();

        /// <summary>请求重绘窗口。</summary>
        void Repaint();
    }
}

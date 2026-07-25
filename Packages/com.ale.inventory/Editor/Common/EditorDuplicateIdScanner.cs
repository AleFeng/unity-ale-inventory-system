using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Editor;

namespace Ale.Inventory.Editor
{
    /// <summary>六大系统中「按 ID 唯一」的实体种类。用于统一驱动重复 ID 扫描、红字高亮与状态栏文案。</summary>
    public enum EInventoryEntityKind
    {
        Item,
        Inventory,
        Shop,
        Crafting,
        Equipment,
        Skill,
    }

    /// <summary>
    /// 库存领域的重复 / 空 ID 扫描。通用的逐条扫描算法（<see cref="EditorIdScanner.Scan{T}"/> /
    /// <see cref="EditorIdScanner.HasNonEmpty"/>）已下沉到 toolkit；本类只保留「六类实体种类」的领域枚举
    /// 与整库扫描 <see cref="ScanAll"/>，并薄转发 <see cref="HasNonEmpty"/> 以保调用点不变。
    /// </summary>
    public static class EditorDuplicateIdScanner
    {
        /// <summary>全部实体种类（遍历顺序 = 状态栏文案顺序）。</summary>
        public static readonly EInventoryEntityKind[] AllKinds =
        {
            EInventoryEntityKind.Item,
            EInventoryEntityKind.Inventory,
            EInventoryEntityKind.Shop,
            EInventoryEntityKind.Crafting,
            EInventoryEntityKind.Equipment,
            EInventoryEntityKind.Skill,
        };

        /// <summary>该种类的中文名词（状态栏与提示文案用）。</summary>
        public static string NounOf(EInventoryEntityKind kind)
        {
            switch (kind)
            {
                case EInventoryEntityKind.Item:      return "道具";
                case EInventoryEntityKind.Inventory: return "仓库";
                case EInventoryEntityKind.Shop:      return "商店";
                case EInventoryEntityKind.Crafting:  return "蓝图";
                case EInventoryEntityKind.Equipment: return "装备组";
                default:                             return "技能";
            }
        }

        /// <summary>扫描整库六类实体，返回「种类 → 重复/空 ID 集合」。</summary>
        public static Dictionary<EInventoryEntityKind, HashSet<string>> ScanAll(InventoryDatabase db)
        {
            var map = new Dictionary<EInventoryEntityKind, HashSet<string>>(AllKinds.Length);
            foreach (var kind in AllKinds) map[kind] = new HashSet<string>();
            if (!db) return map;

            map[EInventoryEntityKind.Item]      = EditorIdScanner.Scan(db.Items,              x => x.id);
            map[EInventoryEntityKind.Inventory] = EditorIdScanner.Scan(db.Inventories,        x => x.id);
            map[EInventoryEntityKind.Shop]      = EditorIdScanner.Scan(db.Shops,              x => x.id);
            map[EInventoryEntityKind.Crafting]  = EditorIdScanner.Scan(db.CraftingBlueprints, x => x.id);
            map[EInventoryEntityKind.Equipment] = EditorIdScanner.Scan(db.EquipmentGroups,    x => x.id);
            map[EInventoryEntityKind.Skill]     = EditorIdScanner.Scan(db.Skills,             x => x.id);
            return map;
        }

        /// <summary>集合中是否含「非空」的重复 ID（薄转发到 <see cref="EditorIdScanner.HasNonEmpty"/>）。</summary>
        public static bool HasNonEmpty(HashSet<string> ids) => EditorIdScanner.HasNonEmpty(ids);
    }
}

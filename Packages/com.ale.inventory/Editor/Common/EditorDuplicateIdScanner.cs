using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;

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
    /// 重复 / 空 ID 扫描。此前六类各写了一份逐字相同的扫描器（道具那份在
    /// <c>DuplicateIdChecker</c>、另五份在 <c>InventoryEditorWindow</c>），
    /// 且两种写法对「空 ID」的判定并不一致 —— 见 <see cref="Scan{T}"/> 的说明。
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

        /// <summary>
        /// 扫描出「重复」或「空白」的 ID 集合，空白 ID 以 <see cref="string.Empty"/> 计入。
        /// <para><b>空 ID 一经出现即计入</b>（不必出现两次）—— 六个 Inspector 的提示文案都是
        /// 「⚠ ID 重复或为空」，此前只有道具那份是这个语义，另五份用的是
        /// <c>if (!seen.Add(id)) result.Add(id)</c>，导致**单个**空 ID 永远不被标记。</para>
        /// </summary>
        public static HashSet<string> Scan<T>(IEnumerable<T> items, Func<T, string> idOf)
        {
            var result = new HashSet<string>();
            if (items == null || idOf == null) return result;

            var seen = new HashSet<string>();
            foreach (var it in items)
            {
                string id = idOf(it);
                if (string.IsNullOrWhiteSpace(id))
                {
                    result.Add(string.Empty);
                    continue;
                }
                if (!seen.Add(id)) result.Add(id);
            }
            return result;
        }

        /// <summary>扫描整库六类实体，返回「种类 → 重复/空 ID 集合」。</summary>
        public static Dictionary<EInventoryEntityKind, HashSet<string>> ScanAll(InventoryDatabase db)
        {
            var map = new Dictionary<EInventoryEntityKind, HashSet<string>>(AllKinds.Length);
            foreach (var kind in AllKinds) map[kind] = new HashSet<string>();
            if (!db) return map;

            map[EInventoryEntityKind.Item]      = Scan(db.Items,              x => x.id);
            map[EInventoryEntityKind.Inventory] = Scan(db.Inventories,        x => x.id);
            map[EInventoryEntityKind.Shop]      = Scan(db.Shops,              x => x.id);
            map[EInventoryEntityKind.Crafting]  = Scan(db.CraftingBlueprints, x => x.id);
            map[EInventoryEntityKind.Equipment] = Scan(db.EquipmentGroups,    x => x.id);
            map[EInventoryEntityKind.Skill]     = Scan(db.Skills,             x => x.id);
            return map;
        }

        /// <summary>集合中是否含「非空」的重复 ID（空 ID 在导出时会被跳过，不阻塞导出）。</summary>
        public static bool HasNonEmpty(HashSet<string> ids)
        {
            if (ids == null) return false;
            foreach (var id in ids)
                if (!string.IsNullOrEmpty(id)) return true;
            return false;
        }
    }
}

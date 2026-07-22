using System.Collections.Generic;
using InventorySystem.Runtime;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 扫描数据库中的道具 ID，返回重复（出现 ≥2 次）或空白的 ID 集合，用于编辑器红色高亮与导出阻止。
    /// </summary>
    public static class DuplicateIdChecker
    {
        /// <summary>返回重复或空白的道具 ID 集合。空白 ID 以空字符串表示并纳入集合。</summary>
        public static HashSet<string> Scan(InventoryDatabase db)
        {
            var result = new HashSet<string>();
            if (db == null) return result;

            var seen = new HashSet<string>();
            foreach (var item in db.Items)
            {
                if (string.IsNullOrWhiteSpace(item.id))
                {
                    result.Add(string.Empty);
                    continue;
                }

                if (!seen.Add(item.id))
                    result.Add(item.id);
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Runtime;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 收集「道具模板 + 功能标签」属性字段 id 的共用工具（去重保序，可选谓词过滤）。
    /// 收拢制作 / 商店配置绘制器与整理设置绘制器里三份逐字近似的收集骨架，差别仅在是否按类型过滤。
    /// </summary>
    internal static class InventoryAttrOptionUtil
    {
        /// <summary>
        /// 收集道具模板与功能标签中所有属性字段 id（先模板后标签、去重保序）；
        /// <paramref name="predicate"/> 非空时仅收满足谓词的字段（如按 <see cref="EFieldType"/> 过滤）。
        /// </summary>
        public static List<string> CollectAttrIds(InventoryDatabase db,
            Func<AttributeDefinition, bool> predicate = null)
        {
            var ids  = new List<string>();
            var seen = new HashSet<string>();
            if (!db) return ids;

            void Collect(List<AttributeDefinition> defs)
            {
                if (defs == null) return;
                foreach (var def in defs)
                    if ((predicate == null || predicate(def))
                        && !string.IsNullOrEmpty(def.id) && seen.Add(def.id))
                        ids.Add(def.id);
            }

            foreach (var tmpl in db.ItemTemplates) Collect(tmpl.attributes);
            foreach (var tag in db.FunctionTags)   Collect(tag.attributes);
            return ids;
        }
    }
}

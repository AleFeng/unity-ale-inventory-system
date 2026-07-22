using System.Collections.Generic;
using System.Text;
using InventorySystem.Runtime;

namespace InventorySystem.Editor
{
    /// <summary>
    /// 道具查询辅助：根据道具来源（模板 + 功能标签）解析其属性定义，用于显示名称与搜索过滤。
    /// </summary>
    public static class ItemQueryUtil
    {
        /// <summary>构建道具的 属性ID -> 属性定义 映射（来源：模板 + 各功能标签，先到先得）。</summary>
        public static Dictionary<string, AttributeDefinition> BuildDefMap(InventoryDatabase db, Item item)
        {
            var map = new Dictionary<string, AttributeDefinition>();
            if (db == null || item == null) return map;

            var template = db.GetTemplate(item.templateRef);
            if (template != null)
            {
                // 模板自有属性字段
                foreach (var d in template.attributes)
                    if (!string.IsNullOrEmpty(d.id) && !map.ContainsKey(d.id)) map[d.id] = d;

                // 模板锁定的功能标签属性字段（template.tagRefs）
                foreach (var tTagName in template.tagRefs)
                {
                    var tTag = db.GetTag(tTagName);
                    if (tTag == null) continue;
                    foreach (var d in tTag.attributes)
                        if (!string.IsNullOrEmpty(d.id) && !map.ContainsKey(d.id)) map[d.id] = d;
                }
            }

            // 道具自身勾选的功能标签属性字段
            foreach (var tagName in item.tagRefs)
            {
                var tag = db.GetTag(tagName);
                if (tag == null) continue;
                foreach (var d in tag.attributes)
                    if (!string.IsNullOrEmpty(d.id) && !map.ContainsKey(d.id)) map[d.id] = d;
            }

            return map;
        }

        /// <summary>返回道具的显示名称：优先取 ID 为「名称」的字符串属性，否则回退到道具 ID。</summary>
        public static string GetDisplayName(InventoryDatabase db, Item item)
        {
            var map = BuildDefMap(db, item);
            foreach (var entry in item.values)
            {
                if (map.TryGetValue(entry.id, out var def)
                    && def.type == EFieldType.String && def.id == "名称")
                {
                    string s = entry.value.AsString;
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
            return string.IsNullOrEmpty(item.id) ? "(未命名)" : item.id;
        }

        /// <summary>构建用于搜索匹配的文本：ID + 所有字符串值 + 枚举显示名 + 标签名。</summary>
        public static string BuildSearchText(InventoryDatabase db, Item item)
        {
            var sb = new StringBuilder();
            sb.Append(item.id).Append(' ');

            foreach (var tag in item.tagRefs)
                sb.Append(tag).Append(' ');

            var map = BuildDefMap(db, item);
            foreach (var entry in item.values)
            {
                if (!map.TryGetValue(entry.id, out var def)) continue;

                if (def.type == EFieldType.String)
                {
                    for (int i = 0; i < entry.value.Count; i++)
                        sb.Append(entry.value.GetString(i)).Append(' ');
                }
                else if (def.type == EFieldType.Enum)
                {
                    var enumType = db.GetEnumType(def.enumTypeRef);
                    if (enumType != null)
                        for (int i = 0; i < entry.value.Count; i++)
                            sb.Append(enumType.GetDisplayName(entry.value.GetInt(i))).Append(' ');
                }
            }

            return sb.ToString();
        }

        /// <summary>判断道具是否匹配搜索词（不区分大小写的包含匹配）。</summary>
        public static bool Matches(InventoryDatabase db, Item item, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            string text = BuildSearchText(db, item);
            return text.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 通用「重复 / 空 ID」扫描。宿主插件把各类「按 ID 唯一」的实体集合传入 <see cref="Scan{T}"/> 即可得到
    /// 需要红字高亮的 ID 集合，并用 <see cref="HasNonEmpty"/> 判断是否存在阻塞导出的非空重复。
    /// </summary>
    public static class EditorIdScanner
    {
        /// <summary>
        /// 扫描出「重复」或「空白」的 ID 集合，空白 ID 以 <see cref="string.Empty"/> 计入。
        /// <para><b>空 ID 一经出现即计入</b>（不必出现两次）—— 与「⚠ ID 重复或为空」的提示语义一致。</para>
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

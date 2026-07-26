#if IS_LOCALIZATION
using System.Collections.Generic;
using Ale.Toolkit.Runtime;

namespace Ale.Toolkit.Editor
{
    /// <summary>本地化工具收集到的一个 <see cref="EFieldType.Text"/> 位置：属性值 + 元素索引 + 语义 Key 路径。</summary>
    public readonly struct TextFieldRef
    {
        public readonly AttributeValue Value;
        public readonly int            Element;
        public readonly string         KeyPath;

        public TextFieldRef(AttributeValue value, int element, string keyPath)
        {
            Value   = value;
            Element = element;
            KeyPath = keyPath;
        }
    }

    /// <summary>
    /// 本地化 Text 字段收集器（与领域无关的收集机制）：把一个个 Text 属性值（标量 / 数组逐元素）按语义 Key 路径
    /// 收进列表，只收<b>有纯文本内容</b>的元素（空字段无需本地化），撞名 Key 自动追加 <c>#n</c> 去重。
    ///
    /// <para>宿主负责「遍历本库哪些对象、生成什么语义 Key 路径」（见宿主各自的 Collect），逐处调用
    /// <see cref="AddText"/> / <see cref="AddEntries"/>；本类只承担与领域无关的收集与去重。</para>
    /// </summary>
    public sealed class TextFieldCollector
    {
        private readonly List<TextFieldRef> _list = new List<TextFieldRef>();
        private readonly HashSet<string>    _seen = new HashSet<string>();

        /// <summary>已收集的全部 Text 位置。</summary>
        public List<TextFieldRef> Result => _list;

        /// <summary>收集属性值列表里所有 Text 条目（keyPath 末段 = 属性 id）。</summary>
        public void AddEntries(IEnumerable<AttributeEntry> values, string keyBase)
        {
            if (values == null) return;
            foreach (var entry in values)
            {
                if (entry?.value == null || entry.value.Type != EFieldType.Text) continue;
                AddText(entry.value, $"{keyBase}-{Id(entry.id)}");
            }
        }

        /// <summary>收集一个 Text 属性值（标量 1 个 / 数组逐元素）。</summary>
        public void AddText(AttributeValue av, string keyBase)
        {
            if (av == null || av.Type != EFieldType.Text) return;
            if (!av.IsArray)
            {
                Add(av, 0, keyBase);
            }
            else
            {
                for (int e = 0; e < av.Count; e++)
                    Add(av, e, $"{keyBase}-{e}");
            }
        }

        private void Add(AttributeValue av, int element, string keyPath)
        {
            // 仅收集有纯文本内容的元素（空字段无需本地化）
            if (string.IsNullOrEmpty(av.GetTextValue(element))) return;
            _list.Add(new TextFieldRef(av, element, Unique(keyPath)));
        }

        /// <summary>保证 Key 唯一：撞名追加 <c>#2</c>、<c>#3</c>…</summary>
        private string Unique(string key)
        {
            if (_seen.Add(key)) return key;
            int n = 2;
            while (!_seen.Add($"{key}#{n}")) n++;
            return $"{key}#{n}";
        }

        /// <summary>Key 段的空值占位（供宿主拼语义 Key 时复用）。</summary>
        public static string Id(string s) => string.IsNullOrEmpty(s) ? "(空)" : s;
    }
}
#endif

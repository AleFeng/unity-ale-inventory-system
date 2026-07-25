using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ale.Toolkit.Runtime;
using Object = UnityEngine.Object;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 反射遍历任意对象图，收集其中全部「对象类」<see cref="AttributeValue"/>（遇 <see cref="UnityEngine.Object"/>
    /// 引用即止）。供 Addressable / 本地化等需要枚举全库资源或文本字段的工具复用，对未来新增的承载集合稳健。
    /// </summary>
    public static class AttributeValueWalker
    {
        /// <summary>
        /// 从 <paramref name="root"/> 的序列化字段起步下钻，收集全部对象类 <see cref="AttributeValue"/>。
        /// <para><paramref name="root"/> 通常是数据库（本身即 <see cref="UnityEngine.Object"/>）——直接下钻其字段，
        /// 不因其本身是 Object 而止步。</para>
        /// </summary>
        public static List<AttributeValue> Collect(object root)
        {
            var sink    = new List<AttributeValue>();
            var visited = new HashSet<object>(RefComparer.Instance);
            if (root != null)
                foreach (var f in root.GetType().GetFields(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    Walk(f.GetValue(root), sink, visited);
            return sink;
        }

        private static void Walk(object node, List<AttributeValue> sink, HashSet<object> visited)
        {
            if (node == null) return;

            // 命中属性值：收集对象类的，停止下钻
            if (node is AttributeValue av)
            {
                if (av.Type.IsObjectBacked()) sink.Add(av);
                return;
            }

            // 外部资源引用 / 值类型（基元 / 枚举 / Vector* / Color 等结构体，均不含 AttributeValue）/ 字符串：不下钻
            if (node is Object) return;
            var type = node.GetType();
            if (type.IsValueType || node is string) return;
            if (!visited.Add(node)) return;

            // 集合逐元素下钻
            if (node is System.Collections.IEnumerable en)
            {
                foreach (var e in en) Walk(e, sink, visited);
                return;
            }

            // 普通 [Serializable] 数据类：逐字段下钻
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var ft = f.FieldType;
                if (ft.IsPrimitive || ft.IsEnum || ft == typeof(string)) continue;
                if (typeof(Object).IsAssignableFrom(ft)) continue;   // 跳过外部资源引用字段
                Walk(f.GetValue(node), sink, visited);
            }
        }

        /// <summary>引用相等比较器（避免值类型 / 重写 Equals 干扰访问过标记）。</summary>
        private sealed class RefComparer : IEqualityComparer<object>
        {
            public static readonly RefComparer Instance = new RefComparer();
            bool IEqualityComparer<object>.Equals(object a, object b) => ReferenceEquals(a, b);
            int IEqualityComparer<object>.GetHashCode(object o) => RuntimeHelpers.GetHashCode(o);
        }
    }
}

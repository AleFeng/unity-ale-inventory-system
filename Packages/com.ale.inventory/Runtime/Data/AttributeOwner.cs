using System;
using System.Collections.Generic;
using UnityEngine;

#if IS_LOCALIZATION
using UnityEngine.Localization;
#endif

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 拥有属性字段集合的对象基类。
    /// 封装懒加载 O(1) 字典缓存、以及属性值的泛型 Get / Set 便捷 API，
    /// 供 <see cref="Item"/> 与 <see cref="EnumItem"/> 共用，避免重复实现。
    ///
    /// <para>子类须实现 <see cref="AttributeEntries"/> 抽象属性，返回自身序列化存储的
    /// <see cref="AttributeEntry"/> 列表；修改该列表后须调用 <see cref="InvalidateEntryCache"/>
    /// 使缓存失效，以便下次 <see cref="GetEntry"/> 时透明重建。</para>
    /// </summary>
    [Serializable]
    public abstract class AttributeOwner
    {
        // 懒加载字典缓存：Key = AttributeEntry.id，Value = 对应条目引用。
        // 标记 [NonSerialized]，Unity 序列化层（JSON / Binary / Asset）完全忽略此字段；
        // 反序列化后字段自动为 null，首次 GetEntry 调用时透明重建，无需任何序列化代码改动。
        [NonSerialized] private Dictionary<string, AttributeEntry> _entryCache;

        /// <summary>
        /// 子类提供自身序列化存储的属性条目列表。
        /// 基类通过此属性构建并维护 O(1) 字典缓存。
        /// </summary>
        protected abstract List<AttributeEntry> AttributeEntries { get; }

        // ── 缓存管理 ───────────────────────────────────────────────────────────────

        /// <summary>
        /// 使字典缓存失效。
        /// 在 <see cref="AttributeEntries"/> 列表内容发生变化（增删改）后调用，
        /// 下次 <see cref="GetEntry"/> 调用时将从列表当前状态透明重建缓存。
        /// <para>子类内部以及直接操作底层列表的容器类（如 <see cref="EnumType"/>）均可调用。</para>
        /// </summary>
        public void InvalidateEntryCache() => _entryCache = null;

        /// <summary>从 <see cref="AttributeEntries"/> 重建 ID → Entry 字典缓存。</summary>
        private void RebuildEntryCache()
        {
            var entries = AttributeEntries;
            _entryCache = new Dictionary<string, AttributeEntry>(entries.Count);
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.id))
                    _entryCache[e.id] = e;
        }

        // ── 属性条目访问 ───────────────────────────────────────────────────────────

        /// <summary>
        /// 按 ID 查找属性条目，未找到返回 null。O(1) 懒加载字典查找。
        /// </summary>
        public AttributeEntry GetEntry(string attrId)
        {
            if (string.IsNullOrEmpty(attrId)) return null;
            if (_entryCache == null) RebuildEntryCache();
            if (_entryCache == null) return null;
            _entryCache.TryGetValue(attrId, out var entry);
            return entry;
        }

        // ── 属性值 获取与设置 ──────────────────────────────────────────────────────

        /// <summary>
        /// 返回指定属性的 <see cref="AttributeValue"/> 引用，未找到返回 null。
        /// 适用于数组类型、LocalizedString、VectorInt4 等泛型层不覆盖的高级场景。
        /// </summary>
        public AttributeValue GetAttributeValue(string attrId)
            => GetEntry(attrId)?.value;

        /// <summary>
        /// 按类型获取指定属性的标量值；属性不存在或类型不匹配时返回 <paramref name="fallback"/>。
        /// <para>支持类型：<c>int</c>、<c>float</c>、<c>bool</c>、<c>string</c>、
        /// <c>Vector2/3/4</c>、<c>Color</c>、<c>Vector2Int/3Int</c>、<c>AnimationCurve</c>，
        /// 以及任意 <see cref="UnityEngine.Object"/> 子类（<c>Sprite</c>、<c>Texture</c> 等）。</para>
        /// <para><c>int</c> 同时适用于 <see cref="EFieldType.Enum"/>（返回枚举整数值）。</para>
        /// <para><c>string</c>：对 <see cref="EFieldType.Text"/> 条目，启用 IS_LOCALIZATION 且本地化引用可解析出非空文本时
        /// 返回本地化文本，否则返回纯文本 fallback；<see cref="EFieldType.String"/> 直接返回原始值。</para>
        /// </summary>
        public T GetAttributeValue<T>(string attrId, T fallback = default)
        {
            var av = GetEntry(attrId)?.value;
            if (av == null) return fallback;

            if (typeof(T) == typeof(int))   return (T)(object)av.AsInt;
            if (typeof(T) == typeof(float)) return (T)(object)av.AsFloat;
            if (typeof(T) == typeof(bool))  return (T)(object)av.AsBool;

            if (typeof(T) == typeof(string))
            {
                if (av.Type == EFieldType.Text)
                {
                    // Text：本地化优先（IS_LOCALIZATION 且引用能解析出非空文本），否则回退纯文本 fallback。
#if IS_LOCALIZATION
                    var (tableRef, entryKey) = av.GetLocalizedStringRef();
                    if (!string.IsNullOrEmpty(tableRef) || !string.IsNullOrEmpty(entryKey))
                    {
                        var ls = new LocalizedString(tableRef, entryKey);
                        string local = ls.GetLocalizedString();
                        if (!string.IsNullOrEmpty(local)) return (T)(object)local;
                    }
#endif
                    return (T)(object)av.GetTextValue();
                }
                return (T)(object)av.AsString;   // String：原样返回（可能用于数据 / 标记，不做本地化）
            }

            if (typeof(T) == typeof(Vector2))        return (T)(object)av.AsVector2;
            if (typeof(T) == typeof(Vector3))        return (T)(object)av.AsVector3;
            if (typeof(T) == typeof(Vector4))        return (T)(object)av.AsVector4;
            if (typeof(T) == typeof(Color))          return (T)(object)av.AsColor;
            if (typeof(T) == typeof(Vector2Int))     return (T)(object)av.GetVector2Int(0);
            if (typeof(T) == typeof(Vector3Int))     return (T)(object)av.GetVector3Int(0);
            if (typeof(T) == typeof(AnimationCurve)) return (T)(object)av.AsAnimationCurve;
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
                return (T)(object)av.AsObject;

            return fallback;
        }

        /// <summary>
        /// 按类型设置指定属性的标量值。
        /// <para>返回 <c>true</c>：属性存在且传入类型在支持列表中；
        /// 返回 <c>false</c>：属性不存在或类型不受支持。</para>
        /// <para>注意：此方法不改变 <see cref="AttributeValue.Type"/>，仅向对应后备列表写值。
        /// 请确保传入类型与属性定义的 <see cref="EFieldType"/> 匹配。</para>
        /// </summary>
        public bool SetAttributeValue<T>(string attrId, T value)
        {
            var av = GetEntry(attrId)?.value;
            if (av == null) return false;

            if (value is int i)                  { av.AsInt            = i;   return true; }
            if (value is float f)                { av.AsFloat          = f;   return true; }
            if (value is bool b)                 { av.AsBool           = b;   return true; }
            if (value is string s)               { av.AsString         = s;   return true; }
            if (value is Vector2 v2)             { av.AsVector2        = v2;  return true; }
            if (value is Vector3 v3)             { av.AsVector3        = v3;  return true; }
            if (value is Vector4 v4)             { av.AsVector4        = v4;  return true; }
            if (value is Color c)                { av.AsColor          = c;   return true; }
            if (value is Vector2Int vi2)         { av.SetVector2Int(0, vi2);  return true; }
            if (value is Vector3Int vi3)         { av.SetVector3Int(0, vi3);  return true; }
            if (value is AnimationCurve ac)      { av.AsAnimationCurve = ac;  return true; }
            if (value is UnityEngine.Object obj) { av.AsObject         = obj; return true; }
            return false;
        }
    }
}

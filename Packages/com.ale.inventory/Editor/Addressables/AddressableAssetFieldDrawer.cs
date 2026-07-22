using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// AssetReference 授权字段绘制器（原生可搜索选择器）。启用 IS_ADDRESSABLE 时经
    /// <c>[InitializeOnLoad]</c> 注入 core 的 <see cref="AttributeFieldDrawer.AddressableFieldDrawer"/>，
    /// 同时服务两类字段：属性系统对象值（按 AttributeValue+index）与配置类固定资源字段（按 cacheKey+fieldKey）。
    ///
    /// 借 <see cref="AssetReferenceHolder"/>（ScriptableObject + SerializedObject + PropertyField）桥接
    /// Unity 原生 AssetReference 绘制器（弹窗列出全部 Addressable 资源、可搜索，选中未登记的资源会自动登记）。
    /// 当前 GUID 由调用方传入，改选后回写 GUID（子资源为 <c>GUID[子名]</c>，与 <see cref="AddressableAssetRefResolver"/> 一致）。
    /// 仿 LocalizedStringHolder 的按键缓存 + 仅主 GUID 变化才重建（防抖）。
    /// </summary>
    [InitializeOnLoad]
    public sealed class AddressableAssetFieldDrawer : IAddressableAssetFieldDrawer
    {
        public static readonly AddressableAssetFieldDrawer Instance = new AddressableAssetFieldDrawer();

        static AddressableAssetFieldDrawer()
        {
            AttributeFieldDrawer.AddressableFieldDrawer = Instance;
        }

        // 统一的 holder 缓存：按字符串键区分每个字段（属性值 av:hash:index / 固定字段 fx:hash:fieldKey）。
        private static readonly Dictionary<string, AssetReferenceHolder> _holders = new Dictionary<string, AssetReferenceHolder>();
        private static readonly Dictionary<string, SerializedObject>     _sos     = new Dictionary<string, SerializedObject>();

        // ── 接口：属性系统对象值 ─────────────────────────────────────────────────────

        public float GetHeight(AttributeValue value, int index)
            => HeightByKey(AvKey(value, index), value.GetObjAddress(index));

        public bool Draw(Rect rect, AttributeValue value, int index, Type objectType, string label, out string newGuid)
            => DrawByKey(rect, AvKey(value, index), value.GetObjAddress(index), label, out newGuid);

        // ── 接口：配置类固定资源字段 ──────────────────────────────────────────────────

        public float GetGuidHeight(object cacheKey, string fieldKey, string currentGuid)
            => HeightByKey(FxKey(cacheKey, fieldKey), currentGuid);

        public bool DrawGuid(Rect rect, object cacheKey, string fieldKey, string currentGuid, Type objectType, string label, out string newGuid)
            => DrawByKey(rect, FxKey(cacheKey, fieldKey), currentGuid, label, out newGuid);

        // ── 接口：拖入对象 → 授权键（+登记进分组，与原生选择器一致） ────────────────────────

        public string ObjectToKey(UnityEngine.Object obj)
            => obj ? AddressableAssetRefResolver.Instance.ToGuid(obj, warnIfUnregistered: true) : string.Empty;

        // ── 统一核心 ─────────────────────────────────────────────────────────────────

        private static string AvKey(AttributeValue v, int idx) => $"av:{RuntimeHelpers.GetHashCode(v)}:{idx}";
        private static string FxKey(object key, string field)  => $"fx:{RuntimeHelpers.GetHashCode(key)}:{field}";

        private static (AssetReferenceHolder holder, SerializedObject so) Ensure(string key)
        {
            if (!_holders.TryGetValue(key, out var holder) || holder == null)
            {
                holder = ScriptableObject.CreateInstance<AssetReferenceHolder>();
                holder.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                _holders[key] = holder;
                _sos[key]     = new SerializedObject(holder);
            }
            var so = _sos.TryGetValue(key, out var cached) && cached != null && cached.targetObject != null
                ? cached
                : (_sos[key] = new SerializedObject(holder));
            return (holder, so);
        }

        /// <summary>把存储的 GUID 同步进 holder（仅主 GUID 不同才重建，忽略子名差异以免每帧重置），返回 (holder, so, prop)。</summary>
        private static (AssetReferenceHolder holder, SerializedObject so, SerializedProperty prop) EnsureSynced(string key, string storedKey)
        {
            var (holder, so) = Ensure(key);
            so.Update();
            string storedGuid = MainGuid(storedKey);
            string holderGuid = holder.reference != null ? holder.reference.AssetGUID : null;
            if ((holderGuid ?? string.Empty) != (storedGuid ?? string.Empty))
            {
                holder.reference = string.IsNullOrEmpty(storedGuid) ? new AssetReference() : new AssetReference(storedGuid);
                so.Update();
            }
            return (holder, so, so.FindProperty("reference"));
        }

        private static float HeightByKey(string key, string storedKey)
        {
            var (_, _, prop) = EnsureSynced(key, storedKey);
            return prop != null
                ? EditorGUI.GetPropertyHeight(prop, GUIContent.none, true)
                : EditorGUIUtility.singleLineHeight;
        }

        private static bool DrawByKey(Rect rect, string key, string storedKey, string label, out string newGuid)
        {
            newGuid = null;
            var (holder, so, prop) = EnsureSynced(key, storedKey);
            if (prop == null) return false;

            EditorGUI.BeginChangeCheck();
            var content = string.IsNullOrEmpty(label) ? GUIContent.none : new GUIContent(label);
            EditorGUI.PropertyField(rect, prop, content, true);
            bool applied = so.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck() || applied)
            {
                newGuid = ReadKey(holder);
                return true;
            }
            return false;
        }

        /// <summary>从 holder 读取当前授权键：主 GUID，若指定了子资源则为 <c>GUID[子名]</c>。</summary>
        private static string ReadKey(AssetReferenceHolder holder)
        {
            var r = holder != null ? holder.reference : null;
            if (r == null || string.IsNullOrEmpty(r.AssetGUID)) return string.Empty;
            string sub = r.SubObjectName;
            return string.IsNullOrEmpty(sub) ? r.AssetGUID : $"{r.AssetGUID}[{sub}]";
        }

        /// <summary>取键中的主 GUID（去掉可能的 <c>[子名]</c> 后缀）。</summary>
        private static string MainGuid(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            int lb = key.IndexOf('[');
            return lb >= 0 ? key.Substring(0, lb) : key;
        }
    }

    /// <summary>桥接原生 AssetReference 绘制器的临时 SO（<see cref="HideFlags.DontSave"/>，不入盘）。</summary>
    internal sealed class AssetReferenceHolder : ScriptableObject
    {
        public AssetReference reference = new AssetReference();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Runtime.AddressableSupport
{
    /// <summary>
    /// Addressable 资源加载/卸载与内存管理核心。按「地址」做引用计数去重加载，
    /// 按「宿主 GameObject」记录其占用的地址，宿主销毁时自动递减计数、归零即释放句柄。
    ///
    /// 仅在启用 IS_ADDRESSABLE 宏时编译（所属程序集受 defineConstraints 约束）。
    /// </summary>
    public static class InventoryAddressableManager
    {
        private sealed class Entry
        {
            public AsyncOperationHandle Handle;
            public bool   Done;
            public Object Result;
            public int    RefCount;
            public List<Action<Object>> Callbacks = new List<Action<Object>>();
        }

        // 地址 → 加载项（引用计数）
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        // 宿主 → 它占用过的地址（每次绑定追加一条，释放时逐条递减，正确处理重复绑定）
        private static readonly Dictionary<GameObject, List<string>> OwnerAddrs =
            new Dictionary<GameObject, List<string>>();

        /// <summary>
        /// 按地址异步加载资源。加载完成（或已缓存）后回调 <paramref name="onLoaded"/>。
        /// <paramref name="owner"/> 非空时登记到宿主，宿主销毁自动释放。
        /// </summary>
        public static void LoadAsync<T>(string address, GameObject owner, Action<T> onLoaded) where T : Object
        {
            if (string.IsNullOrEmpty(address))
            {
                onLoaded?.Invoke(null);
                return;
            }

            RegisterOwner(owner, address);

            if (!Entries.TryGetValue(address, out var e))
            {
                e = new Entry { RefCount = 1 };
                Entries[address] = e;
                BeginLoad<T>(address, e);
            }
            else
            {
                e.RefCount++;
            }

            if (onLoaded != null)
            {
                if (e.Done) onLoaded(e.Result as T);
                else        e.Callbacks.Add(o => onLoaded(o as T));
            }
        }

        /// <summary>释放某宿主名下加载的全部地址（各递减一次引用计数，归零则真正卸载）。</summary>
        public static void Release(GameObject owner)
        {
            if (!owner) return;
            if (!OwnerAddrs.TryGetValue(owner, out var list)) return;

            for (int i = 0; i < list.Count; i++)
                ReleaseAddress(list[i]);

            OwnerAddrs.Remove(owner);
        }

        /// <summary>当前已加载的不同地址数量（调试用）。</summary>
        public static int LoadedCount => Entries.Count;

        // ── 内部 ─────────────────────────────────────────────────────────────────────

        private static void BeginLoad<T>(string address, Entry e) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            e.Handle = handle;
            handle.Completed += h =>
            {
                e.Done   = true;
                e.Result = h.Status == AsyncOperationStatus.Succeeded ? h.Result as Object : null;
                if (h.Status != AsyncOperationStatus.Succeeded)
                    Debug.LogWarning($"[InventoryAddressables] 资源加载失败：{address}");

                var pending = e.Callbacks;
                e.Callbacks = new List<Action<Object>>();
                for (int i = 0; i < pending.Count; i++)
                    pending[i]?.Invoke(e.Result);
            };
        }

        private static void RegisterOwner(GameObject owner, string address)
        {
            if (!owner) return;

            if (!OwnerAddrs.TryGetValue(owner, out var list))
            {
                list = new List<string>();
                OwnerAddrs[owner] = list;

                // 首次为该宿主登记：挂上生命周期跟踪器（销毁时自动释放）
                var tracker = owner.GetComponent<InventoryAssetOwnerTracker>();
                if (!tracker) owner.AddComponent<InventoryAssetOwnerTracker>();
            }
            list.Add(address);
        }

        /// <summary>
        /// 按地址释放一次引用计数（对应一次无宿主 <see cref="LoadAsync{T}"/>），归零则真正卸载。
        /// 供无宿主加载的调用方配对释放；宿主加载由 <see cref="Release(GameObject)"/> 内部调用本方法。
        /// </summary>
        public static void ReleaseAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) return;
            if (!Entries.TryGetValue(address, out var e)) return;

            e.RefCount--;
            if (e.RefCount > 0) return;

            if (e.Handle.IsValid())
                Addressables.Release(e.Handle);
            Entries.Remove(address);
        }
    }
}

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Runtime.AddressableSupport
{
    /// <summary>
    /// 基于 Addressables 的资源加载器。优先使用属性值里的实时引用（兼容直接打包 SO 的场景），
    /// 无实时引用时回退到地址走 Addressable 异步加载（运行时从导出数据加载的常态）。
    ///
    /// 启动时（IS_ADDRESSABLE 启用）自动注册为 <see cref="InventoryAssets.Loader"/>，替换默认的 DirectAssetLoader。
    /// </summary>
    public sealed class AddressableAssetLoader : IInventoryAssetLoader
    {
        public void Load<T>(AttributeValue value, int index, GameObject owner, Action<T> onLoaded) where T : Object
        {
            if (value == null)
            {
                onLoaded?.Invoke(null);
                return;
            }

            // 1) 有实时引用（编辑器内 / 直接打包 SO）→ 同步返回
            var live = value.GetObject(index);
            if (live)
            {
                onLoaded?.Invoke(live as T);
                return;
            }

            // 2) 仅有地址（运行时从导出数据加载）→ Addressable 异步加载
            string address = value.GetObjAddress(index);
            if (string.IsNullOrEmpty(address))
            {
                onLoaded?.Invoke(null);
                return;
            }

            InventoryAddressableManager.LoadAsync(address, owner, onLoaded);
        }

        public void Load<T>(Object liveRef, string address, GameObject owner, Action<T> onLoaded) where T : Object
        {
            // 1) 有实时引用（编辑器内 / 直接打包 SO）→ 同步返回
            if (liveRef) { onLoaded?.Invoke(liveRef as T); return; }
            // 2) 仅有授权地址（IS_ADDRESSABLE 授权 / 运行时）→ Addressable 异步加载
            if (string.IsNullOrEmpty(address)) { onLoaded?.Invoke(null); return; }
            InventoryAddressableManager.LoadAsync(address, owner, onLoaded);
        }

        public void Release(GameObject owner)
        {
            InventoryAddressableManager.Release(owner);
        }

        public void ReleaseAddress(string address)
        {
            InventoryAddressableManager.ReleaseAddress(address);
        }
    }

    /// <summary>启动引导：把激活加载器切换为 Addressable 实现。</summary>
    internal static class AddressableLoaderBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            InventoryAssets.Loader = new AddressableAssetLoader();
        }
    }
}

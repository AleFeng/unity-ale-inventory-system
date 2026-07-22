using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 默认加载器：直接返回 SO 中保存的实时对象引用，同步回调。
    /// 适用于编辑器内、或运行时直接加载 SO（引用未被剥离）的场景。
    /// 不持有任何句柄，<see cref="Release"/> 为空操作。
    /// </summary>
    public sealed class DirectAssetLoader : IInventoryAssetLoader
    {
        public static readonly DirectAssetLoader Instance = new DirectAssetLoader();

        public void Load<T>(AttributeValue value, int index, GameObject owner, Action<T> onLoaded) where T : Object
        {
            if (onLoaded == null) return;
            var obj = value != null ? value.GetObject(index) : null;
            onLoaded(obj as T);
        }

        public void Load<T>(Object liveRef, string address, GameObject owner, Action<T> onLoaded) where T : Object
        {
            // 直接模式：仅返回实时引用（address 忽略）。
            onLoaded?.Invoke(liveRef as T);
        }

        public void Release(GameObject owner)
        {
            // 直接模式无句柄，无需释放。
        }

        public void ReleaseAddress(string address)
        {
            // 直接模式无句柄，无需释放。
        }
    }
}

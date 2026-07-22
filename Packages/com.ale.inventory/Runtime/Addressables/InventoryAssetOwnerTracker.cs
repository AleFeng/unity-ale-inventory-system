using UnityEngine;

namespace Ale.Inventory.Runtime.AddressableSupport
{
    /// <summary>
    /// 挂在资源绑定宿主 GameObject 上的生命周期跟踪器。宿主销毁时自动通知
    /// <see cref="InventoryAddressableManager"/> 释放该宿主名下加载的全部 Addressable 句柄。
    /// 由管理器在首次为某宿主登记资源时自动添加，调用方无需手动挂载。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")] // 隐藏，不在 Add Component 菜单中出现
    internal sealed class InventoryAssetOwnerTracker : MonoBehaviour
    {
        private void OnDestroy()
        {
            InventoryAddressableManager.Release(gameObject);
        }
    }
}

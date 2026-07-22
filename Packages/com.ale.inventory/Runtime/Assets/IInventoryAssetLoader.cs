using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 资源取用加载器抽象。<see cref="InventoryAssets"/> 门面把所有取用请求委派给当前激活的加载器。
    ///
    /// 默认实现为 <see cref="DirectAssetLoader"/>（同步返回 SO 里的实时引用，供编辑器/直接模式）；
    /// 启用 IS_ADDRESSABLE 宏时，受约束的 Addressable 程序集会在运行时把激活加载器替换为
    /// 基于 Addressables 的异步实现（按地址加载 + 引用计数自动卸载）。core 程序集对 Addressables 零依赖。
    /// </summary>
    public interface IInventoryAssetLoader
    {
        /// <summary>
        /// 取用 <paramref name="value"/> 在 <paramref name="index"/> 处的对象资源。
        /// 加载完成（或同步可得）后回调 <paramref name="onLoaded"/>。
        /// <paramref name="owner"/> 为生命周期宿主（可空）：非空时加载的句柄随宿主销毁自动释放。
        /// </summary>
        void Load<T>(AttributeValue value, int index, GameObject owner, Action<T> onLoaded) where T : Object;

        /// <summary>
        /// 取用固定资源引用（配置类的具名字段，如 <c>Skill.icon</c>）：优先用实时引用 <paramref name="liveRef"/>，
        /// 无引用时回退 <paramref name="address"/> 走 Addressable 异步加载（直接模式仅返回 <paramref name="liveRef"/>）。
        /// </summary>
        void Load<T>(Object liveRef, string address, GameObject owner, Action<T> onLoaded) where T : Object;

        /// <summary>释放某宿主名下加载的全部资源句柄（直接模式为空操作）。</summary>
        void Release(GameObject owner);

        /// <summary>
        /// 释放一次由无宿主 <c>Load</c> 按地址加载的资源句柄（归零则卸载）。直接模式为空操作。
        /// 供无宿主加载（<paramref name="address"/> 取自 <see cref="AttributeValue.GetObjAddress"/>）配对释放。
        /// </summary>
        void ReleaseAddress(string address);
    }
}

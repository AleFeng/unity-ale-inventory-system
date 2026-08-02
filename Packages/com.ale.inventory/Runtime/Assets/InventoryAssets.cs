using System;
using Ale.Toolkit.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 资源取用门面（库存侧）。通用能力已下沉到 <see cref="ToolkitAssets"/>；本类为薄适配层，
    /// 保留库存插件既有调用点的签名不变，并额外提供「按道具 + 属性 key 绑定」这一带领域类型的重载。
    ///
    /// <para>除下方 <see cref="Bind{T}(Item,string,GameObject,Action{T},int)"/> 外，其余方法一律转发
    /// <see cref="ToolkitAssets"/>；<see cref="Loader"/> 亦为其转发属性（读写同一底层加载器）。</para>
    /// </summary>
    public static class InventoryAssets
    {
        /// <summary>当前激活的加载器（转发 <see cref="ToolkitAssets.Loader"/>）。</summary>
        public static IAssetLoader Loader
        {
            get => ToolkitAssets.Loader;
            set => ToolkitAssets.Loader = value;
        }

        /// <summary>绑定属性值的资源到宿主：加载完成后回调 <paramref name="set"/>，宿主销毁时自动释放。</summary>
        public static void Bind<T>(AttributeValue value, GameObject owner, Action<T> set, int index = 0) where T : Object
            => ToolkitAssets.Bind(value, owner, set, index);

        /// <summary>
        /// 绑定固定资源引用（配置类的具名字段：实时引用 + Addressable 地址一对）到宿主：
        /// 直接模式同步返回 <paramref name="liveRef"/>；授权模式无实时引用时按 <paramref name="address"/> 异步加载。
        /// </summary>
        public static void Bind<T>(Object liveRef, string address, GameObject owner, Action<T> set) where T : Object
            => ToolkitAssets.Bind(liveRef, address, owner, set);

        /// <summary>按道具 + 属性 key 绑定资源到宿主。找不到该属性时回调 null。</summary>
        public static void Bind<T>(Item item, string key, GameObject owner, Action<T> set, int index = 0) where T : Object
        {
            if (item == null || set == null) return;
            var entry = item.GetEntry(key);
            if (entry == null || entry.value == null) { set(null); return; }
            ToolkitAssets.Bind(entry.value, owner, set, index);
        }

        /// <summary>加载属性值的资源并回调，不绑定任何宿主（须自行配对 <see cref="Release(AttributeValue,int)"/>）。</summary>
        public static void Load<T>(AttributeValue value, Action<T> onLoaded, int index = 0) where T : Object
            => ToolkitAssets.Load(value, onLoaded, index);

        /// <summary>释放宿主名下加载的全部资源句柄（直接模式为空操作）。</summary>
        public static void Release(GameObject owner) => ToolkitAssets.Release(owner);

        /// <summary>释放由无宿主 <see cref="Load{T}"/> 加载的资源句柄（按属性值在 <paramref name="index"/> 处的地址）。</summary>
        public static void Release(AttributeValue value, int index = 0) => ToolkitAssets.Release(value, index);
    }
}

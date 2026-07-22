using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 资源取用统一门面。无论底层是「直接实时引用」还是「Addressable 异步加载」，
    /// 调用方代码完全一致：
    /// <code>
    ///   InventoryAssets.Bind&lt;Sprite&gt;(item, "icon", image.gameObject, s =&gt; image.sprite = s);
    /// </code>
    /// 有实时引用时同步赋值；只有地址（运行时从导出数据加载）时异步加载完成后再赋值；
    /// 宿主 GameObject 销毁时自动释放对应句柄。
    ///
    /// core 程序集对 Addressables 零依赖：默认 <see cref="Loader"/> 为 <see cref="DirectAssetLoader"/>，
    /// 启用 IS_ADDRESSABLE 后由 Addressable 运行时程序集在启动时替换为异步加载器。
    /// </summary>
    public static class InventoryAssets
    {
        /// <summary>当前激活的加载器。默认直接模式；Addressable 程序集启动时会覆盖此值。</summary>
        public static IInventoryAssetLoader Loader = DirectAssetLoader.Instance;

        // ── Bind：跟踪宿主、自动释放 ────────────────────────────────────────────────

        /// <summary>绑定属性值的资源到宿主：加载完成后回调 <paramref name="set"/>，宿主销毁时自动释放。</summary>
        public static void Bind<T>(AttributeValue value, GameObject owner, Action<T> set, int index = 0) where T : Object
        {
            if (value == null || set == null) return;
            (Loader ?? DirectAssetLoader.Instance).Load(value, index, owner, set);
        }

        /// <summary>
        /// 绑定固定资源引用（配置类具名字段，如 <c>Skill.icon</c> + <c>Skill.iconAddress</c>）到宿主：
        /// 直接模式同步返回 <paramref name="liveRef"/>；授权模式无实时引用时按 <paramref name="address"/> 异步加载。
        /// 宿主销毁时自动释放句柄。
        /// </summary>
        public static void Bind<T>(Object liveRef, string address, GameObject owner, Action<T> set) where T : Object
        {
            if (set == null) return;
            (Loader ?? DirectAssetLoader.Instance).Load(liveRef, address, owner, set);
        }

        /// <summary>按道具 + 属性 key 绑定资源到宿主。找不到该属性时回调 null。</summary>
        public static void Bind<T>(Item item, string key, GameObject owner, Action<T> set, int index = 0) where T : Object
        {
            if (item == null || set == null) return;
            var entry = item.GetEntry(key);
            if (entry == null || entry.value == null) { set(null); return; }
            Bind(entry.value, owner, set, index);
        }

        // ── Load：不跟踪宿主，调用方自行管理生命周期 ────────────────────────────────

        /// <summary>
        /// 加载属性值的资源并回调，不绑定任何宿主。
        /// <para>Addressable 模式下句柄不随宿主自动释放，调用方用完后必须调用
        /// <see cref="Release(AttributeValue,int)"/>（传入同一 value 与 index）配对释放，否则句柄泄漏。
        /// 若能提供宿主 GameObject，优先用 <see cref="Bind{T}(AttributeValue,GameObject,Action{T},int)"/>（自动释放）。</para>
        /// </summary>
        public static void Load<T>(AttributeValue value, Action<T> onLoaded, int index = 0) where T : Object
        {
            if (value == null || onLoaded == null) return;
            (Loader ?? DirectAssetLoader.Instance).Load(value, index, null, onLoaded);
        }

        // ── Release ─────────────────────────────────────────────────────────────────

        /// <summary>释放宿主名下加载的全部资源句柄（直接模式为空操作）。</summary>
        public static void Release(GameObject owner)
        {
            (Loader ?? DirectAssetLoader.Instance).Release(owner);
        }

        /// <summary>
        /// 释放由无宿主 <see cref="Load{T}(AttributeValue,Action{T},int)"/> 加载的资源句柄（按属性值在 <paramref name="index"/> 处的地址）。
        /// 与该无宿主 Load 配对使用；直接模式为空操作。
        /// </summary>
        public static void Release(AttributeValue value, int index = 0)
        {
            if (value == null) return;
            (Loader ?? DirectAssetLoader.Instance).ReleaseAddress(value.GetObjAddress(index));
        }

        // ── 道具条目级 预载 / 释放 ──────────────────────────────────────────────────

        /// <summary>
        /// 预载某道具所有对象类字段（Sprite/Prefab/Texture/Material/AudioClip/AnimationClip/PhysicsMaterial*）到宿主。
        /// 仅做加载与句柄登记，不赋值；宿主销毁或调用 <see cref="ReleaseItem"/> 时统一释放。
        /// 即“用到的道具资源自动加载”。
        /// </summary>
        public static void PreloadItem(Item item, GameObject owner)
        {
            if (item == null) return;
            var loader = Loader ?? DirectAssetLoader.Instance;
            foreach (var entry in item.values)
            {
                var v = entry?.value;
                if (v == null || !v.Type.IsObjectBacked()) continue;
                int count = Mathf.Max(1, v.Count);
                for (int i = 0; i < count; i++)
                    loader.Load<Object>(v, i, owner, null);
            }
        }

        /// <summary>释放某道具在宿主名下预载的资源（等价于释放该宿主的全部句柄）。即“不用的道具资源自动卸载”。</summary>
        public static void ReleaseItem(Item item, GameObject owner)
        {
            Release(owner);
        }
    }
}

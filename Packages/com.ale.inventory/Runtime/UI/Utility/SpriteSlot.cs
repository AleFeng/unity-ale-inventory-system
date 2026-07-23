using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 一个 <see cref="Image"/> 的异步图片绑定槽。封装「释放旧句柄 → 自增代次 → 经
    /// <see cref="InventoryAssets"/> 取图 → 回调中丢弃过期结果」这一固定流程。
    ///
    /// <para><b>为什么需要代次守卫：</b>启用 IS_ADDRESSABLE 时取图是异步的，而承载它的格子
    /// 往往是对象池复用的 —— 上一次绑定的加载可能在格子已改绑别的道具之后才回来，
    /// 直接赋值就会串图。每次绑定自增代次、回调里比对，过期结果直接丢弃。</para>
    ///
    /// <para>包内此前有 5 处各写了一遍 <c>_xxxGen</c> 计数器与同样的释放 / 比对代码。</para>
    ///
    /// <para><b>注意是 class 不是 struct：</b>绑定回调是捕获 <c>this</c> 的闭包，
    /// C# 不允许 struct 的实例方法内的 lambda 访问 <c>this</c>。</para>
    /// </summary>
    public sealed class SpriteSlot
    {
        // 异步加载代次：每次绑定 / 清空自增，回调据此丢弃过期结果。
        private int _gen;

        /// <summary>
        /// 从属性值绑定图片（属性值内承载 Sprite 直接引用或 Addressable 授权 GUID）。
        /// <paramref name="value"/> 为 null 时等同 <see cref="Clear"/>。
        /// </summary>
        /// <param name="image">目标图片组件；为空则什么都不做。</param>
        /// <param name="value">承载 Sprite 的属性值。</param>
        /// <param name="index">属性值为数组时的元素下标。</param>
        /// <param name="toggleEnabled">
        /// true = 由本槽按取图结果开关 <see cref="Behaviour.enabled"/>（无图则隐藏）；
        /// false = 不碰 enabled，由调用方自行控制（如品质背景框需要常驻显示底框）。
        /// </param>
        public void Bind(Image image, AttributeValue value, int index = 0, bool toggleEnabled = true)
        {
            if (!BeginBind(image, toggleEnabled, value != null, out int gen)) return;
            InventoryAssets.Bind<Sprite>(value, image.gameObject, s => Apply(image, s, gen, toggleEnabled), index);
        }

        /// <summary>
        /// 从「直接引用 + Addressable 授权 GUID」绑定图片（技能图标等不走属性值的字段）。
        /// 两者皆空时等同 <see cref="Clear"/>。
        /// </summary>
        public void Bind(Image image, Object liveRef, string address, bool toggleEnabled = true)
        {
            bool hasSource = liveRef || !string.IsNullOrEmpty(address);
            if (!BeginBind(image, toggleEnabled, hasSource, out int gen)) return;
            InventoryAssets.Bind<Sprite>(liveRef, address, image.gameObject,
                s => Apply(image, s, gen, toggleEnabled));
        }

        /// <summary>清空图片显示：释放句柄、作废未完成的加载回调。</summary>
        /// <param name="disable">true = 同时把 <see cref="Behaviour.enabled"/> 置 false。</param>
        public void Clear(Image image, bool disable = true)
        {
            if (!image) { _gen++; return; }
            InventoryAssets.Release(image.gameObject);
            _gen++;
            image.sprite = null;
            if (disable) image.enabled = false;
        }

        /// <summary>作废未完成的加载回调（不动 Image，供无 Image 引用的场景）。</summary>
        public void Invalidate() => _gen++;

        // 绑定前置：释放旧句柄、自增代次；无图源时就地清空并返回 false。
        private bool BeginBind(Image image, bool toggleEnabled, bool hasSource, out int gen)
        {
            gen = 0;
            if (!image) return false;

            InventoryAssets.Release(image.gameObject);
            gen = ++_gen;

            if (hasSource) return true;

            image.sprite = null;
            if (toggleEnabled) image.enabled = false;
            return false;
        }

        // 取图回调：代次过期 / Image 已销毁则丢弃。
        private void Apply(Image image, Sprite sprite, int gen, bool toggleEnabled)
        {
            if (gen != _gen || !image) return;
            image.sprite = sprite;
            if (toggleEnabled) image.enabled = sprite;
        }
    }
}

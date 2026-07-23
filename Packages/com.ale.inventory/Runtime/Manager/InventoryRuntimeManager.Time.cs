using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// <see cref="InventoryRuntimeManager"/> 的时间服务分部：为商店刷新等周期性逻辑提供
    /// 可由游戏层接管的「当前时间」。
    ///
    /// <para>各时钟类型的获取器由游戏层在 GameInstance 中注册，把耦合集中到一处；
    /// 未注册的类型一律回退系统本地时间，因此「本地时间」无需注册。</para>
    /// </summary>
    public partial class InventoryRuntimeManager
    {

        // 各时钟类型的当前时间获取器（由游戏层在 GameInstance 中注册，将耦合集中到 GameInstance）。
        // 未注册的类型一律回退系统本地时间，从而无需为「本地时间」单独注册。
        private readonly Dictionary<ShopTimeType, Func<DateTime>> _timeGetters
            = new Dictionary<ShopTimeType, Func<DateTime>>();

        /// <summary>
        /// 注册某种时钟类型的当前时间获取器，使 <see cref="GetNow"/> 能返回游戏层的
        /// 「游戏时间 / 服务器时间」。通常在 GameInstance 中统一对接，例如：
        /// <code>InventoryRuntimeManager.Instance.RegisterTimeGetter(ShopTimeType.ServerTime, () =&gt; timeMgr.ServerNow);</code>
        /// 重复注册同一类型将覆盖之前的获取器；传入 null 则注销该类型。
        /// 「本地时间」无需注册——未注册时即回退系统本地时间。
        /// </summary>
        public void RegisterTimeGetter(ShopTimeType type, Func<DateTime> getter)
        {
            if (getter == null)
            {
                _timeGetters.Remove(type);
                return;
            }
            if (_timeGetters.ContainsKey(type))
                Debug.LogWarning($"[InventoryRuntimeManager] 时间获取器 '{type}' 已注册，将覆盖之前的注册。");
            _timeGetters[type] = getter;
        }

        /// <summary>注销某种时钟类型的时间获取器（之后该类型回退系统本地时间）。</summary>
        public void UnregisterTimeGetter(ShopTimeType type) => _timeGetters.Remove(type);

        /// <summary>
        /// 获取指定时钟类型的当前时间。优先走已注册的获取器；
        /// 未注册 / 获取器返回默认值时回退系统本地时间（<see cref="System.DateTime.Now"/>）。
        /// 商店刷新等周期性逻辑统一经此取时。
        /// </summary>
        public DateTime GetNow(ShopTimeType type)
        {
            if (_timeGetters.TryGetValue(type, out var getter) && getter != null)
            {
                var now = getter.Invoke();
                if (now != default) return now;
            }
            return DateTime.Now;
        }

    }
}

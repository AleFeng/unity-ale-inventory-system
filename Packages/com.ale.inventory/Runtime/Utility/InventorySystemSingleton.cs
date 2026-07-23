using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    // ── 单例静态状态重置中枢 ────────────────────────────────────────────────────────
    /// <summary>
    /// 单例静态状态的重置中枢（非泛型）。
    ///
    /// <para><b>为什么需要它：</b>关闭 Domain Reload
    /// （Project Settings → Editor → Enter Play Mode Options → 取消 Reload Domain）时，
    /// 静态字段会跨播放会话残留——上一次 Play 注册的数据库、装备状态、商店进度、
    /// 以及 <c>IsQuitting</c> 标记都会带进下一次 Play。而
    /// <see cref="RuntimeInitializeOnLoadMethodAttribute"/> 无法直接标注在泛型类型的方法上
    /// （Unity 不知道该用哪个 T 去调用），故由各闭合泛型在首次创建实例时把自己的重置动作
    /// 登记到本类，再由本类在每次播放开始（<see cref="RuntimeInitializeLoadType.SubsystemRegistration"/>）
    /// 时统一执行。</para>
    ///
    /// <para>Domain Reload 开启（默认）时本机制为无害的空转：登记表本身也会被重置，
    /// 每次播放开始时为空。</para>
    /// </summary>
    internal static class InventorySingletonRegistry
    {
        private static readonly List<Action> Resetters = new List<Action>();

        /// <summary>登记一个「播放开始时执行」的静态状态重置动作（幂等，重复登记忽略）。</summary>
        internal static void Register(Action resetter)
        {
            if (resetter != null && !Resetters.Contains(resetter))
                Resetters.Add(resetter);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            // 关闭 Domain Reload 时，Resetters 与其中的委托同样跨会话存活——
            // 正是靠这一点，新会话开始时才能找到上次登记的重置动作。
            for (int i = 0; i < Resetters.Count; i++)
                Resetters[i]?.Invoke();
        }
    }

    // ── 非 MonoBehaviour 单例基类 ──────────────────────────────────────────────────
    // 参考 Fs.Utility.Singleton.Singleton<T> 的设计，独立实现，不引用任何 Fs 命名空间。
    // 供 InventoryDataManager 等无需 MonoBehaviour 生命周期的管理器使用。
    /// <summary>
    /// InventorySystem 插件内部普通（非 MonoBehaviour）单例基类。
    /// 首次访问 <see cref="Instance"/> 时通过 <see cref="Activator.CreateInstance{T}"/>
    /// 自动实例化，并调用 <see cref="Init"/>；子类通过 <c>protected override void Init()</c> 执行初始化逻辑。
    ///
    /// <para><b>仅限主线程访问。</b><see cref="Instance"/> 的「检查 + 创建」并非原子操作，
    /// 字段虽标 <c>volatile</c> 也不构成线程安全；Unity 侧的调用方本就在主线程，无需额外同步。</para>
    /// </summary>
    public abstract class InventorySystemSingleton<T> where T : InventorySystemSingleton<T>
    {
        private static volatile T _instance;

        // 是否已向 InventorySingletonRegistry 登记过重置动作（每个闭合泛型各一份）。
        private static bool _resetHookRegistered;

        #region 实例与生命周期

        /// <summary>单例实例。首次访问时自动创建并调用 Init()。</summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    EnsureResetHook();

                    // 退出播放 / 退出应用时置位 IsQuitting。先减后加，避免重复订阅。
                    Application.quitting -= HandleQuitting;
                    Application.quitting += HandleQuitting;

                    // 构造函数中会将 _instance 赋值为 (T)this
                    Activator.CreateInstance<T>();
                    _instance.Init();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 应用程序（或编辑器播放模式）是否正在退出。退出时不应再访问单例。
        /// 由 <see cref="Application.quitting"/> 置位，并在下一次播放开始时复位。
        /// </summary>
        public static bool IsQuitting { get; private set; }

        /// <summary>子类构造函数必须通过 base() 隐式调用，确保 _instance 被赋值。</summary>
        protected InventorySystemSingleton()
        {
            _instance = (T)this;
        }

        /// <summary>首次实例化后立即调用。子类覆写以执行初始化逻辑。</summary>
        protected virtual void Init() { }

        /// <summary>销毁单例实例，将 _instance 置空。</summary>
        public virtual void Destroy()
        {
            Application.quitting -= HandleQuitting;
            _instance = null;
        }

        #endregion

        #region 静态状态重置（关闭 Domain Reload 时）

        private static void EnsureResetHook()
        {
            if (_resetHookRegistered) return;
            _resetHookRegistered = true;
            InventorySingletonRegistry.Register(ResetStatics);
        }

        private static void HandleQuitting() => IsQuitting = true;

        /// <summary>播放开始时复位全部静态状态（关闭 Domain Reload 时残留的实例与退出标记）。</summary>
        private static void ResetStatics()
        {
            Application.quitting -= HandleQuitting;
            _instance  = null;
            IsQuitting = false;
            // _resetHookRegistered 保持 true：登记表同样跨会话存活，无需重复登记。
        }

        #endregion
    }

    // ── MonoBehaviour 单例基类 ──────────────────────────────────────────────────────
    // 参考 Fs.Utility.Singleton.MonoBehaviourSingleton<T> 的设计，独立实现。
    // 供 InventoryRuntimeManager 等需要 MonoBehaviour 生命周期的管理器使用。
    /// <summary>
    /// InventorySystem 插件内部 MonoBehaviour 单例基类。
    /// 在 Scene 中挂载此组件后，<c>Awake</c> 时自动设置单例实例并调用 <see cref="Init"/>；
    /// <c>DontDestroyOnLoad</c> 保证跨场景持久；重复实例时后来者自动销毁。
    /// </summary>
    public abstract class InventorySystemMonoBehaviourSingleton<T>
        : MonoBehaviour where T : InventorySystemMonoBehaviourSingleton<T>
    {
        private static T _instance;

        // 是否已向 InventorySingletonRegistry 登记过重置动作（每个闭合泛型各一份）。
        private static bool _resetHookRegistered;

        #region 实例与生命周期

        /// <summary>单例实例。若尚未 Awake 则返回 null。</summary>
        public static T Instance => _instance;

        /// <summary>
        /// 应用程序（或编辑器播放模式）是否正在退出。退出时不应再访问单例。
        /// 由 <see cref="OnApplicationQuit"/> 置位，并在下一次播放开始时复位。
        /// </summary>
        public static bool IsQuitting { get; private set; }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // 重复实例：销毁当前 GameObject，保留已有的实例
                Destroy(gameObject);
                return;
            }

            EnsureResetHook();

            _instance = (T)this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            IsQuitting = true;
        }

        /// <summary>Awake 中单例设置完毕后调用（替代 Awake 中的初始化逻辑）。子类覆写此方法。</summary>
        protected virtual void Init() { }

        #endregion

        #region 静态状态重置（关闭 Domain Reload 时）

        private static void EnsureResetHook()
        {
            if (_resetHookRegistered) return;
            _resetHookRegistered = true;
            InventorySingletonRegistry.Register(ResetStatics);
        }

        /// <summary>播放开始时复位全部静态状态（关闭 Domain Reload 时残留的实例引用与退出标记）。</summary>
        private static void ResetStatics()
        {
            _instance  = null;
            IsQuitting = false;
        }

        #endregion
    }
}

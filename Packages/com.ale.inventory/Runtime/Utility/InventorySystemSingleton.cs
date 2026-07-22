using System;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    // ── 非 MonoBehaviour 单例基类 ──────────────────────────────────────────────────
    // 参考 Fs.Utility.Singleton.Singleton<T> 的设计，独立实现，不引用任何 Fs 命名空间。
    // 供 InventoryDataManager 等无需 MonoBehaviour 生命周期的管理器使用。
    /// <summary>
    /// InventorySystem 插件内部普通（非 MonoBehaviour）单例基类。
    /// 首次访问 <see cref="Instance"/> 时通过 <see cref="Activator.CreateInstance{T}"/>
    /// 自动实例化，并调用 <see cref="Init"/>；子类通过 <c>protected override void Init()</c> 执行初始化逻辑。
    /// </summary>
    public abstract class InventorySystemSingleton<T> where T : InventorySystemSingleton<T>
    {
        private static volatile T _instance;

        /// <summary>单例实例。首次访问时自动创建并调用 Init()。</summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 构造函数中会将 _instance 赋值为 (T)this
                    Activator.CreateInstance<T>();
                    _instance.Init();
                }
                return _instance;
            }
        }

        /// <summary>应用程序是否正在退出。退出时不应再访问单例。</summary>
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
            _instance = null;
        }
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

        /// <summary>单例实例。若尚未 Awake 则返回 null。</summary>
        public static T Instance => _instance;

        /// <summary>应用程序是否正在退出。退出时不应再访问单例。</summary>
        public static bool IsQuitting { get; private set; }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // 重复实例：销毁当前 GameObject，保留已有的实例
                Destroy(gameObject);
                return;
            }

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
    }
}

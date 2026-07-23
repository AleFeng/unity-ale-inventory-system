using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 子项实例池（纯 C# 辅助类，非 MonoBehaviour）。管理「按需实例化 → 逐帧复用 → 多余的回收隐藏」，
    /// 包内十余处「价格格 / 标签行 / 属性行 / 装备槽 / 货币格 / 加成条目」共用。
    ///
    /// <para><b>用法：</b></para>
    /// <code>
    /// _pool.Configure(prefab, container);
    /// _pool.Begin();
    /// foreach (var d in data)
    /// {
    ///     var w = _pool.Next();
    ///     if (!w) break;              // 未配置预制体 / 容器
    ///     w.SetData(d);
    /// }
    /// _pool.End();                    // 多余实例 SetActive(false)
    /// </code>
    ///
    /// <para><b>创建时的一次性初始化</b>（如订阅事件）走 <c>Next(out bool created)</c>：
    /// 复用的实例不会重复订阅，无需「先减后加」的防重复写法。</para>
    ///
    /// <para><b>回收行为</b>默认是 <c>SetActive(false)</c>；需要别的（如 <c>SetEmpty()</c>、
    /// 释放 Addressable 句柄）传 <see cref="End(Action{T})"/> 的委托 —— 此时委托<b>完全接管</b>回收，
    /// 不再自动隐藏。</para>
    /// </summary>
    /// <typeparam name="T">子项组件类型。</typeparam>
    public sealed class UiwWidgetPool<T> where T : Component
    {
        private readonly List<T> _items = new List<T>();

        private T         _prefab;
        private Transform _parent;
        private int       _active;

        #region 查询

        /// <summary>已实例化的子项总数（含被回收隐藏的）。</summary>
        public int Count => _items.Count;

        /// <summary>本轮 <see cref="Begin"/> 以来取用的子项数量。</summary>
        public int ActiveCount => _active;

        /// <summary>全部已实例化子项（含隐藏的），供批量设置公共属性用。</summary>
        public IReadOnlyList<T> Items => _items;

        /// <summary>按下标取子项；越界返回 null。</summary>
        public T At(int index) => index >= 0 && index < _items.Count ? _items[index] : null;

        #endregion

        #region 配置与取用

        /// <summary>绑定预制体与父容器。可重复调用；已建实例不受影响（父容器变化仅影响此后新建的实例）。</summary>
        public void Configure(T prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        /// <summary>开始新一轮取用（把游标复位到 0）。</summary>
        public void Begin() => _active = 0;

        /// <summary>取用下一个子项（不足则实例化），激活并返回。未配置预制体 / 容器时返回 null。</summary>
        public T Next() => Next(out _);

        /// <summary>
        /// 取用下一个子项（不足则实例化），激活并返回。未配置预制体 / 容器时返回 null。
        /// </summary>
        /// <param name="created">true = 本次是新实例化的（供一次性初始化，如订阅事件）。</param>
        public T Next(out bool created)
        {
            created = false;
            if (!_prefab || !_parent) return null;

            while (_items.Count <= _active)
            {
                _items.Add(UnityEngine.Object.Instantiate(_prefab, _parent));
                created = true;
            }

            var w = _items[_active++];
            if (!w) return null;
            if (!w.gameObject.activeSelf) w.gameObject.SetActive(true);
            return w;
        }

        #endregion

        #region 回收

        /// <summary>结束本轮：把未取用的子项 <c>SetActive(false)</c>。</summary>
        public void End()
        {
            for (int i = _active; i < _items.Count; i++)
                if (_items[i] && _items[i].gameObject.activeSelf)
                    _items[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// 结束本轮：把未取用的子项交给 <paramref name="recycle"/> 处理。
        /// 委托完全接管回收行为（不再自动隐藏）；传 null 等同 <see cref="End()"/>。
        /// </summary>
        public void End(Action<T> recycle)
        {
            if (recycle == null) { End(); return; }
            for (int i = _active; i < _items.Count; i++)
                if (_items[i]) recycle(_items[i]);
        }

        /// <summary>回收全部子项（等同 <c>Begin(); End();</c>）。</summary>
        public void RecycleAll()
        {
            Begin();
            End();
        }

        /// <summary>回收全部子项，回收行为由 <paramref name="recycle"/> 接管。</summary>
        public void RecycleAll(Action<T> recycle)
        {
            Begin();
            End(recycle);
        }

        /// <summary>销毁全部子项实例并清空。</summary>
        public void Clear()
        {
            foreach (var w in _items)
                if (w) UnityEngine.Object.Destroy(w.gameObject);
            _items.Clear();
            _active = 0;
        }

        #endregion
    }
}

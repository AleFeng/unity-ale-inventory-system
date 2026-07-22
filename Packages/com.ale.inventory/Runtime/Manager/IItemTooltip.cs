using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 道具悬停详情弹窗的运行时抽象。具体实现在 UI 层（<c>UiwItemTooltip</c>）。
    ///
    /// <para>定义于 Runtime 程序集，使 <see cref="InventoryRuntimeManager"/> 能在不反向依赖
    /// UI 程序集的前提下，集中持有并对外提供全局唯一的悬停弹窗（依赖倒置）。</para>
    /// </summary>
    public interface IItemTooltip
    {
        /// <summary>在光标处（屏幕坐标）显示指定道具的详情弹窗并淡入。<paramref name="count"/> 为持有数量（显示在数量文本）。</summary>
        void Show(string itemId, int count, Vector2 screenPos);

        /// <summary>开始原位淡出弹窗（位置保持不变）。</summary>
        void Hide();
    }
}

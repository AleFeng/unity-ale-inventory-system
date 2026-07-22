using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 挂在 <see cref="ScrollRect.viewport"/> 上的轻量辅助组件。覆写
    /// <see cref="UIBehaviour.OnRectTransformDimensionsChange"/> 仅置脏 / 广播事件（不直接操作 UI，
    /// 避免在 Canvas Rebuild 循环内引发 "rebuild list" 错误）。
    ///
    /// <para>独立为<b>非泛型</b>顶层类型（单独文件）：供泛型虚拟滚动列表基类
    /// <see cref="UiwInventoryListBase{TData,TCell}"/> 以单一具体类型 AddComponent / GetComponent。
    /// 若嵌套在泛型类内，每个闭合泛型会各生成一个不同的组件类型，导致查找 / 挂载不可靠。
    /// 仅在运行时经 AddComponent 挂载，不在编辑器中手动添加。</para>
    /// </summary>
    internal sealed class ViewportSizeWatcher : UIBehaviour
    {
        public event Action OnChanged;
        
        /// <summary>
        /// 当 RectTransform 尺寸发生变化时，触发 <see cref="OnChanged"/> 事件。
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            OnChanged?.Invoke();
        }
    }
}

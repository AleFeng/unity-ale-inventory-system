using UnityEngine;
using UnityEngine.EventSystems;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 「悬停弹出详情」能力的基类。封装 进入 / 移出 / 停用 三条路径与「本格是否正显示弹窗」的标记，
    /// 由 <see cref="UiwInventoryItemBase"/>（道具格）与 <see cref="UiwSkillEntry"/>（技能条目）共用 ——
    /// 两者此前是同一段逻辑的两份拷贝。
    ///
    /// <para><b>为什么需要 <see cref="OnDisable"/> 这条路径：</b>本物体被停用时（列表回收 / 面板关闭 /
    /// 快速装备后本格被隐藏）Unity <b>不会</b>派发 <see cref="OnPointerExit"/>，弹窗会残留在屏幕上。
    /// 因此停用时若本格正显示弹窗则主动关闭 —— 且只关本格触发的那次，不误伤其它格。</para>
    ///
    /// <para><b>子类契约：</b>各自持有「是否启用」的序列化开关与载荷（道具 ID / 技能对象），
    /// 并实现 <see cref="ShowHoverTooltip"/> / <see cref="HideHoverTooltip"/> 转调对应的全局弹窗。
    /// 覆写 <see cref="OnPointerEnter"/> / <see cref="OnPointerExit"/> / <see cref="OnDisable"/> 时
    /// <b>务必调用 base</b>，否则会丢掉弹窗能力或造成弹窗残留。</para>
    /// </summary>
    public abstract class UiwHoverTooltipSource : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // 本格当前是否正显示（由本格触发且尚未隐藏）详情弹窗。
        private bool _tooltipShown;

        /// <summary>本格当前是否正显示由自己触发的弹窗。</summary>
        protected bool HoverTooltipShown => _tooltipShown;

        #region 子类契约

        /// <summary>是否启用悬停弹窗（子类的序列化开关）。</summary>
        protected abstract bool HoverTooltipEnabled { get; }

        /// <summary>当前是否已绑定可供弹窗显示的内容（未绑定则不弹）。</summary>
        protected abstract bool HasHoverTooltipPayload { get; }

        /// <summary>在光标处显示本格的详情弹窗。调用时 <see cref="InventoryRuntimeManager.Instance"/> 保证非空。</summary>
        protected abstract void ShowHoverTooltip(Vector2 screenPos);

        /// <summary>隐藏详情弹窗。调用时 <see cref="InventoryRuntimeManager.Instance"/> 保证非空。</summary>
        protected abstract void HideHoverTooltip();

        #endregion

        #region 悬停事件

        /// <summary>鼠标进入：启用弹窗且已绑定内容时，在光标处显示本格的详情弹窗。</summary>
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (!HoverTooltipEnabled || !HasHoverTooltipPayload || !InventoryRuntimeManager.Instance) return;
            ShowHoverTooltip(eventData != null ? eventData.position : (Vector2)Input.mousePosition);
            _tooltipShown = true;
        }

        /// <summary>鼠标移出：隐藏详情弹窗。</summary>
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (!HoverTooltipEnabled) return;
            _tooltipShown = false;
            if (InventoryRuntimeManager.Instance) HideHoverTooltip();
        }

        /// <summary>本物体被停用时关闭本格残留的弹窗（停用不派发 <see cref="OnPointerExit"/>）。</summary>
        protected virtual void OnDisable() => HideHoverTooltipIfShowing();

        /// <summary>若本格当前正显示详情弹窗，则隐藏并复位标记（仅隐藏由本格触发的弹窗，不误伤其它格）。</summary>
        protected void HideHoverTooltipIfShowing()
        {
            if (!_tooltipShown) return;
            _tooltipShown = false;
            if (InventoryRuntimeManager.Instance) HideHoverTooltip();
        }

        #endregion
    }
}

using UnityEngine;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 技能悬停详情弹窗的运行时抽象。具体实现在 UI 层（<c>UiwSkillTooltip</c>）。
    ///
    /// <para>定义于 Runtime 程序集，使 <see cref="InventoryRuntimeManager"/> 能在不反向依赖
    /// UI 程序集的前提下，集中持有并对外提供全局唯一的技能悬停弹窗（依赖倒置）。</para>
    /// </summary>
    public interface ISkillTooltip
    {
        /// <summary>在光标处（屏幕坐标）显示指定技能的详情弹窗并淡入。</summary>
        void Show(Skill skill, Vector2 screenPos);

        /// <summary>开始原位淡出弹窗（位置保持不变）。</summary>
        void Hide();
    }
}

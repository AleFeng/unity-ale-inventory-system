#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 通用折叠页签（MonoBehaviour）。一个可点击 <see cref="Button"/>，左侧为状态图标 <see cref="icon"/>（Image）、
    /// 右侧为名称文本 <see cref="label"/>（Text / TMP_Text）。既可作普通页签（隐藏图标、仅文本），
    /// 也可作可折叠分组标题（左侧图标表示展开 / 折叠）。供 <see cref="UiwCraftingGroupFilter"/> 等以「实例化」方式复用。
    ///
    /// <para>用法：宿主实例化本预制体后，用 <see cref="SetLabel"/> 设文本、<see cref="SetIcon"/> 设 / 清左侧图标、
    /// <see cref="SetNormalColor"/> 控制选中高亮、<see cref="AddClickListener"/> 注册点击。</para>
    /// </summary>
    public class UiwFoldTab : MonoBehaviour
    {
        [Tooltip("页签点击按钮（通常在根节点）。")]
        public Button        button;
        [Tooltip("左侧图标（折叠状态或自定义图标；无图标时其节点自动隐藏）。")]
        public Image         icon;
        [Tooltip("右侧文本（页签名称）。")]
        public InventoryText label;

        /// <summary>设置右侧文本。</summary>
        public void SetLabel(string text)
        {
            if (label) label.text = text;
        }

        /// <summary>设置左侧图标：sprite 非空则显示并赋图；为空则隐藏图标节点（仅文本）。</summary>
        public void SetIcon(Sprite sprite)
        {
            if (!icon) return;
            icon.sprite = sprite;
            bool show = sprite != null;
            if (icon.gameObject.activeSelf != show) icon.gameObject.SetActive(show);
        }

        /// <summary>设置按钮 normalColor（用于选中 / 普通态高亮）。</summary>
        public void SetNormalColor(Color color)
        {
            if (!button) return;
            var colors = button.colors;
            colors.normalColor = color;
            button.colors = colors;
        }

        /// <summary>注册点击回调（叠加，不覆盖已有监听）。</summary>
        public void AddClickListener(Action onClick)
        {
            if (button && onClick != null) button.onClick.AddListener(() => onClick());
        }
    }
}

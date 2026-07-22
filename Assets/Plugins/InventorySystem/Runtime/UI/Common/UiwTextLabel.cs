#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 功能标签 UI 组件。
    /// 由一个背景图（<see cref="Image"/>）和一个文本（<see cref="InventoryText"/>）组成，
    /// 用于在道具详情格子中显示道具所属的功能标签。
    /// </summary>
    public class UiwTextLabel : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("背景图片组件。")]
        [SerializeField] private Image backgroundImage;
        [Tooltip("标签文本组件。")]
        [SerializeField] private InventoryText labelText;

        /// <summary>
        /// 是否 已完成初始化。
        /// </summary>
        private bool _isInitialized;
        // 默认 背景图。
        private Sprite _defaultSprite;
        
        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            if (backgroundImage) _defaultSprite = backgroundImage.sprite;
        }

        /// <summary>一次性设置背景图、背景颜色和文本内容。</summary>
        public void Setup(Sprite sprite, Color color, string text)
        {
            // 初始化
            if (!_isInitialized) Init();
            
            // 设置 背景图
            if (backgroundImage)
            {
                // 设置 背景图。当sprite参数未指定时，使用默认图。
                if (sprite) backgroundImage.sprite = sprite;
                else backgroundImage.sprite = _defaultSprite;
                // 设置 背景图 颜色。
                backgroundImage.color  = color;
            }
            // 设置 标签文本
            if (labelText) labelText.text = text;
        }

        /// <summary>仅更新背景 Sprite（保留当前颜色 / 文本），供异步加载（Addressable）完成后回填。传 null 用默认底图。</summary>
        public void SetBackgroundSprite(Sprite sprite)
        {
            if (!_isInitialized) Init();
            if (!backgroundImage) return;
            backgroundImage.sprite = sprite ? sprite : _defaultSprite;
        }
    }
}

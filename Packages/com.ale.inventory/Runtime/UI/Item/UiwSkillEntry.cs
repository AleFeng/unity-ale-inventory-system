#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 技能条目显示组件（网格 / 顺序列表共用；网格预制体可不接描述与自定义字段）。
    /// 显示：图标、名称、「位阶」枚举项背景框，以及可选的描述与自定义属性字段行。
    /// 鼠标悬停经 <see cref="UiwSkillTooltip"/> 弹出技能详情
    /// （进入 / 移出 / 停用三条路径由基类 <see cref="UiwHoverTooltipSource"/> 统一处理）。
    /// </summary>
    public class UiwSkillEntry : UiwHoverTooltipSource
    {
        [Header("技能信息")]
        [Tooltip("技能图标图片。")]
        public Image iconImage;
        [Tooltip("技能名称文本。")]
        public InventoryText nameText;
        [Tooltip("可选：描述文本（详情行用；网格模式可不接）。")]
        public InventoryText descText;
        [Tooltip("名称为空时是否回退显示技能 ID。")]
        public bool fallbackToId = true;

        [Header("位阶背景框")]
        [Tooltip("「位阶」枚举项背景框图片，Sprite 从位阶枚举项的属性中读取。未配置则不显示。")]
        public Image rankBackground;
        [Tooltip("技能上「位阶」枚举属性字段 ID。")]
        public string rankAttrId = "位阶";
        [Tooltip("位阶枚举项上「背景框」属性字段 ID（存 Sprite）。")]
        public string rankBackgroundAttrId = "背景框";

        [Header("自定义属性字段（可选，详情行）")]
        [Tooltip("要显示的自定义属性字段 Key 列表（每个非空值生成一行）。")]
        public string[] customFieldKeys;
        [Tooltip("自定义字段行父容器（与 customFieldLinePrefab 同时配置时逐行生成）。")]
        public Transform customFieldContainer;
        [Tooltip("自定义字段行预制体（单个 InventoryText）。")]
        public InventoryText customFieldLinePrefab;

        [Header("悬停详情")]
        [Tooltip("是否在悬停时弹出技能详情（经 UiwSkillTooltip）。")]
        public bool showTooltip = true;

        private Skill _skill;

        // 自定义字段行：本条目本身就是虚拟滚动的池化格子，若每次重绑都销毁重建这些行，
        // 滚动时会持续产生 GameObject 的销毁 / 实例化开销。改用实例池按需复用。
        private readonly UiwWidgetPool<InventoryText> _customLinePool = new UiwWidgetPool<InventoryText>();

        /// <summary>当前绑定的技能。</summary>
        public Skill Skill => _skill;

        // ── 悬停详情（基类 UiwHoverTooltipSource 的契约）────────────────────────────
        protected override bool HoverTooltipEnabled    => showTooltip;
        protected override bool HasHoverTooltipPayload => _skill != null;

        protected override void ShowHoverTooltip(Vector2 screenPos)
            => InventoryRuntimeManager.Instance.ShowSkillTooltip(_skill, screenPos);

        protected override void HideHoverTooltip()
            => InventoryRuntimeManager.Instance.HideSkillTooltip();

        /// <summary>绑定并显示一个技能。</summary>
        public void SetSkill(Skill skill)
        {
            _skill = skill;
            gameObject.SetActive(true);
            if (skill == null) { Clear(); return; }

            _iconSlot.Bind(iconImage, skill.icon, skill.iconAddress);
            if (nameText)  nameText.text = UiwSkillText.ResolveName(skill, fallbackToId);
            if (descText)  descText.text = UiwSkillText.ResolveDescription(skill);
            ApplyRankBackground(skill);
            ApplyCustomFields(skill);
        }

        /// <summary>清空显示（供空态 / 对象池回收复用）。</summary>
        public void Clear()
        {
            _skill = null;
            _iconSlot.Clear(iconImage);
            if (nameText)       nameText.text = string.Empty;
            if (descText)       descText.text = string.Empty;
            _rankBgSlot.Clear(rankBackground);
            ClearCustomLines();
        }

        // 图标 / 位阶背景的异步绑定槽（内建代次守卫，对象池复用时避免错图）。见 SpriteSlot。
        private readonly SpriteSlot _iconSlot   = new SpriteSlot();
        private readonly SpriteSlot _rankBgSlot = new SpriteSlot();

        private void ApplyRankBackground(Skill skill)
        {
            var enumItem = SkillRankUtil.Resolve(skill, rankAttrId);
            var bgEntry  = enumItem != null && !string.IsNullOrEmpty(rankBackgroundAttrId)
                ? enumItem.GetEntry(rankBackgroundAttrId) : null;
            _rankBgSlot.Bind(rankBackground, bgEntry?.value);
        }

        private void ApplyCustomFields(Skill skill)
        {
            _customLinePool.Configure(customFieldLinePrefab, customFieldContainer);
            _customLinePool.Begin();
            if (customFieldKeys != null)
                foreach (var key in customFieldKeys)
                {
                    string val = UiwSkillText.ResolveCustomField(skill, key);
                    if (string.IsNullOrEmpty(val)) continue;
                    var line = _customLinePool.Next();
                    if (!line) break;
                    line.text = val;
                }
            _customLinePool.End();
        }

        private void ClearCustomLines() => _customLinePool.RecycleAll();
    }
}

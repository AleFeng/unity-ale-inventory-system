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
    /// 技能信息悬停弹窗（实现 <see cref="ISkillTooltip"/>）。渲染技能固定字段（图标 / 名称 / 描述）、「位阶」枚举项名称，
    /// 以及组件上配置的自定义属性字段 Key 列表对应的值。
    ///
    /// <para>预制体配置在 <see cref="InventoryRuntimeManager"/> 的 <c>skillTooltipPrefab</c> 上，运行时由其全局实例化一次，
    /// 并经 <see cref="InventoryRuntimeManager.ShowSkillTooltip"/> / <see cref="InventoryRuntimeManager.HideSkillTooltip"/> 统一对外调用。</para>
    ///
    /// <para>光标定位、淡入淡出与「淡出期间的待显示队列」均来自
    /// <see cref="UiwTooltipBase{TPayload}"/>；本类只负责内容渲染。</para>
    /// </summary>
    public class UiwSkillTooltip : UiwTooltipBase<Skill>, ISkillTooltip
    {
        [Header("内容")]
        [Tooltip("技能图标。")]
        public Image iconImage;
        [Tooltip("技能名称文本。")]
        public InventoryText nameText;
        [Tooltip("技能描述文本。")]
        public InventoryText descText;
        [Tooltip("「位阶」枚举项名称文本（可选）。")]
        public InventoryText rankNameText;

        [Header("位阶")]
        [Tooltip("技能上「位阶」枚举属性字段 ID。")]
        public string rankAttrId = "位阶";
        [Tooltip("位阶枚举项上「名称」属性字段 ID（String / LocalizedString）。")]
        public string rankNameAttrId = "名称";

        [Header("自定义属性字段")]
        [Tooltip("要显示的自定义属性字段 Key 列表（每个非空值生成一行）。")]
        public string[] customFieldKeys;
        [Tooltip("自定义字段行父容器。")]
        public Transform customFieldContainer;
        [Tooltip("自定义字段行预制体（单个 InventoryText）。")]
        public InventoryText customFieldLinePrefab;

        // 图标异步加载世代号：改选 / 清空时自增，回调据此丢弃过期结果。
        private int _iconGen;

        private readonly List<GameObject> _customLines = new List<GameObject>();

        #region 显示

        /// <summary>在光标处（屏幕坐标）显示指定技能的详情并淡入。skill 为空等同 <see cref="UiwTooltipBase{TPayload}.Hide"/>。</summary>
        public void Show(Skill skill, Vector2 screenPos) => ShowTooltip(skill, screenPos);

        #endregion

        #region 内容

        protected override void ApplyContent(Skill skill)
        {
            if (iconImage)
            {
                var owner = iconImage.gameObject;
                InventoryAssets.Release(owner);
                int gen = ++_iconGen;
                InventoryAssets.Bind<Sprite>(skill.icon, skill.iconAddress, owner, s =>
                {
                    if (gen != _iconGen || !iconImage) return;
                    iconImage.sprite  = s;
                    iconImage.enabled = s;
                });
            }
            if (nameText)  nameText.text = UiwSkillText.ResolveName(skill, true);
            if (descText)  descText.text = UiwSkillText.ResolveDescription(skill);

            if (rankNameText)
            {
                var enumItem = SkillRankUtil.Resolve(skill, rankAttrId);
                string rankName = enumItem != null && !string.IsNullOrEmpty(rankNameAttrId)
                    ? enumItem.GetAttributeValue<string>(rankNameAttrId) : string.Empty;
                rankNameText.text = rankName ?? string.Empty;
            }

            BuildCustomFields(skill);
        }

        protected override void ClearContent()
        {
            if (iconImage)
            {
                InventoryAssets.Release(iconImage.gameObject);
                _iconGen++;
                iconImage.sprite = null; iconImage.enabled = false;
            }
            if (nameText)     nameText.text = string.Empty;
            if (descText)     descText.text = string.Empty;
            if (rankNameText) rankNameText.text = string.Empty;
            ClearCustomLines();
        }

        private void BuildCustomFields(Skill skill)
        {
            ClearCustomLines();
            if (!customFieldContainer || !customFieldLinePrefab || customFieldKeys == null) return;
            foreach (var key in customFieldKeys)
            {
                string val = UiwSkillText.ResolveCustomField(skill, key);
                if (string.IsNullOrEmpty(val)) continue;
                var line = Instantiate(customFieldLinePrefab, customFieldContainer);
                line.text = val;
                _customLines.Add(line.gameObject);
            }
        }

        private void ClearCustomLines()
        {
            foreach (var go in _customLines) if (go) Destroy(go);
            _customLines.Clear();
        }

        #endregion
    }
}

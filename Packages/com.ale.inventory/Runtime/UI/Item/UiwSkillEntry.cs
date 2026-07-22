#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 技能条目显示组件（网格 / 顺序列表共用；网格预制体可不接描述与自定义字段）。
    /// 显示：图标、名称、「位阶」枚举项背景框，以及可选的描述与自定义属性字段行。
    /// 鼠标悬停经 <see cref="UiwSkillTooltip"/> 弹出技能详情。
    /// </summary>
    public class UiwSkillEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        private readonly List<GameObject> _customLines = new List<GameObject>();

        // 本格当前是否正显示（由本格触发且尚未隐藏）技能弹窗。用于本格被停用时主动关闭，避免残留。
        private bool _tooltipShown;

        /// <summary>当前绑定的技能。</summary>
        public Skill Skill => _skill;

        /// <summary>绑定并显示一个技能。</summary>
        public void SetSkill(Skill skill)
        {
            _skill = skill;
            gameObject.SetActive(true);
            if (skill == null) { Clear(); return; }

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
            if (nameText)  nameText.text = UiwSkillText.ResolveName(skill, fallbackToId);
            if (descText)  descText.text = UiwSkillText.ResolveDescription(skill);
            ApplyRankBackground(skill);
            ApplyCustomFields(skill);
        }

        /// <summary>清空显示（供空态 / 对象池回收复用）。</summary>
        public void Clear()
        {
            _skill = null;
            if (iconImage)
            {
                InventoryAssets.Release(iconImage.gameObject);
                _iconGen++;
                iconImage.sprite = null; iconImage.enabled = false;
            }
            if (nameText)       nameText.text = string.Empty;
            if (descText)       descText.text = string.Empty;
            if (rankBackground)
            {
                InventoryAssets.Release(rankBackground.gameObject);
                _rankBgGen++;
                rankBackground.sprite  = null;
                rankBackground.enabled = false;
            }
            ClearCustomLines();
        }

        // 图标 / 位阶背景异步加载世代号：每次绑定自增，回调据此丢弃过期结果（对象池复用时避免错图）。
        private int _iconGen;
        private int _rankBgGen;

        private void ApplyRankBackground(Skill skill)
        {
            if (!rankBackground) return;
            var owner = rankBackground.gameObject;
            InventoryAssets.Release(owner);
            int gen = ++_rankBgGen;

            var enumItem = SkillRankUtil.Resolve(skill, rankAttrId);
            var bgEntry  = enumItem != null && !string.IsNullOrEmpty(rankBackgroundAttrId)
                ? enumItem.GetEntry(rankBackgroundAttrId) : null;
            if (bgEntry?.value == null) { rankBackground.sprite = null; rankBackground.enabled = false; return; }

            InventoryAssets.Bind<Sprite>(bgEntry.value, owner, s =>
            {
                if (gen != _rankBgGen || !rankBackground) return;   // 过期结果丢弃
                rankBackground.sprite  = s;
                rankBackground.enabled = s;
            });
        }

        private void ApplyCustomFields(Skill skill)
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!showTooltip || _skill == null || !InventoryRuntimeManager.Instance) return;
            Vector2 pos = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            InventoryRuntimeManager.Instance.ShowSkillTooltip(_skill, pos);
            _tooltipShown = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!showTooltip) return;
            _tooltipShown = false;
            if (InventoryRuntimeManager.Instance) InventoryRuntimeManager.Instance.HideSkillTooltip();
        }

        // 本条目被停用（列表回收 / 面板关闭）时不会派发 OnPointerExit，若本格正显示弹窗则主动隐藏，避免残留。
        private void OnDisable()
        {
            if (!_tooltipShown) return;
            _tooltipShown = false;
            if (InventoryRuntimeManager.Instance) InventoryRuntimeManager.Instance.HideSkillTooltip();
        }
    }
}

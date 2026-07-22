using System;
using System.Collections.Generic;
using UnityEngine;

#if IS_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
#endif

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 装备属性加成面板显示组件。读取 <see cref="EquipmentRuntimeManager.GetTotalBonuses"/> 的结果，
    /// 按 <see cref="EquipmentBonus.GroupTag"/> 分组显示（每个分组一行标题 + 若干属性加成行）。
    /// 由 <see cref="UiwEquipmentView"/> 在装备变化时调用 <see cref="Refresh"/>。
    /// </summary>
    public class UiwEquipmentBonusPanel : MonoBehaviour
    {
        [Header("属性加成")]
        [Tooltip("属性加成条目预制体（UiwEquipmentBonusEntry）。")]
        public UiwEquipmentBonusEntry entryPrefab;
        [Tooltip("条目父节点（通常挂 VerticalLayoutGroup）。为空则用本物体。")]
        public Transform entryContainer;
        [Tooltip("可选：分组标题预制体（每个分组标签一行）。为空则不显示分组标题，平铺所有条目。")]
        public UiwEquipmentBonusEntry groupHeaderPrefab;

        [Header("空状态")]
        [Tooltip("无任何属性加成时，用 entryPrefab 显示一条提示；为空则空状态下什么都不显示。")]
        public string emptyText = "无属性加成";
        
#if IS_LOCALIZATION
        [SerializeField] private LocalizeStringEvent emptyTextLocalized;
#endif

        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>按当前装备组的已装备道具刷新总属性加成显示。</summary>
        public void Refresh(string groupId)
        {
            Clear();

            var mgr = EquipmentRuntimeManager.Instance;
            if (mgr == null || !entryPrefab) return;

            var bonuses = mgr.GetTotalBonuses(groupId);
            var parent  = entryContainer ? entryContainer : transform;

            // 按分组标签聚合（保持首次出现顺序）。
            var order = new List<string>();
            var byTag = new Dictionary<string, List<EquipmentBonus>>();
            foreach (var b in bonuses)
            {
                string funcTag = b.GroupTag ?? string.Empty;
                if (!byTag.TryGetValue(funcTag, out var list))
                {
                    list = new List<EquipmentBonus>();
                    byTag[funcTag] = list;
                    order.Add(funcTag);
                }
                list.Add(b);
            }

            foreach (var funcTag in order)
            {
                if (groupHeaderPrefab && !string.IsNullOrEmpty(funcTag))
                {
                    var header = Instantiate(groupHeaderPrefab, parent);
                    header.SetData(ResolveTagName(funcTag), string.Empty);
                    _spawned.Add(header.gameObject);
                }

                if (string.IsNullOrEmpty(funcTag) == false)
                    foreach (var b in byTag[funcTag])
                    {
                        var attrBonus = Instantiate(entryPrefab, parent);
                        attrBonus.SetData(b.Label, FormatValue(b.Total));
                        _spawned.Add(attrBonus.gameObject);
                    }
            }

            // 空状态：没有任何可显示的属性加成时，用条目预制体显示一条提示文本（数值留空）。
            if (_spawned.Count == 0 && !string.IsNullOrEmpty(emptyText))
            {
                var empty = Instantiate(entryPrefab, parent);
                empty.SetData(ResolveEmptyText(), string.Empty);
                _spawned.Add(empty.gameObject);
            }
        }

        /// <summary>解析空状态提示文本：启用本地化且引用完整时取当前语言文本，否则回退 <see cref="emptyText"/>。</summary>
        private string ResolveEmptyText()
        {
#if IS_LOCALIZATION
            if (emptyTextLocalized && emptyTextLocalized.StringReference != null)
            {
                var resolved = emptyTextLocalized.StringReference.GetLocalizedString();
                if (!string.IsNullOrEmpty(resolved)) return resolved;
            }
#endif
            return emptyText;
        }

        /// <summary>清空所有已生成的条目。</summary>
        public void Clear()
        {
            foreach (var go in _spawned)
                if (go) Destroy(go);
            _spawned.Clear();
        }

        private static string ResolveTagName(string tagId)
        {
            var tag = InventoryDataManager.Instance != null
                ? InventoryDataManager.Instance.GetEquipmentGroupTag(tagId) : null;
            return tag != null ? tag.ResolveDisplayName() : tagId;
        }

        /// <summary>整数时显示整数，否则保留两位小数（去除多余零）。</summary>
        private static string FormatValue(double v)
        {
            if (Math.Abs(v - Math.Round(v)) < 1e-9) return ((long)Math.Round(v)).ToString();
            return v.ToString("0.##");
        }
    }
}

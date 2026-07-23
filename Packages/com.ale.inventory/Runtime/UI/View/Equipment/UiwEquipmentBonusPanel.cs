using System;
using System.Collections.Generic;
using UnityEngine;

#if IS_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
#endif

namespace Ale.Inventory.Runtime.UI
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

        // 标题行与条目行是两种预制体、却交错排在同一父节点下，故用两个池分别复用，
        // 再按统一的 sibling 序号排回交错顺序（原实现每次 Refresh 都销毁重建全部行）。
        private readonly UiwWidgetPool<UiwEquipmentBonusEntry> _headerPool = new UiwWidgetPool<UiwEquipmentBonusEntry>();
        private readonly UiwWidgetPool<UiwEquipmentBonusEntry> _entryPool  = new UiwWidgetPool<UiwEquipmentBonusEntry>();

        // 分组聚合用的复用缓冲（每次 Refresh 清空重填，避免逐次分配）。
        private readonly List<string> _tagOrder = new List<string>();
        private readonly Dictionary<string, List<EquipmentBonus>> _byTag = new Dictionary<string, List<EquipmentBonus>>();

        /// <summary>按当前装备组的已装备道具刷新总属性加成显示。</summary>
        public void Refresh(string groupId)
        {
            var parent = entryContainer ? entryContainer : transform;
            _headerPool.Configure(groupHeaderPrefab, parent);
            _entryPool.Configure(entryPrefab, parent);
            _headerPool.Begin();
            _entryPool.Begin();

            var mgr = EquipmentRuntimeManager.Instance;
            if (mgr == null || !entryPrefab) { _headerPool.End(); _entryPool.End(); return; }

            // 按分组标签聚合（保持首次出现顺序）。
            _tagOrder.Clear();
            foreach (var kv in _byTag) kv.Value.Clear();
            foreach (var b in mgr.GetTotalBonuses(groupId))
            {
                string funcTag = b.GroupTag ?? string.Empty;
                if (!_byTag.TryGetValue(funcTag, out var list))
                {
                    list = new List<EquipmentBonus>();
                    _byTag[funcTag] = list;
                }
                if (list.Count == 0 && !_tagOrder.Contains(funcTag)) _tagOrder.Add(funcTag);
                list.Add(b);
            }

            int sibling = 0;
            foreach (var funcTag in _tagOrder)
            {
                if (groupHeaderPrefab && !string.IsNullOrEmpty(funcTag))
                {
                    var header = _headerPool.Next();
                    if (header)
                    {
                        header.SetData(ResolveTagName(funcTag), string.Empty);
                        header.transform.SetSiblingIndex(sibling++);
                    }
                }

                // 加成条目无条件生成：仅**分组标题**依赖 funcTag 非空（无分组自然没有标题），
                // 条目本身不该受此约束——否则未配分组标签的加成会被静默丢弃，
                // 若全部加成都没有分组，面板还会误显示「无属性加成」空态。
                foreach (var b in _byTag[funcTag])
                {
                    var attrBonus = _entryPool.Next();
                    if (!attrBonus) break;
                    attrBonus.SetData(b.Label, FormatValue(b.Total));
                    attrBonus.transform.SetSiblingIndex(sibling++);
                }
            }

            // 空状态：没有任何可显示的属性加成时，用条目预制体显示一条提示文本（数值留空）。
            if (sibling == 0 && !string.IsNullOrEmpty(emptyText))
            {
                var empty = _entryPool.Next();
                if (empty)
                {
                    empty.SetData(ResolveEmptyText(), string.Empty);
                    empty.transform.SetSiblingIndex(sibling++);
                }
            }

            _headerPool.End();
            _entryPool.End();
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

        /// <summary>隐藏所有已生成的条目（实例保留在池中，供下次刷新复用）。</summary>
        public void Clear()
        {
            _headerPool.RecycleAll();
            _entryPool.RecycleAll();
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

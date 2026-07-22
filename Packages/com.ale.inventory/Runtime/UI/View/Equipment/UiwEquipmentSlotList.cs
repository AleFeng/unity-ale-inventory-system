#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 槽位列表显示组件。显示一个槽位列表的名称，并展示该列表的所有装备槽。
    /// 支持两种装备槽布局方式（<see cref="DisplayMode"/>，在 Inspector 切换；切到对应模式只显示该模式需配置的字段）：
    /// <list type="bullet">
    /// <item><b>自动</b>：在 <see cref="slotContainer"/>（通常挂 HorizontalLayoutGroup）下按 <see cref="slotPrefab"/>
    /// 实例化该列表的全部装备槽。</item>
    /// <item><b>手动</b>：用户在本物体层级下自行摆放好 <see cref="UiwEquipmentSlot"/> 物体，
    /// 通过 <see cref="manualSlots"/>（槽位 ID → 装备槽）逐一指定绑定关系，可实现完全自由的 UI 排版与表现。</item>
    /// </list>
    /// 由 <see cref="UiwEquipmentGroupPanel"/>（或装备选择面板）通过 <see cref="Bind"/> 驱动。
    /// </summary>
    public class UiwEquipmentSlotList : MonoBehaviour
    {
        /// <summary>装备槽布局方式。</summary>
        public enum DisplayMode
        {
            /// <summary>自动：按槽位列表配置实例化全部装备槽（默认）。</summary>
            Auto = 0,
            /// <summary>手动：用户手动摆放装备槽物体，按槽位 ID 逐一绑定。</summary>
            Manual = 1,
        }

        /// <summary>手动模式下的一条「槽位 ID → 装备槽」绑定。</summary>
        [Serializable]
        public class ManualSlotBinding
        {
            [Tooltip("槽位列表配置中的槽位 ID（须与某装备槽的 ID 一致）。")]
            public string slotId;
            [Tooltip("层级中手动摆放的装备槽物体。")]
            public UiwEquipmentSlot slot;
        }

        [Header("槽位列表")]
        [Tooltip("槽位列表名称文本（可选）。")]
        public InventoryText nameText;

        [Header("布局方式")]
        [Tooltip("装备槽布局方式：自动按配置实例化，或手动摆放并按槽位 ID 绑定。")]
        public DisplayMode displayMode = DisplayMode.Auto;

        [Tooltip("装备槽预制体（UiwEquipmentSlot）。")]
        public UiwEquipmentSlot slotPrefab;
        [Tooltip("装备槽父节点（通常挂 HorizontalLayoutGroup）。为空则用本物体。")]
        public Transform slotContainer;

        [Tooltip("手动绑定：每条指定一个槽位 ID 与层级中对应的装备槽物体。需自行在层级中摆放装备槽位置。")]
        public List<ManualSlotBinding> manualSlots = new List<ManualSlotBinding>();

        private readonly List<UiwEquipmentSlot> _slots = new List<UiwEquipmentSlot>();

        /// <summary>所属装备组 ID。</summary>
        public string GroupId { get; private set; }
        /// <summary>槽位列表 ID。</summary>
        public string SlotListId { get; private set; }

        /// <summary>本列表中某装备槽被左键点击。</summary>
        public event Action<UiwEquipmentSlot> SlotClicked;
        /// <summary>本列表中某装备槽被右键点击。</summary>
        public event Action<UiwEquipmentSlot> SlotRightClicked;

        /// <summary>绑定到某装备组的某槽位列表定义并刷新所有装备槽。</summary>
        public void Bind(string groupId, EquipmentSlotList def)
        {
            GroupId    = groupId;
            SlotListId = def != null ? def.id : null;

            if (nameText)
                nameText.text = def != null
                    ? (string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName)
                    : string.Empty;

            if (displayMode == DisplayMode.Manual)
                BindManual(groupId, def);
            else
                BindAuto(groupId, def);
        }

        /// <summary>刷新所有已激活装备槽的显示。</summary>
        public void Refresh()
        {
            if (displayMode == DisplayMode.Manual)
            {
                foreach (var b in manualSlots)
                    if (b != null && b.slot && b.slot.gameObject.activeSelf) b.slot.Refresh();
            }
            else
            {
                foreach (var s in _slots)
                    if (s && s.gameObject.activeSelf) s.Refresh();
            }
        }

        /// <summary>设置当前选中的装备槽（仅该槽显示选中指示，其余清除）。传 null 清除全部。</summary>
        public void SetSelectedSlot(string slotId)
        {
            if (displayMode == DisplayMode.Manual)
            {
                foreach (var b in manualSlots)
                    if (b != null && b.slot)
                        b.slot.SetSelected(b.slot.gameObject.activeSelf && !string.IsNullOrEmpty(slotId) && b.slot.SlotId == slotId);
            }
            else
            {
                foreach (var s in _slots)
                    if (s) s.SetSelected(s.gameObject.activeSelf && !string.IsNullOrEmpty(slotId) && s.SlotId == slotId);
            }
        }

        #region 自动模式（按配置实例化装备槽）

        private void BindAuto(string groupId, EquipmentSlotList def)
        {
            int n = def != null ? def.slots.Count : 0;
            EnsureSlots(n);
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < n)
                {
                    _slots[i].gameObject.SetActive(true);
                    if (def != null) _slots[i].Bind(groupId, def, def.slots[i]);
                }
                else
                {
                    _slots[i].gameObject.SetActive(false);
                }
            }
        }

        private void EnsureSlots(int count)
        {
            if (!slotPrefab) return;
            var parent = slotContainer ? slotContainer : transform;
            while (_slots.Count < count)
            {
                var s = Instantiate(slotPrefab, parent);
                s.Clicked      += OnSlotClicked;
                s.RightClicked += OnSlotRightClicked;
                _slots.Add(s);
            }
        }

        #endregion

        #region 手动模式（用户摆放装备槽，按槽位 ID 绑定）

        private void BindManual(string groupId, EquipmentSlotList def)
        {
            foreach (var b in manualSlots)
            {
                if (b == null || b.slot == null) continue;

                if (def != null && TryLocateSlot(def, b.slotId, out var slotDef))
                {
                    b.slot.gameObject.SetActive(true);
                    // 防止重复订阅：先解绑再绑定（手动装备槽不会被销毁重建，Bind 可能多次调用）。
                    b.slot.Clicked      -= OnSlotClicked;
                    b.slot.RightClicked -= OnSlotRightClicked;
                    b.slot.Clicked      += OnSlotClicked;
                    b.slot.RightClicked += OnSlotRightClicked;
                    b.slot.Bind(groupId, def, slotDef);
                }
                else
                {
                    Debug.LogWarning(
                        $"[UiwEquipmentSlotList] 手动绑定失败：槽位列表「{(def != null ? def.id : SlotListId)}」中找不到槽位 ID「{b.slotId}」。",
                        this);
                }
            }
        }

        /// <summary>在槽位列表定义中按槽位 ID 定位装备槽定义。</summary>
        private static bool TryLocateSlot(EquipmentSlotList def, string slotId, out EquipmentSlot slotDef)
        {
            slotDef = null;
            if (def == null || string.IsNullOrEmpty(slotId) || def.slots == null) return false;
            foreach (var s in def.slots)
            {
                if (s != null && s.id == slotId)
                {
                    slotDef = s;
                    return true;
                }
            }
            return false;
        }

        #endregion

        private void OnSlotClicked(UiwEquipmentSlot s)      => SlotClicked?.Invoke(s);
        private void OnSlotRightClicked(UiwEquipmentSlot s) => SlotRightClicked?.Invoke(s);
    }
}

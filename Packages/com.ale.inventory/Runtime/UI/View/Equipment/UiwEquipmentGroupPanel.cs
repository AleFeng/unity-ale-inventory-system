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
    /// 装备组面板显示组件。显示一个装备组的名称，并展示该装备组的所有槽位列表。
    /// 支持两种槽位列表布局方式（<see cref="DisplayMode"/>，在 Inspector 切换；切到对应模式只显示该模式需配置的字段）：
    /// <list type="bullet">
    /// <item><b>自动</b>：在 <see cref="slotListContainer"/> 下按 <see cref="slotListPrefab"/> 实例化装备组配置中的
    /// 所有槽位列表（每个槽位列表再实例化其装备槽），布局由 Layout 组件自动排布。</item>
    /// <item><b>手动</b>：用户在本物体层级下自行摆放好 <see cref="UiwEquipmentSlotList"/> 物体，
    /// 通过 <see cref="manualSlotLists"/>（槽位列表 ID → 槽位列表）逐一指定绑定关系，可实现完全自由的 UI 排版与表现。</item>
    /// </list>
    ///
    /// <para>可在 Inspector 配置 <see cref="groupId"/> 并勾选 <see cref="bindOnStart"/> 独立使用；
    /// 或由 <see cref="UiwEquipmentView"/> 通过 <see cref="Bind"/> 驱动。</para>
    /// </summary>
    public class UiwEquipmentGroupPanel : MonoBehaviour
    {
        /// <summary>槽位列表布局方式。</summary>
        public enum DisplayMode
        {
            /// <summary>自动：按装备组配置实例化全部槽位列表（默认）。</summary>
            Auto = 0,
            /// <summary>手动：用户手动摆放槽位列表物体，按槽位列表 ID 逐一绑定。</summary>
            Manual = 1,
        }

        /// <summary>手动模式下的一条「槽位列表 ID → 槽位列表」绑定。</summary>
        [Serializable]
        public class ManualSlotListBinding
        {
            [Tooltip("装备组配置中的槽位列表 ID（须与某槽位列表的 ID 一致）。")]
            public string slotListId;
            [Tooltip("层级中手动摆放的槽位列表物体。")]
            public UiwEquipmentSlotList slotList;
        }

        [Header("装备组")]
        [Tooltip("装备组名称文本（可选）。")]
        public InventoryText groupNameText;

        [Header("布局方式")]
        [Tooltip("槽位列表布局方式：自动按配置实例化，或手动摆放并按槽位列表 ID 绑定。")]
        public DisplayMode displayMode = DisplayMode.Auto;

        [Tooltip("槽位列表预制体（UiwEquipmentSlotList）。")]
        public UiwEquipmentSlotList slotListPrefab;
        [Tooltip("槽位列表父节点。为空则用本物体。")]
        public Transform slotListContainer;

        [Tooltip("手动绑定：每条指定一个槽位列表 ID 与层级中对应的槽位列表物体。需自行在层级中摆放槽位列表位置。")]
        public List<ManualSlotListBinding> manualSlotLists = new List<ManualSlotListBinding>();

        [Header("独立使用（可选）")]
        [Tooltip("装备组 ID：勾选「Start 时自动绑定」后据此显示数据。")]
        [SerializeField] private string groupId;
        [Tooltip("Start 时自动按上面的装备组 ID 绑定（独立使用时勾选；由视图驱动时不需要）。")]
        [SerializeField] private bool bindOnStart;

        private readonly List<UiwEquipmentSlotList> _slotLists = new List<UiwEquipmentSlotList>();
        private bool _bound;

        /// <summary>当前绑定的装备组 ID。</summary>
        public string GroupId { get; private set; }

        /// <summary>本面板中某装备槽被左键点击。</summary>
        public event Action<UiwEquipmentSlot> SlotClicked;
        /// <summary>本面板中某装备槽被右键点击。</summary>
        public event Action<UiwEquipmentSlot> SlotRightClicked;

        private void Start()
        {
            if (bindOnStart && !_bound && !string.IsNullOrEmpty(groupId))
                Bind(groupId);
        }

        /// <summary>绑定到某装备组并刷新所有槽位列表。</summary>
        public void Bind(string newGroupId)
        {
            _bound  = true;
            GroupId = newGroupId;

            var group = InventoryDataManager.Instance != null
                ? InventoryDataManager.Instance.GetEquipmentGroup(newGroupId) : null;

            if (groupNameText)
            {
                string nm = group?.displayNameText != null ? group.displayNameText.ResolveText() : null;
                groupNameText.text = !string.IsNullOrEmpty(nm) ? nm
                    : (group != null ? group.id : newGroupId);
            }

            if (displayMode == DisplayMode.Manual)
                BindManual(newGroupId, group);
            else
                BindAuto(newGroupId, group);
        }

        /// <summary>刷新所有已激活槽位列表的显示。</summary>
        public void Refresh()
        {
            if (displayMode == DisplayMode.Manual)
            {
                foreach (var b in manualSlotLists)
                    if (b != null && b.slotList && b.slotList.gameObject.activeSelf) b.slotList.Refresh();
            }
            else
            {
                foreach (var sl in _slotLists)
                    if (sl && sl.gameObject.activeSelf) sl.Refresh();
            }
        }

        #region 自动模式（按配置实例化槽位列表）

        private void BindAuto(string newGroupId, EquipmentGroup group)
        {
            int n = group != null ? group.slotLists.Count : 0;
            EnsureSlotLists(n);
            for (int i = 0; i < _slotLists.Count; i++)
            {
                if (i < n)
                {
                    _slotLists[i].gameObject.SetActive(true);
                    if (group != null) _slotLists[i].Bind(newGroupId, group.slotLists[i]);
                }
                else
                {
                    _slotLists[i].gameObject.SetActive(false);
                }
            }
        }

        private void EnsureSlotLists(int count)
        {
            if (!slotListPrefab) return;
            var parent = slotListContainer ? slotListContainer : transform;
            while (_slotLists.Count < count)
            {
                var sl = Instantiate(slotListPrefab, parent);
                sl.SlotClicked      += OnSlotClicked;
                sl.SlotRightClicked += OnSlotRightClicked;
                _slotLists.Add(sl);
            }
        }

        #endregion

        #region 手动模式（用户摆放槽位列表，按槽位列表 ID 绑定）

        private void BindManual(string newGroupId, EquipmentGroup group)
        {
            foreach (var b in manualSlotLists)
            {
                if (b == null || b.slotList == null) continue;

                if (group != null && TryLocateSlotList(group, b.slotListId, out var listDef))
                {
                    b.slotList.gameObject.SetActive(true);
                    // 防止重复订阅：先解绑再绑定（手动槽位列表不会被销毁重建，Bind 可能多次调用）。
                    b.slotList.SlotClicked      -= OnSlotClicked;
                    b.slotList.SlotRightClicked -= OnSlotRightClicked;
                    b.slotList.SlotClicked      += OnSlotClicked;
                    b.slotList.SlotRightClicked += OnSlotRightClicked;
                    b.slotList.Bind(newGroupId, listDef);
                }
                else
                {
                    Debug.LogWarning(
                        $"[UiwEquipmentGroupPanel] 手动绑定失败：装备组「{newGroupId}」中找不到槽位列表 ID「{b.slotListId}」。",
                        this);
                }
            }
        }

        /// <summary>在装备组中按槽位列表 ID 定位槽位列表定义。</summary>
        private static bool TryLocateSlotList(EquipmentGroup group, string slotListId, out EquipmentSlotList listDef)
        {
            listDef = null;
            if (group == null || string.IsNullOrEmpty(slotListId) || group.slotLists == null) return false;
            foreach (var sl in group.slotLists)
            {
                if (sl != null && sl.id == slotListId)
                {
                    listDef = sl;
                    return true;
                }
            }
            return false;
        }

        #endregion

        private void OnSlotClicked(UiwEquipmentSlot s)
        {
            // 点击装备槽即将打开选择面板并隐藏本面板；GO 停用时 Unity 不会派发 OnPointerExit，
            // 故主动取消该槽的悬停高亮，避免下次显示装备组面板时残留高亮。
            if (s) s.ClearHoverHighlight();
            SlotClicked?.Invoke(s);
        }

        private void OnSlotRightClicked(UiwEquipmentSlot s) => SlotRightClicked?.Invoke(s);
    }
}

#if IS_TMP
using InventoryText = TMPro.TMP_Text;
#else
using InventoryText = UnityEngine.UI.Text;
#endif

using System;
using InventorySystem.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 装备选择面板。点击装备组面板中的某装备槽时打开，替换显示装备组面板。
    /// <list type="bullet">
    ///   <item>顶部：槽位组切换栏（左右按钮在装备组的各槽位列表间切换，显示名称与位置 N/M）。</item>
    ///   <item>中间：当前槽位列表的全部装备槽（左键选中、右键卸下到来源仓库）。</item>
    ///   <item>底部：当前槽位组可装备的道具列表（来自来源仓库、按限制筛选；点击装备）。</item>
    ///   <item>退出：退出按钮或在面板空白处右键，返回装备组面板（<see cref="OnExit"/>）。</item>
    /// </list>
    /// </summary>
    public class UiwEquipmentSelectPanel : MonoBehaviour, IPointerClickHandler
    {
        [Header("切换栏")]
        [Tooltip("上一个槽位列表。")]
        public Button prevButton;
        [Tooltip("下一个槽位列表。")]
        public Button nextButton;
        [Tooltip("当前槽位列表名称文本。")]
        public InventoryText slotListNameText;
        [Tooltip("当前槽位列表位置文本（如 1/3）。")]
        public InventoryText positionText;

        [Header("中间：当前槽位列表")]
        [Tooltip("当前槽位列表显示组件（复用 UiwEquipmentSlotList）。")]
        public UiwEquipmentSlotList slotListView;

        [Header("底部：可装备道具")]
        [Tooltip("可装备道具列表组件。")]
        public UiwEquipmentCandidateList candidateList;

        [Header("退出")]
        [Tooltip("退出按钮（返回装备组面板）。")]
        public Button exitButton;

        /// <summary>面板退出时触发（由视图据此还原显示装备组面板）。</summary>
        public event Action OnExit;

        private string         _groupId;
        private string         _selectedSlotId;
        private int            _index;
        private EquipmentGroup _group;
        private bool           _wired;

        /// <summary>当前选中的装备槽 ID（供快速装备优先定位；无选中时为空）。</summary>
        public string SelectedSlotId => _selectedSlotId;

        private void Awake()
        {
            if (_wired) return;
            _wired = true;
            if (prevButton) prevButton.onClick.AddListener(Prev);
            if (nextButton) nextButton.onClick.AddListener(Next);
            if (exitButton) exitButton.onClick.AddListener(Exit);
        }

        #region 打开 / 关闭

        /// <summary>
        /// 打开选择面板。候选道具来源 / 装备取出 / 卸下放入的仓库均取自装备组配置的「装备仓库」。
        /// </summary>
        /// <param name="groupId">装备组 ID。</param>
        /// <param name="initialSlotListId">初始定位到的槽位列表 ID。</param>
        /// <param name="initialSelectedSlotId">初始选中的装备槽 ID（可空）。</param>
        public void Open(string groupId, string initialSlotListId, string initialSelectedSlotId)
        {
            _groupId        = groupId;
            _selectedSlotId = initialSelectedSlotId;

            gameObject.SetActive(true);

            _group = InventoryDataManager.Instance != null
                ? InventoryDataManager.Instance.GetEquipmentGroup(groupId) : null;
            _index = IndexOfSlotList(initialSlotListId);

            WireChildEvents();
            Subscribe();
            BindCurrent();
        }

        private void Exit()
        {
            Unsubscribe();
            gameObject.SetActive(false);
            OnExit?.Invoke();
        }

        private void OnDestroy() => Unsubscribe();

        #endregion

        #region 切换 / 绑定

        private void BindCurrent()
        {
            var sl    = CurrentSlotList();
            int count = _group != null ? _group.slotLists.Count : 0;

            if (slotListNameText)
                slotListNameText.text = sl != null
                    ? (string.IsNullOrEmpty(sl.displayName) ? sl.id : sl.displayName)
                    : string.Empty;
            if (positionText)
                positionText.text = count > 0 ? $"{_index + 1}/{count}" : string.Empty;

            if (prevButton) prevButton.interactable = count > 1;
            if (nextButton) nextButton.interactable = count > 1;

            if (slotListView)
            {
                slotListView.Bind(_groupId, sl);
                slotListView.SetSelectedSlot(_selectedSlotId);
            }
            if (candidateList)
            {
                // 候选列表显示排序：条件取自装备组自身（sortPriorities / sortTiebreakers，创建时从模板复制、可独立编辑），db 取装备组所属数据库。
                var dm = InventoryDataManager.Instance;
                var db = dm != null ? dm.FindDatabaseForEquipmentGroup(_groupId) : null;
                candidateList.Bind(
                    EquipmentRuntimeManager.Instance != null
                        ? EquipmentRuntimeManager.Instance.GetEquipmentInventories(_groupId) : null,
                    sl,
                    _group?.sortPriorities, _group?.sortTiebreakers, db);
            }
        }

        private void Prev()
        {
            int count = _group != null ? _group.slotLists.Count : 0;
            if (count <= 1) return;
            _index = (_index - 1 + count) % count;
            SelectFirstSlotOfCurrent();
            BindCurrent();
        }

        private void Next()
        {
            int count = _group != null ? _group.slotLists.Count : 0;
            if (count <= 1) return;
            _index = (_index + 1) % count;
            SelectFirstSlotOfCurrent();
            BindCurrent();
        }

        #endregion

        #region 交互：选中 / 卸下 / 装备

        private void HandleSlotClicked(UiwEquipmentSlot slot)
        {
            if (slot == null) return;
            _selectedSlotId = slot.SlotId;
            if (slotListView) slotListView.SetSelectedSlot(_selectedSlotId);
        }

        private void HandleSlotRightClicked(UiwEquipmentSlot slot)
        {
            if (slot == null || EquipmentRuntimeManager.Instance == null) return;
            // 右键卸下：放入装备组配置的「装备仓库」（Index0 起第一个放得下的）。
            // OnEquipmentChanged / OnInventoryChanged 会触发刷新。
            EquipmentRuntimeManager.Instance.UnequipToConfigured(_groupId, slot.SlotId);
        }

        /// <summary>面板空白处右键退出（点到子级道具/槽位时由其自身处理，不冒泡到此）。</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
                Exit();
        }

        #endregion

        #region 事件订阅 / 刷新

        private void WireChildEvents()
        {
            if (slotListView)
            {
                slotListView.SlotClicked      -= HandleSlotClicked;
                slotListView.SlotRightClicked -= HandleSlotRightClicked;
                slotListView.SlotClicked      += HandleSlotClicked;
                slotListView.SlotRightClicked += HandleSlotRightClicked;
            }
            // 候选道具的「右键快速装备 / 左键拖拽装备」与背包格子一致：右键由 UiwInventoryItemCell 广播、
            // UiwEquipmentView 订阅统一处理；左键拖拽到装备槽由 UiwEquipmentSlot 处理。本面板无需接线。
        }

        private void Subscribe()
        {
            if (EquipmentRuntimeManager.Instance != null)
            {
                EquipmentRuntimeManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
                EquipmentRuntimeManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
            }
            if (InventoryRuntimeManager.Instance != null)
            {
                InventoryRuntimeManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
                InventoryRuntimeManager.Instance.OnInventoryChanged += HandleInventoryChanged;
            }
        }

        private void Unsubscribe()
        {
            if (EquipmentRuntimeManager.Instance != null)
                EquipmentRuntimeManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
            if (InventoryRuntimeManager.Instance != null)
                InventoryRuntimeManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }

        private void HandleEquipmentChanged(string groupId)
        {
            if (groupId != _groupId) return;
            if (slotListView)
            {
                slotListView.Refresh();
                slotListView.SetSelectedSlot(_selectedSlotId);
            }
            if (candidateList) candidateList.Refresh();
        }

        private void HandleInventoryChanged(string inventoryId)
        {
            // 变更的仓库属于本装备组的「有效装备仓库」时刷新候选列表。
            if (candidateList && EquipmentRuntimeManager.Instance != null
                && EquipmentRuntimeManager.Instance.IsEquipmentInventory(_groupId, inventoryId))
                candidateList.Refresh();
        }

        #endregion

        #region 辅助

        private EquipmentSlotList CurrentSlotList()
            => (_group != null && _index >= 0 && _index < _group.slotLists.Count)
                ? _group.slotLists[_index] : null;

        /// <summary>将选中槽位重置为当前槽位列表的第 0 个装备槽（切换槽位列表后调用）；列表为空则清空选中。</summary>
        private void SelectFirstSlotOfCurrent()
        {
            var sl = CurrentSlotList();
            _selectedSlotId = (sl != null && sl.slots != null && sl.slots.Count > 0)
                ? sl.slots[0].id : null;
        }

        private int IndexOfSlotList(string slotListId)
        {
            if (_group == null || string.IsNullOrEmpty(slotListId)) return 0;
            for (int i = 0; i < _group.slotLists.Count; i++)
                if (_group.slotLists[i].id == slotListId) return i;
            return 0;
        }

        #endregion
    }
}

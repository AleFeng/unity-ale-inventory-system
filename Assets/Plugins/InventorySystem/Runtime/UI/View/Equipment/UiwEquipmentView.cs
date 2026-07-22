using System;
using UnityEngine;

namespace InventorySystem.Runtime.UI
{
    /// <summary>
    /// 装备主界面（MonoBehaviour，继承 <see cref="UiwViewBase"/>）。
    /// 整合「装备组面板」（左侧：显示装备组全部槽位与已装备道具）与「属性加成面板」（右侧：总属性加成，按分组标签分组）。
    /// </summary>
    public class UiwEquipmentView : UiwViewBase
    {
        [Header("装备组面板")]
        [Tooltip("装备组面板（左侧：槽位列表 + 装备槽）。")]
        public UiwEquipmentGroupPanel groupPanel;

        [Header("属性加成面板")]
        [Tooltip("属性加成面板（右侧：总属性加成，按分组标签分组）。")]
        public UiwEquipmentBonusPanel bonusPanel;

        [Header("装备选择面板（可选）")]
        [Tooltip("装备选择面板：左键点击装备槽时打开，替换装备组面板。为空则左键仅触发 OnSlotClicked。")]
        public UiwEquipmentSelectPanel selectPanel;

        [Header("装备组")]
        [Tooltip("要显示的装备组 ID。可在 Inspector 预设：本视图始终使用该值，直到经 Open(groupId) 或 Inspector 改动。" +
                 "装备取出 / 卸下放入的仓库取自该装备组配置的「装备仓库」。")]
        [SerializeField] private string groupId = "角色装备";

        /// <summary>当前打开的装备组 ID（供背包桥接等读取）。</summary>
        public string CurrentGroupId => groupId;

        /// <summary>装备槽被左键点击时触发（先于打开选择面板，供外部额外挂接）。</summary>
        public event Action<UiwEquipmentSlot> OnSlotClicked;

        #region 打开与关闭

        /// <summary>
        /// 打开装备界面。装备取出 / 卸下放入的仓库均取自装备组配置的「装备仓库」。
        /// </summary>
        /// <param name="groupIdSet">要显示的装备组 ID。</param>
        public void Open(string groupIdSet)
        {
            this.groupId = groupIdSet;
            Open();
        }

        /// <summary>用当前缓存的装备组重新打开装备界面（仓库取自装备组配置的「装备仓库」）。</summary>
        public override void Open()
        {
            base.Open();   // 激活面板（公共步骤）

            var group = InventoryDataManager.Instance != null
                ? InventoryDataManager.Instance.GetEquipmentGroup(groupId) : null;
            if (titleLabel)
                titleLabel.text = group != null
                    ? ResolveTitleText(group.displayNameText != null ? group.displayNameText.ResolveText() : null, groupId)
                    : groupId;

            // 初始显示装备组面板，隐藏选择面板。
            if (selectPanel)
            {
                selectPanel.OnExit -= HandleSelectExit;
                selectPanel.OnExit += HandleSelectExit;
                selectPanel.gameObject.SetActive(false);
            }
            if (groupPanel)
            {
                groupPanel.gameObject.SetActive(true);
                groupPanel.SlotClicked      -= HandleSlotClicked;
                groupPanel.SlotRightClicked -= HandleSlotRightClicked;
                groupPanel.SlotClicked      += HandleSlotClicked;
                groupPanel.SlotRightClicked += HandleSlotRightClicked;
                groupPanel.Bind(groupId);
            }

            RefreshBonus();

            if (EquipmentRuntimeManager.Instance != null)
                EquipmentRuntimeManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;

            // 背包 / 仓库道具右键 → 自动装备到本装备组：直接订阅通用「道具右键」事件（无需单独的桥接组件接线）。
            // 兼容网格格子（UiwInventoryItemCell）与顺序 / 明细行（UiwInventoryItemDetail）——两者右键都经此事件广播。
            UiwInventoryItemEvents.ItemRightClicked -= HandleItemRightClicked;
            UiwInventoryItemEvents.ItemRightClicked += HandleItemRightClicked;
        }

        /// <summary>取消本视图按打开订阅的运行时事件（由基类 <see cref="UiwViewBase.Close"/> 与 OnDestroy 调用）。</summary>
        protected override void Unsubscribe()
        {
            if (EquipmentRuntimeManager.Instance != null)
                EquipmentRuntimeManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
            UiwInventoryItemEvents.ItemRightClicked -= HandleItemRightClicked;
            if (groupPanel)
            {
                groupPanel.SlotClicked      -= HandleSlotClicked;
                groupPanel.SlotRightClicked -= HandleSlotRightClicked;
            }
            if (selectPanel)
                selectPanel.OnExit -= HandleSelectExit;
        }

        /// <summary>用上次打开的装备组重新打开（供基类 <see cref="UiwViewBase.ToggleOpenClose"/>）。</summary>
        protected override void Reopen()
        {
            if (string.IsNullOrEmpty(groupId))
            {
                Debug.LogWarning("[UiwEquipmentView] 切换失败：尚未指定装备组；请先调用 Open(groupId)。");
                return;
            }
            Open(groupId);
        }

        private void OnDestroy() => Unsubscribe();

        #endregion

        #region 事件
        
        private void HandleEquipmentChanged(string groupIdSet)
        {
            if (groupIdSet != groupId) return;
            if (groupPanel) groupPanel.Refresh();
            RefreshBonus();
        }

        private void HandleSlotClicked(UiwEquipmentSlot slot)
        {
            OnSlotClicked?.Invoke(slot);
            if (slot == null || !selectPanel) return;

            // 打开装备选择面板，定位到该槽所属槽位列表并选中该槽；隐藏装备组面板。
            if (groupPanel) groupPanel.gameObject.SetActive(false);
            selectPanel.OnExit -= HandleSelectExit;
            selectPanel.OnExit += HandleSelectExit;
            selectPanel.Open(groupId, slot.SlotListId, slot.SlotId);
        }

        /// <summary>选择面板退出：还原显示装备组面板并刷新。</summary>
        private void HandleSelectExit()
        {
            if (groupPanel)
            {
                groupPanel.gameObject.SetActive(true);
                groupPanel.Refresh();
            }
            RefreshBonus();
        }

        private void HandleSlotRightClicked(UiwEquipmentSlot slot)
        {
            if (slot == null || EquipmentRuntimeManager.Instance == null) return;
            // 右键卸下：放入装备组配置的「装备仓库」（Index0 起第一个放得下的）。OnEquipmentChanged 会触发刷新。
            EquipmentRuntimeManager.Instance.UnequipToConfigured(groupId, slot.SlotId);
        }

        /// <summary>
        /// 背包 / 仓库道具被右键 → 快速装备到本装备组。装入优先级：
        /// ① 装备选择面板打开时，其当前选中的装备槽（占用则替换）；② 第一个可装入的空槽；
        /// ③ 第一个「该道具满足其限制」的已占用槽（Index0 起，卸下原道具放回来源仓库）。
        /// 装备组配置了「装备仓库」时仅处理来自其中仓库的右键；未配置则不限制来源仓库。均不满足时不处理。
        /// </summary>
        private void HandleItemRightClicked(string inventoryId, string itemId)
        {
            if (!gameObject.activeInHierarchy) return;             // 界面未显示时不响应
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(itemId)) return;

            var eq = EquipmentRuntimeManager.Instance;
            if (eq == null) return;

            // 装备组配置了「装备仓库」时，仅处理来自其中仓库的右键；未配置则不限制来源仓库。
            if (eq.GetEquipmentInventories(groupId).Count > 0 && !eq.IsEquipmentInventory(groupId, inventoryId))
                return;

            // 装备选择面板打开时，优先装入其当前选中的装备槽（占用则替换）；否则走空槽 / Index0 回退。
            string preferredSlotId = selectPanel && selectPanel.gameObject.activeInHierarchy
                ? selectPanel.SelectedSlotId : null;

            eq.TryAutoEquipOrReplace(groupId, itemId, inventoryId, preferredSlotId);
        }

        private void RefreshBonus()
        {
            if (bonusPanel) bonusPanel.Refresh(groupId);
        }

        #endregion
    }
}

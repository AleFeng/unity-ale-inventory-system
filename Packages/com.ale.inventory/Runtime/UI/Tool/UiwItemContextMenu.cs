using System.Collections.Generic;
using Ale.Toolkit.Runtime;
using Ale.Toolkit.Runtime.UI;
using UnityEngine;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 道具右键操作菜单（场景全局单例）。在通用菜单 <see cref="UiwContextMenu"/> 之上组装本领域的条目：
    /// <b>查看 / 使用 / 丢弃</b>，并收集上层贡献的额外条目（如装备界面的「装备」，见
    /// <see cref="UiwInventoryItemEvents.CollectingItemMenu"/>）。
    ///
    /// <para>预制体配置在 <see cref="InventoryRuntimeManager"/> 上，运行时由其全局实例化一次并经
    /// <see cref="InventoryRuntimeManager.ShowItemContextMenu"/> 统一对外调用（实现 <see cref="IItemContextMenu"/>）——
    /// 与悬停弹窗 <see cref="UiwItemTooltip"/> 完全同一套宿主模式。</para>
    ///
    /// <para><b>「使用」的显示条件</b>是道具的 <see cref="Item.onUseEffectRefs"/> 非空 —— 这是本包内唯一的
    /// 「可使用」信号（没有 IsUsable 标志、也没有道具类型枚举）；空列表时 <c>UseItem</c> 只会返回
    /// <see cref="EItemUseOutcome.NoEffects"/> 且不扣减，条目显示出来也没有意义。</para>
    ///
    /// <para><b>「丢弃」与不可丢弃道具</b>：道具勾了 <see cref="Item.noDiscard"/> 时，「丢弃」默认<b>置灰保留</b>
    /// 而非隐藏——菜单条目数不随道具跳动，且玩家能直接看出「这个丢不掉」而不是以为菜单少了一项；
    /// 想改成隐藏就打开 <see cref="hideDiscardWhenLocked"/>。</para>
    /// </summary>
    public class UiwItemContextMenu : UiwContextMenu, IItemContextMenu
    {
        /// <summary>场景全局单例（最近一次 Awake 的实例）。</summary>
        public static UiwItemContextMenu Instance { get; private set; }

        #region 配置

        [Header("内置条目 · 文案")]
        [Tooltip("「查看」条目文案。")]
        public TextValue viewLabel = new TextValue("查看");
        [Tooltip("「使用」条目文案。")]
        public TextValue useLabel = new TextValue("使用");
        [Tooltip("「丢弃」条目文案。")]
        public TextValue discardLabel = new TextValue("丢弃");

        [Header("内置条目 · 开关")]
        [Tooltip("显示「查看」（弹出道具详情弹窗）。")]
        public bool enableView = true;
        [Tooltip("显示「使用」（仅当道具配置了使用效果时才会出现）。")]
        public bool enableUse = true;
        [Tooltip("显示「丢弃」（弹出数量选择弹窗）。")]
        public bool enableDiscard = true;
        [Tooltip("道具禁止丢弃（道具配置勾了「不可丢弃」）时，直接隐藏「丢弃」条目而不是置灰保留。")]
        public bool hideDiscardWhenLocked;

        #endregion

        // 条目列表复用同一个 List，避免每次右键都产生一次分配。
        private readonly List<UiwContextMenuItem> _entries = new List<UiwContextMenuItem>();

        #region 生命周期

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        #endregion

        #region 对外接口

        /// <summary>在光标处（屏幕坐标）弹出针对该道具的操作菜单。目标无效时不弹（并关掉已打开的菜单）。</summary>
        public void Show(ItemContextTarget target, Vector2 screenPos)
        {
            if (!target.IsValid) { Close(); return; }

            _entries.Clear();

            if (enableView)
                _entries.Add(new UiwContextMenuItem { Label = viewLabel, OnClick = () => View(target) });

            if (enableUse && IsUsable(target.ItemId))
                _entries.Add(new UiwContextMenuItem { Label = useLabel, OnClick = () => Use(target) });

            if (enableDiscard)
            {
                bool canDiscard = IsDiscardable(target.ItemId);
                if (canDiscard || !hideDiscardWhenLocked)
                    _entries.Add(new UiwContextMenuItem
                    {
                        Label        = discardLabel,
                        Interactable = canDiscard,
                        OnClick      = () => Discard(target)
                    });
            }

            // 上层贡献的额外条目（如装备界面打开时的「装备」），排在内置条目之后。
            UiwInventoryItemEvents.CollectItemMenuEntries(target, _entries);

            Open(_entries, screenPos);
        }

        /// <summary>关闭菜单。未打开时为无操作。</summary>
        public void Hide() => Close();

        #endregion

        #region 内置条目的行为

        /// <summary>道具是否「可使用」：配置了至少一条使用效果。</summary>
        private static bool IsUsable(string itemId)
        {
            var data = InventoryDataManager.Instance;
            if (data == null) return false;
            var item = data.GetItem(itemId);
            return item?.onUseEffectRefs != null && item.onUseEffectRefs.Count > 0;
        }

        /// <summary>
        /// 道具是否允许丢弃：未勾选 <see cref="Item.noDiscard"/>。
        /// 数据管理器缺席（未注册任何数据库）时按<b>可丢弃</b>处理，与
        /// <see cref="InventoryDataManager.IsItemDiscardable"/> 查不到道具时的口径一致——
        /// 拿不到配置就不该把普通道具锁死。
        /// </summary>
        private static bool IsDiscardable(string itemId)
        {
            var data = InventoryDataManager.Instance;
            return data == null || data.IsItemDiscardable(itemId);
        }

        /// <summary>查看：弹出可关闭的道具详情弹窗。</summary>
        private static void View(ItemContextTarget target)
        {
            var mgr = InventoryRuntimeManager.Instance;
            if (mgr == null) return;
            mgr.ShowItemDetailPopup(target.ItemId, target.Count);
        }

        /// <summary>
        /// 使用：按槽位使用 1 个。效果目标上下文取自宿主注入的
        /// <see cref="InventoryRuntimeManager.UseTargetContextProvider"/>——本包不认识任何领域系统，
        /// 未注入时对有使用效果的道具会得到 <see cref="EItemUseOutcome.NoContext"/>（不施加、不扣减）。
        /// </summary>
        private static void Use(ItemContextTarget target)
        {
            var mgr = InventoryRuntimeManager.Instance;
            if (mgr == null) return;
            mgr.UseItemInSlot(target.InventoryId, target.SlotId, mgr.ResolveUseTargetContext(target.InventoryId));
        }

        /// <summary>丢弃：弹出数量选择弹窗（真正的扣减在弹窗里确认后进行）。不可丢弃的道具走不到这里——条目已置灰/隐藏。</summary>
        private static void Discard(ItemContextTarget target)
        {
            var mgr = InventoryRuntimeManager.Instance;
            if (mgr == null) return;
            mgr.ShowItemDiscardPopup(target);
        }

        #endregion
    }
}

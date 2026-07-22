using UnityEngine;
using UnityEngine.UI;

namespace Ale.Inventory.Runtime.UI
{
    /// <summary>
    /// 装备拖拽全局上下文（静态）。记录当前拖拽载荷（被拖道具 ID + 来源），并维护一个跟随光标的幽灵图标。
    /// <list type="bullet">
    ///   <item>拖拽源（候选道具 / 已装备的装备槽）在开始拖拽时调用 <see cref="BeginFromInventory"/> /
    ///   <see cref="BeginFromSlot"/>，结束时调用 <see cref="End"/>。</item>
    ///   <item>放置目标（装备槽）据此判定有效性（绿/红）并在放下时执行装备 / 交换。</item>
    /// </list>
    /// 幽灵 <c>blocksRaycasts=false</c>，确保拖拽中目标槽能收到 PointerEnter / Drop 事件。
    /// </summary>
    public static class UiwEquipmentDragContext
    {
        /// <summary>当前是否正在拖拽装备载荷。</summary>
        public static bool IsDragging { get; private set; }
        /// <summary>被拖拽的道具 ID。</summary>
        public static string ItemId { get; private set; }
        /// <summary>来源仓库 ID（非空表示来自候选道具 / 仓库）。</summary>
        public static string SourceInventoryId { get; private set; }
        /// <summary>来源装备组 ID（与 <see cref="SourceSlotId"/> 同时非空表示来自某装备槽，用于槽↔槽交换）。</summary>
        public static string SourceGroupId { get; private set; }
        /// <summary>来源装备槽 ID。</summary>
        public static string SourceSlotId { get; private set; }

        private static GameObject _ghost;
        // 拖拽来源格子 / 槽：起拖时其图标置半透明，结束时（End）统一复位——
        // 由本上下文持有并复位，使清理不依赖来源自身的 OnEndDrag 触发（装备后来源候选格子可能被停用而收不到 OnEndDrag）。
        private static UiwInventoryItemSlotBase _source;

        /// <summary>从候选道具 / 仓库开始拖拽。</summary>
        public static void BeginFromInventory(string itemId, string sourceInventoryId, UiwInventoryItemSlotBase source, Sprite icon, Canvas canvas, Vector2 screenPos)
            => Begin(itemId, sourceInventoryId, null, null, source, icon, canvas, screenPos);

        /// <summary>从已装备的装备槽开始拖拽（用于槽↔槽交换）。</summary>
        public static void BeginFromSlot(string groupId, string slotId, string itemId, UiwInventoryItemSlotBase source, Sprite icon, Canvas canvas, Vector2 screenPos)
            => Begin(itemId, null, groupId, slotId, source, icon, canvas, screenPos);

        private static void Begin(string itemId, string srcInv, string srcGroup, string srcSlot,
            UiwInventoryItemSlotBase source, Sprite icon, Canvas canvas, Vector2 screenPos)
        {
            // 若上一次拖拽未正常收尾（残留幽灵 / 未复位来源），先清理，避免叠加。
            End();

            IsDragging        = true;
            ItemId            = itemId;
            SourceInventoryId = srcInv;
            SourceGroupId     = srcGroup;
            SourceSlotId      = srcSlot;
            _source           = source;
            if (_source) _source.SetIconAlpha(_source.dragIconAlpha);   // 起拖：来源图标半透明
            CreateGhost(icon, canvas, screenPos);
        }

        /// <summary>更新幽灵图标到当前光标位置。</summary>
        public static void UpdateGhost(Vector2 screenPos)
        {
            if (_ghost) _ghost.transform.position = screenPos;
        }

        /// <summary>结束拖拽：复位来源图标、销毁幽灵并清空载荷。可安全重复调用（幂等）。</summary>
        public static void End()
        {
            IsDragging        = false;
            ItemId            = null;
            SourceInventoryId = null;
            SourceGroupId     = null;
            SourceSlotId      = null;
            if (_source) _source.SetIconAlpha(1f);   // 复位来源图标透明度
            _source = null;
            if (_ghost) UnityEngine.Object.Destroy(_ghost);
            _ghost = null;
        }

        private static void CreateGhost(Sprite icon, Canvas canvas, Vector2 screenPos)
        {
            if (_ghost) UnityEngine.Object.Destroy(_ghost);
            _ghost = null;
            if (!canvas) return;

            _ghost = new GameObject("EquipDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _ghost.transform.SetParent(canvas.transform, false);

            var cg = _ghost.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha          = 0.7f;

            var img = _ghost.GetComponent<Image>();
            img.raycastTarget = false;
            img.sprite        = icon;
            img.enabled       = icon;

            var rect = (RectTransform)_ghost.transform;
            rect.sizeDelta = new Vector2(64f, 64f);

            _ghost.transform.SetAsLastSibling();
            _ghost.transform.position = screenPos;

            // 幽灵图标属于「覆盖式UI」，交由运行时管理器按需强制到指定 Layer（如 UI），与弹窗一致。
            InventoryRuntimeManager.Instance?.ApplyCoverUiLayer(_ghost);
        }
    }
}

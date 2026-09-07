using System;
using System.Collections.Generic;
using Ale.Effect;

namespace Ale.Inventory.Runtime
{
    /// <summary>道具「使用」的结果类别。</summary>
    public enum EItemUseOutcome
    {
        /// <summary>至少一个效果施加成功（含叠加 / 刷新），道具已扣减 1 个。</summary>
        Used = 0,

        /// <summary>所有效果均被阻断（免疫 / 条件 / 概率 / 定义缺失…），不扣减。</summary>
        Blocked = 1,

        /// <summary>道具未配置使用效果（<see cref="Item.onUseEffectRefs"/> 为空），不扣减。</summary>
        NoEffects = 2,

        /// <summary>仓库里没有该道具（或槽位为空）。</summary>
        NotOwned = 10,

        /// <summary>数据管理器中找不到该道具定义。</summary>
        UnknownItem = 11,

        /// <summary>未提供目标上下文（<c>IEffectContext</c>）。</summary>
        NoContext = 12,

        /// <summary>参数为空。</summary>
        InvalidArgument = 13,
    }

    /// <summary>
    /// 道具「使用」结果：类别 / 是否已扣减 / 逐效果施加结果（顺序同 <see cref="Item.onUseEffectRefs"/>；永不为 null）。
    /// </summary>
    public readonly struct ItemUseResult
    {
        private readonly IReadOnlyList<EffectApplyResult> _effectResults;

        public readonly EItemUseOutcome Outcome;

        /// <summary>是否已从仓库扣减 1 个。</summary>
        public readonly bool Consumed;

        /// <summary>逐效果施加结果（永不为 null）。</summary>
        public IReadOnlyList<EffectApplyResult> EffectResults => _effectResults ?? Array.Empty<EffectApplyResult>();

        /// <summary>成功施加（含叠加 / 刷新）的效果数。</summary>
        public int AppliedCount
        {
            get
            {
                int n = 0;
                foreach (var r in EffectResults) if (r.IsSuccess) n++;
                return n;
            }
        }

        /// <summary>是否真正「用掉」了（<see cref="EItemUseOutcome.Used"/>）。</summary>
        public bool IsUsed => Outcome == EItemUseOutcome.Used;

        public ItemUseResult(EItemUseOutcome outcome, bool consumed, IReadOnlyList<EffectApplyResult> effectResults)
        {
            Outcome        = outcome;
            Consumed       = consumed;
            _effectResults = effectResults;
        }

        public static ItemUseResult Failed(EItemUseOutcome outcome) => new ItemUseResult(outcome, false, null);

        public override string ToString() => $"{Outcome} (applied {AppliedCount}/{EffectResults.Count}, consumed={Consumed})";
    }

    /// <summary>道具「使用」事件负载：仓库 / 槽位（按 id 使用时为 null）/ 道具 / 目标主体（上下文 Subject）/ 结果。</summary>
    public readonly struct ItemUseEvent
    {
        public readonly string InventoryId;
        public readonly string SlotId;
        public readonly string ItemId;
        public readonly object TargetSubject;
        public readonly ItemUseResult Result;

        public ItemUseEvent(string inventoryId, string slotId, string itemId, object targetSubject, ItemUseResult result)
        {
            InventoryId   = inventoryId;
            SlotId        = slotId;
            ItemId        = itemId;
            TargetSubject = targetSubject;
            Result        = result;
        }
    }
}

using System;
using System.Collections.Generic;
using Ale.Effect;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// <see cref="InventoryRuntimeManager"/> 的道具「使用」分部（1.12.0）：把道具的 <see cref="Item.onUseEffectRefs"/> 经 toolkit
    /// <see cref="EffectApplier"/> 按序施加到调用方给定的目标上下文（<see cref="IEffectContext"/>，主体 = 效果目标），
    /// 至少一个效果施加成功才扣减 1 个道具。
    ///
    /// <para><b>本包不认识任何领域系统</b>：目标上下文由业务层构造（如角色系统的 <c>ChronicleEffectContext.Create(characterId)</c>），
    /// 效果定义先经上下文的 <see cref="IEffectDefinitionSource"/> 解析，找不到再查全局 <see cref="EffectDefinitionRegistry.Default"/>
    /// （1.13.0 起效果由 toolkit 效果库 <c>EffectDatabase</c> 承载，<c>EffectDataManager</c> 注册时登记为来源；本库不再持有效果定义，
    /// 道具的效果引用与其它系统定义的效果都按 id 解析）。</para>
    /// </summary>
    public partial class InventoryRuntimeManager
    {
        /// <summary>道具使用后派发（无论是否扣减；参数校验失败不派发）。</summary>
        public event Action<ItemUseEvent> OnItemUsed;

        /// <summary>
        /// 「使用」的目标上下文提供者：由<b>宿主注入</b>，按仓库 ID 返回该仓库所属主体的效果上下文
        /// （如角色系统的 <c>ChronicleEffectContext.Create(characterId)</c>）。
        ///
        /// <para>存在的理由：本包不认识任何领域系统，构造不出 <see cref="IEffectContext"/>；但 UI 层的
        /// 「右键 → 使用」需要一个不必逐次传参的取用点。未注入时 <see cref="ResolveUseTargetContext"/> 返回 null，
        /// 对有使用效果的道具即得到 <see cref="EItemUseOutcome.NoContext"/>（不施加、不扣减）。</para>
        ///
        /// <para>直接调用 <see cref="UseItem"/> / <see cref="UseItemInSlot"/> 的业务代码不受影响——
        /// 它们照旧显式传入上下文，本提供者只服务于「调用方拿不到上下文」的通用 UI 路径。</para>
        /// </summary>
        public Func<string, IEffectContext> UseTargetContextProvider { get; set; }

        /// <summary>按仓库 ID 解析「使用」的目标上下文；未注入提供者时返回 null。</summary>
        public IEffectContext ResolveUseTargetContext(string inventoryId)
            => UseTargetContextProvider?.Invoke(inventoryId);

        /// <summary>
        /// 按道具 id 使用：仓库须持有该道具；按序施加其使用效果，至少一个成功则从仓库扣减 1 个（<see cref="TryRemoveItemById"/>）。
        /// <paramref name="sourceTag"/> 空 → <c>item:{itemId}</c>（活动效果的来源标记，供按来源移除）。
        /// </summary>
        public ItemUseResult UseItem(string inventoryId, string itemId, IEffectContext targetContext, int level = 1, string sourceTag = null)
            => UseItemCore(inventoryId, null, itemId, targetContext, level, sourceTag);

        /// <summary>按槽位使用：从指定槽位扣减（拖拽 / 右键槽位「使用」的入口）。</summary>
        public ItemUseResult UseItemInSlot(string inventoryId, string slotId, IEffectContext targetContext, int level = 1, string sourceTag = null)
        {
            if (string.IsNullOrEmpty(inventoryId) || string.IsNullOrEmpty(slotId))
                return ItemUseResult.Failed(EItemUseOutcome.InvalidArgument);
            var slot = GetSlot(inventoryId, slotId);
            if (slot == null || string.IsNullOrEmpty(slot.itemId) || slot.count <= 0)
                return ItemUseResult.Failed(EItemUseOutcome.NotOwned);
            return UseItemCore(inventoryId, slotId, slot.itemId, targetContext, level, sourceTag);
        }

        private ItemUseResult UseItemCore(string inventoryId, string slotId, string itemId, IEffectContext ctx, int level, string sourceTag)
        {
            if (string.IsNullOrEmpty(inventoryId) || string.IsNullOrEmpty(itemId))
                return ItemUseResult.Failed(EItemUseOutcome.InvalidArgument);

            var item = InventoryDataManager.Instance.GetItem(itemId);
            if (item == null) return ItemUseResult.Failed(EItemUseOutcome.UnknownItem);
            if (slotId == null && !HasItem(inventoryId, itemId)) return ItemUseResult.Failed(EItemUseOutcome.NotOwned);

            var refs = item.onUseEffectRefs;
            ItemUseResult result;
            if (refs == null || refs.Count == 0)
            {
                result = new ItemUseResult(EItemUseOutcome.NoEffects, false, null);
            }
            else if (ctx == null)
            {
                return ItemUseResult.Failed(EItemUseOutcome.NoContext);
            }
            else
            {
                var results = new List<EffectApplyResult>(refs.Count);
                int applied = 0;
                foreach (var effectId in refs)
                {
                    if (string.IsNullOrEmpty(effectId)) continue;
                    var request = new EffectApplyRequest(effectId, level) { SourceTag = sourceTag ?? ("item:" + itemId) };
                    var r = EffectApplier.Apply(request, ctx);
                    results.Add(r);
                    if (r.IsSuccess) applied++;
                }

                bool consumed = false;
                if (applied > 0)
                    consumed = slotId != null ? TryRemoveItem(inventoryId, slotId, 1) : TryRemoveItemById(inventoryId, itemId, 1);
                result = new ItemUseResult(applied > 0 ? EItemUseOutcome.Used : EItemUseOutcome.Blocked, consumed, results);
            }

            OnItemUsed?.Invoke(new ItemUseEvent(inventoryId, slotId, itemId, ctx?.Subject, result));
            return result;
        }
    }
}

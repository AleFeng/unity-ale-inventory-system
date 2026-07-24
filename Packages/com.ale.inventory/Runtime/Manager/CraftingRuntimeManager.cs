using UnityEngine;
using Ale.Toolkit.Runtime;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 制作系统运行时管理器（非 MonoBehaviour 单例，首次访问自动创建）。
    ///
    /// <para>职责：</para>
    /// <list type="bullet">
    ///   <item>持有量统计：跨蓝图的 <see cref="CraftingBlueprint.craftInventoryRefs"/> 汇总</item>
    ///   <item>可制作次数：按各消耗道具持有量计算，并受蓝图「连续制作次数」上限约束</item>
    ///   <item>执行一次制作：跨制作仓库按优先级扣除材料、放入产出</item>
    /// </list>
    ///
    /// <para>单次「制作」动作的连续次数、计时与进度由 UI 层（视图）驱动并循环调用
    /// <see cref="CraftOnce(string)"/>；本管理器无运行时状态、不存档（与 <see cref="ShopRuntimeManager"/> 一致）。</para>
    ///
    /// <para>道具数据经 <see cref="InventoryDataManager"/> 查询；仓库读写一律经
    /// <see cref="InventoryRuntimeManager"/>（消耗/产出会自动触发其 OnInventoryChanged，UI 据此刷新）。</para>
    /// </summary>
    public class CraftingRuntimeManager : ToolkitSingleton<CraftingRuntimeManager>
    {
        protected override void Init()
        {
            // 蓝图目录来自已注册数据库；无需预初始化。
        }

        #region 查询：持有量 / 可制作次数

        /// <summary>统计某道具在该蓝图所有制作仓库中的持有总数量。</summary>
        public int GetOwnedAcross(CraftingBlueprint bp, string itemId)
        {
            var invMgr = InventoryRuntimeManager.Instance;
            if (invMgr == null || bp == null || string.IsNullOrEmpty(itemId)) return 0;
            long total = 0;
            foreach (var refId in bp.craftInventoryRefs)
                total += invMgr.GetTotalCount(refId, itemId);
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        /// <summary>
        /// 仅按材料计算当前可制作的最大次数：取各消耗道具 floor(持有量 / 单次消耗) 的最小值。
        /// 不受蓝图「连续制作次数」上限约束（用于「可制作次数」展示）。无消耗道具时返回 0。
        /// </summary>
        public int GetMaxCraftableByMaterials(CraftingBlueprint bp)
        {
            if (bp == null || InventoryRuntimeManager.Instance == null) return 0;
            if (bp.inputs == null || bp.inputs.Count == 0) return 0;

            int max         = int.MaxValue;
            bool anyValid   = false;
            foreach (var input in bp.inputs)
            {
                if (input == null || string.IsNullOrEmpty(input.itemId)) continue;
                anyValid  = true;
                int per   = Mathf.Max(1, input.count);
                int owned = GetOwnedAcross(bp, input.itemId);
                max = Mathf.Min(max, owned / per);
            }
            if (!anyValid) return 0;   // 全部消耗项无效 → 不可制作
            return Mathf.Max(0, max);
        }

        /// <summary>
        /// 计算单次「制作」动作允许选择的最大次数：在材料可制作次数（<see cref="GetMaxCraftableByMaterials"/>）
        /// 基础上，再按蓝图「连续制作次数」上限（<see cref="CraftingBlueprint.maxCraftCount"/> ≥ 0 时）取小。
        /// 用于限制制作次数选择器（连续制作批量），而非「可制作次数」展示。
        /// </summary>
        public int GetMaxCraftable(CraftingBlueprint bp)
        {
            int max = GetMaxCraftableByMaterials(bp);
            if (bp != null && bp.maxCraftCount >= 0) max = Mathf.Min(max, bp.maxCraftCount);
            return Mathf.Max(0, max);
        }

        /// <summary>当前材料是否足够制作至少一次（忽略「连续制作次数」上限，仅看材料）。</summary>
        public bool CanCraftOnce(CraftingBlueprint bp)
        {
            if (bp == null || InventoryRuntimeManager.Instance == null) return false;
            if (bp.inputs == null || bp.inputs.Count == 0) return false;

            bool anyValid = false;
            foreach (var input in bp.inputs)
            {
                if (input == null || string.IsNullOrEmpty(input.itemId)) continue;
                anyValid = true;
                if (GetOwnedAcross(bp, input.itemId) < Mathf.Max(1, input.count)) return false;
            }
            return anyValid;
        }

        #endregion

        #region 执行制作

        /// <summary>按蓝图 ID 执行一次制作。</summary>
        public bool CraftOnce(string blueprintId)
            => CraftOnce(InventoryDataManager.Instance?.GetCraftingBlueprint(blueprintId));

        /// <summary>
        /// 执行一次制作：先校验全部消耗充足，再跨制作仓库按优先级扣除材料、放入产出。
        /// 任一材料不足则不执行并返回 false。先扣材料再放产出（先腾出空间）。
        /// 产出超出所有制作仓库容量时，超出部分按容量丢弃（与商店交易一致）。
        /// </summary>
        public bool CraftOnce(CraftingBlueprint bp)
        {
            if (!CanCraftOnce(bp)) return false;

            // 1. 扣除材料（跨制作仓库按优先级分摊）
            foreach (var input in bp.inputs)
            {
                if (input == null || string.IsNullOrEmpty(input.itemId)) continue;
                RemoveAcross(bp, input.itemId, Mathf.Max(1, input.count));
            }

            // 2. 放入产出（跨制作仓库按优先级分摊）
            if (bp.outputs != null)
                foreach (var output in bp.outputs)
                {
                    if (output == null || string.IsNullOrEmpty(output.itemId)) continue;
                    AddAcross(bp, output.itemId, Mathf.Max(1, output.count));
                }

            return true;
        }

        #endregion

        #region 内部辅助

        /// <summary>跨制作仓库移除指定道具（按持有顺序分摊）。调用前应已确认总量充足。</summary>
        private static void RemoveAcross(CraftingBlueprint bp, string itemId, int amount)
        {
            var invMgr = InventoryRuntimeManager.Instance;
            if (invMgr == null) return;
            int remaining = amount;
            foreach (var refId in bp.craftInventoryRefs)
            {
                if (remaining <= 0) break;
                int owned = invMgr.GetTotalCount(refId, itemId);
                int take  = Mathf.Min(owned, remaining);
                if (take > 0)
                {
                    invMgr.TryRemoveItemById(refId, itemId, take);
                    remaining -= take;
                }
            }
        }

        /// <summary>跨制作仓库添加指定道具（按剩余容量分摊）；全部放不下时超出部分丢弃。</summary>
        private static void AddAcross(CraftingBlueprint bp, string itemId, int count)
        {
            var invMgr = InventoryRuntimeManager.Instance;
            if (invMgr == null) return;
            int remaining = count;
            foreach (var refId in bp.craftInventoryRefs)
            {
                if (remaining <= 0) break;
                int free = invMgr.GetFreeSpaceFor(refId, itemId);
                int add  = Mathf.Min(free, remaining);
                if (add > 0)
                {
                    invMgr.TryAddItem(refId, itemId, add);
                    remaining -= add;
                }
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Ale.Effect;
using Ale.GameplayTags;

#pragma warning disable 618   // LegacyEffects / LegacyGameplayTags 已标记 Obsolete：本文件正是其迁移通道

namespace Ale.Inventory.Runtime
{
    /// <summary>legacy 效果 / Gameplay 标签迁移报告。</summary>
    public sealed class InventoryLegacyMigrationReport
    {
        /// <summary>已迁入效果库的效果 id。</summary>
        public readonly List<string> MigratedEffectIds = new List<string>();

        /// <summary>因目标库已有同 id 而跳过（仍留在 legacy 字段）的效果 id。</summary>
        public readonly List<string> SkippedEffectIds = new List<string>();

        /// <summary>因 id 为空而忽略（从 legacy 字段移除）的条目数。</summary>
        public int InvalidEffects;

        /// <summary>并入目标库的 Gameplay 标签数。</summary>
        public int TagsAdded;

        /// <summary>目标库已有同名而未重复并入的标签数。</summary>
        public int TagsSkipped;

        /// <summary>迁移后来源库的 legacy 字段是否已空。</summary>
        public bool LegacyCleared;

        public int  MigratedCount => MigratedEffectIds.Count;
        public bool HasConflicts  => SkippedEffectIds.Count > 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("效果：迁入 ").Append(MigratedEffectIds.Count).Append(" 条");
            if (SkippedEffectIds.Count > 0)
                sb.Append("，跳过 ").Append(SkippedEffectIds.Count).Append(" 条（目标库已有同 id：").Append(string.Join(", ", SkippedEffectIds)).Append('）');
            if (InvalidEffects > 0)
                sb.Append("，忽略 ").Append(InvalidEffects).Append(" 条（空 id）");
            sb.Append("；Gameplay 标签：并入 ").Append(TagsAdded).Append(" 条，已存在 ").Append(TagsSkipped).Append(" 条");
            sb.Append(LegacyCleared ? "；来源库 legacy 字段已清空。" : "；来源库 legacy 字段仍保留冲突条目，处理后可重跑。");
            return sb.ToString();
        }
    }

    /// <summary>
    /// 1.12.0 库存库内的效果 / Gameplay 标签（legacy 字段）→ toolkit 效果库 <see cref="EffectDatabase"/> 的迁移（纯数据，编辑器菜单与测试共用）。
    /// 逐条把 <see cref="EffectDefinition"/> 转为 <see cref="EffectEntry"/>（定义深拷贝并归一；显示名取定义的 <c>displayName</c>；不挂模板）
    /// 并从 legacy 字段移除；目标库已有同 id 的效果跳过、报告并留在 legacy 字段（不覆盖）；标签按归一名去重并入。
    /// </summary>
    public static class InventoryLegacyEffects
    {
        /// <summary>把一条 legacy 效果定义转为效果库条目（不挂模板；定义深拷贝并归一；显示名取定义 displayName）。</summary>
        public static EffectEntry ToEntry(EffectDefinition legacy)
        {
            if (legacy == null) return null;
            var e = new EffectEntry(legacy.id) { definition = legacy.Clone() };
            if (!string.IsNullOrEmpty(legacy.displayName) && legacy.displayName != legacy.id)
                e.displayText.SetTextValue(0, legacy.displayName);
            e.Normalize();
            return e;
        }

        /// <summary>
        /// 把 <paramref name="source"/> 的 legacy 效果与标签迁入 <paramref name="target"/>。
        /// <paramref name="removeFromLegacy"/> 为真时：迁入 / 忽略的效果与全部标签从 legacy 字段移除，仅冲突条目保留（处理后可重跑）。
        /// </summary>
        public static InventoryLegacyMigrationReport MigrateInto(InventoryDatabase source, EffectDatabase target, bool removeFromLegacy = true)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            var report    = new InventoryLegacyMigrationReport();
            var remaining = new List<EffectDefinition>();

            foreach (var legacy in source.LegacyEffects)
            {
                if (legacy == null) continue;
                if (string.IsNullOrWhiteSpace(legacy.id)) { report.InvalidEffects++; continue; }
                if (target.GetEffect(legacy.id) != null)
                {
                    report.SkippedEffectIds.Add(legacy.id);
                    remaining.Add(legacy);
                    continue;
                }
                target.Effects.Add(ToEntry(legacy));
                report.MigratedEffectIds.Add(legacy.id);
            }

            foreach (var tag in source.LegacyGameplayTags)
            {
                if (tag == null || string.IsNullOrWhiteSpace(tag.name)) continue;
                if (HasTag(target, tag.name)) { report.TagsSkipped++; continue; }
                target.GameplayTags.Add(tag.Clone());
                report.TagsAdded++;
            }

            if (removeFromLegacy)
            {
                source.LegacyEffects.Clear();
                source.LegacyEffects.AddRange(remaining);
                source.LegacyGameplayTags.Clear();
            }
            report.LegacyCleared = !source.HasLegacyEffectData;
            return report;
        }

        private static bool HasTag(EffectDatabase db, string name)
        {
            string norm = GameplayTag.Normalize(name) ?? name;
            foreach (var t in db.GameplayTags)
            {
                if (t == null || string.IsNullOrEmpty(t.name)) continue;
                if ((GameplayTag.Normalize(t.name) ?? t.name) == norm) return true;
            }
            return false;
        }
    }
}

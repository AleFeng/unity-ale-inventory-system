using System.Collections.Generic;
using System.Linq;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.UI;
using NUnit.Framework;
using UnityEngine;

namespace Ale.Inventory.Tests
{
    /// <summary>多维过滤：主分组单选 × 副分组单选 × 副分组多选，各种组合的筛选结果。</summary>
    public class UiwGroupTagMultiFilterTests
    {
        private GameObject _go;
        private UiwGroupTagMultiFilter _filter;
        private const int Slot = 0, Tier = 1, Skill = 2;

        // 条目：名字 → (主分组, 副分组)
        private static readonly Dictionary<string, (string primary, string[] secondary)> Entries = new Dictionary<string, (string, string[])>
        {
            { "剑T1",       ("slot_weapon", new[] { "tier_t1" }) },
            { "剑T3·迅捷",  ("slot_weapon", new[] { "tier_t3", "skill_swift" }) },
            { "剑T3·坚韧",  ("slot_weapon", new[] { "tier_t3", "skill_tough" }) },
            { "头盔T3·迅捷", ("slot_head",   new[] { "tier_t3", "skill_swift" }) },
            { "头盔T1",     ("slot_head",   new[] { "tier_t1", "ver_modded" }) },
        };

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("filter");
            _filter = _go.AddComponent<UiwGroupTagMultiFilter>();
            _filter.dimensions.Add(new UiwGroupTagMultiFilter.Dimension { name = "部位", field = UiwGroupTagMultiFilter.TagField.Primary, tagPrefix = "slot_" });
            _filter.dimensions.Add(new UiwGroupTagMultiFilter.Dimension { name = "品质", tagPrefix = "tier_" });
            _filter.dimensions.Add(new UiwGroupTagMultiFilter.Dimension { name = "技能", tagPrefix = "skill_", multiSelect = true });
            var ids = new[] { "slot_weapon", "slot_head", "tier_t1", "tier_t3", "skill_swift", "skill_tough", "ver_modded" };
            _filter.SetTags(ids.Select(id => (Ale.Toolkit.Runtime.GroupTag)new CraftingGroupTag(id, id)));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private string[] Shown() => Entries.Where(e => _filter.Matches(e.Value.primary, e.Value.secondary)).Select(e => e.Key).ToArray();

        [Test]
        public void DimensionTags_FollowPrefix()
        {
            CollectionAssert.AreEqual(new[] { "slot_weapon", "slot_head" }, _filter.GetDimensionTags(Slot));
            CollectionAssert.AreEqual(new[] { "tier_t1", "tier_t3" }, _filter.GetDimensionTags(Tier));
            CollectionAssert.AreEqual(new[] { "skill_swift", "skill_tough" }, _filter.GetDimensionTags(Skill));
        }

        [Test]
        public void Combinations_FilterOrthogonally()
        {
            CollectionAssert.AreEquivalent(Entries.Keys, Shown(), "都不选 = 全部");

            _filter.Select(Slot, "slot_weapon");
            CollectionAssert.AreEquivalent(new[] { "剑T1", "剑T3·迅捷", "剑T3·坚韧" }, Shown(), "部位：武器");

            _filter.Select(Tier, "tier_t3");
            CollectionAssert.AreEquivalent(new[] { "剑T3·迅捷", "剑T3·坚韧" }, Shown(), "武器 × T3");

            _filter.Select(Skill, "skill_swift");
            CollectionAssert.AreEquivalent(new[] { "剑T3·迅捷" }, Shown(), "武器 × T3 × 迅捷");

            _filter.Select(Skill, "skill_tough");
            CollectionAssert.AreEquivalent(new[] { "剑T3·迅捷", "剑T3·坚韧" }, Shown(), "技能多选取并集");

            _filter.Select(Skill, "skill_swift");
            CollectionAssert.AreEquivalent(new[] { "剑T3·坚韧" }, Shown(), "再点一次移除");

            _filter.Clear(Skill);
            _filter.Clear(Slot);
            CollectionAssert.AreEquivalent(new[] { "剑T3·迅捷", "剑T3·坚韧", "头盔T3·迅捷" }, Shown(), "只留品质 T3");

            _filter.Select(Tier, "tier_t1");
            CollectionAssert.AreEquivalent(new[] { "剑T1", "头盔T1" }, Shown(), "单选换成 T1");
            CollectionAssert.AreEquivalent(new[] { "tier_t1" }, _filter.GetSelected(Tier));
        }

        [Test]
        public void SkillSelected_ExcludesEntriesWithoutSkill()
        {
            _filter.Select(Skill, "skill_swift");
            CollectionAssert.AreEquivalent(new[] { "剑T3·迅捷", "头盔T3·迅捷" }, Shown());
        }

        [Test]
        public void ForeignTag_Ignored_ChangeEventFiresOncePerChange()
        {
            int changes = 0;
            _filter.OnFilterChanged += () => changes++;
            _filter.Select(Tier, "skill_swift");   // 不属于品质维
            _filter.Select(Tier, "tier_t3");
            _filter.Select(Tier, "tier_t3");       // 单选再点同一个：不变
            _filter.Clear(Skill);                  // 本来就空：不变
            Assert.AreEqual(1, changes);
            CollectionAssert.AreEquivalent(new[] { "tier_t3" }, _filter.GetSelected(Tier));
        }

        [Test]
        public void TagIds_OverridePrefix_AndKeepOrder()
        {
            _filter.dimensions[Tier].tagIds = new List<string> { "tier_t3", "tier_t1" };
            _filter.SetTags(new[] { new CraftingGroupTag("tier_t1"), new CraftingGroupTag("tier_t3") });
            CollectionAssert.AreEqual(new[] { "tier_t3", "tier_t1" }, _filter.GetDimensionTags(Tier));
            Assert.AreEqual(0, _filter.GetSelected(Tier).Count, "SetTags 重置为不限");
        }
    }
}

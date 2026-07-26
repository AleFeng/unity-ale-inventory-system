using Ale.Toolkit.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 仓库系统数据文件（ScriptableObject）。存储：枚举类型、功能标签、道具模板、道具、仓库模板、仓库。
    /// 编辑器始终且仅在该资产上工作；JSON / 二进制 仅作为单向导出格式（见 Serialization 目录）。
    /// 通过菜单 Assets/Create/InventorySystem/Inventory Database 创建（由编辑器菜单项处理，支持模板）。
    /// </summary>
    public class InventoryDatabase : ScriptableObject, IEnumTypeSource
    {
        [SerializeField] private List<EnumType>          enumTypes          = new List<EnumType>();
        [SerializeField] private List<Tag>               functionTags       = new List<Tag>();
        [SerializeField] private List<ItemTemplate>      itemTemplates      = new List<ItemTemplate>();
        [SerializeField] private List<Item>              items              = new List<Item>();
        [SerializeField] private List<InventoryTemplate> inventoryTemplates    = new List<InventoryTemplate>();
        [SerializeField] private List<Inventory>         inventories           = new List<Inventory>();
        [SerializeField] private List<AttributeDefinition> sortOptionAttributes = new List<AttributeDefinition>();
        [SerializeField] private List<SortOption>          sortOptions          = new List<SortOption>();
        [SerializeField] private List<NumberFormatConfig>  numberFormatConfigs  = new List<NumberFormatConfig>();
        [SerializeField] private List<ShopTemplate>        shopTemplates        = new List<ShopTemplate>();
        [SerializeField] private List<Shop>                shops                = new List<Shop>();
        [SerializeField] private List<CraftingGroupTag>          craftingGroupTags          = new List<CraftingGroupTag>();
        [SerializeField] private List<CraftingBlueprintTemplate> craftingBlueprintTemplates = new List<CraftingBlueprintTemplate>();
        [SerializeField] private List<CraftingBlueprint>         craftingBlueprints         = new List<CraftingBlueprint>();
        [SerializeField] private List<EquipmentGroupTag>         equipmentGroupTags         = new List<EquipmentGroupTag>();
        [SerializeField] private List<EquipmentGroupTemplate>    equipmentGroupTemplates    = new List<EquipmentGroupTemplate>();
        [SerializeField] private List<EquipmentGroup>            equipmentGroups            = new List<EquipmentGroup>();
        [SerializeField] private List<SkillGroupTag>             skillGroupTags             = new List<SkillGroupTag>();
        [SerializeField] private List<SkillTemplate>             skillTemplates             = new List<SkillTemplate>();
        [SerializeField] private List<Skill>                     skills                     = new List<Skill>();

        #region 访问器
        /// <summary>
        /// 枚举类型 列表
        /// </summary>
        public List<EnumType>          EnumTypes          => enumTypes;

        // 显式实现 IEnumTypeSource：公开属性返回 List，此处以只读视图满足接口（供编辑器绘制器取用）。
        IReadOnlyList<EnumType> IEnumTypeSource.EnumTypes => enumTypes;

        /// <summary>
        /// 功能标签 列表
        /// </summary>
        public List<Tag>               FunctionTags       => functionTags;
        
        /// <summary>
        /// 道具模板 列表
        /// </summary>
        public List<ItemTemplate>      ItemTemplates      => itemTemplates;
        
        /// <summary>
        /// 道具 列表
        /// </summary>
        public List<Item>              Items              => items;
        
        /// <summary>
        /// 仓库模板 列表
        /// </summary>
        public List<InventoryTemplate> InventoryTemplates => inventoryTemplates;
        
        /// <summary>
        /// 仓库 列表
        /// </summary>
        public List<Inventory>         Inventories        => inventories;

        /// <summary>
        /// 整理选项 共用属性字段定义（schema）
        /// </summary>
        public List<AttributeDefinition> SortOptionAttributes => sortOptionAttributes;

        /// <summary>
        /// 整理选项 列表（由 <see cref="RebuildSortOptions"/> 自动生成，不可手动增删）
        /// </summary>
        public List<SortOption> SortOptions => sortOptions;

        /// <summary>
        /// 数字格式配置 列表（中心化命名配置，供仓库/模板按名引用）
        /// </summary>
        public List<NumberFormatConfig> NumberFormatConfigs => numberFormatConfigs;

        /// <summary>
        /// 商店模板 列表
        /// </summary>
        public List<ShopTemplate> ShopTemplates => shopTemplates;

        /// <summary>
        /// 商店 列表
        /// </summary>
        public List<Shop> Shops => shops;

        /// <summary>
        /// 制作-分组标签 列表
        /// </summary>
        public List<CraftingGroupTag> CraftingGroupTags => craftingGroupTags;

        /// <summary>
        /// 制作-蓝图模板 列表
        /// </summary>
        public List<CraftingBlueprintTemplate> CraftingBlueprintTemplates => craftingBlueprintTemplates;

        /// <summary>
        /// 制作-蓝图 列表
        /// </summary>
        public List<CraftingBlueprint> CraftingBlueprints => craftingBlueprints;

        /// <summary>
        /// 装备-分组标签 列表
        /// </summary>
        public List<EquipmentGroupTag> EquipmentGroupTags => equipmentGroupTags;

        /// <summary>
        /// 装备-装备组模板 列表
        /// </summary>
        public List<EquipmentGroupTemplate> EquipmentGroupTemplates => equipmentGroupTemplates;

        /// <summary>
        /// 装备-装备组 列表
        /// </summary>
        public List<EquipmentGroup> EquipmentGroups => equipmentGroups;

        /// <summary>
        /// 技能-分组标签 列表
        /// </summary>
        public List<SkillGroupTag> SkillGroupTags => skillGroupTags;

        /// <summary>
        /// 技能-技能模板 列表
        /// </summary>
        public List<SkillTemplate> SkillTemplates => skillTemplates;

        /// <summary>
        /// 技能 列表
        /// </summary>
        public List<Skill> Skills => skills;

        #endregion

        #region 按键查找

        /// <summary>
        /// 在列表中按键线性查找首个匹配项，未找到返回 null。
        /// <para>下方 19 个 <c>GetXxx</c> 曾各自重复这段循环。传入的键选择器均为<b>无捕获</b> lambda，
        /// 由编译器缓存为静态委托，不产生每次调用的分配。</para>
        /// <para><b>复杂度：</b>本资产是编辑期数据源，查找为 O(n)。运行时的高频查询请走
        /// <see cref="InventoryDataManager"/>——它对全部条目建有 O(1) 字典索引，且可跨多个数据库。</para>
        /// </summary>
        private static T Find<T>(List<T> list, string key, Func<T, string> keyOf) where T : class
        {
            if (string.IsNullOrEmpty(key) || list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e != null && keyOf(e) == key) return e;
            }
            return null;
        }

        // ── 按名称 ────────────────────────────────────────────────────────────────

        /// <summary>按名称查找枚举类型，未找到返回 null。</summary>
        public EnumType GetEnumType(string enumName) => Find(enumTypes, enumName, e => e.name);

        /// <summary>按名称查找功能标签，未找到返回 null。</summary>
        public Tag GetTag(string tagName) => Find(functionTags, tagName, t => t.name);

        /// <summary>按名称查找道具模板，未找到返回 null。</summary>
        public ItemTemplate GetTemplate(string templateName) => Find(itemTemplates, templateName, t => t.name);

        /// <summary>按名称查找仓库模板，未找到返回 null。</summary>
        public InventoryTemplate GetInventoryTemplate(string templateName)
            => Find(inventoryTemplates, templateName, t => t.name);

        /// <summary>按名称查找数字格式配置，未找到返回 null。</summary>
        public NumberFormatConfig GetNumberFormatConfig(string configName)
            => Find(numberFormatConfigs, configName, c => c.name);

        /// <summary>按名称查找商店模板，未找到返回 null。</summary>
        public ShopTemplate GetShopTemplate(string templateName) => Find(shopTemplates, templateName, t => t.name);

        /// <summary>按名称查找蓝图模板，未找到返回 null。</summary>
        public CraftingBlueprintTemplate GetCraftingBlueprintTemplate(string templateName)
            => Find(craftingBlueprintTemplates, templateName, t => t.name);

        /// <summary>按名称查找装备组模板，未找到返回 null。</summary>
        public EquipmentGroupTemplate GetEquipmentGroupTemplate(string templateName)
            => Find(equipmentGroupTemplates, templateName, t => t.name);

        /// <summary>按名称查找技能模板，未找到返回 null。</summary>
        public SkillTemplate GetSkillTemplate(string templateName) => Find(skillTemplates, templateName, t => t.name);

        // ── 按 ID ─────────────────────────────────────────────────────────────────

        /// <summary>按 ID 查找道具，未找到返回 null。</summary>
        public Item GetItem(string itemId) => Find(items, itemId, i => i.id);

        /// <summary>按 ID 查找仓库，未找到返回 null。</summary>
        public Inventory GetInventory(string inventoryId) => Find(inventories, inventoryId, inv => inv.id);

        /// <summary>按 ID 查找商店，未找到返回 null。</summary>
        public Shop GetShop(string shopId) => Find(shops, shopId, s => s.id);

        /// <summary>按 ID 查找制作分组标签，未找到返回 null。</summary>
        public CraftingGroupTag GetCraftingGroupTag(string tagId) => Find(craftingGroupTags, tagId, t => t.id);

        /// <summary>按 ID 查找蓝图，未找到返回 null。</summary>
        public CraftingBlueprint GetCraftingBlueprint(string blueprintId)
            => Find(craftingBlueprints, blueprintId, b => b.id);

        /// <summary>按 ID 查找装备分组标签，未找到返回 null。</summary>
        public EquipmentGroupTag GetEquipmentGroupTag(string tagId) => Find(equipmentGroupTags, tagId, t => t.id);

        /// <summary>按 ID 查找装备组，未找到返回 null。</summary>
        public EquipmentGroup GetEquipmentGroup(string groupId) => Find(equipmentGroups, groupId, g => g.id);

        /// <summary>按 ID 查找技能分组标签，未找到返回 null。</summary>
        public SkillGroupTag GetSkillGroupTag(string tagId) => Find(skillGroupTags, tagId, t => t.id);

        /// <summary>按 ID 查找技能，未找到返回 null。</summary>
        public Skill GetSkill(string skillId) => Find(skills, skillId, s => s.id);

        /// <summary>按 field 查找整理选项，未找到返回 null。</summary>
        public SortOption GetSortOption(string field) => Find(sortOptions, field, so => so.field);

        #endregion

        #region 整理选项同步

        /// <summary>
        /// 从所有 <see cref="InventoryTemplate"/> 的 <c>sortPriorities</c> 与 <c>sortTiebreakers</c>
        /// 自动同步 <see cref="sortOptions"/> 列表：
        /// <list type="bullet">
        ///   <item>新出现的 field → 追加 <see cref="SortOption"/>；</item>
        ///   <item>已消失的 field → 移除对应条目；</item>
        ///   <item>保留现有条目的属性值；对每个条目按 <see cref="sortOptionAttributes"/> schema 增补或移除属性值条目。</item>
        /// </list>
        /// </summary>
        public void RebuildSortOptions()
        {
            // 收集所有可用排序字段（领域逻辑：主键 + 道具模板属性 + 功能标签属性 + 功能页签序号）。
            // 与「整理列表」下拉选项保持一致。
            var orderedFields = new List<string>();
            var seen          = new HashSet<string>();

            void AddField(string id)
            {
                if (!string.IsNullOrEmpty(id) && seen.Add(id))
                    orderedFields.Add(id);
            }

            AddField(SortFieldKeys.Id);            // 道具 ID 始终第一个
            foreach (var tmpl in itemTemplates)    // 道具模板属性
                foreach (var def in tmpl.attributes)
                    AddField(def.id);
            foreach (var tag in functionTags)      // 功能标签属性
                foreach (var def in tag.attributes)
                    AddField(def.id);
            if (functionTags.Count > 0)            // 功能页签顺序（有功能标签时才加入）
                AddField(SortFieldKeys.TagOrder);

            // 通用同步（增删 / 重排 / 旧版内置字段迁移 / schema 属性值同步）下沉至 toolkit。
            SortOptionSync.Rebuild(sortOptions, orderedFields, sortOptionAttributes);
        }

        #endregion

        #region 商品 guid 补齐

        /// <summary>
        /// 为所有商店 / 商店模板的商品组与商品补发缺失的稳定 <c>guid</c>
        /// （见 <see cref="ShopCommodityGroup.guid"/> / <see cref="ShopCommodity.guid"/>，用作交易进度的存档键）。
        /// 1.4.0 及更早的数据没有该字段，由配置编辑器在打开 / 编辑数据库时调用补齐。
        /// <para>幂等——已有 guid 的条目不动；返回是否发生改动，供调用方决定是否 <c>SetDirty</c>。</para>
        /// </summary>
        public bool EnsureShopEntryGuids()
        {
            bool changed = false;
            foreach (var s in shops)         changed |= EnsureGroupGuids(s?.groups);
            foreach (var t in shopTemplates) changed |= EnsureGroupGuids(t?.groups);
            return changed;
        }

        private static bool EnsureGroupGuids(List<ShopCommodityGroup> groups)
        {
            if (groups == null) return false;
            bool changed = false;
            foreach (var g in groups)
            {
                if (g == null) continue;
                if (string.IsNullOrEmpty(g.guid)) { g.guid = NewShopEntryGuid(); changed = true; }
                if (g.commodities == null) continue;
                foreach (var c in g.commodities)
                {
                    if (c == null) continue;
                    if (string.IsNullOrEmpty(c.guid)) { c.guid = NewShopEntryGuid(); changed = true; }
                }
            }
            return changed;
        }

        /// <summary>生成一个商品组 / 商品的稳定 guid。编辑器新建条目时也用它，保证格式统一。</summary>
        public static string NewShopEntryGuid() => System.Guid.NewGuid().ToString("N");

        #endregion

        #region 校验

        /// <summary>
        /// 校验数据有效性。当存在重复道具 ID、重复仓库 ID 时返回 false，
        /// 并在 <paramref name="errors"/> 中给出说明。导出前会调用此方法以阻止导出非法数据。
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            var seen       = new HashSet<string>();
            var duplicates = new HashSet<string>();
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.id)) continue;
                if (!seen.Add(item.id)) duplicates.Add(item.id);
            }
            if (duplicates.Count > 0)
                errors.Add("存在重复的道具 ID：" + string.Join(", ", duplicates));

            var invSeen       = new HashSet<string>();
            var invDuplicates = new HashSet<string>();
            foreach (var inv in inventories)
            {
                if (string.IsNullOrWhiteSpace(inv.id)) continue;
                if (!invSeen.Add(inv.id)) invDuplicates.Add(inv.id);
            }
            if (invDuplicates.Count > 0)
                errors.Add("存在重复的仓库 ID：" + string.Join(", ", invDuplicates));

            var shopSeen       = new HashSet<string>();
            var shopDuplicates = new HashSet<string>();
            foreach (var shop in shops)
            {
                if (string.IsNullOrWhiteSpace(shop.id)) continue;
                if (!shopSeen.Add(shop.id)) shopDuplicates.Add(shop.id);
            }
            if (shopDuplicates.Count > 0)
                errors.Add("存在重复的商店 ID：" + string.Join(", ", shopDuplicates));

            var invalidShopItemRefs = new HashSet<string>();
            foreach (var shop in shops)
                foreach (var group in shop.groups)
                    foreach (var c in group.commodities)
                        if (!string.IsNullOrEmpty(c.itemId) && GetItem(c.itemId) == null)
                            invalidShopItemRefs.Add($"{shop.id}:{c.itemId}");
            if (invalidShopItemRefs.Count > 0)
                errors.Add("商店商品引用了不存在的道具：" + string.Join(", ", invalidShopItemRefs));

            var bpSeen       = new HashSet<string>();
            var bpDuplicates = new HashSet<string>();
            foreach (var bp in craftingBlueprints)
            {
                if (string.IsNullOrWhiteSpace(bp.id)) continue;
                if (!bpSeen.Add(bp.id)) bpDuplicates.Add(bp.id);
            }
            if (bpDuplicates.Count > 0)
                errors.Add("存在重复的蓝图 ID：" + string.Join(", ", bpDuplicates));

            var invalidBpItemRefs = new HashSet<string>();
            foreach (var bp in craftingBlueprints)
            {
                foreach (var o in bp.outputs)
                    if (!string.IsNullOrEmpty(o.itemId) && GetItem(o.itemId) == null)
                        invalidBpItemRefs.Add($"{bp.id}:产出:{o.itemId}");
                foreach (var i in bp.inputs)
                    if (!string.IsNullOrEmpty(i.itemId) && GetItem(i.itemId) == null)
                        invalidBpItemRefs.Add($"{bp.id}:消耗:{i.itemId}");
            }
            if (invalidBpItemRefs.Count > 0)
                errors.Add("蓝图引用了不存在的道具：" + string.Join(", ", invalidBpItemRefs));

            var egSeen       = new HashSet<string>();
            var egDuplicates = new HashSet<string>();
            foreach (var g in equipmentGroups)
            {
                if (string.IsNullOrWhiteSpace(g.id)) continue;
                if (!egSeen.Add(g.id)) egDuplicates.Add(g.id);
            }
            if (egDuplicates.Count > 0)
                errors.Add("存在重复的装备组 ID：" + string.Join(", ", egDuplicates));

            var skillSeen       = new HashSet<string>();
            var skillDuplicates = new HashSet<string>();
            foreach (var s in skills)
            {
                if (string.IsNullOrWhiteSpace(s.id)) continue;
                if (!skillSeen.Add(s.id)) skillDuplicates.Add(s.id);
            }
            if (skillDuplicates.Count > 0)
                errors.Add("存在重复的技能 ID：" + string.Join(", ", skillDuplicates));

            return errors.Count == 0;
        }

        #endregion

        #region 深拷贝（模板支持）

        /// <summary>
        /// 用另一个数据库的全部数据深拷贝覆盖自身。
        /// 供创建新数据文件时「使用模板」功能调用——将模板数据克隆到新建的空资产中。
        /// </summary>
        public void CloneFrom(InventoryDatabase source)
        {
            if (!source) return;
            enumTypes          = source.enumTypes.Select(e => e.Clone()).ToList();
            functionTags       = source.functionTags.Select(t => t.Clone()).ToList();
            itemTemplates      = source.itemTemplates.Select(t => t.Clone()).ToList();
            items              = source.items.Select(i => i.Clone()).ToList();
            inventoryTemplates   = source.inventoryTemplates.Select(t => t.Clone()).ToList();
            inventories          = source.inventories.Select(inv => inv.Clone()).ToList();
            sortOptionAttributes = source.sortOptionAttributes.Select(d => d.Clone()).ToList();
            sortOptions          = source.sortOptions.Select(s => s.Clone()).ToList();
            numberFormatConfigs  = source.numberFormatConfigs.Select(c => c.Clone()).ToList();
            shopTemplates        = source.shopTemplates.Select(t => t.Clone()).ToList();
            shops                = source.shops.Select(s => s.Clone()).ToList();
            craftingGroupTags          = source.craftingGroupTags.Select(t => t.Clone()).ToList();
            craftingBlueprintTemplates = source.craftingBlueprintTemplates.Select(t => t.Clone()).ToList();
            craftingBlueprints         = source.craftingBlueprints.Select(b => b.Clone()).ToList();
            equipmentGroupTags         = source.equipmentGroupTags.Select(t => t.Clone()).ToList();
            equipmentGroupTemplates    = source.equipmentGroupTemplates.Select(t => t.Clone()).ToList();
            equipmentGroups            = source.equipmentGroups.Select(g => g.Clone()).ToList();
            skillGroupTags             = source.skillGroupTags.Select(t => t.Clone()).ToList();
            skillTemplates             = source.skillTemplates.Select(t => t.Clone()).ToList();
            skills                     = source.skills.Select(s => s.Clone()).ToList();
        }

        #endregion

        #region 数据填充
        /// <summary>
        /// 添加 枚举类型
        /// </summary>
        /// <param name="enumName"></param>
        /// <param name="itemNames"></param>
        public void AddEnumType(string enumName, params string[] itemNames)
        {
            if (GetEnumType(enumName) != null) return;
            var enumType = new EnumType(enumName);
            enumType.SeedItems(itemNames);
            enumTypes.Add(enumType);
        }
        
        /// <summary>
        /// 添加 功能标签
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="description"></param>
        public void AddFunctionTag(string tagName, string description)
        {
            if (GetTag(tagName) != null) return;
            functionTags.Add(new Tag(tagName, description));
        }
        #endregion
    }
}

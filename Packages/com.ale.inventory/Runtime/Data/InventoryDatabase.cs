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
    public class InventoryDatabase : ScriptableObject
    {
        [SerializeField] private List<EnumType>          enumTypes          = new List<EnumType>();
        [SerializeField] private List<FunctionTag>       functionTags       = new List<FunctionTag>();
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

        // 本数据库关联的 Unity Localization String Table 集合的 SharedTableData GUID（1:1）。
        // 由本地化工具窗口写入/读取，用于稳定定位所属表集合（表可改名，GUID 不变）。core 仅存字符串、不依赖 Localization 包。
        [SerializeField] private string localizationTableCollectionGuid;

        #region 访问器
        /// <summary>
        /// 枚举类型 列表
        /// </summary>
        public List<EnumType>          EnumTypes          => enumTypes;
        
        /// <summary>
        /// 功能标签 列表
        /// </summary>
        public List<FunctionTag>       FunctionTags       => functionTags;
        
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

        /// <summary>
        /// 关联的 Localization String Table 集合的 SharedTableData GUID（1:1；空 = 未关联）。
        /// 仅由本地化工具窗口读写。
        /// </summary>
        public string LocalizationTableCollectionGuid
        {
            get => localizationTableCollectionGuid;
            set => localizationTableCollectionGuid = value;
        }

        /// <summary>按名称查找枚举类型，未找到返回 null。</summary>
        public EnumType GetEnumType(string enumName)
        {
            if (string.IsNullOrEmpty(enumName)) return null;
            foreach (var e in enumTypes)
                if (e.name == enumName) return e;
            return null;
        }

        /// <summary>按名称查找功能标签，未找到返回 null。</summary>
        public FunctionTag GetTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return null;
            foreach (var t in functionTags)
                if (t.name == tagName) return t;
            return null;
        }

        /// <summary>按名称查找道具模板，未找到返回 null。</summary>
        public ItemTemplate GetTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            foreach (var t in itemTemplates)
                if (t.name == templateName) return t;
            return null;
        }

        /// <summary>按 ID 查找道具，未找到返回 null。</summary>
        public Item GetItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            foreach (var item in items)
                if (item.id == itemId) return item;
            return null;
        }

        /// <summary>按名称查找仓库模板，未找到返回 null。</summary>
        public InventoryTemplate GetInventoryTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            foreach (var t in inventoryTemplates)
                if (t.name == templateName) return t;
            return null;
        }

        /// <summary>按 ID 查找仓库，未找到返回 null。</summary>
        public Inventory GetInventory(string inventoryId)
        {
            if (string.IsNullOrEmpty(inventoryId)) return null;
            foreach (var inv in inventories)
                if (inv.id == inventoryId) return inv;
            return null;
        }

        /// <summary>按 field 查找整理选项，未找到返回 null。</summary>
        public SortOption GetSortOption(string field)
        {
            if (string.IsNullOrEmpty(field)) return null;
            foreach (var so in sortOptions)
                if (so.field == field) return so;
            return null;
        }

        /// <summary>按名称查找数字格式配置，未找到返回 null。</summary>
        public NumberFormatConfig GetNumberFormatConfig(string configName)
        {
            if (string.IsNullOrEmpty(configName)) return null;
            foreach (var c in numberFormatConfigs)
                if (c.name == configName) return c;
            return null;
        }

        /// <summary>按名称查找商店模板，未找到返回 null。</summary>
        public ShopTemplate GetShopTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            foreach (var t in shopTemplates)
                if (t.name == templateName) return t;
            return null;
        }

        /// <summary>按 ID 查找商店，未找到返回 null。</summary>
        public Shop GetShop(string shopId)
        {
            if (string.IsNullOrEmpty(shopId)) return null;
            foreach (var s in shops)
                if (s.id == shopId) return s;
            return null;
        }

        /// <summary>按 ID 查找制作分组标签，未找到返回 null。</summary>
        public CraftingGroupTag GetCraftingGroupTag(string tagId)
        {
            if (string.IsNullOrEmpty(tagId)) return null;
            foreach (var t in craftingGroupTags)
                if (t.id == tagId) return t;
            return null;
        }

        /// <summary>按名称查找蓝图模板，未找到返回 null。</summary>
        public CraftingBlueprintTemplate GetCraftingBlueprintTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            foreach (var t in craftingBlueprintTemplates)
                if (t.name == templateName) return t;
            return null;
        }

        /// <summary>按 ID 查找蓝图，未找到返回 null。</summary>
        public CraftingBlueprint GetCraftingBlueprint(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId)) return null;
            foreach (var b in craftingBlueprints)
                if (b.id == blueprintId) return b;
            return null;
        }

        /// <summary>按 ID 查找装备分组标签，未找到返回 null。</summary>
        public EquipmentGroupTag GetEquipmentGroupTag(string tagId)
        {
            if (string.IsNullOrEmpty(tagId)) return null;
            foreach (var t in equipmentGroupTags)
                if (t.id == tagId) return t;
            return null;
        }

        /// <summary>按名称查找装备组模板，未找到返回 null。</summary>
        public EquipmentGroupTemplate GetEquipmentGroupTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            foreach (var t in equipmentGroupTemplates)
                if (t.name == templateName) return t;
            return null;
        }

        /// <summary>按 ID 查找装备组，未找到返回 null。</summary>
        public EquipmentGroup GetEquipmentGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return null;
            foreach (var g in equipmentGroups)
                if (g.id == groupId) return g;
            return null;
        }

        /// <summary>按 ID 查找技能分组标签，未找到返回 null。</summary>
        public SkillGroupTag GetSkillGroupTag(string tagId)
        {
            if (string.IsNullOrEmpty(tagId)) return null;
            foreach (var t in skillGroupTags)
                if (t.id == tagId) return t;
            return null;
        }

        /// <summary>按名称查找技能模板，未找到返回 null。</summary>
        public SkillTemplate GetSkillTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            foreach (var t in skillTemplates)
                if (t.name == templateName) return t;
            return null;
        }

        /// <summary>按 ID 查找技能，未找到返回 null。</summary>
        public Skill GetSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            foreach (var s in skills)
                if (s.id == skillId) return s;
            return null;
        }

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
            // 收集所有可用排序字段（与"整理列表"下拉选项一致）。
            // fieldOrder 一表三用：去重、下方的「是否仍是可用字段」判定、以及排序时的 O(1) 序号查询。
            var fieldOrder    = new Dictionary<string, int>();
            var orderedFields = new List<string>();

            void AddField(string id)
            {
                if (string.IsNullOrEmpty(id) || fieldOrder.ContainsKey(id)) return;
                fieldOrder[id] = orderedFields.Count;
                orderedFields.Add(id);
            }

            // 道具 ID 始终第一个
            AddField("__id__");

            // 道具模板属性
            foreach (var tmpl in itemTemplates)
                foreach (var def in tmpl.attributes)
                    AddField(def.id);

            // 功能标签属性
            foreach (var tag in functionTags)
                foreach (var def in tag.attributes)
                    AddField(def.id);

            // 功能页签顺序（有功能标签时才加入）
            if (functionTags.Count > 0)
                AddField("__tagOrder__");

            // 移除不再是可用排序字段的 SortOption（field 为 null 的脏数据一并清掉）
            sortOptions.RemoveAll(so => so.field == null || !fieldOrder.ContainsKey(so.field));

            // 追加新出现的 field，按首次出现顺序插入。
            // 先把现有 field 收进集合，避免逐个 GetSortOption 线性查找（原为 O(n²)）。
            var existingFields = new HashSet<string>();
            foreach (var so in sortOptions)
                existingFields.Add(so.field);
            foreach (var field in orderedFields)
                if (existingFields.Add(field))
                    sortOptions.Add(new SortOption(field));

            // 按 orderedFields 顺序排序（维持与模板中 sortPriorities 顺序一致）。
            // 用序号字典而非 List.IndexOf：后者在比较器内是 O(n)，会让整个排序退化为 O(n² log n)。
            sortOptions.Sort((a, b) =>
            {
                int ia = a.field != null && fieldOrder.TryGetValue(a.field, out int va) ? va : -1;
                int ib = b.field != null && fieldOrder.TryGetValue(b.field, out int vb) ? vb : -1;
                return ia.CompareTo(ib);
            });

            // 一次性迁移：把旧版存为通用属性值的「名称」「忽略ID」搬进内置字段（displayName / ignoreIds），
            // 随后从 schema 移除这两个定义，下方的属性同步会清掉各选项里对应的残留属性值。幂等（迁移完毕后为空操作）。
            MigrateBuiltinSortFields();

            // 对每个 SortOption 按 schema 同步 attributeValues（增补缺失、移除孤立）
            foreach (var so in sortOptions)
            {
                foreach (var def in sortOptionAttributes)
                {
                    AttributeEntry existing = null;
                    foreach (var e in so.attributeValues)
                        if (e.id == def.id) { existing = e; break; }

                    if (existing == null)
                    {
                        so.attributeValues.Add(new AttributeEntry(def.id, def.CreateValue()));
                    }
                    else if (existing.value == null
                          || existing.value.Type    != def.type
                          || existing.value.IsArray != def.isArray)
                    {
                        existing.value = def.CreateValue();
                    }
                }
                so.attributeValues.RemoveAll(e =>
                {
                    foreach (var def in sortOptionAttributes)
                        if (def.id == e.id) return false;
                    return true;
                });
                so.InvalidateEntryCache();
            }
        }

        /// <summary>
        /// 一次性迁移旧版整理选项数据：把存为通用属性值的「名称」「忽略ID」搬进内置字段
        /// （<see cref="SortOption.displayName"/> / <see cref="SortOption.ignoreIds"/>），
        /// 再从 <see cref="sortOptionAttributes"/> 移除这两个保留定义。仅当内置字段为空时迁移，避免覆盖已编辑值；
        /// 迁移完成后（schema 中已无这两项、各选项残留值被同步移除）本方法为空操作。
        /// </summary>
        private void MigrateBuiltinSortFields()
        {
            foreach (var so in sortOptions)
            {
                so.NormalizeDisplayName();

                // 名称 → displayName（内置纯文本 / 本地化引用均为空才迁移）
                var nameEntry = so.GetEntry(SortOption.LegacyNameAttrId);
                if (nameEntry?.value != null)
                {
                    var (t, k) = so.displayName.GetLocalizedStringRef();
                    bool targetEmpty = string.IsNullOrEmpty(so.displayName.GetTextValue())
                                       && string.IsNullOrEmpty(t) && string.IsNullOrEmpty(k);
                    if (targetEmpty)
                    {
                        var v = nameEntry.value;
                        if (v.Type == EFieldType.Text)
                        {
                            so.displayName.SetTextValue(0, v.GetTextValue());
                            var (vt, vk) = v.GetLocalizedStringRef();
                            so.displayName.SetLocalizedStringRef(0, vt, vk);
                        }
                        else
                        {
                            so.displayName.SetTextValue(0, v.AsString ?? string.Empty);
                        }
                    }
                }

                // 忽略ID → ignoreIds（内置列表为空才迁移；跳过旧版默认的空占位串，使默认条目数为 0）
                var ignoreEntry = so.GetEntry(SortOption.LegacyIgnoreAttrId);
                if (ignoreEntry?.value?.StringArray != null
                    && (so.ignoreIds == null || so.ignoreIds.Count == 0))
                {
                    if (so.ignoreIds == null) so.ignoreIds = new List<string>();
                    foreach (var s in ignoreEntry.value.StringArray)
                        if (!string.IsNullOrWhiteSpace(s))
                            so.ignoreIds.Add(s);
                }
            }

            // 移除保留定义，使其不再作为通用属性字段出现（下方同步会清掉各选项对应残留值）。
            sortOptionAttributes.RemoveAll(d => d != null
                && (d.id == SortOption.LegacyNameAttrId || d.id == SortOption.LegacyIgnoreAttrId));
        }

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
            functionTags.Add(new FunctionTag(tagName, description));
        }

        /// <summary>对外暴露的默认数据填充入口（供编辑器在新建资产后显式调用）。</summary>
        public void SeedDefaults()
        {
            
        }
        #endregion
    }
}

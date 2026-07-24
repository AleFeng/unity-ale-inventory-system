using System;
using System.Collections.Generic;
using Ale.Toolkit.Runtime;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// 仓库系统运行时数据访问单例。统一管理一个或多个 <see cref="InventoryDatabase"/>，
    /// 对外提供按 ID/名称 跨库查询道具、枚举、功能标签、模板的接口。
    /// 继承自插件内部单例基类 <see cref="ToolkitSingleton{T}"/>，不依赖 Fs 框架。
    ///
    /// <para><b>查询索引：</b>所有 <c>GetXxx(id/name)</c> 均走惰性构建的字典（O(1)），
    /// 而非逐库线性遍历——UI 每个格子绑定、排序比较器每次两两比较都会打这些接口，
    /// 线性查找会随道具总量放大成显著开销。索引在注册 / 注销 / 清空数据库后置脏，
    /// 下次查询时重建一次。构建按 <see cref="Databases"/> 顺序「先到先得」，
    /// 与旧的「第一个命中的数据库优先」语义完全一致。</para>
    /// </summary>
    public class InventoryDataManager : ToolkitSingleton<InventoryDataManager>
    {
        private readonly List<InventoryDatabase> _databases = new List<InventoryDatabase>();

        /// <summary>已注册的数据库列表。</summary>
        public IReadOnlyList<InventoryDatabase> Databases => _databases;

        protected override void Init()
        {
            // 无需额外初始化逻辑；数据库由外部显式注册或加载。
        }

        #region 注册 / 加载

        /// <summary>注册一个数据库（去重）。</summary>
        public void Register(InventoryDatabase database)
        {
            if (!database) return;
            if (_databases.Contains(database)) return;
            _databases.Add(database);
            InvalidateIndex();
        }

        /// <summary>注销一个数据库。</summary>
        public void Unregister(InventoryDatabase database)
        {
            if (!database) return;
            if (_databases.Remove(database))
                InvalidateIndex();
        }

        /// <summary>清空所有已注册数据库。</summary>
        public void ClearDatabases()
        {
            _databases.Clear();
            InvalidateIndex();
        }

        /// <summary>
        /// 从 JSON 文本反序列化为一个新的 <see cref="InventoryDatabase"/> 并注册。
        /// 运行时无 AssetDatabase，Sprite 等对象引用会保持为空（仅保留导出时的 GUID 于 DTO）。
        /// </summary>
        public InventoryDatabase LoadFromJson(string json)
        {
            var db = Serialization.InventoryJsonSerializer.Import(json, null);
            Register(db);
            return db;
        }

        /// <summary>从二进制数据反序列化为一个新的 <see cref="InventoryDatabase"/> 并注册。</summary>
        public InventoryDatabase LoadFromBinary(byte[] bytes)
        {
            var db = Serialization.InventoryBinarySerializer.Import(bytes, null);
            Register(db);
            return db;
        }

        #endregion

        #region 查询索引

        // 惰性构建标记：注册 / 注销 / 清空后置脏，下次查询时重建一次。
        private bool _indexDirty = true;

        private readonly Dictionary<string, Item>                     _items         = new Dictionary<string, Item>();
        private readonly Dictionary<string, EnumType>                 _enumTypes     = new Dictionary<string, EnumType>();
        private readonly Dictionary<string, FunctionTag>              _tags          = new Dictionary<string, FunctionTag>();
        private readonly Dictionary<string, ItemTemplate>             _itemTemplates = new Dictionary<string, ItemTemplate>();
        private readonly Dictionary<string, Inventory>                _inventories   = new Dictionary<string, Inventory>();
        private readonly Dictionary<string, Shop>                     _shops         = new Dictionary<string, Shop>();
        private readonly Dictionary<string, CraftingBlueprint>        _blueprints    = new Dictionary<string, CraftingBlueprint>();
        private readonly Dictionary<string, CraftingGroupTag>         _craftTags     = new Dictionary<string, CraftingGroupTag>();
        private readonly Dictionary<string, EquipmentGroup>           _equipGroups   = new Dictionary<string, EquipmentGroup>();
        private readonly Dictionary<string, EquipmentGroupTemplate>   _equipTmpls    = new Dictionary<string, EquipmentGroupTemplate>();
        private readonly Dictionary<string, EquipmentGroupTag>        _equipTags     = new Dictionary<string, EquipmentGroupTag>();
        private readonly Dictionary<string, Skill>                    _skills        = new Dictionary<string, Skill>();
        private readonly Dictionary<string, SkillGroupTag>            _skillTags     = new Dictionary<string, SkillGroupTag>();
        private readonly Dictionary<string, SkillTemplate>            _skillTmpls    = new Dictionary<string, SkillTemplate>();
        private readonly Dictionary<string, NumberFormatConfig>       _numberFormats = new Dictionary<string, NumberFormatConfig>();

        // 「条目 ID → 所属数据库」，供 FindDatabaseForXxx 使用。
        private readonly Dictionary<string, InventoryDatabase> _inventoryOwner  = new Dictionary<string, InventoryDatabase>();
        private readonly Dictionary<string, InventoryDatabase> _shopOwner       = new Dictionary<string, InventoryDatabase>();
        private readonly Dictionary<string, InventoryDatabase> _equipGroupOwner = new Dictionary<string, InventoryDatabase>();

        /// <summary>
        /// 使查询索引失效，下次查询时重建。注册 / 注销 / 清空数据库时自动调用；
        /// 若在运行期直接改动了某个已注册数据库的内容（通常只在编辑器播放模式下发生），需手动调用。
        /// </summary>
        public void InvalidateIndex() => _indexDirty = true;

        private void EnsureIndex()
        {
            if (!_indexDirty) return;
            _indexDirty = false;

            ClearIndex();
            foreach (var db in _databases)
            {
                if (!db) continue;
                Index(_items,         db, db.Items,                   x => x.id);
                Index(_enumTypes,     db, db.EnumTypes,               x => x.name);
                Index(_tags,          db, db.FunctionTags,            x => x.name);
                Index(_itemTemplates, db, db.ItemTemplates,           x => x.name);
                Index(_inventories,   db, db.Inventories,             x => x.id,   _inventoryOwner);
                Index(_shops,         db, db.Shops,                   x => x.id,   _shopOwner);
                Index(_blueprints,    db, db.CraftingBlueprints,      x => x.id);
                Index(_craftTags,     db, db.CraftingGroupTags,       x => x.id);
                Index(_equipGroups,   db, db.EquipmentGroups,         x => x.id,   _equipGroupOwner);
                Index(_equipTmpls,    db, db.EquipmentGroupTemplates, x => x.name);
                Index(_equipTags,     db, db.EquipmentGroupTags,      x => x.id);
                Index(_skills,        db, db.Skills,                  x => x.id);
                Index(_skillTags,     db, db.SkillGroupTags,          x => x.id);
                Index(_skillTmpls,    db, db.SkillTemplates,          x => x.name);
                Index(_numberFormats, db, db.NumberFormatConfigs,     x => x.name);
            }
        }

        private void ClearIndex()
        {
            _items.Clear();         _enumTypes.Clear();   _tags.Clear();        _itemTemplates.Clear();
            _inventories.Clear();   _shops.Clear();       _blueprints.Clear();  _craftTags.Clear();
            _equipGroups.Clear();   _equipTmpls.Clear();  _equipTags.Clear();   _skills.Clear();
            _skillTags.Clear();     _skillTmpls.Clear();  _numberFormats.Clear();
            _inventoryOwner.Clear(); _shopOwner.Clear();  _equipGroupOwner.Clear();
        }

        /// <summary>
        /// 把一个数据库的某个条目列表并入索引。已存在的键<b>不覆盖</b>——即「先注册的数据库优先」，
        /// 与旧的逐库线性查找语义一致。<paramref name="ownerMap"/> 非空时同时记录条目所属数据库。
        /// </summary>
        private static void Index<TValue>(Dictionary<string, TValue> map, InventoryDatabase db,
            List<TValue> source, Func<TValue, string> keySelector,
            Dictionary<string, InventoryDatabase> ownerMap = null)
        {
            if (source == null) return;
            foreach (var v in source)
            {
                if (v == null) continue;
                string key = keySelector(v);
                if (string.IsNullOrEmpty(key) || map.ContainsKey(key)) continue;
                map[key] = v;
                if (ownerMap != null) ownerMap[key] = db;
            }
        }

        private TValue Lookup<TValue>(Dictionary<string, TValue> map, string key)
        {
            if (string.IsNullOrEmpty(key)) return default;
            EnsureIndex();
            return map.TryGetValue(key, out var value) ? value : default;
        }

        #endregion

        #region 跨库查询

        /// <summary>按 ID 跨所有已注册数据库查找道具，未找到返回 null。</summary>
        public Item GetItem(string itemId) => Lookup(_items, itemId);

        /// <summary>按名称跨库查找枚举类型，未找到返回 null。</summary>
        public EnumType GetEnumType(string enumName) => Lookup(_enumTypes, enumName);

        /// <summary>按名称跨库查找功能标签，未找到返回 null。</summary>
        public FunctionTag GetTag(string tagName) => Lookup(_tags, tagName);

        /// <summary>按名称跨库查找道具模板，未找到返回 null。</summary>
        public ItemTemplate GetTemplate(string templateName) => Lookup(_itemTemplates, templateName);

        /// <summary>按 ID 跨所有已注册数据库查找仓库，未找到返回 null。</summary>
        public Inventory GetInventory(string inventoryId) => Lookup(_inventories, inventoryId);

        /// <summary>查找指定仓库 ID 所属的数据库，未找到返回 null。</summary>
        public InventoryDatabase FindDatabaseForInventory(string inventoryId)
            => Lookup(_inventoryOwner, inventoryId);

        /// <summary>按 ID 跨所有已注册数据库查找商店，未找到返回 null。</summary>
        public Shop GetShop(string shopId) => Lookup(_shops, shopId);

        /// <summary>查找指定商店 ID 所属的数据库，未找到返回 null。</summary>
        public InventoryDatabase FindDatabaseForShop(string shopId) => Lookup(_shopOwner, shopId);

        /// <summary>按 ID 跨所有已注册数据库查找制作蓝图，未找到返回 null。</summary>
        public CraftingBlueprint GetCraftingBlueprint(string blueprintId) => Lookup(_blueprints, blueprintId);

        /// <summary>按 ID 跨所有已注册数据库查找制作分组标签，未找到返回 null。</summary>
        public CraftingGroupTag GetCraftingGroupTag(string tagId) => Lookup(_craftTags, tagId);

        /// <summary>按 ID 跨所有已注册数据库查找装备组，未找到返回 null。</summary>
        public EquipmentGroup GetEquipmentGroup(string groupId) => Lookup(_equipGroups, groupId);

        /// <summary>查找指定装备组 ID 所属的数据库，未找到返回 null。</summary>
        public InventoryDatabase FindDatabaseForEquipmentGroup(string groupId)
            => Lookup(_equipGroupOwner, groupId);

        /// <summary>按名称跨所有已注册数据库查找装备组模板，未找到返回 null。</summary>
        public EquipmentGroupTemplate GetEquipmentGroupTemplate(string templateName)
            => Lookup(_equipTmpls, templateName);

        /// <summary>按 ID 跨所有已注册数据库查找装备分组标签，未找到返回 null。</summary>
        public EquipmentGroupTag GetEquipmentGroupTag(string tagId) => Lookup(_equipTags, tagId);

        /// <summary>按 ID 跨所有已注册数据库查找技能，未找到返回 null。</summary>
        public Skill GetSkill(string skillId) => Lookup(_skills, skillId);

        /// <summary>按 ID 跨所有已注册数据库查找技能分组标签，未找到返回 null。</summary>
        public SkillGroupTag GetSkillGroupTag(string tagId) => Lookup(_skillTags, tagId);

        /// <summary>按名称跨所有已注册数据库查找技能模板，未找到返回 null。</summary>
        public SkillTemplate GetSkillTemplate(string templateName) => Lookup(_skillTmpls, templateName);

        /// <summary>按名称跨所有已注册数据库查找数字格式配置，未找到返回 null。</summary>
        public NumberFormatConfig GetNumberFormatConfig(string configName)
            => Lookup(_numberFormats, configName);

        /// <summary>枚举所有已注册数据库中的全部技能（供运行时「数据库来源」使用）。</summary>
        public IEnumerable<Skill> GetAllSkills()
        {
            foreach (var db in _databases)
            {
                if (!db) continue;   // 未赋值 / 已销毁的数据库槽位（与 EnsureIndex 的守卫一致）
                foreach (var s in db.Skills)
                    yield return s;
            }
        }

        /// <summary>枚举所有已注册数据库中的全部技能分组标签（供技能 UI 生成分组页签）。</summary>
        public IEnumerable<SkillGroupTag> GetAllSkillGroupTags()
        {
            foreach (var db in _databases)
            {
                if (!db) continue;
                foreach (var t in db.SkillGroupTags)
                    yield return t;
            }
        }

        /// <summary>判断道具是否在仓库仓库中隐藏（读取 <see cref="Item.hideInInventory"/>）。道具不存在时返回 false。</summary>
        public bool IsItemHiddenInList(string itemId)
        {
            var item = GetItem(itemId);
            return item != null && item.hideInInventory;
        }

        /// <summary>
        /// 判断道具是否拥有指定功能标签。
        /// 有效来源：道具自身 <see cref="Item.tagRefs"/> 或其模板的 <see cref="ItemTemplate.tagRefs"/>。
        /// </summary>
        public bool ItemHasTag(string itemId, string tagName)
        {
            var item = GetItem(itemId);
            if (item == null) return false;
            if (item.tagRefs.Contains(tagName)) return true;
            var tmpl = GetTemplate(item.templateRef);
            return tmpl != null && tmpl.tagRefs.Contains(tagName);
        }

        #endregion
    }
}

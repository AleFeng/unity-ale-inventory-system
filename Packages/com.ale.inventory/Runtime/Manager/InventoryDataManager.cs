using System.Collections.Generic;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 仓库系统运行时数据访问单例。统一管理一个或多个 <see cref="InventoryDatabase"/>，
    /// 对外提供按 ID/名称 跨库查询道具、枚举、功能标签、模板的接口。
    /// 继承自插件内部单例基类 <see cref="InventorySystemSingleton{T}"/>，不依赖 Fs 框架。
    /// </summary>
    public class InventoryDataManager : InventorySystemSingleton<InventoryDataManager>
    {
        private readonly List<InventoryDatabase> _databases = new List<InventoryDatabase>();

        /// <summary>已注册的数据库列表。</summary>
        public IReadOnlyList<InventoryDatabase> Databases => _databases;

        protected override void Init()
        {
            // 第一期无需额外初始化逻辑；数据库由外部显式注册或加载。
        }

        #region 注册 / 加载

        /// <summary>注册一个数据库（去重）。</summary>
        public void Register(InventoryDatabase database)
        {
            if (!database) return;
            if (!_databases.Contains(database))
                _databases.Add(database);
        }

        /// <summary>注销一个数据库。</summary>
        public void Unregister(InventoryDatabase database)
        {
            if (!database) return;
            _databases.Remove(database);
        }

        /// <summary>清空所有已注册数据库。</summary>
        public void ClearDatabases()
        {
            _databases.Clear();
        }

        /// <summary>
        /// 从 JSON 文本反序列化为一个新的 <see cref="InventoryDatabase"/> 并注册。
        /// 运行时无 AssetDatabase，Sprite 等对象引用会保持为空（仅保留导出时的 GUID 于 DTO，第一期不做运行时资源解析）。
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

        #region 跨库查询

        /// <summary>按 ID 跨所有已注册数据库查找道具，未找到返回 null。</summary>
        public Item GetItem(string itemId)
        {
            foreach (var db in _databases)
            {
                var item = db.GetItem(itemId);
                if (item != null) return item;
            }
            return null;
        }

        /// <summary>按名称跨库查找枚举类型，未找到返回 null。</summary>
        public EnumType GetEnumType(string enumName)
        {
            foreach (var db in _databases)
            {
                var e = db.GetEnumType(enumName);
                if (e != null) return e;
            }
            return null;
        }

        /// <summary>按名称跨库查找功能标签，未找到返回 null。</summary>
        public FunctionTag GetTag(string tagName)
        {
            foreach (var db in _databases)
            {
                var t = db.GetTag(tagName);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>按名称跨库查找道具模板，未找到返回 null。</summary>
        public ItemTemplate GetTemplate(string templateName)
        {
            foreach (var db in _databases)
            {
                var t = db.GetTemplate(templateName);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>按 ID 跨所有已注册数据库查找仓库，未找到返回 null。</summary>
        public Inventory GetInventory(string inventoryId)
        {
            foreach (var db in _databases)
            {
                var inv = db.GetInventory(inventoryId);
                if (inv != null) return inv;
            }
            return null;
        }

        /// <summary>查找指定仓库 ID 所属的数据库，未找到返回 null。</summary>
        public InventoryDatabase FindDatabaseForInventory(string inventoryId)
        {
            foreach (var db in _databases)
                if (db.GetInventory(inventoryId) != null) return db;
            return null;
        }

        /// <summary>按 ID 跨所有已注册数据库查找商店，未找到返回 null。</summary>
        public Shop GetShop(string shopId)
        {
            foreach (var db in _databases)
            {
                var shop = db.GetShop(shopId);
                if (shop != null) return shop;
            }
            return null;
        }

        /// <summary>查找指定商店 ID 所属的数据库，未找到返回 null。</summary>
        public InventoryDatabase FindDatabaseForShop(string shopId)
        {
            foreach (var db in _databases)
                if (db.GetShop(shopId) != null) return db;
            return null;
        }

        /// <summary>按 ID 跨所有已注册数据库查找制作蓝图，未找到返回 null。</summary>
        public CraftingBlueprint GetCraftingBlueprint(string blueprintId)
        {
            foreach (var db in _databases)
            {
                var bp = db.GetCraftingBlueprint(blueprintId);
                if (bp != null) return bp;
            }
            return null;
        }

        /// <summary>按 ID 跨所有已注册数据库查找制作分组标签，未找到返回 null。</summary>
        public CraftingGroupTag GetCraftingGroupTag(string tagId)
        {
            foreach (var db in _databases)
            {
                var t = db.GetCraftingGroupTag(tagId);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>按 ID 跨所有已注册数据库查找装备组，未找到返回 null。</summary>
        public EquipmentGroup GetEquipmentGroup(string groupId)
        {
            foreach (var db in _databases)
            {
                var g = db.GetEquipmentGroup(groupId);
                if (g != null) return g;
            }
            return null;
        }

        /// <summary>查找指定装备组 ID 所属的数据库，未找到返回 null。</summary>
        public InventoryDatabase FindDatabaseForEquipmentGroup(string groupId)
        {
            foreach (var db in _databases)
                if (db.GetEquipmentGroup(groupId) != null) return db;
            return null;
        }

        /// <summary>按名称跨所有已注册数据库查找装备组模板，未找到返回 null。</summary>
        public EquipmentGroupTemplate GetEquipmentGroupTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            foreach (var db in _databases)
            {
                var t = db.GetEquipmentGroupTemplate(templateName);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>按 ID 跨所有已注册数据库查找装备分组标签，未找到返回 null。</summary>
        public EquipmentGroupTag GetEquipmentGroupTag(string tagId)
        {
            foreach (var db in _databases)
            {
                var t = db.GetEquipmentGroupTag(tagId);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>按 ID 跨所有已注册数据库查找技能，未找到返回 null。</summary>
        public Skill GetSkill(string skillId)
        {
            foreach (var db in _databases)
            {
                var s = db.GetSkill(skillId);
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>按 ID 跨所有已注册数据库查找技能分组标签，未找到返回 null。</summary>
        public SkillGroupTag GetSkillGroupTag(string tagId)
        {
            foreach (var db in _databases)
            {
                var t = db.GetSkillGroupTag(tagId);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>按名称跨所有已注册数据库查找技能模板，未找到返回 null。</summary>
        public SkillTemplate GetSkillTemplate(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            foreach (var db in _databases)
            {
                var t = db.GetSkillTemplate(templateName);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>枚举所有已注册数据库中的全部技能（供运行时「数据库来源」使用）。</summary>
        public IEnumerable<Skill> GetAllSkills()
        {
            foreach (var db in _databases)
                foreach (var s in db.Skills)
                    yield return s;
        }

        /// <summary>枚举所有已注册数据库中的全部技能分组标签（供技能 UI 生成分组页签）。</summary>
        public IEnumerable<SkillGroupTag> GetAllSkillGroupTags()
        {
            foreach (var db in _databases)
                foreach (var t in db.SkillGroupTags)
                    yield return t;
        }

        /// <summary>按名称跨所有已注册数据库查找数字格式配置，未找到返回 null。</summary>
        public NumberFormatConfig GetNumberFormatConfig(string configName)
        {
            if (string.IsNullOrEmpty(configName)) return null;
            foreach (var db in _databases)
            {
                var c = db.GetNumberFormatConfig(configName);
                if (c != null) return c;
            }
            return null;
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

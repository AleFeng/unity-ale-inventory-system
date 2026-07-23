using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.UI;

#if  IS_TMP
using TMPro;
#endif

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;

    /// <summary>仓库系统配置数据（枚举 / 功能标签 / 道具模板 / 道具 / 仓库 / 商店等）的生成。</summary>
    public static partial class InventoryDemoWizard
    {
        #region 仓库系统 配置数据
        /// <summary>
        /// 创建 仓库系统配置数据。
        /// </summary>
        /// <returns></returns>
        static void GetOrCreateDatabase()
        {
            string path = DatabasePath;
            var db = ScriptableObject.CreateInstance<InventoryDatabase>();

            // ── 枚举类型 ─────────────────────────────────────────────────────────
            db.AddEnumType("品质", "粗糙", "普通", "优秀", "稀有", "史诗", "传说", "神话");
            db.AddEnumType("部位", "头部", "颈部", "肩部", "胸部", "背部", "腰部", "腿部", "脚部", "手腕", "手部", "饰品", "主手", "副手");
            db.AddEnumType("装备类型", "布甲", "皮甲", "锁甲", "板甲", "其他");
            db.AddEnumType("武器主类型", "近战单手", "近战双手", "远程单手", "远程双手", "法术单手", "法术双手", "副手");
            db.AddEnumType("武器次类型", "拳套", "匕首", "剑", "斧", "锤", "长柄", "法杖", "弓", "弩", "枪", "枪械", "盾", "其他");
            
            // ── 功能标签 ─────────────────────────────────────────────────────────
            db.AddFunctionTag("信息",   "道具的基础信息。");
            db.AddFunctionTag("消耗品",  "可直接使用并消耗的道具，如药水、食物。");
            db.AddFunctionTag("材料",    "用于合成或制作的原材料，不可直接使用。");
            db.AddFunctionTag("装备",    "可穿戴的衣物，用于防护与属性提升。");
            db.AddFunctionTag("武器",    "可持有的武器，用于攻击与属性提升。");
            db.AddFunctionTag("任务物品", "与特定任务相关的关键道具，通常不可丢弃或出售。");
            db.AddFunctionTag("货币",    "用于交易的货币类道具。");
            // ── 功能标签"信息" 属性字段 ─────────────────────────────────────────
            var tagInfo = db.GetTag("信息");
            tagInfo.attributes.Add(new AttributeDefinition("名称", EFieldType.String));
            tagInfo.attributes.Add(new AttributeDefinition("描述", EFieldType.String));
            tagInfo.attributes.Add(new AttributeDefinition("品质", EFieldType.Enum, enumTypeRef: "品质"));
            tagInfo.attributes.Add(new AttributeDefinition("图标", EFieldType.Sprite));
            tagInfo.attributes.Add(new AttributeDefinition("货币ID:价格", EFieldType.StringIntPair, true)); // 价格：货币ID→价格（供商店 priceAttrSource 读取）
            // ── 功能标签"消耗品" 属性字段 ─────────────────────────────────────────
            var tagConsumable = db.GetTag("消耗品");
            tagConsumable.attributes.Add(new AttributeDefinition("效果ID", EFieldType.String));
            tagConsumable.attributes.Add(new AttributeDefinition("效果值", EFieldType.Int));
            // ── 功能标签"材料" 属性字段 ─────────────────────────────────────────
            var tagMaterial = db.GetTag("材料");
            tagMaterial.attributes.Add(new AttributeDefinition("替代材料ID-数量", EFieldType.VectorInt2, true));
            // ── 功能标签"装备" 属性字段 ───────────────────────────────────────────
            var tagEquip = db.GetTag("装备");
            tagEquip.attributes.Add(new AttributeDefinition("部位", EFieldType.Enum, enumTypeRef: "部位"));
            tagEquip.attributes.Add(new AttributeDefinition("装备类型", EFieldType.Enum, enumTypeRef: "装备类型"));
            tagEquip.attributes.Add(new AttributeDefinition("物品等级", EFieldType.Int));  // 装备系统：总加成示例字段
            tagEquip.attributes.Add(new AttributeDefinition("防御力",   EFieldType.Int));
            tagEquip.attributes.Add(new AttributeDefinition("生命值",   EFieldType.Int));
            // ── 功能标签"武器" 属性字段 ───────────────────────────────────────────
            var tagWeapon = db.GetTag("武器");
            tagWeapon.attributes.Add(new AttributeDefinition("部位", EFieldType.Enum, enumTypeRef: "部位"));
            tagWeapon.attributes.Add(new AttributeDefinition("武器主类型", EFieldType.Enum, enumTypeRef: "武器主类型"));
            tagWeapon.attributes.Add(new AttributeDefinition("武器次类型", EFieldType.Enum, enumTypeRef: "武器次类型"));
            tagWeapon.attributes.Add(new AttributeDefinition("物品等级", EFieldType.Int));  // 装备系统：总加成示例字段
            tagWeapon.attributes.Add(new AttributeDefinition("攻击力",   EFieldType.Int));
            // ── 功能标签"任务物品" 属性字段 ───────────────────────────────────────────
            var tagQuest = db.GetTag("任务物品");
            tagQuest.attributes.Add(new AttributeDefinition("任务ID", EFieldType.String));
            // ── 功能标签"货币" 属性字段 ───────────────────────────────────────────
            var tagCurrency = db.GetTag("货币");
            tagCurrency.attributes.Add(new AttributeDefinition("替代货币ID",  EFieldType.String, true));
            tagCurrency.attributes.Add(new AttributeDefinition("替代货币比例",  EFieldType.VectorInt2, true));
            
            // ── 道具模板 ────────────────────────────────────────────────────────
            var tmplConsumable = new ItemTemplate("消耗品")
            {
                color = new Color(0.35f, 0.85f, 0.45f)  // 绿色
            };
            tmplConsumable.tagRefs.Add("信息");
            tmplConsumable.tagRefs.Add("消耗品");
            db.ItemTemplates.Add(tmplConsumable);

            var tmplMaterial = new ItemTemplate("材料")
            {
                color = new Color(0.85f, 0.75f, 0.30f)  // 黄褐色
            };
            tmplMaterial.tagRefs.Add("信息");
            tmplMaterial.tagRefs.Add("材料");
            db.ItemTemplates.Add(tmplMaterial);

            var tmplEquip = new ItemTemplate("装备")
            {
                color = new Color(0.9f, 0.4f, 0.35f)    // 红色
            };
            tmplEquip.tagRefs.Add("信息");
            tmplEquip.tagRefs.Add("装备");
            db.ItemTemplates.Add(tmplEquip);

            var tmplWeapon = new ItemTemplate("武器")
            {
                color = new Color(0.95f, 0.55f, 0.15f)  // 橙色
            };
            tmplWeapon.tagRefs.Add("信息");
            tmplWeapon.tagRefs.Add("武器");
            db.ItemTemplates.Add(tmplWeapon);

            var tmplQuest = new ItemTemplate("任务物品")
            {
                color = new Color(0.70f, 0.35f, 0.90f)  // 紫色
            };
            tmplQuest.tagRefs.Add("信息");
            tmplQuest.tagRefs.Add("任务物品");
            db.ItemTemplates.Add(tmplQuest);

            var tmplCurrency = new ItemTemplate("货币")
            {
                color = new Color(1.00f, 0.85f, 0.10f)  // 金色
            };
            tmplCurrency.tagRefs.Add("信息");
            tmplCurrency.tagRefs.Add("货币");
            db.ItemTemplates.Add(tmplCurrency);
            
            // ── 测试道具 ─────────────────────────────────────────────────
            // 消耗品
            AddItem(db, "治疗药水", "消耗品", weight: 0.1f, stackLimit: 99, goldPrice: 50);
            AddItem(db, "法力药水", "消耗品", weight: 0.1f, stackLimit: 99, goldPrice: 60);
            AddItem(db, "体力药水", "消耗品", weight: 0.1f, stackLimit: 99, goldPrice: 40);
            AddItem(db, "复苏药水", "消耗品", weight: 0.3f, stackLimit: 10, goldPrice: 200);
            AddItem(db, "面包",    "消耗品", weight: 0.5f, stackLimit: 20, goldPrice: 10);
            // 材料
            AddItem(db, "药草",  "材料",   weight: 0.05f, stackLimit: 99);
            AddItem(db, "铁矿",     "材料",   weight: 1.0f,  stackLimit: 50);
            AddItem(db, "秘银矿",   "材料",   weight: 1.5f,  stackLimit: 50);
            AddItem(db, "法力水晶", "材料",   weight: 0.2f,  stackLimit: 99);
            AddItem(db, "旧皮革",   "材料",   weight: 0.8f,  stackLimit: 30);
            // 装备
            AddItem(db, "破布衣", "装备",   weight: 1.0f, stackLimit: 1, goldPrice: 30);
            AddItem(db, "旧皮甲", "装备",   weight: 2.0f, stackLimit: 1, goldPrice: 80);
            AddItem(db, "旧链甲", "装备",   weight: 4.0f, stackLimit: 1, goldPrice: 150);
            AddItem(db, "铁盔",   "装备",   weight: 1.5f, stackLimit: 1, goldPrice: 60);
            AddItem(db, "旧皮鞋", "装备",   weight: 0.8f, stackLimit: 1, goldPrice: 40);
            // 武器
            AddItem(db, "铁剑",    "武器",   weight: 2.5f, stackLimit: 1, goldPrice: 200);
            AddItem(db, "钢剑",    "武器",   weight: 3.0f, stackLimit: 1, goldPrice: 350);
            AddItem(db, "铁斧",    "武器",   weight: 3.5f, stackLimit: 1, goldPrice: 220);
            AddItem(db, "橡木法杖", "武器",   weight: 2.0f, stackLimit: 1, goldPrice: 280);
            AddItem(db, "木弓",    "武器",   weight: 1.5f, stackLimit: 1, goldPrice: 150);
            AddItem(db, "铁匕首",  "武器",   weight: 1.0f, stackLimit: 1, goldPrice: 120);
            // 任务物品
            AddItem(db, "破旧的钥匙", "任务物品", weight: 0.1f, stackLimit: 1);
            AddItem(db, "损坏的卷轴", "任务物品", weight: 0.2f, stackLimit: 1);
            AddItem(db, "奇怪的雕像", "任务物品", weight: 0.5f, stackLimit: 1);
            // 货币
            AddItem(db, "金币", "货币",   weight: 0f, stackLimit: 999);
            AddItem(db, "银币", "货币",   weight: 0f, stackLimit: 999);
            AddItem(db, "铜币", "货币",   weight: 0f, stackLimit: 999);

            // ── 仓库模板 ─────────────────────────────────────────────────────────
            var invTmpl = new InventoryTemplate("背包模板")
            {
                color    = new Color(0.5f, 0.7f, 1.0f),
                capacity = 20,
            };
            invTmpl.filterTagRefs.Add("消耗品");
            invTmpl.filterTagRefs.Add("材料");
            invTmpl.filterTagRefs.Add("装备");
            invTmpl.filterTagRefs.Add("武器");
            invTmpl.filterTagRefs.Add("任务物品");
            invTmpl.filterTagRefs.Add("货币");
            // 整理列表（sortPriorities）：UI 下拉菜单中显示的排序选项
            invTmpl.sortPriorities.Add(new SortPriority("功能标签")); // → __tagOrder__
            invTmpl.sortPriorities.Add(new SortPriority("品质"));
            invTmpl.sortPriorities.Add(new SortPriority("部位"));
            // 整理优先级（sortTiebreakers）：主条件相同时的次级比较顺序
            invTmpl.sortTiebreakers.Add(new SortPriority("功能标签"));
            invTmpl.sortTiebreakers.Add(new SortPriority("品质"));
            invTmpl.sortTiebreakers.Add(new SortPriority("部位"));
            invTmpl.sortTiebreakers.Add(new SortPriority("道具ID")); // → __id__
            db.InventoryTemplates.Add(invTmpl);

            // ── 仓库实例 ─────────────────────────────────────────────────────────
            var backpack = new Inventory("背包", "背包模板")
            {
                capacity = 30,
            };
            backpack.filterTagRefs.Add("消耗品");
            backpack.filterTagRefs.Add("材料");
            backpack.filterTagRefs.Add("装备");
            backpack.filterTagRefs.Add("武器");
            backpack.filterTagRefs.Add("任务物品");
            backpack.filterTagRefs.Add("货币");
            // 整理列表
            backpack.sortPriorities.Add(new SortPriority("功能标签"));
            backpack.sortPriorities.Add(new SortPriority("品质"));
            backpack.sortPriorities.Add(new SortPriority("部位"));
            // 整理优先级
            backpack.sortTiebreakers.Add(new SortPriority("功能标签"));
            backpack.sortTiebreakers.Add(new SortPriority("品质"));
            backpack.sortTiebreakers.Add(new SortPriority("部位"));
            backpack.sortTiebreakers.Add(new SortPriority("道具ID"));
            db.Inventories.Add(backpack);

            // ── 商店 ─────────────────────────────────────────────────────────────
            // 售卖店：用背包中的货币购买；价格取道具「货币ID:价格」属性 × priceMultiplier。
            var shop = new Shop("杂货商店")
            {
                shopType        = ShopType.Sell,
                priceAttrSource = "货币ID:价格",
            };
            shop.displayNameText.SetTextValue(0, "杂货商店");
            shop.tradeInventoryRefs.Add("背包");

            var groupDaily = new ShopCommodityGroup { name = "日常补给" };
            groupDaily.commodities.Add(new ShopCommodity { itemId = "治疗药水", count = 1, tradeLimit = -1 });
            groupDaily.commodities.Add(new ShopCommodity { itemId = "法力药水", count = 1, tradeLimit = -1 });
            groupDaily.commodities.Add(new ShopCommodity { itemId = "体力药水", count = 1, tradeLimit = -1 });
            groupDaily.commodities.Add(new ShopCommodity { itemId = "面包",    count = 5, tradeLimit = 10 });
            shop.groups.Add(groupDaily);

            var groupGear = new ShopCommodityGroup { name = "武器装备" };
            groupGear.commodities.Add(new ShopCommodity { itemId = "铁剑",   count = 1, tradeLimit = 3 });
            groupGear.commodities.Add(new ShopCommodity { itemId = "木弓",   count = 1, tradeLimit = 3 });
            groupGear.commodities.Add(new ShopCommodity { itemId = "旧皮甲", count = 1, tradeLimit = 2 });
            shop.groups.Add(groupGear);

            db.Shops.Add(shop);

            // ── 制作系统：分组标签（主分组 武器/防具/食品/药物 + 副分组 近战/远程/单手/双手）──
            db.CraftingGroupTags.Add(new CraftingGroupTag("武器", "武器") { color = new Color(0.95f, 0.55f, 0.15f) });
            db.CraftingGroupTags.Add(new CraftingGroupTag("防具", "防具") { color = new Color(0.90f, 0.40f, 0.35f) });
            db.CraftingGroupTags.Add(new CraftingGroupTag("食品", "食品") { color = new Color(0.85f, 0.75f, 0.30f) });
            db.CraftingGroupTags.Add(new CraftingGroupTag("药物", "药物") { color = new Color(0.35f, 0.85f, 0.45f) });
            db.CraftingGroupTags.Add(new CraftingGroupTag("近战", "近战") { color = Color.gray });
            db.CraftingGroupTags.Add(new CraftingGroupTag("远程", "远程") { color = Color.gray });
            db.CraftingGroupTags.Add(new CraftingGroupTag("单手", "单手") { color = Color.gray });
            db.CraftingGroupTags.Add(new CraftingGroupTag("双手", "双手") { color = Color.gray });

            // ── 制作系统：蓝图模板（整理设置为模板级；属性显示取主产出道具的「品质」）──
            var craftTmpl = new CraftingBlueprintTemplate("装备制作")
            {
                color         = new Color(0.5f, 0.7f, 1.0f),
                craftTime     = 2f,
                maxCraftCount = -1,
            };
            craftTmpl.craftInventoryRefs.Add("背包");
            craftTmpl.sortPriorities.Add(new SortPriority("品质"));
            craftTmpl.sortPriorities.Add(new SortPriority("道具ID"));
            craftTmpl.sortTiebreakers.Add(new SortPriority("道具ID"));
            craftTmpl.attributeDisplays.Add(new CraftingAttributeDisplay("品质", "品质"));
            db.CraftingBlueprintTemplates.Add(craftTmpl);

            // ── 制作系统：示例蓝图（产出装备 / 消耗材料；制作仓库 = 背包）──
            AddBlueprint(db, "bp_铁剑", "铁剑", "装备制作", "武器", new[] { "近战", "单手" },
                new[] { ("铁剑", 1) }, new[] { ("铁矿", 3) }, 2f);
            AddBlueprint(db, "bp_钢剑", "钢剑", "装备制作", "武器", new[] { "近战", "单手" },
                new[] { ("钢剑", 1) }, new[] { ("秘银矿", 2), ("铁矿", 2) }, 3f);
            AddBlueprint(db, "bp_铁斧", "铁斧", "装备制作", "武器", new[] { "近战", "双手" },
                new[] { ("铁斧", 1) }, new[] { ("铁矿", 4) }, 3f);
            AddBlueprint(db, "bp_木弓", "木弓", "装备制作", "武器", new[] { "远程", "双手" },
                new[] { ("木弓", 1) }, new[] { ("旧皮革", 2) }, 2.5f);
            AddBlueprint(db, "bp_旧皮甲", "旧皮甲", "装备制作", "防具", null,
                new[] { ("旧皮甲", 1), ("旧皮鞋", 1) }, new[] { ("旧皮革", 4) }, 2.5f);
            AddBlueprint(db, "bp_铁盔", "铁盔", "装备制作", "防具", null,
                new[] { ("铁盔", 1) }, new[] { ("铁矿", 2) }, 2f);
            AddBlueprint(db, "bp_治疗药水", "治疗药水", "装备制作", "药物", null,
                new[] { ("治疗药水", 1) }, new[] { ("药草", 2) }, 1.5f);
            AddBlueprint(db, "bp_面包", "面包", "装备制作", "食品", null,
                new[] { ("面包", 2) }, new[] { ("药草", 1) }, 1f);

            // ── 装备系统：分组标签（用于总属性加成的分组显示）──
            db.EquipmentGroupTags.Add(new EquipmentGroupTag("等级",   "等级")   { color = new Color(0.60f, 0.60f, 0.95f) });
            db.EquipmentGroupTags.Add(new EquipmentGroupTag("主属性", "主属性") { color = new Color(0.95f, 0.55f, 0.15f) });
            db.EquipmentGroupTags.Add(new EquipmentGroupTag("副属性", "副属性") { color = new Color(0.35f, 0.85f, 0.45f) });

            // ── 装备系统：装备组模板（承载全部可配置项：槽位列表 + 装备属性字段）──
            var equipTmpl = new EquipmentGroupTemplate("角色装备")
            {
                color = new Color(0.5f, 0.7f, 1.0f),
            };
            equipTmpl.equipmentInventoryRefs.Add("背包");   // 装备仓库：卸下装备时从此列表 Index0 起找第一个放得下的仓库
            // 槽位列表「武器」：限制 功能标签 = 武器；含 主手 / 副手 两个槽位
            var slWeapon = new EquipmentSlotList("weapon_list", "武器");
            slWeapon.requiredTags.Add("武器");
            slWeapon.slots.Add(new EquipmentSlot("slot_mainhand", "主手"));
            slWeapon.slots.Add(new EquipmentSlot("slot_offhand",  "副手"));
            equipTmpl.slotLists.Add(slWeapon);
            // 槽位列表「防具」：限制 功能标签 = 装备；含 头部 / 胸部 / 脚部 三个槽位
            var slArmor = new EquipmentSlotList("armor_list", "防具");
            slArmor.requiredTags.Add("装备");
            slArmor.slots.Add(new EquipmentSlot("slot_head",  "头部"));
            slArmor.slots.Add(new EquipmentSlot("slot_chest", "胸部"));
            slArmor.slots.Add(new EquipmentSlot("slot_feet",  "脚部"));
            equipTmpl.slotLists.Add(slArmor);
            // 装备属性字段列表（总属性加成，按分组标签分组显示）
            equipTmpl.attributeDisplays.Add(new EquipmentAttributeDisplay("物品等级", "等级"));
            equipTmpl.attributeDisplays.Add(new EquipmentAttributeDisplay("攻击力",   "主属性"));
            equipTmpl.attributeDisplays.Add(new EquipmentAttributeDisplay("防御力",   "主属性"));
            equipTmpl.attributeDisplays.Add(new EquipmentAttributeDisplay("生命值",   "副属性"));
            db.EquipmentGroupTemplates.Add(equipTmpl);

            // ── 装备系统：装备组（从模板深拷贝全部配置，仿编辑器「从模板添加」）──
            var equipGroup = new EquipmentGroup("角色装备", "角色装备");
            equipGroup.equipmentInventoryRefs = new List<string>(equipTmpl.equipmentInventoryRefs);
            foreach (var sl in equipTmpl.slotLists)         equipGroup.slotLists.Add(sl.Clone());
            foreach (var ad in equipTmpl.attributeDisplays) equipGroup.attributeDisplays.Add(ad.Clone());
            equipGroup.RebuildAttributes(db);
            db.EquipmentGroups.Add(equipGroup);

            SaveDatabaseAsset(db, path);
        }

        /// <summary>
        /// 把新建好的数据库写入资产路径。
        /// <para>已存在时<b>就地覆盖</b>（把内容 <see cref="EditorUtility.CopySerialized"/> 进原资产实例）而非删除重建：
        /// <see cref="AssetDatabase.CreateAsset"/> 会先删掉同名资产，连带换掉 GUID，从而静默打断
        /// <c>InventoryManager.prefab</c> 与各 UI 预制体对数据库的引用。理由同 <see cref="SavePrefab"/>。</para>
        /// </summary>
        static void SaveDatabaseAsset(InventoryDatabase db, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<InventoryDatabase>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(db, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(db);   // 临时实例，内容已拷进原资产
            }
            else
            {
                AssetDatabase.CreateAsset(db, path);
            }
        }

        /// <summary>添加 制作蓝图（产出/消耗为 (道具ID, 数量) 元组数组；制作仓库固定为「背包」）。</summary>
        static void AddBlueprint(InventoryDatabase db, string id, string displayName, string template,
            string primaryGroup, string[] secondaryGroups,
            (string id, int count)[] outputs, (string id, int count)[] inputs, float craftTime)
        {
            var bp = new CraftingBlueprint(id, template)
            {
                primaryGroupTag = primaryGroup,
                craftTime       = craftTime,
                maxCraftCount   = -1,
            };
            bp.displayText.SetTextValue(0, displayName);
            if (secondaryGroups != null) bp.secondaryGroupTags.AddRange(secondaryGroups);
            foreach (var o in outputs) bp.outputs.Add(new CraftingItemAmount(o.id, o.count));
            foreach (var i in inputs)  bp.inputs.Add(new CraftingItemAmount(i.id, i.count));
            // 制作仓库 / UI 配置（属性字段显示等）为模板级配置，由 RebuildAttributes 从「装备制作」模板镜像同步。
            bp.RebuildAttributes(db);
            db.CraftingBlueprints.Add(bp);
        }
        
        /// <summary>
        /// 创建 数字格式 配置数据
        /// </summary>
        /// <returns></returns>
        static NumberFormatConfig GetOrCreateNumberFormat()
        {
            var cfg = new NumberFormatConfig();
            var locale = new NumberFormatLocale { languageCode = "" };   // 默认回退语言

            // 后缀现为 Text（纯文本 fallback + 可选本地化引用），构造后写入纯文本值。
            var ruleM = new NumberFormatRule { threshold = 1_000_000, divisor = 1_000_000, decimalPlaces = 1 };
            ruleM.suffixText.SetTextValue(0, "M");
            var ruleK = new NumberFormatRule { threshold = 1_000, divisor = 1_000, decimalPlaces = 1 };
            ruleK.suffixText.SetTextValue(0, "K");
            locale.rules.Add(ruleM);
            locale.rules.Add(ruleK);

            cfg.locales.Add(locale);
            return cfg;
        }

        /// <summary>
        /// 添加 道具数据
        /// </summary>
        /// <param name="db"></param>
        /// <param name="id"></param>
        /// <param name="templateName"></param>
        /// <param name="weight"></param>
        /// <param name="stackLimit"></param>
        /// <param name="goldPrice"></param>
        static void AddItem
        (
            InventoryDatabase db, 
            string id, 
            string templateName,
            float weight = 0f,
            int stackLimit = 0,
            int goldPrice = 0
        )
        {
            var item = new Item(id, templateName) { weight = weight, stackLimit = stackLimit };
            item.RebuildAttributes(db);
            db.Items.Add(item);

            // 设置 属性字段的值
            item.SetAttributeValue("名称", id);
            item.SetAttributeValue("品质", Random.Range(0, 6));
            item.SetAttributeValue("部位", Random.Range(0, 12));
            item.SetAttributeValue("装备类型", Random.Range(0, 4));
            item.SetAttributeValue("武器主类型", Random.Range(0, 6));
            item.SetAttributeValue("武器次类型", Random.Range(0, 12));
            // 装备系统 总加成示例字段（仅在道具具备对应属性时生效，其余 SetAttributeValue 静默跳过）
            item.SetAttributeValue("物品等级", Random.Range(1, 60));
            item.SetAttributeValue("攻击力",   Random.Range(5, 50));
            item.SetAttributeValue("防御力",   Random.Range(3, 30));
            item.SetAttributeValue("生命值",   Random.Range(10, 100));

            // 价格（StringIntPair 数组首元素 金币→goldPrice），供商店 priceAttrSource 读取
            if (goldPrice > 0)
            {
                var priceAv = item.GetAttributeValue("货币ID:价格");
                if (priceAv != null)
                {
                    if (priceAv.Count == 0) priceAv.AddElement();
                    priceAv.SetStringIntPair(0, "金币", goldPrice);
                }
            }
        }
        #endregion
    }
}

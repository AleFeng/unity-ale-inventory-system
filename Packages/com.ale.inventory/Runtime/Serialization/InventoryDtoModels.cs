using Ale.Toolkit.Runtime;
using Ale.Toolkit.Runtime.Serialization;
using System;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// 库存领域的扁平 DTO 模型，用于导出 JSON / 二进制 与运行时加载。与运行时数据模型一一镜像，
    /// 区别在于 Unity 对象引用以 GUID 字符串承载（便于跨工程移植），而非 instanceID。
    /// 所有字段为 public 且类型受 JsonUtility 支持（基础类型 + 数组 + 嵌套 [Serializable]）。
    ///
    /// <para>属性系统 / 分组标签 / 模板基类 / 数字格式 / 整理排序等通用 DTO 定义在 toolkit 的
    /// <see cref="Ale.Toolkit.Runtime.Serialization.ToolkitDtoModels"/> 一族里，本文件只放<b>领域 DTO</b>
    /// （道具 / 仓库 / 商店 / 制作 / 装备 / 技能），并按需引用 / 派生通用 DTO。与运行时模型的双向映射见
    /// <see cref="InventoryDtoMapper"/>（按系统拆成多个分部文件）。</para>
    /// </summary>
    [Serializable]
    public class InventoryDatabaseDto
    {
        public int version = InventoryDtoMapper.Version;

        // ── 道具系统 ──────────────────────────────────────────────────────────────
        public EnumTypeDto[]    enumTypes;
        public FunctionTagDto[] functionTags;
        public ItemTemplateDto[] itemTemplates;
        public ItemDto[]        items;

        // ── 仓库系统（v6 新增）────────────────────────────────────────────────────
        public InventoryTemplateDto[]   inventoryTemplates;
        public InventoryDto[]           inventories;
        public AttributeDefinitionDto[] sortOptionAttributes;
        public SortOptionDto[]          sortOptions;
        public NumberFormatConfigDto[]  numberFormatConfigs;

        // ── 商店系统（v6 新增）────────────────────────────────────────────────────
        public ShopTemplateDto[] shopTemplates;
        public ShopDto[]         shops;

        // ── 制作系统（v6 新增）────────────────────────────────────────────────────
        public GroupTagDto[]                  craftingGroupTags;
        public CraftingBlueprintTemplateDto[] craftingBlueprintTemplates;
        public CraftingBlueprintDto[]         craftingBlueprints;

        // ── 装备系统（v6 新增）────────────────────────────────────────────────────
        public GroupTagDto[]               equipmentGroupTags;
        public EquipmentGroupTemplateDto[] equipmentGroupTemplates;
        public EquipmentGroupDto[]         equipmentGroups;

        // ── 技能系统（v6 新增）────────────────────────────────────────────────────
        public GroupTagDto[]     skillGroupTags;
        public SkillTemplateDto[] skillTemplates;
        public SkillDto[]        skills;

        /// <summary>关联的 Localization String Table 集合的 SharedTableData GUID（v6 新增；空 = 未关联）。</summary>
        public string localizationTableCollectionGuid;
    }

    #region 道具系统

    [Serializable]
    public class FunctionTagDto
    {
        public string name;
        /// <summary>
        /// 描述的纯文本 fallback。v5 及更早唯一的描述载体；v6 起完整描述见 <see cref="descriptionText"/>，
        /// 本字段仍随导出写出（供只认旧格式的消费方），导入时仅在 <see cref="descriptionText"/> 缺省时启用。
        /// </summary>
        public string description;
        public AttributeDefinitionDto[] attributes;

        // ── UI 显示配置（v6 新增；此前整体不入导出）────────────────────────────
        /// <summary>UI 显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayNameText;
        /// <summary>描述（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto descriptionText;
        /// <summary>标签背景 Sprite 的 GUID / Addressable 地址（约定同 <see cref="AttributeValueDto.objGuids"/>）。</summary>
        public string backgroundSpriteGuid;
        /// <summary>标签背景颜色，RGBA 四个 0-1 浮点。缺省按 <c>Color.white</c> 处理。</summary>
        public float[] backgroundColor;
        /// <summary>UI 中隐藏此标签。</summary>
        public bool hideInUI;
    }

    [Serializable]
    public class ItemTemplateDto : ConfigTemplateDto
    {
        /// <summary>模板默认携带的功能标签名称列表（v4 新增）。</summary>
        public string[] tagRefs;

        // ── v6 新增：此前静默丢弃的道具默认值 ──────────────────────────────────
        public float weight;
        public int   stackLimit;
        public bool  hideInInventory;
    }

    [Serializable]
    public class ItemDto
    {
        public string id;
        public string templateRef;
        public string[] tagRefs;
        public AttributeEntryDto[] values;

        // ── v6 新增：此前静默丢弃的道具本体字段 ────────────────────────────────
        public float weight;
        public int   stackLimit;
        public bool  hideInInventory;
    }

    #endregion

    #region 仓库系统

    [Serializable]
    public class InventoryTemplateDto : ConfigTemplateDto
    {
        public int   capacity;
        public float weightLimit;
        public string[] allowPutTagRefs;
        public string[] allowTakeTagRefs;
        public string[] allowOperateTagRefs;
        public string[] filterTagRefs;
        public bool showAllFilterTab;
        public bool autoSort;
        public bool dragSort;
        public string numberFormatRef;
        public SortPriorityDto[] sortPriorities;
        public SortPriorityDto[] sortTiebreakers;
    }

    [Serializable]
    public class InventoryDto
    {
        public string id;
        public string templateRef;
        /// <summary>显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayNameText;
        /// <summary>描述（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto descriptionText;
        public int   capacity;
        public float weightLimit;
        public string[] allowPutTagRefs;
        public string[] allowTakeTagRefs;
        public string[] allowOperateTagRefs;
        public string[] filterTagRefs;
        public bool showAllFilterTab;
        public bool autoSort;
        public bool dragSort;
        public string numberFormatRef;
        public SortPriorityDto[] sortPriorities;
        public SortPriorityDto[] sortTiebreakers;
        /// <summary>来自模板的自定义属性值。</summary>
        public AttributeEntryDto[] values;
    }

    #endregion

    #region 商店系统

    [Serializable]
    public class ShopRefreshScheduleDto
    {
        public int    refreshType;
        public int    timeType;
        public string timeZoneId;
        public int    hour;
        public int    minute;
        public int    dayOfWeek;
        public int    dayOfMonth;
    }

    [Serializable]
    public class ShopCommodityDto
    {
        public string guid;
        public string itemId;
        public int    count;
        public float  priceMultiplier;
        public int    tradeLimit;
        public bool   overrideRefresh;
        public ShopRefreshScheduleDto refresh;
    }

    [Serializable]
    public class ShopCommodityGroupDto
    {
        public string guid;
        public string name;
        public string description;
        public ShopRefreshScheduleDto refresh;
        public ShopCommodityDto[] commodities;
    }

    [Serializable]
    public class ShopTemplateDto : ConfigTemplateDto
    {
        public int shopType;
        public string[] tradeInventoryRefs;
        public string[] tradeTagRefs;
        public string[] filterTagRefs;
        public bool showAllFilterTab;
        public string numberFormatRef;
        public string priceAttrSource;
        public SortPriorityDto[] sortPriorities;
        public SortPriorityDto[] sortTiebreakers;
        public ShopCommodityGroupDto[] groups;
    }

    [Serializable]
    public class ShopDto
    {
        public string id;
        public string templateRef;
        /// <summary>显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayNameText;
        /// <summary>描述（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto descriptionText;
        public int shopType;
        public string[] tradeInventoryRefs;
        public string[] tradeTagRefs;
        public string[] filterTagRefs;
        public bool showAllFilterTab;
        public string numberFormatRef;
        public string priceAttrSource;
        public SortPriorityDto[] sortPriorities;
        public SortPriorityDto[] sortTiebreakers;
        public ShopCommodityGroupDto[] groups;
        /// <summary>来自模板的自定义属性值。</summary>
        public AttributeEntryDto[] values;
    }

    #endregion

    #region 制作系统

    [Serializable]
    public class CraftingItemAmountDto
    {
        public string itemId;
        public int    count;
    }

    [Serializable]
    public class CraftingAttributeDisplayDto
    {
        public string label;
        public string attrId;
    }

    [Serializable]
    public class CraftingBlueprintTemplateDto : ConfigTemplateDto
    {
        public float  craftTime;
        public int    maxCraftCount;
        public string[] craftInventoryRefs;
        public string numberFormatRef;
        public SortPriorityDto[] sortPriorities;
        public SortPriorityDto[] sortTiebreakers;
        public CraftingAttributeDisplayDto[] attributeDisplays;
    }

    [Serializable]
    public class CraftingBlueprintDto
    {
        public string id;
        public string templateRef;
        /// <summary>显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayText;
        /// <summary>描述（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto descriptionText;
        public string primaryGroupTag;
        public string[] secondaryGroupTags;
        public CraftingItemAmountDto[] outputs;
        public CraftingItemAmountDto[] inputs;
        public float  craftTime;
        public int    maxCraftCount;
        public string[] craftInventoryRefs;
        public string numberFormatRef;
        public CraftingAttributeDisplayDto[] attributeDisplays;
        /// <summary>来自模板的自定义属性值。</summary>
        public AttributeEntryDto[] values;
    }

    #endregion

    #region 装备系统

    [Serializable]
    public class EquipmentSlotFilterDto
    {
        public string attrId;
        public AttributeValueDto value;
    }

    [Serializable]
    public class EquipmentSlotDto
    {
        public string id;
        public string displayName;
        public EquipmentSlotFilterDto[] filters;
    }

    [Serializable]
    public class EquipmentEnumConstraintDto
    {
        public string enumTypeRef;
        public int[]  allowedValues;
    }

    [Serializable]
    public class EquipmentSlotListDto
    {
        public string id;
        public string displayName;
        public string description;
        public string[] requiredTags;
        public EquipmentEnumConstraintDto[] enumConstraints;
        public EquipmentSlotDto[] slots;
    }

    [Serializable]
    public class EquipmentAttributeDisplayDto
    {
        public string attrId;
        public string groupTag;
        /// <summary>显示名覆盖（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto label;
        public string enumLabelAttrId;
    }

    [Serializable]
    public class EquipmentGroupTemplateDto : ConfigTemplateDto
    {
        public string[] equipmentInventoryRefs;
        public EquipmentSlotListDto[] slotLists;
        public EquipmentAttributeDisplayDto[] attributeDisplays;
        public SortPriorityDto[] sortPriorities;
        public SortPriorityDto[] sortTiebreakers;
    }

    [Serializable]
    public class EquipmentGroupDto
    {
        public string id;
        public string templateRef;
        /// <summary>显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayNameText;
        /// <summary>描述（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto descriptionText;
        public string[] equipmentInventoryRefs;
        public EquipmentSlotListDto[] slotLists;
        public EquipmentAttributeDisplayDto[] attributeDisplays;
        public SortPriorityDto[] sortPriorities;
        public SortPriorityDto[] sortTiebreakers;
        /// <summary>来自模板的自定义属性值。</summary>
        public AttributeEntryDto[] values;
    }

    #endregion

    #region 技能系统

    [Serializable]
    public class SkillTemplateDto : ConfigTemplateDto
    {
        /// <summary>默认显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayText;
        /// <summary>默认描述（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto descriptionText;
        /// <summary>默认图标的 GUID / Addressable 地址（约定同 <see cref="AttributeValueDto.objGuids"/>）。</summary>
        public string iconGuid;
        public string primaryGroupTag;
        public string[] secondaryGroupTags;
    }

    [Serializable]
    public class SkillDto
    {
        public string id;
        public string templateRef;
        /// <summary>显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayText;
        /// <summary>描述（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto descriptionText;
        /// <summary>图标的 GUID / Addressable 地址（约定同 <see cref="AttributeValueDto.objGuids"/>）。</summary>
        public string iconGuid;
        public string primaryGroupTag;
        public string[] secondaryGroupTags;
        /// <summary>来自模板的自定义属性值。</summary>
        public AttributeEntryDto[] values;
    }

    #endregion
}

using System;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// 扁平 DTO 模型，用于导出 JSON / 二进制 与运行时加载。与运行时数据模型一一镜像，
    /// 区别在于 Unity 对象引用以 GUID 字符串承载（便于跨工程移植），而非 instanceID。
    /// 所有字段为 public 且类型受 JsonUtility 支持（基础类型 + 数组 + 嵌套 [Serializable]）。
    ///
    /// <para>本文件<b>只放 DTO 定义</b>；与运行时模型的双向映射见 <see cref="InventoryDtoMapper"/>
    /// （按系统拆成多个分部文件）。</para>
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

        /// <summary>关联的 Localization String Table 集合的 SharedTableData GUID（v6 新增；空 = 未关联）。</summary>
        public string localizationTableCollectionGuid;
    }

    #region 属性系统

    [Serializable]
    public class AttributeValueDto
    {
        public int      type;
        public bool     isArray;
        public string   enumTypeRef;
        public int[]    ints;
        public float[]  floats;
        public string[] strings;
        public string[] objGuids;
        /// <summary>
        /// AnimationCurve 序列化数据。每个元素对应一条曲线，格式：
        /// 关键帧以 '|' 分隔，每帧 7 个值以 ',' 分隔：time,value,inTangent,outTangent,inWeight,outWeight,weightedMode。
        /// </summary>
        public string[] curveData;
    }

    [Serializable]
    public class AttributeDefinitionDto
    {
        public string id;
        public int type;
        public bool isArray;
        public string enumTypeRef;
        public AttributeValueDto defaultValue;
    }

    [Serializable]
    public class AttributeEntryDto
    {
        public string id;
        public AttributeValueDto value;
    }

    /// <summary>
    /// 六大系统模板共有的三项（对应运行时的 <see cref="ConfigTemplateBase"/>）：名称、色点、属性字段定义。
    /// 各具体模板 DTO 由此派生——JsonUtility 与 Unity 序列化一样会把基类的 public 字段并入子类。
    /// </summary>
    [Serializable]
    public class ConfigTemplateDto
    {
        public string name;
        /// <summary>模板色点，RGBA 四个 0-1 浮点（v6 新增）。缺省（v5 及更早的数据）按 <c>Color.gray</c> 处理。</summary>
        public float[] color;
        public AttributeDefinitionDto[] attributes;
    }

    #endregion

    #region 道具系统

    [Serializable]
    public class EnumItemDto
    {
        public string name;
        public int value;
        /// <summary>枚举项携带的自定义属性值。</summary>
        public AttributeEntryDto[] attributeValues;
    }

    [Serializable]
    public class EnumTypeDto
    {
        public string name;
        public EnumItemDto[] items;
        public int nextValue;
        /// <summary>枚举类型的属性字段定义（所有枚举项共享 schema）。</summary>
        public AttributeDefinitionDto[] attributes;
    }

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
    public class SortPriorityDto
    {
        public string field;
        public bool ascending;
    }

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

    [Serializable]
    public class SortOptionDto
    {
        public string field;
        /// <summary>内置：排序下拉显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayName;
        /// <summary>内置：排序时忽略（跳过）的条目 ID 列表。</summary>
        public string[] ignoreIds;
        /// <summary>额外自定义属性值（schema 见 <see cref="InventoryDatabaseDto.sortOptionAttributes"/>）。</summary>
        public AttributeEntryDto[] attributeValues;
    }

    [Serializable]
    public class NumberFormatRuleDto
    {
        public long   threshold;
        public double divisor;
        /// <summary>后缀（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto suffixText;
        public int    decimalPlaces;
    }

    [Serializable]
    public class NumberFormatLocaleDto
    {
        public string languageCode;
        public NumberFormatRuleDto[] rules;
    }

    [Serializable]
    public class NumberFormatConfigDto
    {
        public string name;
        public NumberFormatLocaleDto[] locales;
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
}

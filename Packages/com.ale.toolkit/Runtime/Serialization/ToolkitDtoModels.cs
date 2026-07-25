using System;

namespace Ale.Toolkit.Runtime.Serialization
{
    /// <summary>
    /// Toolkit 通用序列化 DTO 模型：属性系统（属性值 / 定义 / 条目 / 枚举）、分组标签、模板公共基类、
    /// 数字格式、整理排序。与运行时数据模型一一镜像，Unity 对象引用以 GUID 字符串承载（便于跨工程移植）。
    /// 所有字段为 public 且类型受 JsonUtility 支持（基础类型 + 数组 + 嵌套 [Serializable]）。
    ///
    /// <para>宿主插件（如库存系统）的领域 DTO 定义在各自包内，并按需引用 / 派生本文件的通用 DTO
    /// （如各系统模板 DTO 派生自 <see cref="ConfigTemplateDto"/>）。双向映射见 <see cref="ToolkitDtoMapper"/>，
    /// 二进制读写见 <see cref="ToolkitBinaryCodec"/>。</para>
    /// </summary>
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
    /// 各系统模板共有的三项（对应运行时的 <see cref="ConfigTemplateBase"/>）：名称、色点、属性字段定义。
    /// 各具体模板 DTO 由此派生——JsonUtility 与 Unity 序列化一样会把基类的 public 字段并入子类。
    /// </summary>
    [Serializable]
    public class ConfigTemplateDto
    {
        public string name;
        /// <summary>模板色点，RGBA 四个 0-1 浮点。缺省（旧数据）按 <c>Color.gray</c> 处理。</summary>
        public float[] color;
        public AttributeDefinitionDto[] attributes;
    }

    /// <summary>
    /// 分组标签共有的四项（对应运行时的 <see cref="GroupTag"/>）：ID、显示名、描述、列表色点。
    /// 各系统的分组标签在数据上同形，故共用本 DTO（各自一个数组，互不混淆）。
    /// </summary>
    [Serializable]
    public class GroupTagDto
    {
        public string id;
        /// <summary>显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayName;
        /// <summary>描述（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto description;
        /// <summary>列表色点，RGBA 四个 0-1 浮点。缺省按 <c>Color.gray</c> 处理。</summary>
        public float[] color;
    }

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

    #endregion

    #region 数字格式

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

    #region 整理排序

    [Serializable]
    public class SortPriorityDto
    {
        public string field;
        public bool ascending;
    }

    [Serializable]
    public class SortOptionDto
    {
        public string field;
        /// <summary>内置：排序下拉显示名（Text：纯文本 fallback + 本地化引用）。</summary>
        public AttributeValueDto displayName;
        /// <summary>内置：排序时忽略（跳过）的条目 ID 列表。</summary>
        public string[] ignoreIds;
        /// <summary>额外自定义属性值（schema 由宿主定义，如库存的 <c>InventoryDatabaseDto.sortOptionAttributes</c>）。</summary>
        public AttributeEntryDto[] attributeValues;
    }

    #endregion
}

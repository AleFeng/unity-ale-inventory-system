using System;
using System.Text;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// <see cref="AttributeValue"/> 的文本 / 展示分部：<see cref="EFieldType.Text"/> 的纯文本 fallback 与
    /// 本地化引用读写、把任意类型格式化为显示字符串、以及供整理排序使用的可比较数值投影。
    /// </summary>
    public partial class AttributeValue
    {
        #region Text 访问器（纯文本 fallback + 本地化引用）

        /// <summary>
        /// 获取指定 <see cref="EFieldType.Text"/> 元素的纯文本值（fallback，始终存在，存于首槽 [element*3]）。
        /// <paramref name="element"/> 为逻辑索引（标量传 0）。
        /// </summary>
        public string GetTextValue(int element = 0)
        {
            int idx = element * 3;
            return idx < strings.Count ? strings[idx] : string.Empty;
        }

        /// <summary>设置指定 <see cref="EFieldType.Text"/> 元素的纯文本值（fallback）。</summary>
        public void SetTextValue(int element, string value)
        {
            int idx = element * 3;
            EnsureStringCapacity(idx + 3);
            strings[idx] = value ?? string.Empty;
        }

        /// <summary>
        /// 获取指定 <see cref="EFieldType.Text"/> 元素的本地化引用（tableRef + entryKey，存于后两槽 [element*3+1]/[element*3+2]）。
        /// 纯字符串读取，无需本地化包；启用 IS_LOCALIZATION 时由上层据此构建 LocalizedString 取文本。
        /// </summary>
        public (string tableRef, string entryKey) GetLocalizedStringRef(int element = 0)
        {
            int idx = element * 3;
            string tableRef = idx + 1 < strings.Count ? strings[idx + 1] : string.Empty;
            string entryKey = idx + 2 < strings.Count ? strings[idx + 2] : string.Empty;
            return (tableRef, entryKey);
        }

        /// <summary>设置指定 <see cref="EFieldType.Text"/> 元素的本地化引用（tableRef + entryKey）。</summary>
        public void SetLocalizedStringRef(int element, string tableRef, string entryKey)
        {
            int idx = element * 3;
            EnsureStringCapacity(idx + 3);
            strings[idx + 1] = tableRef ?? string.Empty;
            strings[idx + 2] = entryKey ?? string.Empty;
        }

        /// <summary>
        /// 解析本 <see cref="EFieldType.Text"/> 值的显示文本：启用 IS_LOCALIZATION 且本地化引用可解析出非空文本时
        /// 返回本地化文本，否则返回纯文本 fallback；均为空时返回空串。<paramref name="element"/> 为逻辑索引（标量传 0）。
        /// </summary>
        public string ResolveText(int element = 0)
        {
#if IS_LOCALIZATION
            var (tableRef, entryKey) = GetLocalizedStringRef(element);
            if (!string.IsNullOrEmpty(tableRef) || !string.IsNullOrEmpty(entryKey))
            {
                var ls = new UnityEngine.Localization.LocalizedString(tableRef, entryKey);
                string local = ls.GetLocalizedString();
                if (!string.IsNullOrEmpty(local)) return local;
            }
#endif
            return GetTextValue(element);
        }

        // Text 刻意以扁平字符串（纯文本 + tableRef + entryKey）承载，
        // 以兼容原生序列化 / Undo 与 JSON / 二进制导出管线（区别于各配置类的固定本地化字段，后者直接用 LocalizedString）。
        // 如需构建 LocalizedString 对象（IS_LOCALIZATION 启用时）：
        //   var (tableRef, entryKey) = attrValue.GetLocalizedStringRef(element);
        //   var ls = new UnityEngine.Localization.LocalizedString(tableRef, entryKey);

        #endregion

        #region 显示字符串

        /// <summary>
        /// 将本属性值格式化为可读的显示字符串，按 <see cref="Type"/> 拼接不同形式：
        /// <list type="bullet">
        ///   <item>单值（Int / Float / Bool / Enum / String）：直接转字符串（Enum 解析为枚举项名称）。</item>
        ///   <item>多分量（Vector2/3/4 / Color / VectorInt2/3/4）：列出全部分量，如 "(1, 2, 3)"。</item>
        ///   <item>StringIntPair：同时显示键与值，如 "铁矿: 3"。</item>
        ///   <item>EnumIntPair：显示枚举项名称与值，如 "力量: 10"。</item>
        ///   <item>对象引用（Sprite/Prefab/…）：显示资源名称；AnimationCurve 显示关键帧数。</item>
        /// </list>
        /// 数组形态逐元素格式化后用 <paramref name="separator"/> 连接。
        /// </summary>
        /// <param name="separator">数组元素之间的分隔符（默认 "、"）。</param>
        public string ToDisplayString(string separator = "、")
        {
            if (!isArray) return ElementToDisplayString(0);

            int n = Count;
            if (n == 0) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(separator);
                sb.Append(ElementToDisplayString(i));
            }
            return sb.ToString();
        }

        /// <summary>格式化单个逻辑元素（标量传 0）为显示字符串。</summary>
        private string ElementToDisplayString(int element)
        {
            switch (type)
            {
                case EFieldType.Int:    return GetInt(element).ToString();
                case EFieldType.Bool:   return GetInt(element) != 0 ? "是" : "否";
                case EFieldType.Float:  return FloatStr(GetFloat(element));
                case EFieldType.String: return GetString(element);
                case EFieldType.Enum:   return EnumValueName(GetInt(element));

                case EFieldType.Vector2: { var v = GetVector2(element); return $"({FloatStr(v.x)}, {FloatStr(v.y)})"; }
                case EFieldType.Vector3: { var v = GetVector3(element); return $"({FloatStr(v.x)}, {FloatStr(v.y)}, {FloatStr(v.z)})"; }
                case EFieldType.Vector4: { var v = GetVector4(element); return $"({FloatStr(v.x)}, {FloatStr(v.y)}, {FloatStr(v.z)}, {FloatStr(v.w)})"; }
                case EFieldType.Color:   { var c = GetColor(element); return $"RGBA({FloatStr(c.r)}, {FloatStr(c.g)}, {FloatStr(c.b)}, {FloatStr(c.a)})"; }

                case EFieldType.VectorInt2: { int b = element * 2; return $"({IntAt(b)}, {IntAt(b + 1)})"; }
                case EFieldType.VectorInt3: { int b = element * 3; return $"({IntAt(b)}, {IntAt(b + 1)}, {IntAt(b + 2)})"; }
                case EFieldType.VectorInt4: { int b = element * 4; return $"({IntAt(b)}, {IntAt(b + 1)}, {IntAt(b + 2)}, {IntAt(b + 3)})"; }

                case EFieldType.StringIntPair:
                {
                    var (key, val) = GetStringIntPair(element);
                    return $"{key}: {val}";
                }

                case EFieldType.EnumIntPair:
                {
                    var (enumValue, val) = GetEnumIntPair(element);
                    return $"{EnumValueName(enumValue)}: {val}";
                }

                case EFieldType.Text:
                {
                    // 优先展示纯文本；为空时退回本地化引用（entryKey / tableRef）
                    string plain = GetString(element * 3);
                    if (!string.IsNullOrEmpty(plain)) return plain;
                    string entryKey = GetString(element * 3 + 2);
                    if (!string.IsNullOrEmpty(entryKey)) return entryKey;
                    return GetString(element * 3 + 1);
                }

                case EFieldType.AnimationCurve:
                {
                    var c = GetAnimationCurve(element);
                    return c != null ? $"曲线({c.length} 关键帧)" : string.Empty;
                }

                default:
                    // 对象引用类（Sprite / Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial*）
                    if (type.IsObjectBacked())
                    {
                        var o = GetObject(element);
                        return o ? o.name : string.Empty;
                    }
                    return string.Empty;
            }
        }

        /// <summary>把枚举的整数值解析为枚举项名称；无法解析时回退为整数字符串。</summary>
        private string EnumValueName(int rawValue)
        {
            var dm = InventoryDataManager.Instance;
            var et = dm != null ? dm.GetEnumType(enumTypeRef) : null;
            var ei = et != null ? et.GetItemByValue(rawValue) : null;
            return ei != null ? ei.name : rawValue.ToString();
        }

        /// <summary>非破坏性读取整数后备列表（越界返回 0，不扩容，避免显示时改动数据）。</summary>
        private int IntAt(int index) => index >= 0 && index < ints.Count ? ints[index] : 0;

        /// <summary>浮点显示格式（最多两位小数，去除多余零）。</summary>
        private static string FloatStr(float v) => v.ToString("0.##");

        #endregion

        #region 排序比较数值

        /// <summary>
        /// 将本属性值转换为可用于排序比较的数值（double），按 <see cref="Type"/> 取值：
        /// <list type="bullet">
        ///   <item>Int / Bool / Enum / Float：取标量数值。</item>
        ///   <item>Vector2/3/4 / Color / VectorInt2/3/4：取向量模长（magnitude）。</item>
        ///   <item>StringIntPair / EnumIntPair：取其中的整数值。</item>
        ///   <item>其余（String / Text / 对象引用 / AnimationCurve）：无可比数值，返回 0。</item>
        /// </list>
        /// 数组形态以首个元素为准。
        /// </summary>
        public double ToComparableNumber() => ElementToComparableNumber(0);

        /// <summary>
        /// 将指定逻辑元素转换为可比较数值（double）。语义同 <see cref="ToComparableNumber"/>，
        /// 但可指定元素索引，供数组形态逐元素取数（如装备属性加成按元素拆分累加）。
        /// </summary>
        public double ElementToComparableNumber(int element)
        {
            switch (type)
            {
                case EFieldType.Int:
                case EFieldType.Bool:
                case EFieldType.Enum:    return GetInt(element);
                case EFieldType.Float:   return GetFloat(element);

                case EFieldType.Vector2: return GetVector2(element).magnitude;
                case EFieldType.Vector3: return GetVector3(element).magnitude;
                case EFieldType.Vector4: return GetVector4(element).magnitude;
                case EFieldType.Color:   { Vector4 c = GetColor(element); return c.magnitude; }

                case EFieldType.VectorInt2: return VectorIntMagnitude(2, element);
                case EFieldType.VectorInt3: return VectorIntMagnitude(3, element);
                case EFieldType.VectorInt4: return VectorIntMagnitude(4, element);

                case EFieldType.StringIntPair: return GetStringIntPair(element).value;
                case EFieldType.EnumIntPair:   return GetEnumIntPair(element).value;

                default: return 0.0;
            }
        }

        /// <summary>计算指定 VectorInt 元素的模长（非破坏性读取整数后备列表）。</summary>
        private double VectorIntMagnitude(int dim, int element)
        {
            int    baseIndex = element * dim;
            double sumSq     = 0.0;
            for (int i = 0; i < dim; i++)
            {
                double c = IntAt(baseIndex + i);
                sumSq += c * c;
            }
            return Math.Sqrt(sumSq);
        }

        #endregion
    }
}

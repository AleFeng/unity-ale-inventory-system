using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InventorySystem.Runtime
{
    /// <summary>
    /// 灵活属性值（标签联合 / 变体）。整个仓库系统属性系统的核心。
    ///
    /// 一个实例携带一种 <see cref="EFieldType"/> 与一个 <see cref="IsArray"/> 标志，并用一组固定的、
    /// 按类型分类的后备列表存储数据：标量存元素 [0]，数组存 [0..n]。Vector2/3/4 与 Color 以已知步长
    /// 压平进浮点后备列表，Bool/Enum 以整数后备列表存储。Unity 对象引用（Sprite）存于对象后备列表。
    ///
    /// 该扁平表示在 ScriptableObject 中原生序列化、原生支持 Undo/Redo，无需任何 PropertyDrawer 多态处理；
    /// 导出 JSON/二进制时由 DTO 层读取这些列表（Sprite 引用在导出端转为 GUID）。
    /// </summary>
    [Serializable]
    public class AttributeValue
    {
        [SerializeField] private EFieldType type;
        [SerializeField] private bool isArray;
        // 当 type == Enum 时，记录所属枚举类型名称（值存于 ints，存的是不可变的 EnumItem.value）。
        [SerializeField] private string enumTypeRef;

        // 仅当前类型对应的后备列表会被填充，其余保持为空。
        [SerializeField] private List<int>            ints    = new List<int>();
        [SerializeField] private List<float>          floats  = new List<float>();
        [SerializeField] private List<string>         strings = new List<string>();
        [SerializeField] private List<Object>         objRefs = new List<Object>();
        [SerializeField] private List<AnimationCurve> curves  = new List<AnimationCurve>();

        // 对象类字段的 Addressable 地址 / 授权 GUID（与 objRefs 平行，步长 1）。两种来源：
        //   ① 运行时从导出数据导入后于内存中填充；
        //   ② 启用 IS_ADDRESSABLE 时，编辑器以 AssetReference 授权，GUID 直接存入此列表，
        //      对应的 objRefs 槽留空（不硬引用资源，避免加载数据库即把资源一并拉进内存）。
        // 纯字符串存储，core 程序集对 Addressables 零依赖；取用门面优先用 objRefs 实时引用，无引用时回退此地址走异步加载。
        // IS_ADDRESSABLE 未启用（直接模式）时此列表保持为空。
        [SerializeField] private List<string> objAddresses = new List<string>();

        #region 构造

        public AttributeValue()
        {
        }

        public AttributeValue(EFieldType newType, bool asArray = false, string newEnumRef = null)
        {
            type        = newType;
            isArray     = asArray;
            enumTypeRef = newEnumRef;
        }

        #endregion

        #region 属性

        /// <summary>数据类型。</summary>
        public EFieldType Type => type;

        /// <summary>是否为数组形态。</summary>
        public bool IsArray => isArray;

        /// <summary>当类型为 <see cref="EFieldType.Enum"/> 或 <see cref="EFieldType.EnumIntPair"/> 时，所引用的枚举类型名称。</summary>
        public string EnumTypeRef
        {
            get => enumTypeRef;
            set => enumTypeRef = value;
        }

        /// <summary>当前逻辑元素数量（标量恒为 0 或 1，数组为实际长度）。</summary>
        public int Count
        {
            get
            {
                if (type.IsIntBacked())             return ints.Count;
                if (type.IsIntVectorBacked())        return ints.Count / type.IntStride();
                if (type == EFieldType.EnumIntPair)  return ints.Count / 2;   // ints 步长 2：枚举值 + 整数值
                if (type.IsFloatBacked())            return floats.Count / ElementStride();
                if (type == EFieldType.Text)             return strings.Count / 3;
                if (type == EFieldType.StringIntPair)    return strings.Count;  // strings 与 ints 平行同步
                if (type == EFieldType.String)           return strings.Count;
                if (type.IsObjectBacked())           return objRefs.Count;
                if (type.IsAnimationCurveBacked())   return curves.Count;
                return 0;
            }
        }

        #endregion

        #region 类型变更

        /// <summary>每个逻辑元素在浮点后备列表中占用的步长（Float = 1）。</summary>
        private int ElementStride()
        {
            if (type == EFieldType.Float) return 1;
            int s = type.FloatStride();
            return s > 0 ? s : 1;
        }

        /// <summary>
        /// 切换类型 / 数组形态，并清空所有后备列表（数据无法跨类型保留）。
        /// </summary>
        public void ChangeType(EFieldType newType, bool asArray, string newEnumRef = null)
        {
            type        = newType;
            isArray     = asArray;
            enumTypeRef = newEnumRef;
            ints.Clear();
            floats.Clear();
            strings.Clear();
            objRefs.Clear();
            curves.Clear();
        }

        /// <summary>设置是否为数组形态。从数组切回标量时仅保留首个元素。</summary>
        public void SetIsArray(bool asArray)
        {
            if (isArray == asArray) return;
            isArray = asArray;
            if (!asArray)
            {
                // VectorInt 类型：单个元素占 IntStride() 个整数槽，保留前 stride 个
                if (type.IsIntVectorBacked())
                {
                    int s = type.IntStride();
                    if (ints.Count > s) ints.RemoveRange(s, ints.Count - s);
                }
                else if (type == EFieldType.EnumIntPair)
                {
                    // EnumIntPair 每个元素占 2 个整数槽（枚举值 + 整数值），标量保留前 2 槽。
                    if (ints.Count > 2) ints.RemoveRange(2, ints.Count - 2);
                }
                else
                {
                    TrimToFirst(ints);
                }
                TrimToFirstFloats();
                // Text 每个元素 3 个槽，标量保留前 3 槽；String 保留前 1 槽。
                if (type == EFieldType.Text)
                {
                    if (strings.Count > 3) strings.RemoveRange(3, strings.Count - 3);
                }
                else
                {
                    TrimToFirst(strings);
                }
                TrimToFirst(objRefs);
                TrimToFirst(curves);
            }
        }

        private static void TrimToFirst<T>(List<T> list)
        {
            if (list.Count > 1) list.RemoveRange(1, list.Count - 1);
        }

        private void TrimToFirstFloats()
        {
            int stride = ElementStride();
            if (floats.Count > stride) floats.RemoveRange(stride, floats.Count - stride);
        }

        #endregion

        #region 标量访问器（操作元素 0）

        public int AsInt
        {
            get => ints.Count > 0 ? ints[0] : 0;
            set => SetSingle(ints, value);
        }

        public bool AsBool
        {
            get => ints.Count > 0 && ints[0] != 0;
            set => SetSingle(ints, value ? 1 : 0);
        }

        /// <summary>枚举的不可变整数值（对应 <see cref="EnumItem.Value"/>）。</summary>
        public int AsEnumValue
        {
            get => ints.Count > 0 ? ints[0] : 0;
            set => SetSingle(ints, value);
        }

        public float AsFloat
        {
            get => floats.Count > 0 ? floats[0] : 0f;
            set => SetSingle(floats, value);
        }

        public string AsString
        {
            get => strings.Count > 0 ? strings[0] : string.Empty;
            set => SetSingle(strings, value ?? string.Empty);
        }

        public Vector2 AsVector2
        {
            get => ReadVector(0, 2);
            set => WriteVector(0, new[] { value.x, value.y });
        }

        public Vector3 AsVector3
        {
            get { var v = ReadVector(0, 3); return new Vector3(v.x, v.y, v.z); }
            set => WriteVector(0, new[] { value.x, value.y, value.z });
        }

        public Vector4 AsVector4
        {
            get => ReadVector(0, 4);
            set => WriteVector(0, new[] { value.x, value.y, value.z, value.w });
        }

        public Color AsColor
        {
            get { var v = ReadVector(0, 4); return new Color(v.x, v.y, v.z, v.w); }
            set => WriteVector(0, new[] { value.r, value.g, value.b, value.a });
        }

        public Object AsObject
        {
            get => objRefs.Count > 0 ? objRefs[0] : null;
            set => SetSingle(objRefs, value);
        }

        public Sprite AsSprite
        {
            get => AsObject as Sprite;
            set => AsObject = value;
        }

        public AudioClip AsAudioClip
        {
            get => AsObject as AudioClip;
            set => AsObject = value;
        }

        public AnimationClip AsAnimationClip
        {
            get => AsObject as AnimationClip;
            set => AsObject = value;
        }

        public AnimationCurve AsAnimationCurve
        {
            get => curves.Count > 0 ? curves[0] : null;
            set
            {
                var c = value ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
                if (curves.Count == 0) curves.Add(c); else curves[0] = c;
            }
        }

        public GameObject AsPrefab
        {
            get => AsObject as GameObject;
            set => AsObject = value;
        }

        public Texture AsTexture
        {
            get => AsObject as Texture;
            set => AsObject = value;
        }

        public Material AsMaterial
        {
            get => AsObject as Material;
            set => AsObject = value;
        }

        private static void SetSingle<T>(List<T> list, T value)
        {
            if (list.Count == 0) list.Add(value);
            else list[0] = value;
        }

        #endregion

        #region 向量读写（按元素索引，步长由类型决定）

        private Vector4 ReadVector(int element, int stride)
        {
            int baseIndex = element * ElementStride();
            float x = baseIndex + 0 < floats.Count ? floats[baseIndex + 0] : 0f;
            float y = stride > 1 && baseIndex + 1 < floats.Count ? floats[baseIndex + 1] : 0f;
            float z = stride > 2 && baseIndex + 2 < floats.Count ? floats[baseIndex + 2] : 0f;
            float w = stride > 3 && baseIndex + 3 < floats.Count ? floats[baseIndex + 3] : 0f;
            return new Vector4(x, y, z, w);
        }

        private void WriteVector(int element, float[] components)
        {
            int stride = ElementStride();
            int baseIndex = element * stride;
            EnsureFloatCapacity(baseIndex + stride);
            for (int i = 0; i < components.Length && i < stride; i++)
                floats[baseIndex + i] = components[i];
        }

        private void EnsureFloatCapacity(int count)
        {
            while (floats.Count < count) floats.Add(0f);
        }

        #endregion
        
        #region 数组访问器

        public IReadOnlyList<int>    IntArray    => ints;
        public IReadOnlyList<float>  FloatArray  => floats;
        public IReadOnlyList<string> StringArray => strings;
        public IReadOnlyList<Object> ObjectArray => objRefs;

        /// <summary>在数组末尾追加一个空元素（按当前类型初始化为默认值）。</summary>
        public void AddElement()
        {
            if      (type.IsIntBacked())             ints.Add(0);
            else if (type.IsIntVectorBacked())        { int s = type.IntStride(); for (int i = 0; i < s; i++) ints.Add(0); }
            else if (type.IsFloatBacked())            { int s = ElementStride(); for (int i = 0; i < s; i++) floats.Add(0f); }
            else if (type == EFieldType.Text)            { strings.Add(string.Empty); strings.Add(string.Empty); strings.Add(string.Empty); }
            else if (type == EFieldType.StringIntPair)   { strings.Add(string.Empty); ints.Add(0); }
            else if (type == EFieldType.EnumIntPair)     { ints.Add(0); ints.Add(0); }
            else if (type == EFieldType.String)      strings.Add(string.Empty);
            else if (type.IsObjectBacked())           { objRefs.Add(null); objAddresses.Add(string.Empty); }
            else if (type.IsAnimationCurveBacked())   curves.Add(AnimationCurve.Linear(0f, 0f, 1f, 1f));
        }

        /// <summary>移除指定索引的数组元素。</summary>
        public void RemoveElement(int element)
        {
            if (element < 0 || element >= Count) return;
            if      (type.IsIntBacked())             ints.RemoveAt(element);
            else if (type.IsIntVectorBacked())        { int s = type.IntStride(); ints.RemoveRange(element * s, s); }
            else if (type.IsFloatBacked())            { int s = ElementStride(); floats.RemoveRange(element * s, s); }
            else if (type == EFieldType.Text)            strings.RemoveRange(element * 3, 3);
            else if (type == EFieldType.StringIntPair)   { strings.RemoveAt(element); ints.RemoveAt(element); }
            else if (type == EFieldType.EnumIntPair)     ints.RemoveRange(element * 2, 2);
            else if (type == EFieldType.String)      strings.RemoveAt(element);
            else if (type.IsObjectBacked())           { objRefs.RemoveAt(element); if (element < objAddresses.Count) objAddresses.RemoveAt(element); }
            else if (type.IsAnimationCurveBacked())   curves.RemoveAt(element);
        }

        /// <summary>
        /// 按给定「新顺序」（元素为原逻辑索引的一个排列）重排数组元素，同步作用于所有相关后备列表
        /// （按各自步长整段搬移，保持平行列表同步）。供 Inspector 拖拽重排使用。
        /// </summary>
        public void ReorderElements(IReadOnlyList<int> newOrder)
        {
            if (newOrder == null || newOrder.Count == 0) return;

            int intStride = 0;
            if      (type.IsIntBacked())               intStride = 1;
            else if (type.IsIntVectorBacked())          intStride = type.IntStride();
            else if (type == EFieldType.EnumIntPair)    intStride = 2;
            else if (type == EFieldType.StringIntPair)  intStride = 1;   // ints 与 strings 平行，步长 1

            int floatStride  = type.IsFloatBacked() ? ElementStride() : 0;
            int stringStride = type.StringStride();
            int objStride    = type.IsObjectBacked() ? 1 : 0;
            int curveStride  = type.IsAnimationCurveBacked() ? 1 : 0;

            ReorderBacking(ints,          intStride,    newOrder);
            ReorderBacking(floats,        floatStride,  newOrder);
            ReorderBacking(strings,       stringStride, newOrder);
            ReorderBacking(objRefs,       objStride,    newOrder);
            ReorderBacking(curves,        curveStride,  newOrder);
            ReorderBacking(objAddresses, objStride,    newOrder);
        }

        /// <summary>按 <paramref name="newOrder"/>（原元素索引排列）以 <paramref name="stride"/> 为步长整段重排后备列表。</summary>
        private static void ReorderBacking<T>(List<T> list, int stride, IReadOnlyList<int> newOrder)
        {
            if (list == null || stride <= 0 || list.Count == 0) return;
            int count = list.Count / stride;
            var result = new List<T>(list.Count);
            foreach (int oldIdx in newOrder)
            {
                if (oldIdx < 0 || oldIdx >= count) continue;
                int b = oldIdx * stride;
                for (int k = 0; k < stride; k++) result.Add(list[b + k]);
            }
            if (result.Count != list.Count) return;   // 排列不完整则放弃，避免丢数据
            list.Clear();
            list.AddRange(result);
        }

        public int GetInt(int element)       => element < ints.Count   ? ints[element]   : 0;
        public void SetInt(int element, int value)   { EnsureIntCapacity(element + 1);    ints[element]   = value; }

        public float GetFloat(int element)   => element < floats.Count  ? floats[element]  : 0f;
        public void SetFloat(int element, float value) { EnsureFloatCapacity(element + 1); floats[element]  = value; }

        public string GetString(int element) => element < strings.Count ? strings[element] : string.Empty;
        public void SetString(int element, string value) { EnsureStringCapacity(element + 1); strings[element] = value ?? string.Empty; }

        /// <summary>
        /// 读取指定元素的 StringIntPair (key, value)。
        /// <para>key 来自字符串后备列表，value 来自整数后备列表；两列表平行同步。</para>
        /// </summary>
        public (string key, int value) GetStringIntPair(int element)
        {
            string key = element < strings.Count ? strings[element] : string.Empty;
            int    val = element < ints.Count    ? ints[element]    : 0;
            return (key, val);
        }

        /// <summary>
        /// 设置指定元素的 StringIntPair (key, value)。
        /// 同时扩容字符串和整数后备列表，保持两列表长度同步。
        /// </summary>
        public void SetStringIntPair(int element, string key, int value)
        {
            EnsureStringCapacity(element + 1);
            EnsureIntCapacity(element + 1);
            strings[element] = key ?? string.Empty;
            ints[element]    = value;
        }

        /// <summary>
        /// 读取指定元素的 EnumIntPair (enumValue, value)。
        /// <para>enumValue 为枚举的不可变值（对应 <see cref="EnumItem.Value"/>），value 为整数值；
        /// 二者平铺存于整数后备列表，步长 2。</para>
        /// </summary>
        public (int enumValue, int value) GetEnumIntPair(int element)
        {
            int b   = element * 2;
            int key = b     < ints.Count ? ints[b]     : 0;
            int val = b + 1 < ints.Count ? ints[b + 1] : 0;
            return (key, val);
        }

        /// <summary>
        /// 设置指定元素的 EnumIntPair (enumValue, value)。
        /// 扩容整数后备列表以容纳该元素的两个整数槽（步长 2）。
        /// </summary>
        public void SetEnumIntPair(int element, int enumValue, int value)
        {
            int b = element * 2;
            EnsureIntCapacity(b + 2);
            ints[b]     = enumValue;
            ints[b + 1] = value;
        }

        public Object GetObject(int element) => element < objRefs.Count ? objRefs[element] : null;
        public void SetObject(int element, Object value) { EnsureObjectCapacity(element + 1); objRefs[element] = value; }

        /// <summary>读取指定元素的 Addressable 地址（运行时导入后填充；编辑期为空）。无则返回空串。</summary>
        public string GetObjAddress(int element)
            => objAddresses != null && element >= 0 && element < objAddresses.Count
                ? objAddresses[element] : string.Empty;

        /// <summary>该元素是否存在非空 Addressable 地址。</summary>
        public bool HasObjAddress(int element) => !string.IsNullOrEmpty(GetObjAddress(element));

        /// <summary>
        /// 设置指定元素的 Addressable 地址 / 授权 GUID（扩容以容纳该槽，前面缺口补空串）。
        /// 供 IS_ADDRESSABLE 编辑器授权（AssetReference 选择器）与迁移工具写入。
        /// </summary>
        public void SetObjAddress(int element, string guid)
        {
            if (element < 0) return;
            EnsureAddressCapacity(element + 1);
            objAddresses[element] = guid ?? string.Empty;
        }

        public AnimationCurve GetAnimationCurve(int element)
            => element < curves.Count ? curves[element] : null;
        public void SetAnimationCurve(int element, AnimationCurve value)
        {
            EnsureCurveCapacity(element + 1);
            curves[element] = value ?? new AnimationCurve();
        }

        public Vector2 GetVector2(int element) => ReadVector(element, 2);
        public void SetVector2(int element, Vector2 v) => WriteVector(element, new[] { v.x, v.y });
        public Vector3 GetVector3(int element) { var v = ReadVector(element, 3); return new Vector3(v.x, v.y, v.z); }
        public void SetVector3(int element, Vector3 v) => WriteVector(element, new[] { v.x, v.y, v.z });
        public Vector4 GetVector4(int element) => ReadVector(element, 4);
        public void SetVector4(int element, Vector4 v) => WriteVector(element, new[] { v.x, v.y, v.z, v.w });
        public Color GetColor(int element) { var v = ReadVector(element, 4); return new Color(v.x, v.y, v.z, v.w); }
        public void SetColor(int element, Color c) => WriteVector(element, new[] { c.r, c.g, c.b, c.a });

        // ── 整数向量（VectorInt2 / VectorInt3 / VectorInt4）──────────────────────────

        public Vector2Int GetVector2Int(int element)
        {
            int b = element * 2;
            EnsureIntCapacity(b + 2);
            return new Vector2Int(ints[b], ints[b + 1]);
        }
        public void SetVector2Int(int element, Vector2Int v)
        {
            int b = element * 2;
            EnsureIntCapacity(b + 2);
            ints[b] = v.x; ints[b + 1] = v.y;
        }

        public Vector3Int GetVector3Int(int element)
        {
            int b = element * 3;
            EnsureIntCapacity(b + 3);
            return new Vector3Int(ints[b], ints[b + 1], ints[b + 2]);
        }
        public void SetVector3Int(int element, Vector3Int v)
        {
            int b = element * 3;
            EnsureIntCapacity(b + 3);
            ints[b] = v.x; ints[b + 1] = v.y; ints[b + 2] = v.z;
        }

        /// <summary>读取 VectorInt4 的四个分量（Unity 无原生 Vector4Int，以元组返回）。</summary>
        public (int x, int y, int z, int w) GetVector4Int(int element)
        {
            int b = element * 4;
            EnsureIntCapacity(b + 4);
            return (ints[b], ints[b + 1], ints[b + 2], ints[b + 3]);
        }
        public void SetVector4Int(int element, int x, int y, int z, int w)
        {
            int b = element * 4;
            EnsureIntCapacity(b + 4);
            ints[b] = x; ints[b + 1] = y; ints[b + 2] = z; ints[b + 3] = w;
        }

        private void EnsureIntCapacity(int count)    { while (ints.Count    < count) ints.Add(0); }
        private void EnsureStringCapacity(int count)  { while (strings.Count < count) strings.Add(string.Empty); }
        private void EnsureObjectCapacity(int count)  { while (objRefs.Count < count) objRefs.Add(null); }
        private void EnsureAddressCapacity(int count) { while (objAddresses.Count < count) objAddresses.Add(string.Empty); }
        // AnimationCurve 默认值：(0,0)→(1,1) 直线，保证新建曲线有明确形状
        private void EnsureCurveCapacity(int count)   { while (curves.Count  < count) curves.Add(AnimationCurve.Linear(0f, 0f, 1f, 1f)); }

        #endregion

        #region 原始数据读写（供序列化层使用）

        /// <summary>直接读取整数后备列表。供导出层使用。</summary>
        public List<int>    RawInts    => ints;
        /// <summary>直接读取浮点后备列表。供导出层使用。</summary>
        public List<float>  RawFloats  => floats;
        /// <summary>直接读取字符串后备列表。供导出层使用。</summary>
        public List<string> RawStrings => strings;
        /// <summary>直接读取对象引用后备列表。供导出层使用。</summary>
        public List<Object> RawObjects => objRefs;
        /// <summary>直接读取动画曲线后备列表。供导出层使用。</summary>
        public List<AnimationCurve> RawCurves => curves;
        /// <summary>直接读取对象 Addressable 地址列表（运行时填充）。供取用门面/加载器读。</summary>
        public List<string> RawObjAddresses => objAddresses ??= new List<string>();

        /// <summary>
        /// 用原始后备数据整体覆盖该值（供导入层从 DTO 重建）。传入 null 的列表视为空。
        /// 参数名刻意与字段名不同，避免歧义，无需 this. 前缀。
        /// </summary>
        public void SetRaw(EFieldType fieldType, bool array, string enumRef,
            IList<int> intList, IList<float> floatList, IList<string> stringList, IList<Object> objList,
            IList<AnimationCurve> curveList = null, IList<string> addressList = null)
        {
            type        = fieldType;
            isArray     = array;
            enumTypeRef = enumRef;
            ints    = intList    != null ? new List<int>(intList)       : new List<int>();
            floats  = floatList  != null ? new List<float>(floatList)   : new List<float>();
            strings = stringList != null ? new List<string>(stringList) : new List<string>();
            objRefs = objList    != null ? new List<Object>(objList)    : new List<Object>();
            curves  = curveList  != null ? new List<AnimationCurve>(curveList) : new List<AnimationCurve>();
            objAddresses = addressList != null ? new List<string>(addressList) : new List<string>();
        }

        #endregion

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

        #region 克隆

        /// <summary>深拷贝。对象引用为浅拷贝（共享同一 Unity 资源引用）；曲线为深拷贝。</summary>
        public AttributeValue Clone()
        {
            var clonedCurves = new List<AnimationCurve>(curves.Count);
            foreach (var c in curves)
                clonedCurves.Add(c != null ? new AnimationCurve(c.keys) : new AnimationCurve());

            return new AttributeValue(type, isArray, enumTypeRef)
            {
                ints    = new List<int>(ints),
                floats  = new List<float>(floats),
                strings = new List<string>(strings),
                objRefs = new List<Object>(objRefs),
                curves  = clonedCurves,
                objAddresses = objAddresses != null ? new List<string>(objAddresses) : new List<string>()
            };
        }

        #endregion
    }
}

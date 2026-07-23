using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// <see cref="AttributeValue"/> 的数组元素访问分部：按逻辑索引对各后备列表增 / 删 / 改 / 重排，
    /// 以及供序列化层直读 / 覆写原始后备数据。各后备列表的步长统一由核心文件的 <c>GetStrides</c> 决定。
    /// </summary>
    public partial class AttributeValue
    {
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
            else if (type.IsObjectBacked())           { objRefs.Add(null); EnsureAddressCapacity(objRefs.Count); }
            else if (type.IsAnimationCurveBacked())   curves.Add(AnimationCurve.Linear(0f, 0f, 1f, 1f));
        }

        /// <summary>移除指定索引的数组元素（各后备列表按其步长整段移除，保持平行列表同步）。</summary>
        public void RemoveElement(int element)
        {
            if (element < 0 || element >= Count) return;
            GetStrides(out int intS, out int floatS, out int stringS, out int objS, out int curveS);
            RemoveBacking(ints,    intS,    element);
            RemoveBacking(floats,  floatS,  element);
            RemoveBacking(strings, stringS, element);
            RemoveBacking(objRefs, objS,    element);
            RemoveBacking(curves,  curveS,  element);
            RemoveBacking(objAddresses, objS, element);   // 与 objRefs 平行
        }

        /// <summary>从后备列表移除第 <paramref name="element"/> 个逻辑元素占用的 <paramref name="stride"/> 个槽（步长 0 表示不参与）。</summary>
        private static void RemoveBacking<T>(List<T> list, int stride, int element)
        {
            if (list == null || stride <= 0) return;
            int b = element * stride;
            if (b + stride <= list.Count) list.RemoveRange(b, stride);
        }

        /// <summary>
        /// 按给定「新顺序」（元素为原逻辑索引的一个排列）重排数组元素，同步作用于所有相关后备列表
        /// （按各自步长整段搬移，保持平行列表同步）。供 Inspector 拖拽重排使用。
        /// </summary>
        public void ReorderElements(IReadOnlyList<int> newOrder)
        {
            if (newOrder == null || newOrder.Count == 0) return;

            GetStrides(out int intS, out int floatS, out int stringS, out int objS, out int curveS);
            ReorderBacking(ints,    intS,    newOrder);
            ReorderBacking(floats,  floatS,  newOrder);
            ReorderBacking(strings, stringS, newOrder);
            ReorderBacking(objRefs, objS,    newOrder);
            ReorderBacking(curves,  curveS,  newOrder);
            ReorderBacking(objAddresses, objS, newOrder);   // 与 objRefs 平行
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

        // 读取一律双侧越界保护：负索引与超长索引都回落到默认值，绝不抛异常、也绝不扩容后备列表
        // （getter 扩容会把序列化列表改脏，见 GetVector*Int 的说明）。
        public int GetInt(int element)       => element >= 0 && element < ints.Count   ? ints[element]   : 0;
        public void SetInt(int element, int value)   { EnsureIntCapacity(element + 1);    ints[element]   = value; }

        public float GetFloat(int element)   => element >= 0 && element < floats.Count  ? floats[element]  : 0f;
        public void SetFloat(int element, float value) { EnsureFloatCapacity(element + 1); floats[element]  = value; }

        public string GetString(int element) => element >= 0 && element < strings.Count ? strings[element] : string.Empty;
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

        public Object GetObject(int element) => element >= 0 && element < objRefs.Count ? objRefs[element] : null;

        /// <summary>
        /// 设置指定元素的对象引用。同时扩容 <c>objAddresses</c>，保持两个平行列表等长——
        /// 长度不一致会让 <see cref="ReorderElements"/> 按错误的元素数重排地址列表。
        /// </summary>
        public void SetObject(int element, Object value)
        {
            EnsureObjectCapacity(element + 1);
            EnsureAddressCapacity(element + 1);
            objRefs[element] = value;
        }

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
            => element >= 0 && element < curves.Count ? curves[element] : null;
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

        // 读取用 IntAt 做越界保护而**不扩容**：getter 里调 EnsureIntCapacity 会向 ints 追加元素，
        // 于是「在 Inspector 里看一眼 VectorInt 属性」就把资产改脏了。语义与 ReadVector 一致：缺失分量读作 0。
        public Vector2Int GetVector2Int(int element)
        {
            int b = element * 2;
            return new Vector2Int(IntAt(b), IntAt(b + 1));
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
            return new Vector3Int(IntAt(b), IntAt(b + 1), IntAt(b + 2));
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
            return (IntAt(b), IntAt(b + 1), IntAt(b + 2), IntAt(b + 3));
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
        private void EnsureAddressCapacity(int count)
        {
            objAddresses ??= new List<string>();   // 旧数据反序列化后可能为 null
            while (objAddresses.Count < count) objAddresses.Add(string.Empty);
        }
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
    }
}

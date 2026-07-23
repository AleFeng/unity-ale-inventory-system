using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Runtime
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
    ///
    /// <para><b>本类按职责拆为多个分部文件</b>（partial 不影响序列化，全部序列化字段留在本文件）：
    /// 本文件为核心（字段 / 构造 / 属性 / 类型步长与类型变更 / 标量与向量访问器 / 克隆）；
    /// <c>.Elements.cs</c> 为数组元素访问器与原始数据读写；<c>.Text.cs</c> 为 Text 访问器 / 显示字符串 / 排序比较数值。</para>
    /// </summary>
    [Serializable]
    public partial class AttributeValue
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
                // 由每个逻辑元素在各后备列表中占用的步长反推元素数：取首个被本类型使用的后备列表
                // （优先级 float → string → int → obj → curve；同一类型仅一类为主，成对类型的两列平行、结果相同）。
                GetStrides(out int intS, out int floatS, out int stringS, out int objS, out int curveS);
                if (floatS  > 0) return floats.Count  / floatS;
                if (stringS > 0) return strings.Count / stringS;
                if (intS    > 0) return ints.Count    / intS;
                if (objS    > 0) return objRefs.Count / objS;
                if (curveS  > 0) return curves.Count  / curveS;
                return 0;
            }
        }

        #endregion

        #region 类型步长

        /// <summary>
        /// 返回当前类型下，<b>每个逻辑元素</b>在各后备列表中占用的步长（0 = 该列表不参与本类型）。
        /// 元素计数、增删、重排、数组↔标量裁剪全部据此推导，避免「新增一个 <see cref="EFieldType"/>
        /// 却漏改其中某处」的分歧。
        /// <para>成对类型的两个平行列表步长同时非 0：<see cref="EFieldType.StringIntPair"/> 为 int=1 & string=1；
        /// <see cref="EFieldType.EnumIntPair"/> 为 int=2。对象类的 <c>objAddresses</c> 与 <c>objRefs</c> 平行，
        /// 步长同 <paramref name="objStride"/>，此处不单列。</para>
        /// </summary>
        private void GetStrides(out int intStride, out int floatStride,
            out int stringStride, out int objStride, out int curveStride)
        {
            intStride = floatStride = stringStride = objStride = curveStride = 0;

            if      (type.IsIntBacked())            intStride = 1;
            else if (type.IsIntVectorBacked())      intStride = type.IntStride();          // 2 / 3 / 4
            else if (type == EFieldType.EnumIntPair) intStride = 2;                          // 枚举值 + 整数值

            if (type.IsFloatBacked()) floatStride = ElementStride();                         // Float=1，Vector*/Color=2/3/4

            if (type == EFieldType.StringIntPair) { stringStride = 1; intStride = 1; }        // strings 与 ints 平行
            else                                   stringStride = type.StringStride();        // String=1，Text=3，其余 0

            if (type.IsObjectBacked())         objStride   = 1;
            if (type.IsAnimationCurveBacked()) curveStride = 1;
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
            // 与 objRefs 平行，必须一并清空：否则改成别的类型再改回对象类时，
            // 旧资源的授权 GUID 仍残留，取用门面会在 objRefs 为空时按它加载到上一个资源。
            objAddresses?.Clear();
        }

        /// <summary>设置是否为数组形态。从数组切回标量时仅保留首个元素（各后备列表按其步长裁到 1 个元素）。</summary>
        public void SetIsArray(bool asArray)
        {
            if (isArray == asArray) return;
            isArray = asArray;
            if (asArray) return;

            // 「保留首个元素」= 各后备列表按其步长保留前 stride 个槽（步长 0 的列表不参与本类型，裁到 0）。
            GetStrides(out int intS, out int floatS, out int stringS, out int objS, out int curveS);
            TrimBacking(ints,    intS);
            TrimBacking(floats,  floatS);
            TrimBacking(strings, stringS);
            TrimBacking(objRefs, objS);
            TrimBacking(curves,  curveS);
            TrimBacking(objAddresses, objS);   // 与 objRefs 平行，步长同 objS，同步裁剪
        }

        /// <summary>把后备列表裁到首个逻辑元素（保留前 <paramref name="stride"/> 个槽；步长 0 表示本类型不用该列表，裁空）。</summary>
        private static void TrimBacking<T>(List<T> list, int stride)
        {
            // 旧数据反序列化后 objAddresses 可能为 null（该字段晚于其余后备列表加入），故容 null。
            if (list != null && list.Count > stride) list.RemoveRange(stride, list.Count - stride);
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
            // 走 SetObject 而非 SetSingle，使 objAddresses 一并保持等长（见 SetObject 说明）。
            set => SetObject(0, value);
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

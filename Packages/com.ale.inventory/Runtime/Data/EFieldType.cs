namespace InventorySystem.Runtime
{
    /// <summary>
    /// 属性字段的数据类型。每个 <see cref="AttributeValue"/> 都携带其中一种类型，
    /// 并可通过 <see cref="AttributeValue.IsArray"/> 切换为数组形态。
    /// </summary>
    public enum EFieldType
    {
        /// <summary>整数。</summary>
        Int = 0,
        /// <summary>单精度浮点数。</summary>
        Float = 1,
        /// <summary>字符串。</summary>
        String = 2,
        /// <summary>布尔值（内部以 0/1 存储于整数后备列表）。</summary>
        Bool = 3,
        /// <summary>枚举值（存储所引用枚举类型的不可变整数值，配合 <see cref="AttributeValue.EnumTypeRef"/>）。</summary>
        Enum = 4,
        /// <summary>二维向量（压平存储于浮点后备列表，步长 2）。</summary>
        Vector2 = 5,
        /// <summary>三维向量（压平存储于浮点后备列表，步长 3）。</summary>
        Vector3 = 6,
        /// <summary>四维向量（压平存储于浮点后备列表，步长 4）。</summary>
        Vector4 = 7,
        /// <summary>颜色（RGBA 压平存储于浮点后备列表，步长 4）。</summary>
        Color = 8,
        /// <summary>Unity 资源引用（如 Sprite，存储于对象后备列表；导出时转为 GUID）。</summary>
        Sprite = 9,
        /// <summary>
        /// 文本类型。区别于 <see cref="String"/>（后者用于数据 / 标记，不一定用于展示文本）：
        /// <c>Text</c> 专用于展示文本，<b>始终</b>携带一个纯文本值作 fallback；启用 IS_LOCALIZATION 宏时额外携带
        /// Unity Localization 引用（表 + 条目），运行时优先取本地化文本、取不到再回退纯文本。
        /// 以字符串后备列表承载，每个逻辑元素占 3 个槽：[i*3]=纯文本、[i*3+1]=tableRef、[i*3+2]=entryKey。
        /// 枚举值固定为 10，无论 IS_LOCALIZATION 是否启用均保持稳定，防止数据损坏。
        /// </summary>
        Text = 10,
        /// <summary>Unity GameObject 预制件引用（存储于对象后备列表；导出时转为 GUID）。</summary>
        Prefab = 11,
        /// <summary>Unity Texture 贴图引用（存储于对象后备列表；导出时转为 GUID）。</summary>
        Texture = 12,
        /// <summary>Unity Material 材质引用（存储于对象后备列表；导出时转为 GUID）。</summary>
        Material = 13,
        /// <summary>Unity AudioClip 音频剪辑引用（存储于对象后备列表；导出时转为 GUID）。</summary>
        AudioClip = 14,
        /// <summary>Unity AnimationClip 动画剪辑引用（存储于对象后备列表；导出时转为 GUID）。</summary>
        AnimationClip = 15,
        /// <summary>Unity PhysicsMaterial 3D 物理材质引用（存储于对象后备列表；导出时转为 GUID）。</summary>
        PhysicsMaterial = 16,
        /// <summary>Unity PhysicsMaterial2D 2D 物理材质引用（存储于对象后备列表；导出时转为 GUID）。</summary>
        PhysicsMaterial2D = 17,
        /// <summary>
        /// Unity AnimationCurve 动画曲线（存储于专用曲线后备列表；导出时序列化为关键帧字符串）。
        /// </summary>
        AnimationCurve = 18,
        /// <summary>二维整数向量（压平存储于整数后备列表，步长 2）。</summary>
        VectorInt2 = 19,
        /// <summary>三维整数向量（压平存储于整数后备列表，步长 3）。</summary>
        VectorInt3 = 20,
        /// <summary>四维整数向量（压平存储于整数后备列表，步长 4）。</summary>
        VectorInt4 = 21,
        /// <summary>
        /// 字符串-整数对。每个逻辑元素由一个字符串键（存于字符串后备列表，步长 1）和
        /// 一个整数值（存于整数后备列表，步长 1）组成，两列表平行同步。
        /// 典型用途：属性ID：属性数量、材料ID：材料数量 等键值配对场景。支持数组。
        /// 在需要使用Float的情况下，也建议使用千分之一的整数来存储小数（如 1.5 -> 1500），以避免浮点精度问题。
        /// </summary>
        StringIntPair = 22,
        /// <summary>
        /// 枚举-整数对。每个逻辑元素由一个枚举键与一个整数值组成，二者平铺存于整数后备列表
        /// （步长 2：[i*2] = 枚举不可变值 <see cref="EnumItem.Value"/>，[i*2+1] = 整数值）；
        /// 枚举类型经 <see cref="AttributeValue.EnumTypeRef"/> 记录，来源与 <see cref="Enum"/> 相同。
        /// 典型用途：角色属性类型:加成数值，用于配置装备对指定角色属性的数值加成。支持数组。
        /// </summary>
        EnumIntPair = 23,
    }

    /// <summary>
    /// <see cref="EFieldType"/> 的辅助方法。
    /// </summary>
    public static class FieldTypeUtility
    {
        /// <summary>
        /// 返回该类型在浮点后备列表中每个元素占用的步长；非浮点承载类型返回 0。
        /// </summary>
        public static int FloatStride(this EFieldType type)
        {
            switch (type)
            {
                case EFieldType.Vector2: return 2;
                case EFieldType.Vector3: return 3;
                case EFieldType.Vector4: return 4;
                case EFieldType.Color: return 4;
                default: return 0;
            }
        }

        /// <summary>该类型是否以浮点后备列表承载数据（Float / Vector* / Color）。</summary>
        public static bool IsFloatBacked(this EFieldType type)
        {
            return type == EFieldType.Float || type.FloatStride() > 0;
        }

        /// <summary>
        /// 该类型是否以整数后备列表承载数据（Int / Bool / Enum），且每个逻辑元素仅占 1 个整数槽。
        /// </summary>
        public static bool IsIntBacked(this EFieldType type)
        {
            return type == EFieldType.Int || type == EFieldType.Bool || type == EFieldType.Enum;
        }

        /// <summary>
        /// 返回该类型在整数后备列表中每个元素占用的步长（仅 VectorInt2/3/4 返回 2/3/4；其余返回 0）。
        /// </summary>
        public static int IntStride(this EFieldType type)
        {
            switch (type)
            {
                case EFieldType.VectorInt2: return 2;
                case EFieldType.VectorInt3: return 3;
                case EFieldType.VectorInt4: return 4;
                default: return 0;
            }
        }

        /// <summary>
        /// 该类型是否以整数后备列表承载多分量向量（VectorInt2 / VectorInt3 / VectorInt4）。
        /// <para>与 <see cref="IsIntBacked"/> 互斥：<c>IsIntBacked</c> 表示步长=1 的整数类型。</para>
        /// </summary>
        public static bool IsIntVectorBacked(this EFieldType type)
        {
            return type == EFieldType.VectorInt2
                || type == EFieldType.VectorInt3
                || type == EFieldType.VectorInt4;
        }

        /// <summary>
        /// 该类型是否为 Unity 对象引用
        /// （Sprite / Prefab / Texture / Material / AudioClip / AnimationClip / PhysicsMaterial / PhysicsMaterial2D）。
        /// </summary>
        public static bool IsObjectBacked(this EFieldType type)
        {
            return type == EFieldType.Sprite
                || type == EFieldType.Prefab
                || type == EFieldType.Texture
                || type == EFieldType.Material
                || type == EFieldType.AudioClip
                || type == EFieldType.AnimationClip
                || type == EFieldType.PhysicsMaterial
                || type == EFieldType.PhysicsMaterial2D;
        }

        /// <summary>该类型是否以专用动画曲线后备列表承载（AnimationCurve）。</summary>
        public static bool IsAnimationCurveBacked(this EFieldType type)
        {
            return type == EFieldType.AnimationCurve;
        }

        /// <summary>
        /// 该类型是否以字符串后备列表承载数据（String / Text / StringIntPair）。
        /// </summary>
        public static bool IsStringBacked(this EFieldType type)
        {
            return type == EFieldType.String
                || type == EFieldType.Text
                || type == EFieldType.StringIntPair;
        }

        /// <summary>
        /// 该类型在字符串后备列表中每个逻辑元素占用的槽数。
        /// String = 1；Text = 3（纯文本 + tableRef + entryKey）；StringIntPair = 1；其余 = 0。
        /// </summary>
        public static int StringStride(this EFieldType type)
        {
            if (type == EFieldType.Text)          return 3;
            if (type == EFieldType.String)        return 1;
            if (type == EFieldType.StringIntPair) return 1;
            return 0;
        }
    }
}

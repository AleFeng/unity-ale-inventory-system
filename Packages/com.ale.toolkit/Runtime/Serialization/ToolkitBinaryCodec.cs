using System;
using System.IO;

namespace Ale.Toolkit.Runtime.Serialization
{
    /// <summary>
    /// Toolkit 通用二进制读写辅助：属性系统 DTO（属性值 / 定义 / 条目 / 枚举）、分组标签 DTO 的紧凑读写，
    /// 以及各宿主序列化器共用的基础读写（长度前缀数组、字符串、Int/Float/Str 数组）。
    ///
    /// <para>只操作 DTO 与 <see cref="BinaryWriter"/> / <see cref="BinaryReader"/>，与运行时模型无关。
    /// 宿主插件的领域序列化器（如库存的 <c>InventoryBinarySerializer</c>）负责文件头、顶层 Export/Import
    /// 与领域块，调用本类完成属性系统与通用条目部分。导出格式保持不变，仅读写代码所在的类不同。</para>
    /// </summary>
    public static class ToolkitBinaryCodec
    {
        #region 基础读写

        public static void WriteStr(BinaryWriter w, string s) => w.Write(s ?? string.Empty);
        public static string ReadStr(BinaryReader r) => r.ReadString();

        public static void WriteIntArray(BinaryWriter w, int[] a)
        {
            int n = a?.Length ?? 0;
            w.Write(n);
            for (int i = 0; i < n; i++) w.Write(a[i]);
        }

        public static int[] ReadIntArray(BinaryReader r)
        {
            int n = r.ReadInt32();
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = r.ReadInt32();
            return a;
        }

        public static void WriteFloatArray(BinaryWriter w, float[] a)
        {
            int n = a?.Length ?? 0;
            w.Write(n);
            for (int i = 0; i < n; i++) w.Write(a[i]);
        }

        public static float[] ReadFloatArray(BinaryReader r)
        {
            int n = r.ReadInt32();
            var a = new float[n];
            for (int i = 0; i < n; i++) a[i] = r.ReadSingle();
            return a;
        }

        public static void WriteStrArray(BinaryWriter w, string[] a)
        {
            int n = a?.Length ?? 0;
            w.Write(n);
            for (int i = 0; i < n; i++) WriteStr(w, a[i]);
        }

        public static string[] ReadStrArray(BinaryReader r)
        {
            int n = r.ReadInt32();
            var a = new string[n];
            for (int i = 0; i < n; i++) a[i] = ReadStr(r);
            return a;
        }

        public static void WriteArray<T>(BinaryWriter w, T[] a, Action<BinaryWriter, T> write)
        {
            int n = a?.Length ?? 0;
            w.Write(n);
            for (int i = 0; i < n; i++) write(w, a[i]);
        }

        public static T[] ReadArray<T>(BinaryReader r, Func<BinaryReader, T> read)
        {
            int n = r.ReadInt32();
            var a = new T[n];
            for (int i = 0; i < n; i++) a[i] = read(r);
            return a;
        }

        #endregion

        #region 属性值 / 定义 / 条目

        public static void WriteValue(BinaryWriter w, AttributeValueDto v)
        {
            v ??= new AttributeValueDto();
            w.Write(v.type);
            w.Write(v.isArray);
            WriteStr(w, v.enumTypeRef);
            WriteIntArray(w, v.ints);
            WriteFloatArray(w, v.floats);
            WriteStrArray(w, v.strings);
            WriteStrArray(w, v.objGuids);
            WriteStrArray(w, v.curveData);  // v5: AnimationCurve 关键帧字符串数组
        }

        public static AttributeValueDto ReadValue(BinaryReader r)
        {
            return new AttributeValueDto
            {
                type        = r.ReadInt32(),
                isArray     = r.ReadBoolean(),
                enumTypeRef = ReadStr(r),
                ints        = ReadIntArray(r),
                floats      = ReadFloatArray(r),
                strings     = ReadStrArray(r),
                objGuids    = ReadStrArray(r),
                curveData   = ReadStrArray(r)   // v5: AnimationCurve 关键帧字符串数组
            };
        }

        public static void WriteDefinition(BinaryWriter w, AttributeDefinitionDto d)
        {
            WriteStr(w, d.id);
            w.Write(d.type);
            w.Write(d.isArray);
            WriteStr(w, d.enumTypeRef);
            WriteValue(w, d.defaultValue);
        }

        public static AttributeDefinitionDto ReadDefinition(BinaryReader r)
        {
            return new AttributeDefinitionDto
            {
                id = ReadStr(r),
                type = r.ReadInt32(),
                isArray = r.ReadBoolean(),
                enumTypeRef = ReadStr(r),
                defaultValue = ReadValue(r)
            };
        }

        /// <summary>属性值条目数组（id + value）——各系统实体的 values / attributeValues 共用。</summary>
        public static void WriteEntries(BinaryWriter w, AttributeEntryDto[] entries)
            => WriteArray(w, entries, (bw, e) => { WriteStr(bw, e.id); WriteValue(bw, e.value); });

        public static AttributeEntryDto[] ReadEntries(BinaryReader r)
            => ReadArray(r, br => new AttributeEntryDto { id = ReadStr(br), value = ReadValue(br) });

        #endregion

        #region 枚举类型 / 分组标签

        public static void WriteEnumType(BinaryWriter w, EnumTypeDto e)
        {
            WriteStr(w, e.name);
            w.Write(e.nextValue);
            WriteArray(w, e.attributes, WriteDefinition);
            WriteArray(w, e.items, (bw, it) =>
            {
                WriteStr(bw, it.name);
                bw.Write(it.value);
                WriteEntries(bw, it.attributeValues);
            });
        }

        public static EnumTypeDto ReadEnumType(BinaryReader r)
        {
            var e = new EnumTypeDto { name = ReadStr(r), nextValue = r.ReadInt32() };
            e.attributes = ReadArray(r, ReadDefinition);
            e.items = ReadArray(r, br =>
            {
                var it = new EnumItemDto { name = ReadStr(br), value = br.ReadInt32() };
                it.attributeValues = ReadEntries(br);
                return it;
            });
            return e;
        }

        /// <summary>分组标签（id + 显示名 + 描述 + 色点）——各系统共用。</summary>
        public static void WriteGroupTag(BinaryWriter w, GroupTagDto t)
        {
            WriteStr(w, t.id);
            WriteValue(w, t.displayName);
            WriteValue(w, t.description);
            WriteFloatArray(w, t.color);
        }

        public static GroupTagDto ReadGroupTag(BinaryReader r)
        {
            return new GroupTagDto
            {
                id          = ReadStr(r),
                displayName = ReadValue(r),
                description = ReadValue(r),
                color       = ReadFloatArray(r)
            };
        }

        #endregion
    }
}

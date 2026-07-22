using System.IO;
using System.Text;
using UnityEngine;

namespace InventorySystem.Runtime.Serialization
{
    /// <summary>
    /// 仓库系统二进制序列化器。导出：DB -> 紧凑 byte[]（带魔数与版本头）；导入：byte[] -> 新的 InventoryDatabase 实例。
    /// 与 JSON 一样为单向导出格式，适合正式发布时使用。
    /// </summary>
    public static class InventoryBinarySerializer
    {
        // 魔数 "INVD"，用于快速校验格式。
        private const int Magic = 0x494E5644;

        #region 导出

        public static byte[] Export(InventoryDatabase db, IAssetRefResolver resolver)
        {
            var dto = InventoryDtoMapper.ToDto(db, resolver);
            using var stream = new MemoryStream();
            using (var w = new BinaryWriter(stream, Encoding.UTF8))
            {
                w.Write(Magic);
                w.Write(InventoryDtoMapper.Version);

                WriteArray(w, dto.enumTypes, WriteEnumType);
                WriteArray(w, dto.functionTags, WriteFunctionTag);
                WriteArray(w, dto.itemTemplates, WriteItemTemplate);
                WriteArray(w, dto.items, WriteItem);
            }
            return stream.ToArray();
        }

        private static void WriteEnumType(BinaryWriter w, EnumTypeDto e)
        {
            WriteStr(w, e.name);
            w.Write(e.nextValue);
            WriteArray(w, e.attributes, WriteDefinition);
            WriteArray(w, e.items, (bw, it) =>
            {
                WriteStr(bw, it.name);
                bw.Write(it.value);
                WriteArray(bw, it.attributeValues,
                    (bw2, av) => { WriteStr(bw2, av.id); WriteValue(bw2, av.value); });
            });
        }

        private static void WriteFunctionTag(BinaryWriter w, FunctionTagDto t)
        {
            WriteStr(w, t.name);
            WriteStr(w, t.description);
            WriteArray(w, t.attributes, WriteDefinition);
        }

        private static void WriteItemTemplate(BinaryWriter w, ItemTemplateDto t)
        {
            WriteStr(w, t.name);
            WriteArray(w, t.attributes, WriteDefinition);
            WriteStrArray(w, t.tagRefs);
        }

        private static void WriteItem(BinaryWriter w, ItemDto item)
        {
            WriteStr(w, item.id);
            WriteStr(w, item.templateRef);
            WriteStrArray(w, item.tagRefs);
            WriteArray(w, item.values, (bw, v) => { WriteStr(bw, v.id); WriteValue(bw, v.value); });
        }

        private static void WriteDefinition(BinaryWriter w, AttributeDefinitionDto d)
        {
            WriteStr(w, d.id);
            w.Write(d.type);
            w.Write(d.isArray);
            WriteStr(w, d.enumTypeRef);
            WriteValue(w, d.defaultValue);
        }

        private static void WriteValue(BinaryWriter w, AttributeValueDto v)
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

        #endregion

        #region 导入

        public static InventoryDatabase Import(byte[] bytes, IAssetRefResolver resolver)
        {
            var db = ScriptableObject.CreateInstance<InventoryDatabase>();
            ImportInto(bytes, db, resolver);
            return db;
        }

        public static void ImportInto(byte[] bytes, InventoryDatabase target, IAssetRefResolver resolver)
        {
            if (bytes == null || bytes.Length < 8 || target == null) return;

            using var stream = new MemoryStream(bytes);
            using var r = new BinaryReader(stream, Encoding.UTF8);

            int magic = r.ReadInt32();
            if (magic != Magic)
            {
                Debug.LogError("[InventoryBinarySerializer] 魔数不匹配，数据格式无效。");
                return;
            }

            int version = r.ReadInt32();
            if (version != InventoryDtoMapper.Version)
                Debug.LogWarning($"[InventoryBinarySerializer] 版本不一致（文件 {version}，当前 {InventoryDtoMapper.Version}），尝试按当前格式解析。");

            var dto = new InventoryDatabaseDto
            {
                version = version,
                enumTypes = ReadArray(r, ReadEnumType),
                functionTags = ReadArray(r, ReadFunctionTag),
                itemTemplates = ReadArray(r, ReadItemTemplate),
                items = ReadArray(r, ReadItem)
            };

            InventoryDtoMapper.FromDto(dto, target, resolver);
        }

        private static EnumTypeDto ReadEnumType(BinaryReader r)
        {
            var e = new EnumTypeDto { name = ReadStr(r), nextValue = r.ReadInt32() };
            e.attributes = ReadArray(r, ReadDefinition);
            e.items = ReadArray(r, br =>
            {
                var it = new EnumItemDto { name = ReadStr(br), value = br.ReadInt32() };
                it.attributeValues = ReadArray(br,
                    br2 => new AttributeEntryDto { id = ReadStr(br2), value = ReadValue(br2) });
                return it;
            });
            return e;
        }

        private static FunctionTagDto ReadFunctionTag(BinaryReader r)
        {
            return new FunctionTagDto
            {
                name = ReadStr(r),
                description = ReadStr(r),
                attributes = ReadArray(r, ReadDefinition)
            };
        }

        private static ItemTemplateDto ReadItemTemplate(BinaryReader r)
        {
            return new ItemTemplateDto
            {
                name = ReadStr(r),
                attributes = ReadArray(r, ReadDefinition),
                tagRefs = ReadStrArray(r)
            };
        }

        private static ItemDto ReadItem(BinaryReader r)
        {
            return new ItemDto
            {
                id = ReadStr(r),
                templateRef = ReadStr(r),
                tagRefs = ReadStrArray(r),
                values = ReadArray(r, br => new AttributeEntryDto { id = ReadStr(br), value = ReadValue(br) })
            };
        }

        private static AttributeDefinitionDto ReadDefinition(BinaryReader r)
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

        private static AttributeValueDto ReadValue(BinaryReader r)
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

        #endregion

        #region 基础读写辅助

        private static void WriteStr(BinaryWriter w, string s) => w.Write(s ?? string.Empty);
        private static string ReadStr(BinaryReader r) => r.ReadString();

        private static void WriteIntArray(BinaryWriter w, int[] a)
        {
            int n = a?.Length ?? 0;
            w.Write(n);
            for (int i = 0; i < n; i++) w.Write(a[i]);
        }

        private static int[] ReadIntArray(BinaryReader r)
        {
            int n = r.ReadInt32();
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = r.ReadInt32();
            return a;
        }

        private static void WriteFloatArray(BinaryWriter w, float[] a)
        {
            int n = a?.Length ?? 0;
            w.Write(n);
            for (int i = 0; i < n; i++) w.Write(a[i]);
        }

        private static float[] ReadFloatArray(BinaryReader r)
        {
            int n = r.ReadInt32();
            var a = new float[n];
            for (int i = 0; i < n; i++) a[i] = r.ReadSingle();
            return a;
        }

        private static void WriteStrArray(BinaryWriter w, string[] a)
        {
            int n = a?.Length ?? 0;
            w.Write(n);
            for (int i = 0; i < n; i++) WriteStr(w, a[i]);
        }

        private static string[] ReadStrArray(BinaryReader r)
        {
            int n = r.ReadInt32();
            var a = new string[n];
            for (int i = 0; i < n; i++) a[i] = ReadStr(r);
            return a;
        }

        private static void WriteArray<T>(BinaryWriter w, T[] a, System.Action<BinaryWriter, T> write)
        {
            int n = a?.Length ?? 0;
            w.Write(n);
            for (int i = 0; i < n; i++) write(w, a[i]);
        }

        private static T[] ReadArray<T>(BinaryReader r, System.Func<BinaryReader, T> read)
        {
            int n = r.ReadInt32();
            var a = new T[n];
            for (int i = 0; i < n; i++) a[i] = read(r);
            return a;
        }

        #endregion
    }
}

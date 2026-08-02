using System.IO;
using System.Text;
using UnityEngine;
using Ale.Toolkit.Runtime;
using Ale.Toolkit.Runtime.Serialization;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// 仓库系统二进制序列化器。导出：DB -> 紧凑 byte[]（带魔数与版本头）；导入：byte[] -> 新的 InventoryDatabase 实例。
    /// 与 JSON 一样为单向导出格式，适合正式发布时使用。
    ///
    /// <para><b>向后兼容：</b>v6 起在道具系统数据块之后追加了 仓库 / 商店 / 制作 / 装备 等数据块，
    /// 并给道具系统的三类条目补了若干字段。读取时按文件头里的版本号跳过这些新增部分，因此 v5 及更早导出的
    /// <c>.bytes</c> 仍可正常导入（新增字段取运行时默认值）。反之新版导出的文件旧版读不了——单向导出格式，
    /// 按需重新导出即可。</para>
    ///
    /// <para>本分部承载：顶层 Export / Import、道具系统的块读写、整理条件读写；属性系统 / 枚举 / 分组标签的
    /// 紧凑读写与通用基础读写辅助已下沉到 <see cref="ToolkitBinaryCodec"/>，本类保留同名 <c>private</c> 转发，
    /// 使各分部（<c>.Inventory.cs</c> / <c>.Shop.cs</c> / <c>.Crafting.cs</c> / <c>.Equipment.cs</c>）
    /// 调用点不变。导出格式保持不变，仅读写代码所在的类不同。</para>
    /// </summary>
    public static partial class InventoryBinarySerializer
    {
        // 魔数 "INVD"，用于快速校验格式。
        private const int Magic = 0x494E5644;

        /// <summary>可正确解析的最低格式版本（v5 起 AttributeValue 带 curveData，更早的布局已不兼容）。</summary>
        private const int MinReadableVersion = 5;

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

                // v6 追加：其余四个系统的数据块
                WriteInventoryBlock(w, dto);
                WriteShopBlock(w, dto);
                WriteCraftingBlock(w, dto);
                WriteEquipmentBlock(w, dto);
            }
            return stream.ToArray();
        }

        private static void WriteFunctionTag(BinaryWriter w, FunctionTagDto t)
        {
            WriteStr(w, t.name);
            WriteStr(w, t.description);
            WriteArray(w, t.attributes, WriteDefinition);

            // v6 追加：UI 显示配置
            WriteValue(w, t.displayNameText);
            WriteValue(w, t.descriptionText);
            WriteStr(w, t.backgroundSpriteGuid);
            WriteFloatArray(w, t.backgroundColor);
            w.Write(t.hideInUI);
        }

        private static void WriteItemTemplate(BinaryWriter w, ItemTemplateDto t)
        {
            WriteStr(w, t.name);
            WriteArray(w, t.attributes, WriteDefinition);
            WriteStrArray(w, t.tagRefs);

            // v6 追加：色点 + 道具默认值
            WriteFloatArray(w, t.color);
            w.Write(t.weight);
            w.Write(t.stackLimit);
            w.Write(t.hideInInventory);
        }

        private static void WriteItem(BinaryWriter w, ItemDto item)
        {
            WriteStr(w, item.id);
            WriteStr(w, item.templateRef);
            WriteStrArray(w, item.tagRefs);
            WriteEntries(w, item.values);

            // v6 追加：道具本体字段
            w.Write(item.weight);
            w.Write(item.stackLimit);
            w.Write(item.hideInInventory);
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
            // v5 ≤ 文件版本 ≤ 当前版本 的区间由下方的版本判断逐块兼容，无需告警。
            if (version > InventoryDtoMapper.Version)
                Debug.LogWarning($"[InventoryBinarySerializer] 文件版本（{version}）高于当前支持的版本（{InventoryDtoMapper.Version}），尝试按当前格式解析。");
            else if (version < MinReadableVersion)
                Debug.LogWarning($"[InventoryBinarySerializer] 文件版本过旧（{version}，最低支持 {MinReadableVersion}），解析结果可能不正确，建议重新导出。");

            var dto = new InventoryDatabaseDto
            {
                version = version,
                enumTypes = ReadArray(r, ReadEnumType),
                functionTags = ReadArray(r, br => ReadFunctionTag(br, version)),
                itemTemplates = ReadArray(r, br => ReadItemTemplate(br, version)),
                items = ReadArray(r, br => ReadItem(br, version))
            };

            // v6 追加的数据块；更早的文件到此结束，各列表保持为 null（映射层按空处理）。
            if (version >= InventoryDtoMapper.VersionWithAllSystems)
            {
                ReadInventoryBlock(r, dto);
                ReadShopBlock(r, dto);
                ReadCraftingBlock(r, dto);
                ReadEquipmentBlock(r, dto);
                // 更早/旧版本文件在此之后可能还有旧的扩展数据块或数据库级本地化表 GUID（v6/v7）等尾部数据，均未读、被忽略，向后兼容。
            }

            InventoryDtoMapper.FromDto(dto, target, resolver);
        }

        private static FunctionTagDto ReadFunctionTag(BinaryReader r, int version)
        {
            var t = new FunctionTagDto
            {
                name = ReadStr(r),
                description = ReadStr(r),
                attributes = ReadArray(r, ReadDefinition)
            };
            if (version < InventoryDtoMapper.VersionWithAllSystems) return t;

            t.displayNameText      = ReadValue(r);
            t.descriptionText      = ReadValue(r);
            t.backgroundSpriteGuid = ReadStr(r);
            t.backgroundColor      = ReadFloatArray(r);
            t.hideInUI             = r.ReadBoolean();
            return t;
        }

        private static ItemTemplateDto ReadItemTemplate(BinaryReader r, int version)
        {
            var t = new ItemTemplateDto
            {
                name = ReadStr(r),
                attributes = ReadArray(r, ReadDefinition),
                tagRefs = ReadStrArray(r)
            };
            if (version < InventoryDtoMapper.VersionWithAllSystems) return t;

            t.color           = ReadFloatArray(r);
            t.weight          = r.ReadSingle();
            t.stackLimit      = r.ReadInt32();
            t.hideInInventory = r.ReadBoolean();
            return t;
        }

        private static ItemDto ReadItem(BinaryReader r, int version)
        {
            var item = new ItemDto
            {
                id = ReadStr(r),
                templateRef = ReadStr(r),
                tagRefs = ReadStrArray(r),
                values = ReadEntries(r)
            };
            if (version < InventoryDtoMapper.VersionWithAllSystems) return item;

            item.weight          = r.ReadSingle();
            item.stackLimit      = r.ReadInt32();
            item.hideInInventory = r.ReadBoolean();
            return item;
        }

        #endregion

        #region 整理条件读写（field + ascending）——仓库 / 商店 / 制作 / 装备共用

        private static void WriteSortPriorities(BinaryWriter w, SortPriorityDto[] a)
            => WriteArray(w, a, (bw, sp) => { WriteStr(bw, sp.field); bw.Write(sp.ascending); });

        private static SortPriorityDto[] ReadSortPriorities(BinaryReader r)
            => ReadArray(r, br => new SortPriorityDto { field = ReadStr(br), ascending = br.ReadBoolean() });

        #endregion

        #region 基础读写辅助（转发 toolkit 通用编解码 ToolkitBinaryCodec）

        // 属性值 / 定义 / 条目 / 枚举 / 分组标签的紧凑读写与长度前缀数组、字符串、Int/Float/Str 数组等
        // 通用基础读写已下沉到 ToolkitBinaryCodec。下方保留同名 private 转发，使本类各分部与道具块读写的
        // 调用点（含以方法组作为回调传入 WriteArray/ReadArray 的写法）无需改动。

        private static void WriteStr(BinaryWriter w, string s) => ToolkitBinaryCodec.WriteStr(w, s);
        private static string ReadStr(BinaryReader r) => ToolkitBinaryCodec.ReadStr(r);

        private static void WriteValue(BinaryWriter w, AttributeValueDto v) => ToolkitBinaryCodec.WriteValue(w, v);
        private static AttributeValueDto ReadValue(BinaryReader r) => ToolkitBinaryCodec.ReadValue(r);

        private static void WriteDefinition(BinaryWriter w, AttributeDefinitionDto d) => ToolkitBinaryCodec.WriteDefinition(w, d);
        private static AttributeDefinitionDto ReadDefinition(BinaryReader r) => ToolkitBinaryCodec.ReadDefinition(r);

        private static void WriteEntries(BinaryWriter w, AttributeEntryDto[] entries) => ToolkitBinaryCodec.WriteEntries(w, entries);
        private static AttributeEntryDto[] ReadEntries(BinaryReader r) => ToolkitBinaryCodec.ReadEntries(r);

        private static void WriteEnumType(BinaryWriter w, EnumTypeDto e) => ToolkitBinaryCodec.WriteEnumType(w, e);
        private static EnumTypeDto ReadEnumType(BinaryReader r) => ToolkitBinaryCodec.ReadEnumType(r);

        private static void WriteGroupTag(BinaryWriter w, GroupTagDto t) => ToolkitBinaryCodec.WriteGroupTag(w, t);
        private static GroupTagDto ReadGroupTag(BinaryReader r) => ToolkitBinaryCodec.ReadGroupTag(r);

        private static void WriteIntArray(BinaryWriter w, int[] a) => ToolkitBinaryCodec.WriteIntArray(w, a);
        private static int[] ReadIntArray(BinaryReader r) => ToolkitBinaryCodec.ReadIntArray(r);

        private static void WriteFloatArray(BinaryWriter w, float[] a) => ToolkitBinaryCodec.WriteFloatArray(w, a);
        private static float[] ReadFloatArray(BinaryReader r) => ToolkitBinaryCodec.ReadFloatArray(r);

        private static void WriteStrArray(BinaryWriter w, string[] a) => ToolkitBinaryCodec.WriteStrArray(w, a);
        private static string[] ReadStrArray(BinaryReader r) => ToolkitBinaryCodec.ReadStrArray(r);

        private static void WriteArray<T>(BinaryWriter w, T[] a, System.Action<BinaryWriter, T> write)
            => ToolkitBinaryCodec.WriteArray(w, a, write);

        private static T[] ReadArray<T>(BinaryReader r, System.Func<BinaryReader, T> read)
            => ToolkitBinaryCodec.ReadArray(r, read);

        #endregion
    }
}

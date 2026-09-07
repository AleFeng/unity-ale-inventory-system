using System.IO;
using Ale.Effect;
using Ale.GameplayTags;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// 二进制序列化 · 效果系统数据块（v8 追加，位于装备块之后）：效果定义以 Effect System 的 JSON 串承载
    /// （toolkit <see cref="EffectJson"/>，与角色系统同一格式），Gameplay 标签为 名称 + 注释。
    /// </summary>
    public static partial class InventoryBinarySerializer
    {
        private static void WriteEffectBlock(BinaryWriter w, InventoryDatabaseDto dto)
        {
            WriteArray(w, dto.effects, (bw, e) => WriteStr(bw, e != null ? EffectJson.ToJson(e, false) : string.Empty));
            WriteArray(w, dto.gameplayTags, (bw, t) =>
            {
                WriteStr(bw, t != null ? t.name : null);
                WriteStr(bw, t != null ? t.comment : null);
            });
        }

        private static void ReadEffectBlock(BinaryReader r, InventoryDatabaseDto dto)
        {
            dto.effects = ReadArray(r, br =>
            {
                string json = ReadStr(br);
                return string.IsNullOrEmpty(json) ? null : EffectJson.DefinitionFromJson(json);
            });
            dto.gameplayTags = ReadArray(r, br =>
            {
                string name    = ReadStr(br);
                string comment = ReadStr(br);
                return new GameplayTagDefinition(name, comment);
            });
        }
    }
}

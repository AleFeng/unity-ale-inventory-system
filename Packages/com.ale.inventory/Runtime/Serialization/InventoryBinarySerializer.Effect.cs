using System.IO;
using Ale.Effect;
using Ale.GameplayTags;

namespace Ale.Inventory.Runtime.Serialization
{
    /// <summary>
    /// 二进制序列化 · 效果系统数据块（v8 独有，位于装备块之后；v9 起效果外移至 toolkit EffectDatabase、不再写出，只读旧文件）：
    /// 效果定义以 Effect System 的 JSON 串承载（toolkit <see cref="EffectJson"/>），Gameplay 标签为 名称 + 注释。
    /// </summary>
    public static partial class InventoryBinarySerializer
    {
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

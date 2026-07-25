
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

using Ale.Toolkit.Editor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 共享属性 / 杂项绘制器（<c>AttributeFieldDrawer</c>、<c>AttributeDefinition*Drawer</c>、
    /// <c>SortSettingsDrawer</c>、<c>NumberFormatConfigDrawer</c>、<c>InventoryRefListDrawer</c>）的英 / 日译表。
    /// </summary>
    internal static partial class InventoryEditorL10nTables
    {
        static partial void RegisterDrawers()
        {
            // ── 整理设置绘制器 ────────────────────────────────────────────────────
            Add("整理列表（玩家在 UI 中通过下拉菜单选择排序条件）",
                "Sort list (players pick a sort condition from a dropdown in the UI)",
                "整理リスト（プレイヤーが UI のドロップダウンでソート条件を選択）");
            Add("整理优先级（整理列表条件值相同时，依次对比此列表直至值不同）",
                "Sort priority (when the sort-list condition ties, this list is compared in order until values differ)",
                "整理優先度（整理リストの条件値が同じ場合、この一覧を順に比較して値が異なるまで判定）");
            Add("道具 ID", "Item ID", "アイテム ID");
        }
    }
}

using UnityEditor;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 库存插件的编辑器 UI 译表登记入口。三语本地化<b>引擎</b>已下沉到 toolkit 的
    /// <see cref="Ale.Toolkit.Editor.ToolkitEditorL10n"/>；本类留在库存包内，按区域把领域译文登记进
    /// 引擎的同一张表——从而保住原有的「可省略分部方法」机制：各 <c>InventoryEditorL10n.Table.*.cs</c>
    /// 通过实现对应的 <c>RegisterXxx()</c> 分部方法登记本区域译文，未实现的分部方法在编译期被消除，
    /// 可分步增量补充译表而无需改动本文件。
    ///
    /// <para>以 <c>[InitializeOnLoad]</c> 在编辑器加载时即登记（早于任何窗口打开）。分部方法体内通过
    /// <c>using static Ale.Toolkit.Editor.ToolkitEditorL10n;</c> 使裸的 <c>Add(...)</c> / <c>AddEnum(...)</c>
    /// 直接解析到 toolkit 引擎，方法体一字不改。</para>
    /// </summary>
    [InitializeOnLoad]
    internal static partial class InventoryEditorL10nTables
    {
        static InventoryEditorL10nTables()
        {
            RegisterWelcome();
            RegisterFramework();
            RegisterItem();
            RegisterInventory();
            RegisterShop();
            RegisterCrafting();
            RegisterEquipment();
            RegisterSkill();
            RegisterDrawers();
            RegisterEnums();
            RegisterDemo();
            RegisterComponentInspectors();
        }

        // 各区域译表在对应的分部文件中实现；未实现者编译期消除，可增量补充。
        static partial void RegisterWelcome();
        static partial void RegisterFramework();
        static partial void RegisterItem();
        static partial void RegisterInventory();
        static partial void RegisterShop();
        static partial void RegisterCrafting();
        static partial void RegisterEquipment();
        static partial void RegisterSkill();
        static partial void RegisterDrawers();
        static partial void RegisterEnums();
        static partial void RegisterDemo();
        static partial void RegisterComponentInspectors();
    }
}

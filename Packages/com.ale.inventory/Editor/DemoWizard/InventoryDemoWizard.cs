using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.UI;

#if  IS_TMP
using TMPro;
#endif

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;
    /// <summary>
    /// 编辑器向导：一键生成背包系统测试用的 ScriptableObject 资产和 UI Prefab。
    /// 菜单：Tools > InventorySystem > 生成测试 Prefab
    ///
    /// 一键生成（预制体统一命名 PF_(组件类名)，按类型放入 Demo/Assets/UI/Prefab 的子文件夹，
    /// 与 Runtime/UI 各组件所在子目录一致）：
    ///   · Demo/Data/InventoryDatabase.asset                       — 测试道具 + 背包仓库 + 示例商店
    ///   · Demo/Assets/UI/Prefab/Tab/PF_UiwInventoryTab.prefab
    ///   · Demo/Assets/UI/Prefab/Tab/PF_FilterTabBtn.prefab
    ///   · Demo/Assets/UI/Prefab/Tab/PF_UiwShopGroupTab.prefab
    ///   · Demo/Assets/UI/Prefab/Item/PF_UiwInventoryItemSimple.prefab
    ///   · Demo/Assets/UI/Prefab/Item/PF_UiwInventoryItemCell.prefab
    ///   · Demo/Assets/UI/Prefab/Item/PF_UiwInventoryItemPrice.prefab
    ///   · Demo/Assets/UI/Prefab/Item/PF_UiwInventoryItemDetail.prefab
    ///   · Demo/Assets/UI/Prefab/Item/PF_UiwShopItemDetail.prefab
    ///   · Demo/Assets/UI/Prefab/ItemList/PF_UiwInventoryItemOrderList.prefab
    ///   · Demo/Assets/UI/Prefab/ItemList/PF_UiwInventoryItemGridList.prefab
    ///   · Demo/Assets/UI/Prefab/Common/PF_UiwTextLabel.prefab
    ///   · Demo/Assets/UI/Prefab/View/PF_UiwInventoryView.prefab  （独立面板，可直接拖入已有 Canvas）
    ///   · Demo/Assets/UI/Prefab/View/PF_UiwShopView.prefab       （独立商店面板，UiwShopView）
    ///   · Demo/InventoryManager.prefab
    ///        ├── InventoryRuntimeManager（已绑定 InventoryDatabase，含示例商店「杂货商店」）
    ///        └── Canvas > PF_UiwInventoryView + PF_UiwShopView（预配置测试数据）
    ///
    /// 用法：将 InventoryManager.prefab 拖入场景，点击 Play 即自动填入道具并打开背包 UI。
    ///
    /// IS_TMP 宏支持：
    ///   启用 IS_TMP 时，所有文本节点使用 TMPro.TextMeshProUGUI；
    ///   未启用时使用 UnityEngine.UI.Text。
    /// </summary>
    public static class InventoryDemoWizard
    {
        // ── Demo 根目录（动态解析）──────────────────────────────────────────────
        // 1.4.0 迁入 UPM 包后，Demo 不再固定于 Assets/Plugins/InventorySystem/Demo（该目录已不存在）：
        // 本仓库内在 Assets/Demo，而经 Package Manager 导入 Sample 则落在
        // Assets/Samples/<显示名>/<版本>/Demo。故按标志性图片资产反查根目录，不再写死路径。

        /// <summary>反查失败时的回退根目录（此时静态图片会缺失，仅保证生成流程不中断）。</summary>
        private const string DemoRootFallback = "Assets/Demo";

        /// <summary>标志资产：Demo 图片集里位置最稳定的一张，位于 <c>&lt;DemoRoot&gt;/Assets/UI/Image/Quality/</c> 下。</summary>
        private const string DemoMarkerSprite = "T_Quality_Frame_Poor";

        /// <summary>标志资产所在目录相对 Demo 根的层级（Assets / UI / Image / Quality 共 4 级）。</summary>
        private const int DemoMarkerDepth = 4;

        private static string _demoRoot;
        private static bool   _demoRootWarned;   // 回退告警每次域重载只打一条，避免逐次求值刷屏

        /// <summary>
        /// Demo 根目录（资产路径）。按 <see cref="DemoMarkerSprite"/> 反查，同时覆盖
        /// 「仓库内 Assets/Demo」与「Package Manager 导入的 Assets/Samples/…/Demo」两种落地方式；
        /// 查不到时回退 <see cref="DemoRootFallback"/> 并告警。
        /// <para>只缓存**成功**的解析结果，使用户导入 Sample 后无需重编译即可生效。</para>
        /// </summary>
        private static string DemoRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(_demoRoot) && AssetDatabase.IsValidFolder(_demoRoot))
                    return _demoRoot;

                string found = FindDemoRoot();
                if (!string.IsNullOrEmpty(found))
                {
                    _demoRootWarned = false;
                    return _demoRoot = found;
                }

                if (!_demoRootWarned)
                {
                    _demoRootWarned = true;
                    Debug.LogWarning($"[InventoryDemoWizard] 未找到 Demo 标志资产「{DemoMarkerSprite}」，" +
                        $"回退到「{DemoRootFallback}」。生成出的预制体将缺少静态图片——" +
                        "请先在 Package Manager 中导入本包的「Inventory System Demo」样本。");
                }
                return DemoRootFallback;
            }
        }

        /// <summary>按标志图片资产反查 Demo 根目录；未找到返回 null。</summary>
        private static string FindDemoRoot()
        {
            foreach (string guid in AssetDatabase.FindAssets($"{DemoMarkerSprite} t:Texture2D"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // 只认工程 Assets/ 下的资产；FindAssets 是模糊匹配，故文件名需完全一致
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) continue;
                if (System.IO.Path.GetFileNameWithoutExtension(path) != DemoMarkerSprite) continue;

                // 去掉文件名，再上溯 DemoMarkerDepth 级目录即为 Demo 根
                string dir = path;
                for (int i = 0; i <= DemoMarkerDepth; i++)
                {
                    int sep = dir.LastIndexOf('/');
                    if (sep < 0) { dir = null; break; }
                    dir = dir.Substring(0, sep);
                }
                if (!string.IsNullOrEmpty(dir) && AssetDatabase.IsValidFolder(dir)) return dir;
            }
            return null;
        }

        // 文件夹路径（全部基于 DemoRoot 求值，故为属性而非常量）
        // 预制体根目录：按类型分子目录（Tab/ Item/ ItemList/ Tool/ View/ Common/），与 Runtime/UI 保持一致
        private static string PrefabRoot => DemoRoot + "/Assets/UI/Prefab";
        private static string DataDir    => DemoRoot + "/Data"; // 配置数据文件夹

        // 资产路径
        private static string DatabasePath => DataDir  + "/InventoryDatabase.asset";
        private static string ManagerPath  => DemoRoot + "/InventoryManager.prefab"; // 管理器（Demo 入口，非 UI 组件，置于 Demo 根）

        // Demo 内静态精灵路径（由 LoadSprite 加载并赋给对应 Image）
        private static string SpriteBackSphere   => DemoRoot + "/Assets/UI/Image/Background/T_Back_Sphere.png";
        private static string SpriteQualityPoor  => DemoRoot + "/Assets/UI/Image/Quality/T_Quality_Frame_Poor.png";
        private static string SpriteItemGoldCoin => DemoRoot + "/Assets/UI/Image/Item/T_Item_GoldCoin.png";

        // 预制体名称：统一采用 PF_(组件类名) 形式，便于识别与查找；子文件夹见 PrefabSubfolder()
        private const string KPfInventoryTab       = "PF_UiwInventoryTab";          // 仓库页签 UiwInventoryTab          → Tab/
        private const string KPfFilterButton       = "PF_FilterTabBtn";             // 过滤按钮（UiwFilterTabBar 的按钮）  → Tab/
        private const string KPfFoldTab            = "PF_UiwFoldTab";               // 折叠页签 UiwFoldTab（图标 + 文本）  → Tab/
        private const string KPfItemSimple         = "PF_UiwInventoryItemSimple";   // 简易格子 UiwInventoryItemSimple   → Item/
        private const string KPfItemCell           = "PF_UiwInventoryItemCell";     // 网格格子 UiwInventoryItemCell     → Item/
        private const string KPfItemLabel          = "PF_UiwTextLabel";             // 文本标签 UiwTextLabel             → Common/
        private const string KPfItemPrice          = "PF_UiwInventoryItemPrice";    // 价格货币（UiwInventoryItemSimple 变体）→ Item/
        private const string KPfItemDetail         = "PF_UiwInventoryItemDetail";   // 列表格子 UiwInventoryItemDetail   → Item/
        private const string KPfInventoryOrderList = "PF_UiwInventoryItemOrderList";// 顺序道具列表 UiwInventoryItemOrderList → ItemList/
        private const string KPfInventoryGridList  = "PF_UiwInventoryItemGridList"; // 网格道具列表 UiwInventoryItemGridList  → ItemList/
        private const string KPfInventoryPanel     = "PF_UiwInventoryView";         // 仓库面板 UiwInventoryView         → View/
        private const string KPfShopGroupTab       = "PF_UiwShopGroupTab";          // 商店商品组页签 UiwShopGroupTab     → Tab/
        private const string KPfShopItemDetail     = "PF_UiwShopItemDetail";        // 商店商品条目 UiwShopItemDetail     → Item/
        private const string KPfShopPanel          = "PF_UiwShopView";              // 商店面板 UiwShopView              → View/
        private const string KPfItemTooltip            = "PF_UiwItemTooltip";           // 通用道具悬停弹窗 UiwItemTooltip       → Tool/
        private const string KPfNumberCounter          = "PF_UiwNumberCounter";         // 数量计数器 UiwNumberCounter          → Tool/
        private const string KPfCraftingInputCell      = "PF_UiwCraftingInputCell";     // 制作消耗行 UiwCraftingInputCell      → Item/
        private const string KPfCraftingBlueprintCell  = "PF_UiwCraftingBlueprintCell"; // 蓝图条目 UiwCraftingBlueprintCell    → Item/
        private const string KPfCraftingBlueprintList  = "PF_UiwCraftingBlueprintList"; // 蓝图列表 UiwCraftingBlueprintList    → ItemList/
        private const string KPfCraftingView           = "PF_UiwCraftingView";          // 制作主界面 UiwCraftingView          → View/
        private const string KPfEquipSlot              = "PF_UiwEquipmentSlot";         // 装备槽 UiwEquipmentSlot             → Item/
        private const string KPfEquipCandidateCell     = "PF_UiwEquipmentCandidateCell";// 候选道具格子 UiwInventoryItemCell + GridCellDragHandler → Item/
        private const string KPfEquipBonusEntry        = "PF_UiwEquipmentBonusEntry";   // 属性加成条目 UiwEquipmentBonusEntry  → Item/
        private const string KPfEquipSlotList          = "PF_UiwEquipmentSlotList";     // 槽位列表 UiwEquipmentSlotList        → View/
        private const string KPfEquipCandidateList     = "PF_UiwEquipmentCandidateList";// 候选道具列表 UiwEquipmentCandidateList → View/
        private const string KPfEquipGroupPanel        = "PF_UiwEquipmentGroupPanel";   // 装备组面板 UiwEquipmentGroupPanel    → View/
        private const string KPfEquipBonusPanel        = "PF_UiwEquipmentBonusPanel";   // 属性加成面板 UiwEquipmentBonusPanel  → View/
        private const string KPfEquipSelectPanel       = "PF_UiwEquipmentSelectPanel";  // 装备选择面板 UiwEquipmentSelectPanel  → View/
        private const string KPfEquipView              = "PF_UiwEquipmentView";         // 装备主界面 UiwEquipmentView          → View/
        private const string KPfSkillCell              = "PF_UiwSkillCell";             // 技能网格条目 UiwSkillEntry           → Item/
        private const string KPfSkillDetail            = "PF_UiwSkillDetail";           // 技能列表条目 UiwSkillEntry           → Item/
        private const string KPfSkillGridList          = "PF_UiwSkillGridList";         // 技能网格列表 UiwSkillGridList        → ItemList/
        private const string KPfSkillOrderList         = "PF_UiwSkillOrderList";        // 技能顺序列表 UiwSkillOrderList       → ItemList/
        private const string KPfSkillTooltip           = "PF_UiwSkillTooltip";          // 技能悬停弹窗 UiwSkillTooltip         → Tool/
        private const string KPfSkillView              = "PF_UiwSkillView";             // 技能主界面 UiwSkillView             → View/
        private const string KPfInventoryManager       = "InventoryManager";            // 管理器（Demo 入口）
        
        #region 生成项编排

        /// <summary>单个可生成项的描述符（供 WelcomeWindow 列表渲染与单项/全量生成）。</summary>
        public sealed class GenItem
        {
            public string        Key;
            public string        DisplayName;
            public string        AssetPath;
            public string[]      DepKeys;
            public System.Action Build;
            /// <summary>所属子系统分类（用于 WelcomeWindow 分组折叠显示，取值见 <see cref="Categories"/>）。</summary>
            public string        Category;
        }

        // ── 预制体生成分类（WelcomeWindow 按此分组折叠显示，顺序即显示顺序） ──
        public const string CatCommon    = "通用（数据库 / 管理器）";
        public const string CatItem      = "道具系统";
        public const string CatInventory = "仓库系统";
        public const string CatShop      = "商店系统";
        public const string CatCrafting  = "制作系统";
        public const string CatEquipment = "装备系统";
        public const string CatSkill     = "技能系统";

        /// <summary>分类显示顺序（供 WelcomeWindow 遍历分组）。</summary>
        public static readonly string[] Categories =
        {
            CatCommon, CatItem, CatInventory, CatShop, CatCrafting, CatEquipment, CatSkill
        };

        private static List<GenItem> _items;

        /// <summary>全部可生成项（拓扑有序：依赖先于被依赖者）。</summary>
        public static IReadOnlyList<GenItem> Items => _items ?? (_items = BuildItems());

        /// <summary>预制体 → 子文件夹（与 Runtime/UI 中对应组件所在子文件夹保持一致）。</summary>
        private static string PrefabSubfolder(string name)
        {
            switch (name)
            {
                case KPfInventoryTab:
                case KPfShopGroupTab:
                case KPfFilterButton:
                case KPfFoldTab:            return "Tab";
                case KPfItemSimple:
                case KPfItemCell:
                case KPfItemPrice:
                case KPfItemDetail:
                case KPfShopItemDetail:
                case KPfCraftingInputCell:
                case KPfCraftingBlueprintCell:
                case KPfEquipSlot:
                case KPfEquipCandidateCell:
                case KPfEquipBonusEntry:
                case KPfSkillCell:
                case KPfSkillDetail:        return "Item";
                case KPfInventoryOrderList:
                case KPfCraftingBlueprintList:
                case KPfSkillGridList:
                case KPfSkillOrderList:
                case KPfInventoryGridList:  return "ItemList";
                case KPfItemTooltip:
                case KPfSkillTooltip:
                case KPfNumberCounter:      return "Tool";
                case KPfItemLabel:          return "Common";
                case KPfInventoryPanel:
                case KPfShopPanel:
                case KPfEquipSlotList:
                case KPfEquipCandidateList:
                case KPfEquipGroupPanel:
                case KPfEquipBonusPanel:
                case KPfEquipSelectPanel:
                case KPfEquipView:
                case KPfSkillView:
                case KPfCraftingView:       return "View";
                default:                    return string.Empty;
            }
        }

        /// <summary>预制体资产路径：管理器置于 Demo 根，其余按子文件夹放入 <see cref="PrefabRoot"/>。</summary>
        private static string Pfb(string name)
        {
            if (name == KPfInventoryManager) return ManagerPath;
            string sub = PrefabSubfolder(name);
            return string.IsNullOrEmpty(sub)
                ? PrefabRoot + "/" + name + ".prefab"
                : PrefabRoot + "/" + sub + "/" + name + ".prefab";
        }

        private static List<GenItem> BuildItems()
        {
            NumberFormatConfig Fmt() => GetOrCreateNumberFormat();
            return new List<GenItem>
            {
                new GenItem { Category = CatCommon, Key = "DB",       DisplayName = "数据库 InventoryDatabase",         AssetPath = DatabasePath,                  DepKeys = new string[0],
                    Build = () => GetOrCreateDatabase() },
                new GenItem { Category = CatInventory, Key = "Tab",      DisplayName = $"仓库页签 {KPfInventoryTab}",      AssetPath = Pfb(KPfInventoryTab),           DepKeys = new string[0],
                    Build = () => BuildTabPrefab() },
                new GenItem { Category = CatInventory, Key = "Filter",   DisplayName = $"过滤按钮 {KPfFilterButton}",      AssetPath = Pfb(KPfFilterButton),           DepKeys = new string[0],
                    Build = () => BuildFilterButtonPrefab() },
                new GenItem { Category = CatInventory, Key = "FoldTab",  DisplayName = $"折叠页签 {KPfFoldTab}",           AssetPath = Pfb(KPfFoldTab),                DepKeys = new string[0],
                    Build = () => BuildFoldTabPrefab() },
                new GenItem { Category = CatItem, Key = "Simple",   DisplayName = $"简易格子 {KPfItemSimple}",        AssetPath = Pfb(KPfItemSimple),             DepKeys = new string[0],
                    Build = () => BuildItemSimplePrefab(Fmt()) },
                new GenItem { Category = CatItem, Key = "Cell",     DisplayName = $"网格格子 {KPfItemCell}",          AssetPath = Pfb(KPfItemCell),               DepKeys = new string[0],
                    Build = () => BuildItemCellPrefab(Fmt()) },
                new GenItem { Category = CatItem, Key = "Label",    DisplayName = $"功能标签 {KPfItemLabel}",         AssetPath = Pfb(KPfItemLabel),              DepKeys = new string[0],
                    Build = () => BuildItemLabelPrefab() },
                new GenItem { Category = CatItem, Key = "Price",    DisplayName = $"价格货币 {KPfItemPrice}",         AssetPath = Pfb(KPfItemPrice),              DepKeys = new string[0],
                    Build = () => BuildItemPricePrefab(Fmt()) },
                new GenItem { Category = CatItem, Key = "Detail",   DisplayName = $"列表格子 {KPfItemDetail}",        AssetPath = Pfb(KPfItemDetail),             DepKeys = new[] { "Label", "Price" },
                    Build = () => BuildItemDetailPrefab(Fmt(),
                        LoadPrefabComp<UiwTextLabel>(Pfb(KPfItemLabel)),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemPrice))) },
                // 悬停弹窗：预制体由 InventoryManager 持有，运行时全局实例化一次（见 BuildInventoryManagerPrefab）。
                new GenItem { Category = CatItem, Key = "Tooltip",  DisplayName = $"道具悬停弹窗 {KPfItemTooltip}",   AssetPath = Pfb(KPfItemTooltip),            DepKeys = new[] { "Detail" },
                    Build = () => BuildItemTooltipPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfItemDetail))) },
                new GenItem { Category = CatInventory, Key = "ListPanel",DisplayName = $"列表面板 {KPfInventoryOrderList}", AssetPath = Pfb(KPfInventoryOrderList),    DepKeys = new[] { "Detail" },
                    Build = () => BuildInventoryListPanelPrefab(LoadPrefabComp<UiwInventoryItemDetail>(Pfb(KPfItemDetail))) },
                new GenItem { Category = CatInventory, Key = "Grid",     DisplayName = $"网格面板 {KPfInventoryGridList}",  AssetPath = Pfb(KPfInventoryGridList),     DepKeys = new[] { "Cell" },
                    Build = () => BuildInventoryGridPrefab(LoadPrefabComp<UiwInventoryItemCell>(Pfb(KPfItemCell))) },
                new GenItem { Category = CatInventory, Key = "Panel",    DisplayName = $"仓库面板 {KPfInventoryPanel}",    AssetPath = Pfb(KPfInventoryPanel),         DepKeys = new[] { "Tab", "Filter", "Simple", "ListPanel", "Grid" },
                    Build = () => BuildInventoryPanelPrefab(
                        LoadPrefabComp<UiwInventoryTab>(Pfb(KPfInventoryTab)),
                        LoadPrefabComp<Button>(Pfb(KPfFilterButton)),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemSimple)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfInventoryOrderList)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfInventoryGridList))) },
                new GenItem { Category = CatShop, Key = "ShopTab",  DisplayName = $"商店组页签 {KPfShopGroupTab}",     AssetPath = Pfb(KPfShopGroupTab),           DepKeys = new string[0],
                    Build = () => BuildShopGroupTabPrefab() },
                new GenItem { Category = CatShop, Key = "Counter",  DisplayName = $"数量计数器 {KPfNumberCounter}",    AssetPath = Pfb(KPfNumberCounter),          DepKeys = new string[0],
                    Build = () => BuildNumberCounterPrefab() },
                new GenItem { Category = CatShop, Key = "ShopCell", DisplayName = $"商店商品条目 {KPfShopItemDetail}", AssetPath = Pfb(KPfShopItemDetail),  DepKeys = new[] { "Price", "Counter" },
                    Build = () => BuildShopItemDetailPrefab(Fmt(),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemPrice)),
                        LoadPrefabComp<UiwNumberCounter>(Pfb(KPfNumberCounter))) },
                new GenItem { Category = CatShop, Key = "ShopPanel",DisplayName = $"商店面板 {KPfShopPanel}",         AssetPath = Pfb(KPfShopPanel),              DepKeys = new[] { "ShopTab", "ShopCell", "Simple" },
                    Build = () => BuildShopPanelPrefab(
                        LoadPrefabComp<UiwShopGroupTab>(Pfb(KPfShopGroupTab)),
                        LoadPrefabComp<UiwShopItemDetail>(Pfb(KPfShopItemDetail)),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemSimple))) },
                new GenItem { Category = CatCrafting, Key = "CraftInput", DisplayName = $"制作消耗行 {KPfCraftingInputCell}", AssetPath = Pfb(KPfCraftingInputCell),  DepKeys = new string[0],
                    Build = () => BuildCraftingInputCellPrefab(Fmt()) },
                new GenItem { Category = CatCrafting, Key = "CraftCell",  DisplayName = $"蓝图条目 {KPfCraftingBlueprintCell}", AssetPath = Pfb(KPfCraftingBlueprintCell), DepKeys = new[] { "Label" },
                    Build = () => BuildCraftingBlueprintCellPrefab(Fmt(),
                        LoadPrefabComp<UiwTextLabel>(Pfb(KPfItemLabel))) },
                new GenItem { Category = CatCrafting, Key = "CraftList",  DisplayName = $"蓝图列表 {KPfCraftingBlueprintList}", AssetPath = Pfb(KPfCraftingBlueprintList), DepKeys = new[] { "CraftCell" },
                    Build = () => BuildCraftingBlueprintListPrefab(
                        LoadPrefabComp<UiwCraftingBlueprintCell>(Pfb(KPfCraftingBlueprintCell))) },
                new GenItem { Category = CatCrafting, Key = "CraftView",  DisplayName = $"制作主界面 {KPfCraftingView}", AssetPath = Pfb(KPfCraftingView),          DepKeys = new[] { "CraftList", "CraftInput", "Detail", "Simple", "Tab", "FoldTab", "Counter" },
                    Build = () => BuildCraftingViewPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfCraftingBlueprintList)),
                        LoadPrefabComp<UiwCraftingInputCell>(Pfb(KPfCraftingInputCell)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfItemDetail)),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemSimple)),
                        LoadPrefabComp<UiwInventoryTab>(Pfb(KPfInventoryTab)),
                        LoadPrefabComp<UiwFoldTab>(Pfb(KPfFoldTab)),
                        LoadPrefabComp<UiwNumberCounter>(Pfb(KPfNumberCounter))) },
                new GenItem { Category = CatEquipment, Key = "EquipSlot",     DisplayName = $"装备槽 {KPfEquipSlot}",            AssetPath = Pfb(KPfEquipSlot),          DepKeys = new string[0],
                    Build = () => BuildEquipmentSlotPrefab(Fmt()) },
                new GenItem { Category = CatEquipment, Key = "EquipCandidate",DisplayName = $"候选道具格子 {KPfEquipCandidateCell}", AssetPath = Pfb(KPfEquipCandidateCell), DepKeys = new string[0],
                    Build = () => BuildEquipmentCandidateCellPrefab(Fmt()) },
                new GenItem { Category = CatEquipment, Key = "EquipBonusEntry",DisplayName = $"属性加成条目 {KPfEquipBonusEntry}", AssetPath = Pfb(KPfEquipBonusEntry),   DepKeys = new string[0],
                    Build = () => BuildEquipmentBonusEntryPrefab() },
                new GenItem { Category = CatEquipment, Key = "EquipSlotList",     DisplayName = $"槽位列表 {KPfEquipSlotList}",       AssetPath = Pfb(KPfEquipSlotList),      DepKeys = new[] { "EquipSlot" },
                    Build = () => BuildEquipmentSlotListPrefab(LoadPrefabComp<UiwEquipmentSlot>(Pfb(KPfEquipSlot))) },
                new GenItem { Category = CatEquipment, Key = "EquipCandidateList",DisplayName = $"候选道具列表 {KPfEquipCandidateList}", AssetPath = Pfb(KPfEquipCandidateList), DepKeys = new[] { "EquipCandidate" },
                    Build = () => BuildEquipmentCandidateListPrefab(LoadPrefabComp<UiwInventoryItemCell>(Pfb(KPfEquipCandidateCell))) },
                new GenItem { Category = CatEquipment, Key = "EquipGroupPanel",   DisplayName = $"装备组面板 {KPfEquipGroupPanel}",   AssetPath = Pfb(KPfEquipGroupPanel),    DepKeys = new[] { "EquipSlotList" },
                    Build = () => BuildEquipmentGroupPanelPrefab(LoadPrefabComp<UiwEquipmentSlotList>(Pfb(KPfEquipSlotList))) },
                new GenItem { Category = CatEquipment, Key = "EquipBonusPanel",   DisplayName = $"属性加成面板 {KPfEquipBonusPanel}", AssetPath = Pfb(KPfEquipBonusPanel),    DepKeys = new[] { "EquipBonusEntry" },
                    Build = () => BuildEquipmentBonusPanelPrefab(LoadPrefabComp<UiwEquipmentBonusEntry>(Pfb(KPfEquipBonusEntry))) },
                new GenItem { Category = CatEquipment, Key = "EquipSelectPanel",  DisplayName = $"装备选择面板 {KPfEquipSelectPanel}", AssetPath = Pfb(KPfEquipSelectPanel), DepKeys = new[] { "EquipSlotList", "EquipCandidateList" },
                    Build = () => BuildEquipmentSelectPanelPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipSlotList)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipCandidateList))) },
                new GenItem { Category = CatEquipment, Key = "EquipView",         DisplayName = $"装备主界面 {KPfEquipView}",        AssetPath = Pfb(KPfEquipView),          DepKeys = new[] { "EquipGroupPanel", "EquipBonusPanel", "EquipSelectPanel" },
                    Build = () => BuildEquipmentViewPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipGroupPanel)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipBonusPanel)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipSelectPanel))) },
                new GenItem { Category = CatSkill, Key = "SkillCell",   DisplayName = $"技能网格条目 {KPfSkillCell}",   AssetPath = Pfb(KPfSkillCell),   DepKeys = new string[0],
                    Build = () => BuildSkillCellPrefab() },
                new GenItem { Category = CatSkill, Key = "SkillDetail", DisplayName = $"技能列表条目 {KPfSkillDetail}", AssetPath = Pfb(KPfSkillDetail), DepKeys = new string[0],
                    Build = () => BuildSkillDetailPrefab() },
                new GenItem { Category = CatSkill, Key = "SkillGridList", DisplayName = $"技能网格列表 {KPfSkillGridList}", AssetPath = Pfb(KPfSkillGridList), DepKeys = new[] { "SkillCell" },
                    Build = () => BuildSkillGridListPrefab(LoadPrefabComp<UiwSkillEntry>(Pfb(KPfSkillCell))) },
                new GenItem { Category = CatSkill, Key = "SkillOrderList", DisplayName = $"技能顺序列表 {KPfSkillOrderList}", AssetPath = Pfb(KPfSkillOrderList), DepKeys = new[] { "SkillDetail" },
                    Build = () => BuildSkillOrderListPrefab(LoadPrefabComp<UiwSkillEntry>(Pfb(KPfSkillDetail))) },
                new GenItem { Category = CatSkill, Key = "SkillTooltip", DisplayName = $"技能悬停弹窗 {KPfSkillTooltip}", AssetPath = Pfb(KPfSkillTooltip), DepKeys = new string[0],
                    Build = () => BuildSkillTooltipPrefab() },
                new GenItem { Category = CatSkill, Key = "SkillView", DisplayName = $"技能主界面 {KPfSkillView}", AssetPath = Pfb(KPfSkillView), DepKeys = new[] { "SkillGridList", "SkillOrderList", "Filter" },
                    Build = () => BuildSkillViewPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfSkillGridList)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfSkillOrderList)),
                        LoadPrefabComp<Button>(Pfb(KPfFilterButton))) },
                new GenItem { Category = CatCommon, Key = "Manager",  DisplayName = $"管理器 {KPfInventoryManager}",   AssetPath = Pfb(KPfInventoryManager),       DepKeys = new[] { "DB", "Panel", "ShopPanel", "CraftView", "EquipView", "SkillView", "Tooltip", "SkillTooltip" },
                    Build = () => BuildInventoryManagerPrefab(
                        AssetDatabase.LoadAssetAtPath<InventoryDatabase>(DatabasePath),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfInventoryPanel)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfShopPanel)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfCraftingView)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfItemTooltip)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipView)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfSkillView)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfSkillTooltip))) },
            };
        }

        /// <summary>生成全部：统一覆盖确认一次，按拓扑序构建。</summary>
        public static void GenerateAll()
        {
            EnsureFolders();
            var all = new List<GenItem>(Items);
            if (!ConfirmOverwrite(all)) return;
            BuildSubset(all);
            EditorUtility.DisplayDialog("生成完成",
                $"已生成全部资产：\n{DataDir}/\n{PrefabRoot}/\n\n" +
                "将 InventoryManager.prefab 拖入场景，点击 Play 即可验证。", "OK");
        }

        /// <summary>生成单项：依赖型条目询问是否一并生成子项，再覆盖确认。</summary>
        public static void GenerateItem(string key)
        {
            EnsureFolders();
            var item = FindItem(key);
            if (item == null) return;

            var deps     = CollectWithDeps(item);
            var onlyDeps = deps.Where(d => d != item).ToList();

            List<GenItem> toGen;
            if (onlyDeps.Count > 0)
            {
                int c = EditorUtility.DisplayDialogComplex("依赖提示",
                    $"「{item.DisplayName}」依赖以下子项：\n\n" +
                    string.Join("\n", onlyDeps.Select(d => "· " + d.DisplayName)) +
                    "\n\n是否一并生成这些依赖？",
                    "一并生成", "取消", "仅生成此项");
                if (c == 1) return;
                toGen = c == 0 ? deps : new List<GenItem> { item };
                if (c == 2)
                {
                    var missing = onlyDeps.Where(d => !Exists(d)).ToList();
                    if (missing.Count > 0)
                        Debug.LogWarning("[InventoryDemoWizard] 缺少依赖：" +
                            string.Join("，", missing.Select(m => m.DisplayName)) + "，相关引用可能为空。");
                }
            }
            else
            {
                toGen = new List<GenItem> { item };
            }

            if (!ConfirmOverwrite(toGen)) return;
            BuildSubset(toGen);
        }

        private static GenItem FindItem(string key)
        {
            foreach (var i in Items) if (i.Key == key) return i;
            return null;
        }

        private static bool Exists(GenItem it)
            => AssetDatabase.LoadAssetAtPath<Object>(it.AssetPath) != null;

        /// <summary>返回 item 的传递依赖闭包 ∪ 自身，保持 Items 声明序。</summary>
        private static List<GenItem> CollectWithDeps(GenItem it)
        {
            var picked = new HashSet<string>();
            void Visit(GenItem g)
            {
                if (!picked.Add(g.Key)) return;
                foreach (var dk in g.DepKeys)
                {
                    var d = FindItem(dk);
                    if (d != null) Visit(d);
                }
            }
            Visit(it);
            return Items.Where(g => picked.Contains(g.Key)).ToList();
        }

        /// <summary>列出将被覆盖的已存在资产，弹一次确认；无冲突直接放行。</summary>
        private static bool ConfirmOverwrite(IList<GenItem> toGen)
        {
            var existing = toGen.Where(Exists).ToList();
            if (existing.Count == 0) return true;
            string msg = "以下资产已存在，将被覆盖：\n\n" +
                         string.Join("\n", existing.Select(e => "· " + e.DisplayName));
            return EditorUtility.DisplayDialog("覆盖确认", msg, "覆盖", "取消");
        }

        /// <summary>按给定顺序构建（带进度条），末尾保存刷新。</summary>
        private static void BuildSubset(IList<GenItem> toGen)
        {
            try
            {
                for (int i = 0; i < toGen.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("生成测试资产",
                        toGen[i].DisplayName, (float)i / toGen.Count);
                    toGen[i].Build();
                }
                EditorUtility.DisplayProgressBar("生成测试资产", "保存并刷新资产数据库...", 0.97f);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder(DemoRoot);
            EnsureFolder(DataDir);
            EnsureFolder(PrefabRoot);   // 显式建根：PrefabSubfolder 返回空时预制体直接落在此处
            EnsureFolder(PrefabRoot + "/Tab");
            EnsureFolder(PrefabRoot + "/Item");
            EnsureFolder(PrefabRoot + "/ItemList");
            EnsureFolder(PrefabRoot + "/Tool");
            EnsureFolder(PrefabRoot + "/View");
            EnsureFolder(PrefabRoot + "/Common");
        }

        #endregion

        #region 仓库系统 配置数据
        /// <summary>
        /// 创建 仓库系统配置数据。
        /// </summary>
        /// <returns></returns>
        static void GetOrCreateDatabase()
        {
            string path = DatabasePath;
            DeleteIfExists(path);

            var db = ScriptableObject.CreateInstance<InventoryDatabase>();

            // ── 枚举类型 ─────────────────────────────────────────────────────────
            db.AddEnumType("品质", "粗糙", "普通", "优秀", "稀有", "史诗", "传说", "神话");
            db.AddEnumType("部位", "头部", "颈部", "肩部", "胸部", "背部", "腰部", "腿部", "脚部", "手腕", "手部", "饰品", "主手", "副手");
            db.AddEnumType("装备类型", "布甲", "皮甲", "锁甲", "板甲", "其他");
            db.AddEnumType("武器主类型", "近战单手", "近战双手", "远程单手", "远程双手", "法术单手", "法术双手", "副手");
            db.AddEnumType("武器次类型", "拳套", "匕首", "剑", "斧", "锤", "长柄", "法杖", "弓", "弩", "枪", "枪械", "盾", "其他");
            
            // ── 功能标签 ─────────────────────────────────────────────────────────
            db.AddFunctionTag("信息",   "道具的基础信息。");
            db.AddFunctionTag("消耗品",  "可直接使用并消耗的道具，如药水、食物。");
            db.AddFunctionTag("材料",    "用于合成或制作的原材料，不可直接使用。");
            db.AddFunctionTag("装备",    "可穿戴的衣物，用于防护与属性提升。");
            db.AddFunctionTag("武器",    "可持有的武器，用于攻击与属性提升。");
            db.AddFunctionTag("任务物品", "与特定任务相关的关键道具，通常不可丢弃或出售。");
            db.AddFunctionTag("货币",    "用于交易的货币类道具。");
            // ── 功能标签"信息" 属性字段 ─────────────────────────────────────────
            var tagInfo = db.GetTag("信息");
            tagInfo.attributes.Add(new AttributeDefinition("名称", EFieldType.String));
            tagInfo.attributes.Add(new AttributeDefinition("描述", EFieldType.String));
            tagInfo.attributes.Add(new AttributeDefinition("品质", EFieldType.Enum, enumTypeRef: "品质"));
            tagInfo.attributes.Add(new AttributeDefinition("图标", EFieldType.Sprite));
            tagInfo.attributes.Add(new AttributeDefinition("货币ID:价格", EFieldType.StringIntPair, true)); // 价格：货币ID→价格（供商店 priceAttrSource 读取）
            // ── 功能标签"消耗品" 属性字段 ─────────────────────────────────────────
            var tagConsumable = db.GetTag("消耗品");
            tagConsumable.attributes.Add(new AttributeDefinition("效果ID", EFieldType.String));
            tagConsumable.attributes.Add(new AttributeDefinition("效果值", EFieldType.Int));
            // ── 功能标签"材料" 属性字段 ─────────────────────────────────────────
            var tagMaterial = db.GetTag("材料");
            tagMaterial.attributes.Add(new AttributeDefinition("替代材料ID-数量", EFieldType.VectorInt2, true));
            // ── 功能标签"装备" 属性字段 ───────────────────────────────────────────
            var tagEquip = db.GetTag("装备");
            tagEquip.attributes.Add(new AttributeDefinition("部位", EFieldType.Enum, enumTypeRef: "部位"));
            tagEquip.attributes.Add(new AttributeDefinition("装备类型", EFieldType.Enum, enumTypeRef: "装备类型"));
            tagEquip.attributes.Add(new AttributeDefinition("物品等级", EFieldType.Int));  // 装备系统：总加成示例字段
            tagEquip.attributes.Add(new AttributeDefinition("防御力",   EFieldType.Int));
            tagEquip.attributes.Add(new AttributeDefinition("生命值",   EFieldType.Int));
            // ── 功能标签"武器" 属性字段 ───────────────────────────────────────────
            var tagWeapon = db.GetTag("武器");
            tagWeapon.attributes.Add(new AttributeDefinition("部位", EFieldType.Enum, enumTypeRef: "部位"));
            tagWeapon.attributes.Add(new AttributeDefinition("武器主类型", EFieldType.Enum, enumTypeRef: "武器主类型"));
            tagWeapon.attributes.Add(new AttributeDefinition("武器次类型", EFieldType.Enum, enumTypeRef: "武器次类型"));
            tagWeapon.attributes.Add(new AttributeDefinition("物品等级", EFieldType.Int));  // 装备系统：总加成示例字段
            tagWeapon.attributes.Add(new AttributeDefinition("攻击力",   EFieldType.Int));
            // ── 功能标签"任务物品" 属性字段 ───────────────────────────────────────────
            var tagQuest = db.GetTag("任务物品");
            tagQuest.attributes.Add(new AttributeDefinition("任务ID", EFieldType.String));
            // ── 功能标签"货币" 属性字段 ───────────────────────────────────────────
            var tagCurrency = db.GetTag("货币");
            tagCurrency.attributes.Add(new AttributeDefinition("替代货币ID",  EFieldType.String, true));
            tagCurrency.attributes.Add(new AttributeDefinition("替代货币比例",  EFieldType.VectorInt2, true));
            
            // ── 道具模板 ────────────────────────────────────────────────────────
            var tmplConsumable = new ItemTemplate("消耗品")
            {
                color = new Color(0.35f, 0.85f, 0.45f)  // 绿色
            };
            tmplConsumable.tagRefs.Add("信息");
            tmplConsumable.tagRefs.Add("消耗品");
            db.ItemTemplates.Add(tmplConsumable);

            var tmplMaterial = new ItemTemplate("材料")
            {
                color = new Color(0.85f, 0.75f, 0.30f)  // 黄褐色
            };
            tmplMaterial.tagRefs.Add("信息");
            tmplMaterial.tagRefs.Add("材料");
            db.ItemTemplates.Add(tmplMaterial);

            var tmplEquip = new ItemTemplate("装备")
            {
                color = new Color(0.9f, 0.4f, 0.35f)    // 红色
            };
            tmplEquip.tagRefs.Add("信息");
            tmplEquip.tagRefs.Add("装备");
            db.ItemTemplates.Add(tmplEquip);

            var tmplWeapon = new ItemTemplate("武器")
            {
                color = new Color(0.95f, 0.55f, 0.15f)  // 橙色
            };
            tmplWeapon.tagRefs.Add("信息");
            tmplWeapon.tagRefs.Add("武器");
            db.ItemTemplates.Add(tmplWeapon);

            var tmplQuest = new ItemTemplate("任务物品")
            {
                color = new Color(0.70f, 0.35f, 0.90f)  // 紫色
            };
            tmplQuest.tagRefs.Add("信息");
            tmplQuest.tagRefs.Add("任务物品");
            db.ItemTemplates.Add(tmplQuest);

            var tmplCurrency = new ItemTemplate("货币")
            {
                color = new Color(1.00f, 0.85f, 0.10f)  // 金色
            };
            tmplCurrency.tagRefs.Add("信息");
            tmplCurrency.tagRefs.Add("货币");
            db.ItemTemplates.Add(tmplCurrency);
            
            // ── 测试道具 ─────────────────────────────────────────────────
            // 消耗品
            AddItem(db, "治疗药水", "消耗品", weight: 0.1f, stackLimit: 99, goldPrice: 50);
            AddItem(db, "法力药水", "消耗品", weight: 0.1f, stackLimit: 99, goldPrice: 60);
            AddItem(db, "体力药水", "消耗品", weight: 0.1f, stackLimit: 99, goldPrice: 40);
            AddItem(db, "复苏药水", "消耗品", weight: 0.3f, stackLimit: 10, goldPrice: 200);
            AddItem(db, "面包",    "消耗品", weight: 0.5f, stackLimit: 20, goldPrice: 10);
            // 材料
            AddItem(db, "药草",  "材料",   weight: 0.05f, stackLimit: 99);
            AddItem(db, "铁矿",     "材料",   weight: 1.0f,  stackLimit: 50);
            AddItem(db, "秘银矿",   "材料",   weight: 1.5f,  stackLimit: 50);
            AddItem(db, "法力水晶", "材料",   weight: 0.2f,  stackLimit: 99);
            AddItem(db, "旧皮革",   "材料",   weight: 0.8f,  stackLimit: 30);
            // 装备
            AddItem(db, "破布衣", "装备",   weight: 1.0f, stackLimit: 1, goldPrice: 30);
            AddItem(db, "旧皮甲", "装备",   weight: 2.0f, stackLimit: 1, goldPrice: 80);
            AddItem(db, "旧链甲", "装备",   weight: 4.0f, stackLimit: 1, goldPrice: 150);
            AddItem(db, "铁盔",   "装备",   weight: 1.5f, stackLimit: 1, goldPrice: 60);
            AddItem(db, "旧皮鞋", "装备",   weight: 0.8f, stackLimit: 1, goldPrice: 40);
            // 武器
            AddItem(db, "铁剑",    "武器",   weight: 2.5f, stackLimit: 1, goldPrice: 200);
            AddItem(db, "钢剑",    "武器",   weight: 3.0f, stackLimit: 1, goldPrice: 350);
            AddItem(db, "铁斧",    "武器",   weight: 3.5f, stackLimit: 1, goldPrice: 220);
            AddItem(db, "橡木法杖", "武器",   weight: 2.0f, stackLimit: 1, goldPrice: 280);
            AddItem(db, "木弓",    "武器",   weight: 1.5f, stackLimit: 1, goldPrice: 150);
            AddItem(db, "铁匕首",  "武器",   weight: 1.0f, stackLimit: 1, goldPrice: 120);
            // 任务物品
            AddItem(db, "破旧的钥匙", "任务物品", weight: 0.1f, stackLimit: 1);
            AddItem(db, "损坏的卷轴", "任务物品", weight: 0.2f, stackLimit: 1);
            AddItem(db, "奇怪的雕像", "任务物品", weight: 0.5f, stackLimit: 1);
            // 货币
            AddItem(db, "金币", "货币",   weight: 0f, stackLimit: 999);
            AddItem(db, "银币", "货币",   weight: 0f, stackLimit: 999);
            AddItem(db, "铜币", "货币",   weight: 0f, stackLimit: 999);

            // ── 仓库模板 ─────────────────────────────────────────────────────────
            var invTmpl = new InventoryTemplate("背包模板")
            {
                color    = new Color(0.5f, 0.7f, 1.0f),
                capacity = 20,
            };
            invTmpl.filterTagRefs.Add("消耗品");
            invTmpl.filterTagRefs.Add("材料");
            invTmpl.filterTagRefs.Add("装备");
            invTmpl.filterTagRefs.Add("武器");
            invTmpl.filterTagRefs.Add("任务物品");
            invTmpl.filterTagRefs.Add("货币");
            // 整理列表（sortPriorities）：UI 下拉菜单中显示的排序选项
            invTmpl.sortPriorities.Add(new SortPriority("功能标签")); // → __tagOrder__
            invTmpl.sortPriorities.Add(new SortPriority("品质"));
            invTmpl.sortPriorities.Add(new SortPriority("部位"));
            // 整理优先级（sortTiebreakers）：主条件相同时的次级比较顺序
            invTmpl.sortTiebreakers.Add(new SortPriority("功能标签"));
            invTmpl.sortTiebreakers.Add(new SortPriority("品质"));
            invTmpl.sortTiebreakers.Add(new SortPriority("部位"));
            invTmpl.sortTiebreakers.Add(new SortPriority("道具ID")); // → __id__
            db.InventoryTemplates.Add(invTmpl);

            // ── 仓库实例 ─────────────────────────────────────────────────────────
            var backpack = new Inventory("背包", "背包模板")
            {
                capacity = 30,
            };
            backpack.filterTagRefs.Add("消耗品");
            backpack.filterTagRefs.Add("材料");
            backpack.filterTagRefs.Add("装备");
            backpack.filterTagRefs.Add("武器");
            backpack.filterTagRefs.Add("任务物品");
            backpack.filterTagRefs.Add("货币");
            // 整理列表
            backpack.sortPriorities.Add(new SortPriority("功能标签"));
            backpack.sortPriorities.Add(new SortPriority("品质"));
            backpack.sortPriorities.Add(new SortPriority("部位"));
            // 整理优先级
            backpack.sortTiebreakers.Add(new SortPriority("功能标签"));
            backpack.sortTiebreakers.Add(new SortPriority("品质"));
            backpack.sortTiebreakers.Add(new SortPriority("部位"));
            backpack.sortTiebreakers.Add(new SortPriority("道具ID"));
            db.Inventories.Add(backpack);

            // ── 商店 ─────────────────────────────────────────────────────────────
            // 售卖店：用背包中的货币购买；价格取道具「货币ID:价格」属性 × priceMultiplier。
            var shop = new Shop("杂货商店")
            {
                shopType        = ShopType.Sell,
                priceAttrSource = "货币ID:价格",
            };
            shop.displayNameText.SetTextValue(0, "杂货商店");
            shop.tradeInventoryRefs.Add("背包");

            var groupDaily = new ShopCommodityGroup { name = "日常补给" };
            groupDaily.commodities.Add(new ShopCommodity { itemId = "治疗药水", count = 1, tradeLimit = -1 });
            groupDaily.commodities.Add(new ShopCommodity { itemId = "法力药水", count = 1, tradeLimit = -1 });
            groupDaily.commodities.Add(new ShopCommodity { itemId = "体力药水", count = 1, tradeLimit = -1 });
            groupDaily.commodities.Add(new ShopCommodity { itemId = "面包",    count = 5, tradeLimit = 10 });
            shop.groups.Add(groupDaily);

            var groupGear = new ShopCommodityGroup { name = "武器装备" };
            groupGear.commodities.Add(new ShopCommodity { itemId = "铁剑",   count = 1, tradeLimit = 3 });
            groupGear.commodities.Add(new ShopCommodity { itemId = "木弓",   count = 1, tradeLimit = 3 });
            groupGear.commodities.Add(new ShopCommodity { itemId = "旧皮甲", count = 1, tradeLimit = 2 });
            shop.groups.Add(groupGear);

            db.Shops.Add(shop);

            // ── 制作系统：分组标签（主分组 武器/防具/食品/药物 + 副分组 近战/远程/单手/双手）──
            db.CraftingGroupTags.Add(new CraftingGroupTag("武器", "武器") { color = new Color(0.95f, 0.55f, 0.15f) });
            db.CraftingGroupTags.Add(new CraftingGroupTag("防具", "防具") { color = new Color(0.90f, 0.40f, 0.35f) });
            db.CraftingGroupTags.Add(new CraftingGroupTag("食品", "食品") { color = new Color(0.85f, 0.75f, 0.30f) });
            db.CraftingGroupTags.Add(new CraftingGroupTag("药物", "药物") { color = new Color(0.35f, 0.85f, 0.45f) });
            db.CraftingGroupTags.Add(new CraftingGroupTag("近战", "近战") { color = Color.gray });
            db.CraftingGroupTags.Add(new CraftingGroupTag("远程", "远程") { color = Color.gray });
            db.CraftingGroupTags.Add(new CraftingGroupTag("单手", "单手") { color = Color.gray });
            db.CraftingGroupTags.Add(new CraftingGroupTag("双手", "双手") { color = Color.gray });

            // ── 制作系统：蓝图模板（整理设置为模板级；属性显示取主产出道具的「品质」）──
            var craftTmpl = new CraftingBlueprintTemplate("装备制作")
            {
                color         = new Color(0.5f, 0.7f, 1.0f),
                craftTime     = 2f,
                maxCraftCount = -1,
            };
            craftTmpl.craftInventoryRefs.Add("背包");
            craftTmpl.sortPriorities.Add(new SortPriority("品质"));
            craftTmpl.sortPriorities.Add(new SortPriority("道具ID"));
            craftTmpl.sortTiebreakers.Add(new SortPriority("道具ID"));
            craftTmpl.attributeDisplays.Add(new CraftingAttributeDisplay("品质", "品质"));
            db.CraftingBlueprintTemplates.Add(craftTmpl);

            // ── 制作系统：示例蓝图（产出装备 / 消耗材料；制作仓库 = 背包）──
            AddBlueprint(db, "bp_铁剑", "铁剑", "装备制作", "武器", new[] { "近战", "单手" },
                new[] { ("铁剑", 1) }, new[] { ("铁矿", 3) }, 2f);
            AddBlueprint(db, "bp_钢剑", "钢剑", "装备制作", "武器", new[] { "近战", "单手" },
                new[] { ("钢剑", 1) }, new[] { ("秘银矿", 2), ("铁矿", 2) }, 3f);
            AddBlueprint(db, "bp_铁斧", "铁斧", "装备制作", "武器", new[] { "近战", "双手" },
                new[] { ("铁斧", 1) }, new[] { ("铁矿", 4) }, 3f);
            AddBlueprint(db, "bp_木弓", "木弓", "装备制作", "武器", new[] { "远程", "双手" },
                new[] { ("木弓", 1) }, new[] { ("旧皮革", 2) }, 2.5f);
            AddBlueprint(db, "bp_旧皮甲", "旧皮甲", "装备制作", "防具", null,
                new[] { ("旧皮甲", 1), ("旧皮鞋", 1) }, new[] { ("旧皮革", 4) }, 2.5f);
            AddBlueprint(db, "bp_铁盔", "铁盔", "装备制作", "防具", null,
                new[] { ("铁盔", 1) }, new[] { ("铁矿", 2) }, 2f);
            AddBlueprint(db, "bp_治疗药水", "治疗药水", "装备制作", "药物", null,
                new[] { ("治疗药水", 1) }, new[] { ("药草", 2) }, 1.5f);
            AddBlueprint(db, "bp_面包", "面包", "装备制作", "食品", null,
                new[] { ("面包", 2) }, new[] { ("药草", 1) }, 1f);

            // ── 装备系统：分组标签（用于总属性加成的分组显示）──
            db.EquipmentGroupTags.Add(new EquipmentGroupTag("等级",   "等级")   { color = new Color(0.60f, 0.60f, 0.95f) });
            db.EquipmentGroupTags.Add(new EquipmentGroupTag("主属性", "主属性") { color = new Color(0.95f, 0.55f, 0.15f) });
            db.EquipmentGroupTags.Add(new EquipmentGroupTag("副属性", "副属性") { color = new Color(0.35f, 0.85f, 0.45f) });

            // ── 装备系统：装备组模板（承载全部可配置项：槽位列表 + 装备属性字段）──
            var equipTmpl = new EquipmentGroupTemplate("角色装备")
            {
                color = new Color(0.5f, 0.7f, 1.0f),
            };
            equipTmpl.equipmentInventoryRefs.Add("背包");   // 装备仓库：卸下装备时从此列表 Index0 起找第一个放得下的仓库
            // 槽位列表「武器」：限制 功能标签 = 武器；含 主手 / 副手 两个槽位
            var slWeapon = new EquipmentSlotList("weapon_list", "武器");
            slWeapon.requiredTags.Add("武器");
            slWeapon.slots.Add(new EquipmentSlot("slot_mainhand", "主手"));
            slWeapon.slots.Add(new EquipmentSlot("slot_offhand",  "副手"));
            equipTmpl.slotLists.Add(slWeapon);
            // 槽位列表「防具」：限制 功能标签 = 装备；含 头部 / 胸部 / 脚部 三个槽位
            var slArmor = new EquipmentSlotList("armor_list", "防具");
            slArmor.requiredTags.Add("装备");
            slArmor.slots.Add(new EquipmentSlot("slot_head",  "头部"));
            slArmor.slots.Add(new EquipmentSlot("slot_chest", "胸部"));
            slArmor.slots.Add(new EquipmentSlot("slot_feet",  "脚部"));
            equipTmpl.slotLists.Add(slArmor);
            // 装备属性字段列表（总属性加成，按分组标签分组显示）
            equipTmpl.attributeDisplays.Add(new EquipmentAttributeDisplay("物品等级", "等级"));
            equipTmpl.attributeDisplays.Add(new EquipmentAttributeDisplay("攻击力",   "主属性"));
            equipTmpl.attributeDisplays.Add(new EquipmentAttributeDisplay("防御力",   "主属性"));
            equipTmpl.attributeDisplays.Add(new EquipmentAttributeDisplay("生命值",   "副属性"));
            db.EquipmentGroupTemplates.Add(equipTmpl);

            // ── 装备系统：装备组（从模板深拷贝全部配置，仿编辑器「从模板添加」）──
            var equipGroup = new EquipmentGroup("角色装备", "角色装备");
            equipGroup.equipmentInventoryRefs = new List<string>(equipTmpl.equipmentInventoryRefs);
            foreach (var sl in equipTmpl.slotLists)         equipGroup.slotLists.Add(sl.Clone());
            foreach (var ad in equipTmpl.attributeDisplays) equipGroup.attributeDisplays.Add(ad.Clone());
            equipGroup.RebuildAttributes(db);
            db.EquipmentGroups.Add(equipGroup);

            AssetDatabase.CreateAsset(db, path);
        }

        /// <summary>添加 制作蓝图（产出/消耗为 (道具ID, 数量) 元组数组；制作仓库固定为「背包」）。</summary>
        static void AddBlueprint(InventoryDatabase db, string id, string displayName, string template,
            string primaryGroup, string[] secondaryGroups,
            (string id, int count)[] outputs, (string id, int count)[] inputs, float craftTime)
        {
            var bp = new CraftingBlueprint(id, template)
            {
                primaryGroupTag = primaryGroup,
                craftTime       = craftTime,
                maxCraftCount   = -1,
            };
            bp.displayText.SetTextValue(0, displayName);
            if (secondaryGroups != null) bp.secondaryGroupTags.AddRange(secondaryGroups);
            foreach (var o in outputs) bp.outputs.Add(new CraftingItemAmount(o.id, o.count));
            foreach (var i in inputs)  bp.inputs.Add(new CraftingItemAmount(i.id, i.count));
            // 制作仓库 / UI 配置（属性字段显示等）为模板级配置，由 RebuildAttributes 从「装备制作」模板镜像同步。
            bp.RebuildAttributes(db);
            db.CraftingBlueprints.Add(bp);
        }
        
        /// <summary>
        /// 创建 数字格式 配置数据
        /// </summary>
        /// <returns></returns>
        static NumberFormatConfig GetOrCreateNumberFormat()
        {
            var cfg = new NumberFormatConfig();
            var locale = new NumberFormatLocale { languageCode = "" };   // 默认回退语言

            // 后缀现为 Text（纯文本 fallback + 可选本地化引用），构造后写入纯文本值。
            var ruleM = new NumberFormatRule { threshold = 1_000_000, divisor = 1_000_000, decimalPlaces = 1 };
            ruleM.suffixText.SetTextValue(0, "M");
            var ruleK = new NumberFormatRule { threshold = 1_000, divisor = 1_000, decimalPlaces = 1 };
            ruleK.suffixText.SetTextValue(0, "K");
            locale.rules.Add(ruleM);
            locale.rules.Add(ruleK);

            cfg.locales.Add(locale);
            return cfg;
        }

        /// <summary>
        /// 添加 道具数据
        /// </summary>
        /// <param name="db"></param>
        /// <param name="id"></param>
        /// <param name="templateName"></param>
        /// <param name="weight"></param>
        /// <param name="stackLimit"></param>
        /// <param name="goldPrice"></param>
        static void AddItem
        (
            InventoryDatabase db, 
            string id, 
            string templateName,
            float weight = 0f,
            int stackLimit = 0,
            int goldPrice = 0
        )
        {
            var item = new Item(id, templateName) { weight = weight, stackLimit = stackLimit };
            item.RebuildAttributes(db);
            db.Items.Add(item);

            // 设置 属性字段的值
            item.SetAttributeValue("名称", id);
            item.SetAttributeValue("品质", Random.Range(0, 6));
            item.SetAttributeValue("部位", Random.Range(0, 12));
            item.SetAttributeValue("装备类型", Random.Range(0, 4));
            item.SetAttributeValue("武器主类型", Random.Range(0, 6));
            item.SetAttributeValue("武器次类型", Random.Range(0, 12));
            // 装备系统 总加成示例字段（仅在道具具备对应属性时生效，其余 SetAttributeValue 静默跳过）
            item.SetAttributeValue("物品等级", Random.Range(1, 60));
            item.SetAttributeValue("攻击力",   Random.Range(5, 50));
            item.SetAttributeValue("防御力",   Random.Range(3, 30));
            item.SetAttributeValue("生命值",   Random.Range(10, 100));

            // 价格（StringIntPair 数组首元素 金币→goldPrice），供商店 priceAttrSource 读取
            if (goldPrice > 0)
            {
                var priceAv = item.GetAttributeValue("货币ID:价格");
                if (priceAv != null)
                {
                    if (priceAv.Count == 0) priceAv.AddElement();
                    priceAv.SetStringIntPair(0, "金币", goldPrice);
                }
            }
        }
        #endregion
        
        #region 单例预制体 InventoryManager 
        /// <summary>
        /// 构建 InventoryManager 预制体：包含 InventoryRuntimeManager、Canvas，
        /// 以及调用 <see cref="BuildInventoryPanelPrefab"/> 生成并内嵌的 InventoryPanel。
        /// </summary>
        static void BuildInventoryManagerPrefab(InventoryDatabase db, GameObject panelPrefab,
            GameObject shopPanelPrefab, GameObject craftViewPrefab, GameObject tooltipPrefab,
            GameObject equipViewPrefab, GameObject skillViewPrefab, GameObject skillTooltipPrefab)
        {
            string path = Pfb(KPfInventoryManager);
            DeleteIfExists(path);

            // ── Root ──────────────────────────────────────────────────────────
            var root  = NewGameObject(KPfInventoryManager);

            // InventoryRuntimeManager（绑定数据库）
            var mgr     = root.AddComponent<InventoryRuntimeManager>();
            var mgrSo   = new SerializedObject(mgr);
            var dbsProp = mgrSo.FindProperty("databases");
            dbsProp.arraySize = 1;
            dbsProp.GetArrayElementAtIndex(0).objectReferenceValue = db;
            mgrSo.ApplyModifiedPropertiesWithoutUndo();
            if (!db) Debug.LogWarning("[InventoryDemoWizard] 缺少 InventoryDatabase，请先生成「数据库」项。");

            // 编辑器测试数据：写到管理器上，进入 Play 后由 InventoryRuntimeManager.Init 自动向「背包」填入道具（仅填充数据，不打开界面）
            WriteTestData(mgr);

            // ── Canvas ────────────────────────────────────────────────────────
            var canvasGo = ChildGameObject("Canvas", root.transform);
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── 加载已有 PF_InventoryPanel 实例化到 Canvas 下（不再重建面板）──────
            if (panelPrefab)
                PrefabUtility.InstantiatePrefab(panelPrefab, canvasGo.transform);
            else
                Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_InventoryPanel，请先生成「仓库面板」项。");

            // ── 加载 PF_ShopPanel 实例化到 Canvas 下（向右偏移，避免与仓库面板重叠）──
            if (shopPanelPrefab)
            {
                var shopInst = (GameObject)PrefabUtility.InstantiatePrefab(shopPanelPrefab, canvasGo.transform);
                ((RectTransform)shopInst.transform).anchoredPosition = new Vector2(540f, 0f);
            }
            else
                Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_ShopPanel，请先生成「商店面板」项。");

            // ── 加载 PF_UiwCraftingView（向下偏移，避免与上方面板重叠）──────────────
            if (craftViewPrefab)
            {
                var craftInst = (GameObject)PrefabUtility.InstantiatePrefab(craftViewPrefab, canvasGo.transform);
                ((RectTransform)craftInst.transform).anchoredPosition = new Vector2(0f, -40f);
            }
            else
                Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwCraftingView，请先生成「制作主界面」项。");

            // ── 加载 PF_UiwEquipmentView（向左偏移）+ 启动自动打开（背包右键自动装备由视图自订阅，无需接线）──────────
            if (equipViewPrefab)
            {
                var equipInst = (GameObject)PrefabUtility.InstantiatePrefab(equipViewPrefab, canvasGo.transform);
                ((RectTransform)equipInst.transform).anchoredPosition = new Vector2(-560f, 0f);

                var equipView = equipInst.GetComponent<UiwEquipmentView>();
                if (equipView)
                {
                    // 进入 Play 模式自动打开「角色装备」装备组（装备取出 / 卸下放入的仓库取自该装备组配置的「装备仓库」= 背包），便于一键 Demo 验证
                    var evSo = new SerializedObject(equipView);
                    var pAuto = evSo.FindProperty("autoOpenOnStart");
                    var pGrp  = evSo.FindProperty("_groupId");   // 装备组 ID 已并入暴露的序列化字段
                    if (pAuto != null) pAuto.boolValue   = true;
                    if (pGrp  != null) pGrp.stringValue  = "角色装备";
                    evSo.ApplyModifiedPropertiesWithoutUndo();

                    // 背包右键自动装备无需额外接线：UiwEquipmentView 打开时自订阅通用「道具右键」事件即可。
                }
            }
            else
                Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwEquipmentView，请先生成「装备主界面」项。");

            // ── 加载 PF_UiwSkillView（向右下偏移，避免与其它面板重叠）+ 默认「数据库」来源，Play 后自动打开 ──────
            if (skillViewPrefab)
            {
                var skillInst = (GameObject)PrefabUtility.InstantiatePrefab(skillViewPrefab, canvasGo.transform);
                ((RectTransform)skillInst.transform).anchoredPosition = new Vector2(560f, -40f);
            }
            else
                Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillView，请先生成「技能主界面」项。");

            // ── 悬停弹窗（道具 + 技能）：预制体与父 Canvas 配置到管理器，运行时由管理器各自全局实例化一次 ──────
            var tipSo = new SerializedObject(mgr);
            tipSo.FindProperty("itemTooltipPrefab").objectReferenceValue  = tooltipPrefab;
            tipSo.FindProperty("skillTooltipPrefab").objectReferenceValue = skillTooltipPrefab;
            tipSo.FindProperty("tooltipParent").objectReferenceValue      = canvasGo.transform;
            tipSo.ApplyModifiedPropertiesWithoutUndo();
            if (!tooltipPrefab)
                Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwItemTooltip，请先生成「道具悬停弹窗」项。");
            if (!skillTooltipPrefab)
                Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillTooltip，请先生成「技能悬停弹窗」项。");

            // ── 保存主 Prefab ─────────────────────────────────────────────────
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[InventoryDemoWizard] 主 Prefab 已保存：" + path);
        }
        
        /// <summary>
        /// 写入 测试道具列表（写到运行时管理器上，进入 Play 后由管理器 Init 自动填充）。
        /// </summary>
        /// <param name="mgr">目标 <see cref="InventoryRuntimeManager"/>。</param>
        static void WriteTestData(InventoryRuntimeManager mgr)
        {
            var so = new SerializedObject(mgr);

            var autoPopProp = so.FindProperty("autoPopulateOnStart");
            var invIdProp   = so.FindProperty("testInventoryId");
            var itemsProp   = so.FindProperty("testItems");

            if (autoPopProp == null || invIdProp == null || itemsProp == null)
            {
                Debug.LogWarning("[InventoryDemoWizard] 未找到 InventoryRuntimeManager 的测试字段。" +
                                 "请确认 InventoryRuntimeManager.cs 中已加入 #if UNITY_EDITOR 测试块。");
                return;
            }

            autoPopProp.boolValue   = true;
            invIdProp.stringValue   = "背包";   // 与 GetOrCreateDatabase() 中 Inventory.id 保持一致

            // 预填测试道具（覆盖全部6种类型）
            var entries = new (string id, int count)[]
            {
                // 消耗品
                ("治疗药水", 5),
                ("法力药水", 3),
                ("体力药水", 10),
                ("复苏药水", 2),
                ("面包",    8),
                // 材料
                ("药草",    20),
                ("铁矿",    10),
                ("秘银矿",   3),
                ("法力水晶", 15),
                ("旧皮革",   6),
                // 装备
                ("破布衣", 1),
                ("旧皮鞋", 1),
                ("铁盔",  1),
                // 武器
                ("铁剑",    1),
                ("铁斧",    1),
                ("橡木法杖", 1),
                ("木弓",    1),
                // 任务物品
                ("损坏的卷轴", 1),
                ("奇怪的雕像", 1),
                // 货币（金币充足，便于在商店演示购买）
                ("金币",   5000),
                ("银币",   150),
                ("铜币",   600),
            };
            itemsProp.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var elem = itemsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("itemId").stringValue  = entries[i].id;
                elem.FindPropertyRelative("count").intValue   = entries[i].count;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion
        
        #region UI预制体 InventoryPanel
        /// <summary>
        /// 构建并保存 PF_InventoryPanel 独立预制体。
        /// <para>
        /// 面板以根节点形式创建（无 Canvas 父节点），可直接拖入已有 Canvas 使用。
        /// 保存完毕后临时 GameObject 会被销毁；调用方通过加载返回的资产路径获取预制体实例。
        /// </para>
        /// </summary>
        /// <returns>已保存的 PF_InventoryPanel 预制体的资产路径。</returns>
        static void BuildInventoryPanelPrefab(
            UiwInventoryTab        tabPrefab,
            Button                 filterBtnPrefab,
            UiwInventoryItemSimple itemSimplePrefab,
            GameObject             listPanelPrefab,
            GameObject             gridPrefab)
        {
            string panelPath = Pfb(KPfInventoryPanel);
            DeleteIfExists(panelPath);

            // ── 面板根节点（锚定居中，固定尺寸 540×660）─────────────────────────
            var panelGo = NewGameObject(KPfInventoryPanel);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRt.pivot            = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta        = new Vector2(540f, 660f);
            panelRt.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.97f);

            // VerticalLayoutGroup 管理行堆叠
            var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth      = true; vlg.childControlHeight     = true;
            vlg.childForceExpandWidth  = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(6, 6, 6, 6);

            // UiwInventoryView 组件
            var viewComp = panelGo.AddComponent<UiwInventoryView>();

            // ── Row: 标题栏 ───────────────────────────────────────────────────
            var headerRow = MakeRow("Header", panelGo.transform, 40f, Hex("0D0D17"));
            var hHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            hHlg.childControlWidth = true; hHlg.childForceExpandWidth = true;
            hHlg.childControlHeight = true; hHlg.childForceExpandHeight = true;
            hHlg.padding = new RectOffset(10, 10, 0, 0);

            var titleGo = ChildGameObject("TitleText", headerRow.transform);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddText(titleGo,
                    "仓库系统  [编辑器测试 — Play 后自动填入道具]",
                    13, new Color(0.85f, 0.85f, 0.92f),
                    TextAnchor.MiddleLeft, FontStyle.Bold);

            // ── Row: 页签 (tabContainer) ─────────────────────────────────────
            var tabRow = MakeRow("TabRow", panelGo.transform, 50f, new Color(0f, 0f, 0f, 0f));
            var tHlg = tabRow.AddComponent<HorizontalLayoutGroup>();
            tHlg.childControlWidth = false; tHlg.childForceExpandWidth = false;
            tHlg.childControlHeight = true; tHlg.childForceExpandHeight = true;
            tHlg.spacing = 3f; tHlg.padding = new RectOffset(4, 4, 2, 2);
            viewComp.tabContainer = tabRow.transform;
            viewComp.tabPrefab    = tabPrefab;

            // ── Row: 过滤按钮 ─────────────────────────────────────────────────
            var filterRow = MakeRow("FilterRow", panelGo.transform, 30f, new Color(0f, 0f, 0f, 0f));
            var fHlg = filterRow.AddComponent<HorizontalLayoutGroup>();
            fHlg.childControlWidth = false; fHlg.childForceExpandWidth = false;
            fHlg.childControlHeight = true; fHlg.childForceExpandHeight = true;
            fHlg.spacing = 4f; fHlg.padding = new RectOffset(4, 4, 2, 2);

            // FilterContainer（弹性撑满）
            var filterContGo = ChildGameObject("FilterContainer", filterRow.transform);
            filterContGo.AddComponent<RectTransform>();
            filterContGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var fcHlg = filterContGo.AddComponent<HorizontalLayoutGroup>();
            fcHlg.childControlWidth = false; fcHlg.childForceExpandWidth = false;
            fcHlg.childControlHeight = true; fcHlg.childForceExpandHeight = true;
            fcHlg.spacing = 2f;
            // 过滤页签栏组件（通用 UiwFilterTabBar）
            var filterBar = filterContGo.AddComponent<UiwFilterTabBar>();
            filterBar.filterContainer    = filterContGo.transform;
            filterBar.filterButtonPrefab = filterBtnPrefab;
            viewComp.filterBar = filterBar;

            // ── Row: 排序（下拉 + 升降序 + 整理） ─────────────────────────────
            var sortRow = MakeRow("SortRow", panelGo.transform, 30f, new Color(0f, 0f, 0f, 0f));
            var srHlg = sortRow.AddComponent<HorizontalLayoutGroup>();
            srHlg.childControlWidth      = true;
            srHlg.childForceExpandWidth  = false;
            srHlg.childControlHeight     = true;
            srHlg.childForceExpandHeight = true;
            srHlg.spacing = 4f; srHlg.padding = new RectOffset(4, 4, 2, 2);

            // SortDropdown（弹性撑满左侧）
            // 选项来自仓库"整理列表"（sortPriorities），运行时由 UiwInventoryView.BuildSortDropdown 填充
            var sortDd = MakeDropdown("SortDropdown", sortRow.transform);
            sortDd.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // SortDirection 按钮（固定宽度，点击切换"升序"/"降序"）
            var sdBtnGo = ChildGameObject("SortDirectionButton", sortRow.transform);
            sdBtnGo.AddComponent<RectTransform>();
            var sdLe = sdBtnGo.AddComponent<LayoutElement>();
            sdLe.minWidth = 54f; sdLe.preferredWidth = 54f; sdLe.flexibleWidth = 0f;
            var sdImg = sdBtnGo.AddComponent<Image>();
            sdImg.color = Hex("2A2A4A");
            var sdBtn = sdBtnGo.AddComponent<Button>();
            sdBtn.targetGraphic = sdImg;
            SetButtonColors(sdBtn, Hex("2A2A4A"), Hex("3A3A6A"), Hex("1E1E36"));
            var sdLblGo = ChildGameObject("Label", sdBtnGo.transform);
            Stretch(sdLblGo.AddComponent<RectTransform>());
            var sdLblComp = AddText(sdLblGo, "升序", 11, Color.white);

            // AutoSort 按钮（固定宽度）
            var asBtnGo = ChildGameObject("AutoSortButton", sortRow.transform);
            asBtnGo.AddComponent<RectTransform>();
            var asLe = asBtnGo.AddComponent<LayoutElement>();
            asLe.minWidth = 54f; asLe.preferredWidth = 54f; asLe.flexibleWidth = 0f;
            var asImg = asBtnGo.AddComponent<Image>();
            asImg.color = Hex("2A4A2A");
            var asBtn = asBtnGo.AddComponent<Button>();
            asBtn.targetGraphic = asImg;
            SetButtonColors(asBtn, Hex("2A4A2A"), Hex("3A6A3A"), Hex("1E361E"));
            var asLblGo = ChildGameObject("Label", asBtnGo.transform);
            Stretch(asLblGo.AddComponent<RectTransform>());
            AddText(asLblGo, "整理", 11, Color.white);

            // 排序整理栏组件（通用 UiwSortToolbar：下拉 + 升降序 + 自动整理）
            var sortTb = sortRow.AddComponent<UiwSortToolbar>();
            sortTb.sortDropdown        = sortDd;
            sortTb.sortDirectionButton = sdBtn;
            SetSerializedRef(sortTb, "sortDirectionLabel", sdLblComp);
            sortTb.autoSortButton      = asBtn;
            viewComp.sortToolbar = sortTb;

            // ViewToggle 按钮（列表 / 网格 切换，固定宽度）
            var vtBtnGo = ChildGameObject("ViewToggleButton", sortRow.transform);
            vtBtnGo.AddComponent<RectTransform>();
            var vtLe = vtBtnGo.AddComponent<LayoutElement>();
            vtLe.minWidth = 54f; vtLe.preferredWidth = 54f; vtLe.flexibleWidth = 0f;
            var vtImg = vtBtnGo.AddComponent<Image>();
            vtImg.color = Hex("2A2A4A");
            var vtBtn = vtBtnGo.AddComponent<Button>();
            vtBtn.targetGraphic = vtImg;
            SetButtonColors(vtBtn, Hex("2A2A4A"), Hex("3A3A6A"), Hex("1E1E36"));
            var vtLblGo = ChildGameObject("Label", vtBtnGo.transform);
            Stretch(vtLblGo.AddComponent<RectTransform>());
            var vtLblComp = AddText(vtLblGo, "网格", 11, Color.white);   // 当前为列表模式 → 按钮显示"网格"
            viewComp.viewModeToggleButton = vtBtn;
            SetSerializedRef(viewComp, "viewModeToggleLabel", vtLblComp);

            // ── Row: 货币栏（CurrencyContainer；货币 ID 对应脚本数据库定义）──────
            var currencyRow = MakeRow("CurrencyRow", panelGo.transform, 24f, new Color(0f, 0f, 0f, 0f));
            var crHlg = currencyRow.AddComponent<HorizontalLayoutGroup>();
            crHlg.childControlWidth = false; crHlg.childForceExpandWidth = false;
            crHlg.childControlHeight = true; crHlg.childForceExpandHeight = true;
            crHlg.childAlignment = TextAnchor.MiddleLeft;
            crHlg.spacing = 4f; crHlg.padding = new RectOffset(4, 4, 2, 2);

            var currencyContGo = ChildGameObject("CurrencyContainer", currencyRow.transform);
            currencyContGo.AddComponent<RectTransform>();
            currencyContGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var ccHlg = currencyContGo.AddComponent<HorizontalLayoutGroup>();
            ccHlg.childControlWidth = false; ccHlg.childForceExpandWidth = false;
            ccHlg.childControlHeight = true; ccHlg.childForceExpandHeight = true;
            ccHlg.spacing = 4f;
            // 货币栏组件（通用 UiwCurrencyBar）
            var currencyBar = currencyContGo.AddComponent<UiwCurrencyBar>();
            currencyBar.currencyContainer = currencyContGo.transform;
            currencyBar.currencyPrefab    = itemSimplePrefab;
            currencyBar.currencyItemIds   = new[] { "金币", "银币", "铜币" };
            viewComp.currencyBar = currencyBar;

            // ── 道具区（弹性占满）：内嵌 列表面板 + 网格面板 实例 ─────────────────
            var itemAreaGo = ChildGameObject("ItemArea", panelGo.transform);
            itemAreaGo.AddComponent<RectTransform>();
            var areaLe = itemAreaGo.AddComponent<LayoutElement>();
            areaLe.flexibleHeight = 1f; areaLe.preferredHeight = 9000f;

            if (listPanelPrefab)
            {
                var listInst = (GameObject)PrefabUtility.InstantiatePrefab(listPanelPrefab, itemAreaGo.transform);
                Stretch((RectTransform)listInst.transform);
                viewComp.itemOrderList = listInst.GetComponent<UiwInventoryItemOrderList>();
            }
            if (gridPrefab)
            {
                var gridInst = (GameObject)PrefabUtility.InstantiatePrefab(gridPrefab, itemAreaGo.transform);
                Stretch((RectTransform)gridInst.transform);
                gridInst.SetActive(false);   // 默认列表模式，由 UiwInventoryView.ApplyViewMode 校正
                viewComp.itemGridList = gridInst.GetComponent<UiwInventoryItemGridList>();
            }

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(panelGo);
#endif

            MovePrimaryUiwToTop(panelGo);
            PrefabUtility.SaveAsPrefabAsset(panelGo, panelPath);
            Object.DestroyImmediate(panelGo);   // 临时 GO 使命完成，立即销毁
            Debug.Log("[InventoryDemoWizard] 面板 Prefab 已保存：" + panelPath);
        }
        #endregion
        
        #region UI预制体 页签
        /// <summary>
        /// 仓库页签，在多个仓库之间进行切换。
        /// </summary>
        /// <returns></returns>
        static void BuildTabPrefab()
        {
            string path = Pfb(KPfInventoryTab);
            DeleteIfExists(path);

            // root: Button + Image + UiwInventoryTab
            var root = NewGameObject(KPfInventoryTab);
            SetRectSize(root.AddComponent<RectTransform>(), 108, 36);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = Hex("2D3148");
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            SetButtonColors(btn, Hex("2D3148"), Hex("3D4268"), Hex("22253A"));
            var tab = root.AddComponent<UiwInventoryTab>();

            // Label
            var labelGo = ChildGameObject("Label", root.transform);
            Stretch(labelGo.AddComponent<RectTransform>());
            var labelTxt = AddText(labelGo, "仓库", 14, Color.white);
            SetSerializedRef(tab, "label", labelTxt);   // 兼容 IS_TMP 时改类型

            // SelectedIndicator (底部高亮横条，默认隐藏)
            var selGo = ChildGameObject("SelectedIndicator", root.transform);
            var selRt = selGo.AddComponent<RectTransform>();
            selRt.anchorMin = new Vector2(0f, 0f);
            selRt.anchorMax = new Vector2(1f, 0f);
            selRt.pivot = new Vector2(0.5f, 0f);
            selRt.sizeDelta = new Vector2(0f, 3f);
            selRt.anchoredPosition = Vector2.zero;
            selGo.AddComponent<Image>().color = Hex("66CCFF");
            selGo.SetActive(false);
            tab.selectedIndicator = selGo;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }
        
        /// <summary>
        /// 过滤按钮，适用于 InventoryView 的过滤按钮。
        /// </summary>
        /// <returns></returns>
        static void BuildFilterButtonPrefab()
        {
            string path = Pfb(KPfFilterButton);
            DeleteIfExists(path);

            var root = NewGameObject(KPfFilterButton);
            SetRectSize(root.AddComponent<RectTransform>(), 72, 28);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = Hex("292936");
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            SetButtonColors(btn, Hex("292936"), Hex("3A3A55"), Hex("1E1E2C"));

            var labelGo = ChildGameObject("Label", root.transform);
            Stretch(labelGo.AddComponent<RectTransform>());
            AddText(labelGo, "全部", 12, Color.white);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>
        /// 构建 PF_UiwFoldTab（通用折叠页签：可点击 Button，横向 [左侧图标 Image][右侧名称 Text]，<see cref="UiwFoldTab"/>）。
        /// 默认隐藏左侧图标（宿主按需 SetIcon 显示），供 <see cref="UiwCraftingGroupFilter"/> 等以实例化方式复用。
        /// </summary>
        static void BuildFoldTabPrefab()
        {
            string path = Pfb(KPfFoldTab);
            DeleteIfExists(path);

            // 根：可点击页签（背景图 + Button），横向布局 [图标][文本]
            var root = NewGameObject(KPfFoldTab);
            SetRectSize(root.AddComponent<RectTransform>(), 172f, 26f);
            SetLayoutElement(root, minH: 24, prefH: 24);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = Hex("292936");
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            SetButtonColors(btn, Hex("292936"), Hex("3A3A55"), Hex("1E1E2C"));
            SetHlg(root, new RectOffset(4, 4, 0, 0), 2f, TextAnchor.MiddleLeft, true, true, false, true);

            var foldTab = root.AddComponent<UiwFoldTab>();
            foldTab.button = btn;

            // 左侧折叠 / 自定义图标（默认隐藏；宿主通过 UiwFoldTab.SetIcon 显示）
            var iconGo = ChildGameObject("Icon", root.transform);
            iconGo.AddComponent<RectTransform>();
            SetLayoutElement(iconGo, minW: 16, prefW: 16, minH: 16, prefH: 16);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;   // 不拦截按钮点击
            iconImg.preserveAspect = true;
            foldTab.icon = iconImg;
            iconGo.SetActive(false);

            // 右侧名称文本
            var labelGo = ChildGameObject("Label", root.transform);
            labelGo.AddComponent<RectTransform>();
            SetLayoutElement(labelGo, flexW: 1);
            var labelTxt = AddText(labelGo, "全部", 12, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(foldTab, "label", labelTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }
        #endregion

        #region UI预制体 商店（ShopGroupTab / ShopItemDetail / ShopPanel）

        /// <summary>构建 PF_ShopGroupTab（商店商品组页签：Button + UiwShopGroupTab，对齐 PF_InventoryTab）。</summary>
        static void BuildShopGroupTabPrefab()
        {
            string path = Pfb(KPfShopGroupTab);
            DeleteIfExists(path);

            var root = NewGameObject(KPfShopGroupTab);
            SetRectSize(root.AddComponent<RectTransform>(), 96, 32);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = Hex("2D3148");
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            SetButtonColors(btn, Hex("2D3148"), Hex("3D4268"), Hex("22253A"));
            var tab = root.AddComponent<UiwShopGroupTab>();

            var labelGo = ChildGameObject("Label", root.transform);
            Stretch(labelGo.AddComponent<RectTransform>());
            var labelTxt = AddText(labelGo, "全部", 13, Color.white);
            SetSerializedRef(tab, "label", labelTxt);

            var selGo = ChildGameObject("SelectedIndicator", root.transform);
            var selRt = selGo.AddComponent<RectTransform>();
            selRt.anchorMin = new Vector2(0f, 0f); selRt.anchorMax = new Vector2(1f, 0f);
            selRt.pivot = new Vector2(0.5f, 0f); selRt.sizeDelta = new Vector2(0f, 3f);
            selRt.anchoredPosition = Vector2.zero;
            selGo.AddComponent<Image>().color = Hex("FFCC55");
            selGo.SetActive(false);
            tab.selectedIndicator = selGo;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>
        /// 构建 PF_UiwNumberCounter（通用「数量计数器」：- [值] + 一行，由 <see cref="UiwNumberCounter"/> 统一驱动
        /// +/- 步进、长按连发与数值显示）。商店条目、制作详情等界面通过实例化本预制体复用「调数量」功能。
        /// </summary>
        static void BuildNumberCounterPrefab()
        {
            string path = Pfb(KPfNumberCounter);
            DeleteIfExists(path);

            // 根：横向一行（- 值 +），并带 LayoutElement 便于嵌入父级布局组时给出自然尺寸
            var root = NewGameObject(KPfNumberCounter);
            SetRectSize(root.AddComponent<RectTransform>(), 104f, 28f);
            SetLayoutElement(root, minW: 104, prefW: 104, minH: 28, prefH: 28);
            SetHlg(root, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.MiddleCenter,
                true, true, false, true);

            var counter = root.AddComponent<UiwNumberCounter>();

            // 减少按钮
            var minusBtn = MakeMiniButton("MinusButton", root.transform, "-", Hex("3A2A2A"), Hex("5A3A3A"), Hex("2A1E1E"));
            SetLayoutElement(minusBtn.gameObject, minW: 28, prefW: 28, minH: 28, prefH: 28);
            counter.minusButton = minusBtn;

            // 当前值文本（可在生成后另行挂接 InputField 以支持键入）
            var valueGo = ChildGameObject("ValueText", root.transform);
            valueGo.AddComponent<RectTransform>();
            SetLayoutElement(valueGo, minW: 40, prefW: 40, flexW: 1);
            var valueTxt = AddText(valueGo, "0", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetSerializedRef(counter, "valueText", valueTxt);

            // 增加按钮
            var plusBtn = MakeMiniButton("PlusButton", root.transform, "+", Hex("2A3A2A"), Hex("3A5A3A"), Hex("1E2A1E"));
            SetLayoutElement(plusBtn.gameObject, minW: 28, prefW: 28, minH: 28, prefH: 28);
            counter.plusButton = plusBtn;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_ShopItemDetail（商店商品条目：品质背景 + 图标 + 名称/单价 + 剩余 + -/次数/+，UiwShopItemDetail）。</summary>
        static void BuildShopItemDetailPrefab(NumberFormatConfig numFmt, UiwInventoryItemSimple pricePrefab,
            UiwNumberCounter counterPrefab)
        {
            string path = Pfb(KPfShopItemDetail);
            DeleteIfExists(path);

            var root = NewGameObject(KPfShopItemDetail);
            SetRectSize(root.AddComponent<RectTransform>(), 460f, 56f);
            SetLayoutElement(root, minH: 56, prefH: 56);
            var rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(0.14f, 0.14f, 0.20f, 0.85f);
            var cg = root.AddComponent<CanvasGroup>();
            SetHlg(root, new RectOffset(6, 6, 4, 4), 6f, TextAnchor.MiddleLeft, 
                true, true, false, false);

            var cell = root.AddComponent<UiwShopItemDetail>();
            cell.iconAttrId   = "图标";
            cell.nameAttrId   = "名称";
            cell.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            cell.priceCurrencyPrefab = pricePrefab;
            cell.interactableGroup   = cg;

            // 图标 + 品质背景框（IconFrame 占据 HLG 槽位，内含品质底图 + 图标）
            var frameGo = ChildGameObject("IconFrame", root.transform);
            frameGo.AddComponent<RectTransform>();
            SetLayoutElement(frameGo, minW: 44, prefW: 44, minH: 44, prefH: 44);

            var qualityGo = ChildGameObject("QualityBackground", frameGo.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = Color.white; qualityImg.preserveAspect = true;
            qualityImg.sprite = LoadSprite(SpriteQualityPoor);
            cell.qualityBackground = qualityImg;

            var iconGo = ChildGameObject("Icon", frameGo.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4f, 4f); iconRt.offsetMax = new Vector2(-4f, -4f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true;
            cell.iconImage = iconImg;

            // 信息列（名称 + 单价）
            var infoGo = ChildGameObject("Info", root.transform);
            infoGo.AddComponent<RectTransform>();
            SetLayoutElement(infoGo, flexW: 1, prefH: 43);

            var nameGo = ChildGameObject("NameText", infoGo.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 18, prefH: 18);
            var nameTxt = AddText(nameGo, "商品名称", 13, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(cell, "nameText", nameTxt);

            var priceGo = ChildGameObject("PriceContainer", infoGo.transform);
            var priceRt = priceGo.AddComponent<RectTransform>();
            // 定位到（父级 Info）RectTransform 的左下角
            priceRt.anchorMin        = new Vector2(0f, 0f);
            priceRt.anchorMax        = new Vector2(0f, 0f);
            priceRt.pivot            = new Vector2(0f, 0f);
            priceRt.anchoredPosition = Vector2.zero;
            priceRt.sizeDelta        = new Vector2(200f, 16f);
            SetHlg(priceGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleLeft, false, true, false, true);
            cell.priceContainer = priceGo.transform;

            // 每次交易数量（×N）
            var countGo = ChildGameObject("CountText", root.transform);
            countGo.AddComponent<RectTransform>();
            SetLayoutElement(countGo, minW: 28, prefW: 28);
            var countTxt = AddText(countGo, "×1", 11, new Color(0.7f, 0.8f, 0.9f));
            SetSerializedRef(cell, "countText", countTxt);

            // 剩余可交易次数
            var remGo = ChildGameObject("RemainingText", root.transform);
            remGo.AddComponent<RectTransform>();
            SetLayoutElement(remGo, minW: 64, prefW: 64);
            var remTxt = AddText(remGo, "剩余 ∞", 11, new Color(0.7f, 0.8f, 0.9f));
            SetSerializedRef(cell, "remainingText", remTxt);

            // 数量计数器（实例化通用 PF_UiwNumberCounter，统一驱动 +/- 与显示）
            if (counterPrefab)
            {
                var counterInst = (UiwNumberCounter)PrefabUtility.InstantiatePrefab(counterPrefab, root.transform);
                cell.counter = counterInst;
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwNumberCounter，商店条目无法调整交易次数。");

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_ShopPanel（商店主界面：标题 + 商品组页签 + 货币栏 + 商品列表 + 结算栏，UiwSellShopView）。</summary>
        static void BuildShopPanelPrefab(UiwShopGroupTab groupTabPrefab,
            UiwShopItemDetail cellPrefab, UiwInventoryItemSimple itemSimplePrefab)
        {
            string path = Pfb(KPfShopPanel);
            DeleteIfExists(path);

            // 面板根（居中，480×620）
            var panelGo = NewGameObject(KPfShopPanel);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480f, 620f);
            panelRt.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.97f);

            var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;  vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 4f; vlg.padding = new RectOffset(6, 6, 6, 6);

            // 商店视图已按类型拆分；Demo 生成售卖店视图（杂货商店为售卖类型）。
            // 其它类型请在对应预制体上改挂 UiwRecycleShopView / UiwBarterShopView。
            var view = panelGo.AddComponent<UiwSellShopView>();

            // 标题
            var headerRow = MakeRow("Header", panelGo.transform, 40f, Hex("0D0D17"));
            SetHlg(headerRow, new RectOffset(10, 10, 0, 0), 0f, TextAnchor.MiddleLeft, true, true, true, true);
            var titleGo = ChildGameObject("TitleText", headerRow.transform);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var titleTxt = AddText(titleGo, "商店  [编辑器测试 — Play 后自动打开]", 13,
                new Color(0.85f, 0.85f, 0.92f), TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(view, "titleLabel", titleTxt);

            // 商品组页签
            var tabRow = MakeRow("GroupTabRow", panelGo.transform, 38f, new Color(0f, 0f, 0f, 0f));
            SetHlg(tabRow, new RectOffset(4, 4, 2, 2), 3f, TextAnchor.MiddleLeft, false, true, false, true);
            view.groupTabContainer = tabRow.transform;
            view.groupTabPrefab    = groupTabPrefab;

            // 货币栏
            var currencyRow = MakeRow("CurrencyRow", panelGo.transform, 24f, new Color(0f, 0f, 0f, 0f));
            SetHlg(currencyRow, new RectOffset(4, 4, 2, 2), 4f, TextAnchor.MiddleLeft, false, true, false, true);
            var currencyContGo = ChildGameObject("CurrencyContainer", currencyRow.transform);
            currencyContGo.AddComponent<RectTransform>();
            currencyContGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            SetHlg(currencyContGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleLeft, false, true, false, true);
            var currencyBar = currencyContGo.AddComponent<UiwCurrencyBar>();
            currencyBar.currencyContainer = currencyContGo.transform;
            currencyBar.currencyPrefab    = itemSimplePrefab;
            currencyBar.currencyItemIds   = new[] { "金币", "银币", "铜币" };
            view.currencyBar = currencyBar;

            // 商品列表（垂直滚动）
            var areaGo = ChildGameObject("CommodityArea", panelGo.transform);
            areaGo.AddComponent<RectTransform>();
            var areaLe = areaGo.AddComponent<LayoutElement>();
            areaLe.flexibleHeight = 1f; areaLe.preferredHeight = 9000f;
            areaGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            var sr = areaGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;

            var vpGo = ChildGameObject("Viewport", areaGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;

            sr.viewport = vpRt; sr.content = contentRt;

            // 商品列表（虚拟滚动 UiwShopCommodityList；条目手动定位，不挂 VerticalLayoutGroup / SizeFitter）
            var commodityList = areaGo.AddComponent<UiwShopCommodityList>();
            commodityList.cellPrefab  = cellPrefab;
            commodityList.bufferCount = 1;
            commodityList.scrollRect  = sr;
            commodityList.content     = contentRt;
            view.commodityList = commodityList;

            // 结算栏（总价 + 结算按钮）
            var footerRow = MakeRow("FooterRow", panelGo.transform, 40f, Hex("0D0D17"));
            SetHlg(footerRow, new RectOffset(10, 10, 2, 2), 6f, TextAnchor.MiddleLeft, true, true, false, true);
            var totalGo = ChildGameObject("TotalText", footerRow.transform);
            totalGo.AddComponent<RectTransform>();
            totalGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var totalTxt = AddText(totalGo, "总价：0", 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(view, "totalLabel", totalTxt);

            var settleGo = ChildGameObject("SettleButton", footerRow.transform);
            settleGo.AddComponent<RectTransform>();
            var settleLe = settleGo.AddComponent<LayoutElement>();
            settleLe.minWidth = 72f; settleLe.preferredWidth = 72f;
            var settleImg = settleGo.AddComponent<Image>();
            settleImg.color = Hex("2A4A2A");
            var settleBtn = settleGo.AddComponent<Button>();
            settleBtn.targetGraphic = settleImg;
            SetButtonColors(settleBtn, Hex("2A4A2A"), Hex("3A6A3A"), Hex("1E361E"));
            var settleLblGo = ChildGameObject("Label", settleGo.transform);
            Stretch(settleLblGo.AddComponent<RectTransform>());
            AddText(settleLblGo, "结算", 13, Color.white);
            view.settleButton = settleBtn;

            // 提示行
            var hintRow = MakeRow("HintRow", panelGo.transform, 20f, new Color(0f, 0f, 0f, 0f));
            SetHlg(hintRow, new RectOffset(10, 10, 0, 0), 0f, TextAnchor.MiddleLeft, true, true, true, true);
            var hintGo = ChildGameObject("HintText", hintRow.transform);
            hintGo.AddComponent<RectTransform>();
            hintGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var hintTxt = AddText(hintGo, "", 11, new Color(0.95f, 0.75f, 0.40f), TextAnchor.MiddleLeft);
            SetSerializedRef(view, "hintLabel", hintTxt);

            // 编辑器测试：自动打开「杂货商店」
            var vso = new SerializedObject(view);
            var autoOpenProp = vso.FindProperty("autoOpenOnStart");
            var shopIdProp   = vso.FindProperty("_shopId");   // 商店 ID 已并入暴露的序列化字段
            if (autoOpenProp != null) autoOpenProp.boolValue = true;
            if (shopIdProp   != null) shopIdProp.stringValue = "杂货商店";
            vso.ApplyModifiedPropertiesWithoutUndo();

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(panelGo);
#endif

            MovePrimaryUiwToTop(panelGo);
            PrefabUtility.SaveAsPrefabAsset(panelGo, path);
            Object.DestroyImmediate(panelGo);
            Debug.Log("[InventoryDemoWizard] 商店面板 Prefab 已保存：" + path);
        }

        /// <summary>添加并设置 VerticalLayoutGroup（参数对齐 <see cref="SetHlg"/>）。</summary>
        static void SetVlg(GameObject go, RectOffset padding, float spacing,
            TextAnchor align, bool controlW, bool controlH, bool expandW, bool expandH)
        {
            var g = go.AddComponent<VerticalLayoutGroup>();
            g.padding = padding; g.spacing = spacing; g.childAlignment = align;
            g.childControlWidth = controlW; g.childControlHeight = controlH;
            g.childForceExpandWidth = expandW; g.childForceExpandHeight = expandH;
            g.childScaleWidth = false; g.childScaleHeight = false;
        }

        /// <summary>创建一个带居中文本的小按钮，返回 Button。</summary>
        static Button MakeMiniButton(string name, Transform parent, string label,
            Color normal, Color highlight, Color pressed)
        {
            var go = ChildGameObject(name, parent);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = normal;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            SetButtonColors(btn, normal, highlight, pressed);
            var lblGo = ChildGameObject("Label", go.transform);
            Stretch(lblGo.AddComponent<RectTransform>());
            AddText(lblGo, label, 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return btn;
        }

        #endregion

        #region UI预制体 对齐新增（Label / Price / ListPanel / Grid）

        /// <summary>构建 PF_ItemLabel（功能标签：背景图 + 文本，对齐 Demo 现有预制体）。</summary>
        static void BuildItemLabelPrefab()
        {
            string path = Pfb(KPfItemLabel);
            DeleteIfExists(path);

            var root = NewGameObject(KPfItemLabel);
            SetRectSize(root.AddComponent<RectTransform>(), 28f, 16f);
            var comp = root.AddComponent<UiwTextLabel>();
            SetHlg(root, new RectOffset(4, 4, 0, 0), 0f, TextAnchor.UpperLeft,
                controlW: true, controlH: true, expandW: false, expandH: true);

            // ImgBack（九宫格背景 + 默认精灵）
            var bgGo = ChildGameObject("ImgBack", root.transform);
            Stretch(bgGo.AddComponent<RectTransform>());
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color   = new Color(0.44276467f, 1f, 0.28773582f, 0.9019608f);
            bgImg.sprite  = LoadSprite(SpriteBackSphere);
            bgImg.type    = Image.Type.Sliced;
            bgImg.pixelsPerUnitMultiplier = 12f;
            SetLayoutElement(bgGo, ignore: true);

            // NameText
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = nameRt.anchorMax = Vector2.zero;
            nameRt.sizeDelta = Vector2.zero;
            var nameTxt = AddText(nameGo, "Name", 8, Color.white);

            SetSerializedRef(comp, "backgroundImage", bgImg);
            SetSerializedRef(comp, "labelText", nameTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_ItemPrice（价格货币格：图标 + 数量，UiwInventoryItemSimple）。</summary>
        static void BuildItemPricePrefab(NumberFormatConfig numFmt)
        {
            string path = Pfb(KPfItemPrice);
            DeleteIfExists(path);

            var root = NewGameObject(KPfItemPrice);
            SetRectSize(root.AddComponent<RectTransform>(), 36f, 16f);
            var comp = root.AddComponent<UiwInventoryItemSimple>();
            comp.iconAttrId   = "图标";
            comp.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            var rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(0.18f, 0.18f, 0.25f, 0.85f);
            SetHlg(root, new RectOffset(4, 5, 0, 0), 2f, TextAnchor.MiddleLeft, true, true, false, false);

            // Icon
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = Vector2.zero;
            iconRt.pivot     = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = Vector2.zero;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true;
            comp.iconImage = iconImg;
            SetLayoutElement(iconGo, prefW: 10, prefH: 10);

            // CountText
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = qRt.anchorMax = Vector2.zero;
            qRt.sizeDelta = Vector2.zero;
            var qTxt = AddText(qGo, "999", 10, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(comp, "countText", qTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_InventoryListPanel（独立列表面板：UiwInventoryList + ScrollRect + Scrollbar）。</summary>
        static void BuildInventoryListPanelPrefab(UiwInventoryItemDetail detailPrefab)
        {
            string path = Pfb(KPfInventoryOrderList);
            DeleteIfExists(path);

            var root   = NewGameObject(KPfInventoryOrderList);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = Vector2.zero;
            SetLayoutElement(root, prefH: 9000, flexH: 1);
            var listComp = root.AddComponent<UiwInventoryItemOrderList>();
            listComp.bufferCount = 1;
            listComp.cellPrefab  = detailPrefab;

            // ScrollRect 节点
            var srGo = ChildGameObject("ScrollRect", root.transform);
            Stretch(srGo.AddComponent<RectTransform>());
            srGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            var sr = srGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.elasticity = 0.06f;
            sr.inertia = true; sr.decelerationRate = 0.01f; sr.scrollSensitivity = 40f;

            // Viewport（右留 20px 给滚动条）
            var vpGo = ChildGameObject("Viewport", srGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.pivot = new Vector2(0.5f, 0.5f);
            vpRt.anchoredPosition = new Vector2(-10f, 0f);
            vpRt.sizeDelta = new Vector2(-20f, 0f);
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content（顶部对齐）
            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;

            // Scrollbar Vertical
            var sbGo = ChildGameObject("Scrollbar Vertical", srGo.transform);
            var sbRt = sbGo.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f); sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 1f); sbRt.sizeDelta = new Vector2(16f, 0f);
            var sbImg = sbGo.AddComponent<Image>();
            sbImg.color  = new Color(0.38180846f, 0.38180846f, 0.49056602f, 1f);
            sbImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sbImg.type   = Image.Type.Sliced;
            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            // Sliding Area（无图） + Handle
            var saGo = ChildGameObject("Sliding Area", sbGo.transform);
            var saRt = saGo.AddComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero; saRt.anchorMax = Vector2.one;
            saRt.sizeDelta = new Vector2(-2f, -2f);
            var handleGo = ChildGameObject("Handle", saGo.transform);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero; handleRt.anchorMax = Vector2.zero;
            handleRt.sizeDelta = new Vector2(-4f, -4f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color  = new Color(0.101960786f, 0.101960786f, 0.16078432f, 1f);
            handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            handleImg.type   = Image.Type.Sliced;

            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect    = handleRt;

            sr.content  = contentRt;
            sr.viewport = vpRt;
            sr.verticalScrollbar = scrollbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            listComp.scrollRect = sr;
            listComp.content    = contentRt;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_InventoryGridPanel（网格面板：ScrollRect + 虚拟滚动 UiwInventoryItemGridList，纵向滚动·自动列数）。</summary>
        static void BuildInventoryGridPrefab(UiwInventoryItemCell cellPrefab)
        {
            string path = Pfb(KPfInventoryGridList);
            DeleteIfExists(path);

            var root   = NewGameObject(KPfInventoryGridList);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = Vector2.zero;
            SetLayoutElement(root, prefH: 9000, flexH: 1);
            var comp = root.AddComponent<UiwInventoryItemGridList>();
            comp.bufferCount     = 1;
            comp.cellPrefab      = cellPrefab;
            comp.scrollDirection = EListScrollDirection.Vertical;
            comp.spacing         = new Vector2(6f, 6f);
            comp.padding         = new Vector2(6f, 6f);

            // ScrollRect 节点（网格虚拟滚动：纵向滚动）
            var srGo = ChildGameObject("ScrollRect", root.transform);
            Stretch(srGo.AddComponent<RectTransform>());
            srGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            var sr = srGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.elasticity = 0.06f;
            sr.inertia = true; sr.decelerationRate = 0.01f; sr.scrollSensitivity = 40f;

            // Viewport（右留 20px 给滚动条）
            var vpGo = ChildGameObject("Viewport", srGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.pivot = new Vector2(0.5f, 0.5f);
            vpRt.anchoredPosition = new Vector2(-10f, 0f);
            vpRt.sizeDelta = new Vector2(-20f, 0f);
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content（顶部对齐；格子由虚拟滚动手动定位，不挂 GridLayoutGroup）
            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;

            // Scrollbar Vertical
            var sbGo = ChildGameObject("Scrollbar Vertical", srGo.transform);
            var sbRt = sbGo.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f); sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 1f); sbRt.sizeDelta = new Vector2(16f, 0f);
            var sbImg = sbGo.AddComponent<Image>();
            sbImg.color  = new Color(0.38180846f, 0.38180846f, 0.49056602f, 1f);
            sbImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sbImg.type   = Image.Type.Sliced;
            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            // Sliding Area（无图） + Handle
            var saGo = ChildGameObject("Sliding Area", sbGo.transform);
            var saRt = saGo.AddComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero; saRt.anchorMax = Vector2.one;
            saRt.sizeDelta = new Vector2(-2f, -2f);
            var handleGo = ChildGameObject("Handle", saGo.transform);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero; handleRt.anchorMax = Vector2.zero;
            handleRt.sizeDelta = new Vector2(-4f, -4f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color  = new Color(0.101960786f, 0.101960786f, 0.16078432f, 1f);
            handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            handleImg.type   = Image.Type.Sliced;

            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect    = handleRt;

            sr.content  = contentRt;
            sr.viewport = vpRt;
            sr.verticalScrollbar = scrollbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            comp.scrollRect = sr;
            comp.content    = contentRt;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }

        #endregion

        #region UI预制体 道具单元格
        #region UiwInventoryItemDetail（列表格子）

        /// <summary>
        /// 构建 UiwInventoryItemDetail 的 Prefab，适用于列表布局的行显示。
        /// </summary>
        /// <param name="numFmt"></param>
        /// <param name="labelPrefab"></param>
        /// <param name="pricePrefab"></param>
        /// <returns></returns>
        static void BuildItemDetailPrefab(NumberFormatConfig numFmt,
            UiwTextLabel labelPrefab, UiwInventoryItemSimple pricePrefab)
        {
            string path = Pfb(KPfItemDetail);
            DeleteIfExists(path);

            // root：500×80
            var root = NewGameObject(KPfItemDetail);
            SetRectSize(root.AddComponent<RectTransform>(), 500f, 80f);
            root.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.22f, 0.95f);
            var comp = root.AddComponent<UiwInventoryItemDetail>();
            comp.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;

            // HoverBorder
            var hoverGo = ChildGameObject("HoverBorder", root.transform);
            Stretch(hoverGo.AddComponent<RectTransform>());
            var hoverImg = hoverGo.AddComponent<Image>();
            hoverImg.color = new Color(0.2f, 0.2f, 0.3f, 0f);
            comp.hoverBorder = hoverImg;
            comp.hoverFadeDuration = 0.12f;

            // StackFullIcon（右上角）
            var sfGo = ChildGameObject("StackFullIcon", root.transform);
            var sfRt = sfGo.AddComponent<RectTransform>();
            sfRt.anchorMin = sfRt.anchorMax = Vector2.one; sfRt.pivot = Vector2.one;
            sfRt.sizeDelta = new Vector2(14f, 14f);
            sfRt.anchoredPosition = new Vector2(-8f, -8f);
            var sfImg = sfGo.AddComponent<Image>();
            sfImg.color  = new Color(1f, 0.15566039f, 0.1996756f, 1f);
            sfImg.sprite = LoadSprite(SpriteBackSphere);
            comp.stackFullIcon = sfImg;
            comp.stackFullFadeDuration = 0.12f;

            // QualityBackground（左侧 74×74，含 Icon + IdText）
            var qualityGo = ChildGameObject("QualityBackground", root.transform);
            var qualityRt = qualityGo.AddComponent<RectTransform>();
            qualityRt.anchorMin = qualityRt.anchorMax = new Vector2(0f, 0.5f);
            qualityRt.pivot = new Vector2(0f, 0.5f);
            qualityRt.sizeDelta = new Vector2(74f, 74f);
            qualityRt.anchoredPosition = new Vector2(4f, 0f);
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color  = Color.white;
            qualityImg.sprite = LoadSprite(SpriteQualityPoor);
            qualityImg.preserveAspect = true;
            comp.qualityBackground = qualityImg;

            //   Icon（缩进 8px）— iconImage
            var iconGo = ChildGameObject("Icon", qualityGo.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.sizeDelta = new Vector2(-16f, -16f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color  = Color.white;
            iconImg.sprite = LoadSprite(SpriteItemGoldCoin);
            iconImg.preserveAspect = true;
            comp.iconImage = iconImg;

            //   IdText（底部小字）
            var idGo = ChildGameObject("IdText", qualityGo.transform);
            var idRt = idGo.AddComponent<RectTransform>();
            idRt.anchorMin = idRt.anchorMax = new Vector2(0.5f, 0f); idRt.pivot = new Vector2(0.5f, 0f);
            idRt.sizeDelta = new Vector2(50f, 10f);
            AddText(idGo, "id", 8, new Color(0.7f, 0.7f, 0.7f, 0.6f), TextAnchor.LowerCenter);

            // NameText（含 ContentSizeFitter + ItemTagsContainer）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = Vector2.zero; nameRt.anchorMax = Vector2.one;
            nameRt.pivot = new Vector2(0f, 0.5f);
            nameRt.anchoredPosition = new Vector2(80f, 22f);
            nameRt.sizeDelta = new Vector2(0f, -56f);
            var nameTxt = AddText(nameGo, "道具名称", 14, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            SetSerializedRef(comp, "nameText", nameTxt);
            SetContentSizeFitter(nameGo, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.Unconstrained);

            //   ItemTagsContainer（名称右侧标签容器）
            var tagsGo = ChildGameObject("ItemTagsContainer", nameGo.transform);
            var tagsRt = tagsGo.AddComponent<RectTransform>();
            tagsRt.anchorMin = tagsRt.anchorMax = new Vector2(1f, 0.5f); tagsRt.pivot = new Vector2(0f, 0.5f);
            tagsRt.anchoredPosition = new Vector2(8f, 0f);
            tagsRt.sizeDelta = new Vector2(200f, 20f);
            SetHlg(tagsGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleLeft, true, false, false, false);
            comp.textTagsPrefab    = labelPrefab;
            comp.itemTagsContainer = tagsGo.transform;

            // DescText
            var descGo = ChildGameObject("DescText", root.transform);
            var descRt = descGo.AddComponent<RectTransform>();
            descRt.anchorMin = Vector2.zero; descRt.anchorMax = Vector2.one;
            descRt.anchoredPosition = new Vector2(-32f, -13f);
            descRt.sizeDelta = new Vector2(-224f, -34f);
            var descTxt = AddText(descGo, "道具的详细描述。", 11, Color.white, TextAnchor.UpperLeft);
            SetSerializedRef(comp, "descText", descTxt);

            // CountText（右侧）
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = qRt.anchorMax = new Vector2(1f, 0.5f); qRt.pivot = new Vector2(1f, 0.5f);
            qRt.anchoredPosition = new Vector2(-10f, 0f);
            qRt.sizeDelta = new Vector2(50f, 30f);
            var qTxt = AddText(qGo, "×9999", 14, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleRight, FontStyle.Bold);
            SetSerializedRef(comp, "countText", qTxt);

            // Divider（底部分隔线）
            var lineGo = ChildGameObject("Divider", root.transform);
            var lineRt = lineGo.AddComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 0f); lineRt.anchorMax = new Vector2(1f, 0f);
            lineRt.pivot = new Vector2(0.5f, 0f);
            lineRt.sizeDelta = new Vector2(-8f, 1f);
            lineGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

            // PriceContainer（右下角价格容器）
            var priceGo = ChildGameObject("PriceContainer", root.transform);
            var priceRt = priceGo.AddComponent<RectTransform>();
            priceRt.anchorMin = priceRt.anchorMax = new Vector2(1f, 0f); priceRt.pivot = new Vector2(1f, 0f);
            priceRt.anchoredPosition = new Vector2(-6f, 6f);
            priceRt.sizeDelta = new Vector2(150f, 16f);
            SetHlg(priceGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleRight, true, false, false, false);
            comp.priceCurrencyPrefab = pricePrefab;
            comp.priceContainer      = priceGo.transform;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }
        #endregion
        
        #region UiwInventoryItemCell（网格方形格子）
        
        /// <summary>
        /// 构建 UiwInventoryItemCell 的 Prefab，适用于网格布局的格子显示。
        /// </summary>
        /// <param name="numFmt"></param>
        static void BuildItemCellPrefab(NumberFormatConfig numFmt)
        {
            string path = Pfb(KPfItemCell);
            DeleteIfExists(path);

            const float slotSize = 72f;
            var root   = NewGameObject(KPfItemCell);
            SetRectSize(root.AddComponent<RectTransform>(), slotSize, slotSize);

            // QualityBackground（全覆盖底层）
            var qualityGo  = ChildGameObject("QualityBackground", root.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = Color.white; qualityImg.preserveAspect = false;

            // Icon（四边缩进 6px）
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(6f, 6f); iconRt.offsetMax = new Vector2(-6f, -6f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.49056602f, 0.49056602f, 0.49056602f, 1f);
            iconImg.preserveAspect = true;

            var comp = root.AddComponent<UiwInventoryItemCell>();
            comp.numberFormat      = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            comp.iconImage         = iconImg;
            comp.qualityBackground = qualityImg;

            // NameText（顶部居中）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f); nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.sizeDelta = new Vector2(0f, 16f);
            nameRt.anchoredPosition = new Vector2(0f, -4f);
            var nameTxt = AddText(nameGo, "道具名称", 9, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperCenter);
            SetSerializedRef(comp, "nameText", nameTxt);

            // CountText（右下角）
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(1f, 0f); qRt.anchorMax = new Vector2(1f, 0f);
            qRt.pivot = new Vector2(1f, 0f);
            qRt.sizeDelta = new Vector2(50.4f, 16f);
            qRt.anchoredPosition = new Vector2(-8f, 4f);
            var qTxt = AddText(qGo, "9999", 10, new Color(1f, 1f, 1f, 0.9019608f), TextAnchor.LowerRight);
            SetSerializedRef(comp, "countText", qTxt);

            // HoverBorder（全覆盖，alpha=0）
            var hoverGo = ChildGameObject("HoverBorder", root.transform);
            Stretch(hoverGo.AddComponent<RectTransform>());
            var hoverImg = hoverGo.AddComponent<Image>();
            hoverImg.color = new Color(1f, 1f, 1f, 0f);
            comp.hoverBorder = hoverImg;

            // StackFullIcon（右上角红点）
            var sfGo = ChildGameObject("StackFullIcon", root.transform);
            var sfRt = sfGo.AddComponent<RectTransform>();
            sfRt.anchorMin = new Vector2(1f, 1f); sfRt.anchorMax = new Vector2(1f, 1f);
            sfRt.pivot = Vector2.one;
            sfRt.sizeDelta = new Vector2(10f, 10f);
            sfRt.anchoredPosition = new Vector2(-4f, -4f);
            var sfImg = sfGo.AddComponent<Image>();
            sfImg.color = new Color(1f, 0.2f, 0.2f, 1f);
            comp.stackFullIcon = sfImg;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }
        
        #endregion
        
        #region UiwInventoryItemSimple（货币栏用）
        /// <summary>
        /// 构建 UiwInventoryItemSimple 的 Prefab，适用于货币栏或极简显示场景。
        /// </summary>
        /// <param name="numFmt"></param>
        static void BuildItemSimplePrefab(NumberFormatConfig numFmt)
        {
            string path = Pfb(KPfItemSimple);
            DeleteIfExists(path);

            // root：54×20，横向布局（图标 + 数量）
            var root = NewGameObject(KPfItemSimple);
            SetRectSize(root.AddComponent<RectTransform>(), 54f, 20f);
            var comp = root.AddComponent<UiwInventoryItemSimple>();
            comp.iconAttrId   = "图标";
            comp.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            root.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.25f, 0.85f);
            SetHlg(root, new RectOffset(4, 6, 0, 0), 4f, TextAnchor.MiddleLeft, true, true, false, false);

            // Icon（固定 14×14，由 LayoutElement 控制）
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = Vector2.zero;
            iconRt.pivot     = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = Vector2.zero;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true;
            comp.iconImage = iconImg;
            SetLayoutElement(iconGo, prefW: 14, prefH: 14);

            // CountText
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = qRt.anchorMax = Vector2.zero;
            qRt.sizeDelta = Vector2.zero;
            var qTxt = AddText(qGo, "9999", 12, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(comp, "countText", qTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif

            SavePrefab(root, path);
        }
#endregion
        #endregion
        
        #region IS_TMP && IS_LOCALIZATION 字体事件辅助
        
#if IS_TMP && IS_LOCALIZATION
        /// <summary>
        /// 在 <paramref name="root"/> 上挂载 <see cref="InventoryTmpFontEvent"/>，
        /// 将 WelcomeWindow 中配置的本地化字体引用通过 JsonUtility roundtrip 写入组件，
        /// 然后扫描所有子节点以填充 texts / textEvents 列表并建立双向绑定。
        ///
        /// <para>必须在所有子节点（含 <see cref="InventoryTmpTextEvent"/>）都已添加后调用，
        /// 否则 <see cref="InventoryTmpFontEvent.RefreshComponents"/> 扫描结果不完整。</para>
        /// </summary>
        static void AttachFontEvent(GameObject root)
        {
            var fontEvent = root.AddComponent<InventoryTmpFontEvent>();

            // 将 WelcomeWindow 中配置的本地化字体引用写入 LocalizedAssetEvent 基类的
            // AssetReference（即 m_AssetReference），这才是基类实际用于驱动本地化的字段。
            // JsonUtility roundtrip 可正确复制 LocalizedReference 内已标 [SerializeField] 的
            // m_TableCollectionName / m_TableCollectionNameGuid / m_TableEntryReference 等字段，
            // 并触发 ISerializationCallbackReceiver.OnAfterDeserialize 完成内部状态同步。
            var localizedFont = InventoryWelcomeWindow.WizardLocalizedFont;
            if (localizedFont != null && !localizedFont.IsEmpty)
            {
                string json = JsonUtility.ToJson(localizedFont);
                JsonUtility.FromJsonOverwrite(json, fontEvent.AssetReference);
            }

            fontEvent.RefreshComponents();
        }
#endif

        // ═══════════════════════════════════════════════════════════════════════
        // IS_TMP 感知文本辅助
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// 向 <paramref name="go"/> 添加文本组件并设置基础属性。
        /// <list type="bullet">
        ///   <item>IS_TMP 宏启用时：使用 <c>TMPro.TextMeshProUGUI</c>，
        ///         对齐通过 <see cref="AnchorToTmp"/> 转换，字体样式映射为
        ///         <c>TMPro.FontStyles</c>，并关闭自动换行（enableWordWrapping = false）。</item>
        ///   <item>未启用时：使用 <c>UnityEngine.UI.Text</c>，直接赋值原生属性。</item>
        /// </list>
        /// 返回 <c>Component</c>，可直接传给 <see cref="SetSerializedRef"/>。
        /// </summary>
        static Component AddText(
            GameObject go,
            string     text,
            int        fontSize,
            Color      color,
            TextAnchor anchor    = TextAnchor.MiddleCenter,
            FontStyle  fontStyle = FontStyle.Normal)
        {
#if IS_TMP
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text               = text;
            t.fontSize           = fontSize;
            t.color              = color;
            t.alignment          = AnchorToTmp(anchor);
            t.fontStyle          = fontStyle == FontStyle.Bold
                                       ? FontStyles.Bold
                                       : FontStyles.Normal;
#if UNITY_6000_0_OR_NEWER
            t.textWrappingMode = TextWrappingModes.NoWrap; // 不换行
#else
            t.enableWordWrapping = false; // 不换行（Unity 2022 及以下 TMP 接口）
#endif

            // 应用 WelcomeWindow 中配置的默认字体（留空则 TMP 使用内置默认字体）
            var defaultFont = InventoryEditorPrefs.LoadWizardDefaultTmpFont();
            if (defaultFont) t.font = defaultFont;

#if IS_LOCALIZATION
            // 为每个 TMP 文本节点添加 InventoryTmpTextEvent，
            // 开发者可在生成后为各节点配置本地化字符串引用。
            go.AddComponent<InventoryTmpTextEvent>();
#endif

            return t;
#else
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.fontSize  = fontSize;
            t.color     = color;
            t.alignment = anchor;
            t.fontStyle = fontStyle;
            return t;
#endif
        }

#if IS_TMP
        /// <summary>
        /// 将 <see cref="TextAnchor"/> 九宫格枚举转换为等价的
        /// <see cref="TMPro.TextAlignmentOptions"/> 值。
        /// </summary>
        static TextAlignmentOptions AnchorToTmp(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:  return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:   return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:   return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight:  return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft:    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:  return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:   return TextAlignmentOptions.BottomRight;
                default:                      return TextAlignmentOptions.Center;
            }
        }
#endif
        #endregion

        #region UI预制体 制作系统（Tooltip / InputCell / BlueprintCell / BlueprintList / CraftingView）

        /// <summary>构建 PF_UiwItemTooltip（通用道具悬停弹窗：内嵌一个 UiwInventoryItemDetail 渲染详情）。</summary>
        static void BuildItemTooltipPrefab(GameObject detailPrefab)
        {
            string path = Pfb(KPfItemTooltip);
            DeleteIfExists(path);

            var root = NewGameObject(KPfItemTooltip);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, 90f);
            rt.pivot     = new Vector2(0f, 1f);  // 左上角，便于按光标定位
            root.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.96f);
            var cg = root.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            var tooltip = root.AddComponent<UiwItemTooltip>();
            tooltip.panel       = rt;
            tooltip.canvasGroup = cg;

            if (detailPrefab)
            {
                var detailInst = (GameObject)PrefabUtility.InstantiatePrefab(detailPrefab, root.transform);
                Stretch((RectTransform)detailInst.transform);
                tooltip.detail = detailInst.GetComponent<UiwInventoryItemDetail>();
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwInventoryItemDetail，悬停弹窗内容为空。");

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwCraftingInputCell（消耗道具行：图标 + 名称 + 持有/需求，支持悬停弹窗）。</summary>
        static void BuildCraftingInputCellPrefab(NumberFormatConfig numFmt)
        {
            string path = Pfb(KPfCraftingInputCell);
            DeleteIfExists(path);

            var root = NewGameObject(KPfCraftingInputCell);
            SetRectSize(root.AddComponent<RectTransform>(), 300f, 32f);
            SetLayoutElement(root, minH: 32, prefH: 32);
            root.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.20f, 0.70f);
            SetHlg(root, new RectOffset(6, 6, 2, 2), 6f, TextAnchor.MiddleLeft, true, true, false, false);

            var cell = root.AddComponent<UiwCraftingInputCell>();
            cell.iconAttrId        = "图标";
            cell.nameAttrId        = "名称";
            cell.numberFormat      = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            cell.showDetailTooltip = true;

            var iconGo = ChildGameObject("Icon", root.transform);
            iconGo.AddComponent<RectTransform>();
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            cell.iconImage = iconImg;
            SetLayoutElement(iconGo, minW: 26, prefW: 26, minH: 26, prefH: 26);

            var nameGo = ChildGameObject("NameText", root.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, flexW: 1, minH: 24, prefH: 24);
            var nameTxt = AddText(nameGo, "材料名", 12, Color.white, TextAnchor.MiddleLeft);
            SetSerializedRef(cell, "nameText", nameTxt);

            var amtGo = ChildGameObject("AmountText", root.transform);
            amtGo.AddComponent<RectTransform>();
            SetLayoutElement(amtGo, minW: 64, prefW: 64);
            var amtTxt = AddText(amtGo, "0/0", 12, Color.white, TextAnchor.MiddleRight);
            SetSerializedRef(cell, "amountText", amtTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwCraftingBlueprintCell（蓝图条目：主产出图标 + 蓝图名 + 属性显示行 + 选中条 + 点击选中）。</summary>
        static void BuildCraftingBlueprintCellPrefab(NumberFormatConfig numFmt, UiwTextLabel labelPrefab)
        {
            string path = Pfb(KPfCraftingBlueprintCell);
            DeleteIfExists(path);

            var root = NewGameObject(KPfCraftingBlueprintCell);
            SetRectSize(root.AddComponent<RectTransform>(), 320f, 72f);
            root.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.22f, 0.95f); // 兼作点击射线目标
            SetHlg(root, new RectOffset(8, 6, 4, 4), 8f, TextAnchor.MiddleLeft, true, true, false, false);

            var cell = root.AddComponent<UiwCraftingBlueprintCell>();
            cell.iconAttrId = "图标";
            cell.numberFormat = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;

            // 选中指示条（左侧竖条）
            var selGo = ChildGameObject("SelectedIndicator", root.transform);
            var selRt = selGo.AddComponent<RectTransform>();
            selRt.anchorMin = new Vector2(0f, 0f); selRt.anchorMax = new Vector2(0f, 1f);
            selRt.pivot = new Vector2(0f, 0.5f); selRt.sizeDelta = new Vector2(4f, 0f);
            selRt.anchoredPosition = Vector2.zero;
            var selImg = selGo.AddComponent<Image>(); selImg.color = Hex("66CCFF"); selImg.raycastTarget = false;
            selGo.SetActive(false);
            SetLayoutElement(selGo, ignore: true);
            cell.selectedIndicator = selGo;

            // 主产出图标 + 品质背景框（IconFrame 占据 HLG 槽位，内含品质底图 + 图标）
            var frameGo = ChildGameObject("IconFrame", root.transform);
            frameGo.AddComponent<RectTransform>();
            SetLayoutElement(frameGo, minW: 56, prefW: 56, minH: 56, prefH: 56);

            var qualityGo = ChildGameObject("QualityBackground", frameGo.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = Color.white; qualityImg.preserveAspect = true; qualityImg.raycastTarget = false;
            qualityImg.sprite = LoadSprite(SpriteQualityPoor);
            cell.qualityBackground = qualityImg;

            var iconGo = ChildGameObject("Icon", frameGo.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4f, 4f); iconRt.offsetMax = new Vector2(-4f, -4f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            cell.iconImage = iconImg;

            // 信息列（名称 + 属性行）
            var infoGo = ChildGameObject("Info", root.transform);
            infoGo.AddComponent<RectTransform>();
            SetLayoutElement(infoGo, flexW: 1, prefH: 64);
            SetVlg(infoGo, new RectOffset(0, 0, 2, 2), 2f, TextAnchor.UpperLeft, true, true, true, false);

            var nameGo = ChildGameObject("NameText", infoGo.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 20, prefH: 20);
            var nameTxt = AddText(nameGo, "蓝图名称", 14, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(cell, "nameText", nameTxt);

            var attrGo = ChildGameObject("AttrLines", infoGo.transform);
            attrGo.AddComponent<RectTransform>();
            SetLayoutElement(attrGo, flexH: 1, prefH: 36);
            SetVlg(attrGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, false, true, false, false);
            cell.attrLineContainer = attrGo.transform;
            cell.attrLinePrefab    = labelPrefab;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwCraftingBlueprintList（蓝图虚拟列表：UiwCraftingBlueprintList + ScrollRect + Scrollbar）。</summary>
        static void BuildCraftingBlueprintListPrefab(UiwCraftingBlueprintCell cellPrefab)
        {
            string path = Pfb(KPfCraftingBlueprintList);
            DeleteIfExists(path);

            var root   = NewGameObject(KPfCraftingBlueprintList);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = Vector2.zero;
            SetLayoutElement(root, prefH: 9000, flexH: 1, flexW: 1);
            var listComp = root.AddComponent<UiwCraftingBlueprintList>();
            listComp.bufferCount = 1;
            listComp.cellPrefab  = cellPrefab;

            var srGo = ChildGameObject("ScrollRect", root.transform);
            Stretch(srGo.AddComponent<RectTransform>());
            srGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            var sr = srGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.elasticity = 0.06f;
            sr.inertia = true; sr.decelerationRate = 0.01f; sr.scrollSensitivity = 40f;

            var vpGo = ChildGameObject("Viewport", srGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.pivot = new Vector2(0.5f, 0.5f);
            vpRt.anchoredPosition = new Vector2(-10f, 0f);
            vpRt.sizeDelta = new Vector2(-20f, 0f);
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;

            var sbGo = ChildGameObject("Scrollbar Vertical", srGo.transform);
            var sbRt = sbGo.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f); sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 1f); sbRt.sizeDelta = new Vector2(16f, 0f);
            var sbImg = sbGo.AddComponent<Image>();
            sbImg.color  = new Color(0.38f, 0.38f, 0.49f, 1f);
            sbImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sbImg.type   = Image.Type.Sliced;
            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var saGo = ChildGameObject("Sliding Area", sbGo.transform);
            var saRt = saGo.AddComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero; saRt.anchorMax = Vector2.one;
            saRt.sizeDelta = new Vector2(-2f, -2f);
            var handleGo = ChildGameObject("Handle", saGo.transform);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero; handleRt.anchorMax = Vector2.zero;
            handleRt.sizeDelta = new Vector2(-4f, -4f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color  = new Color(0.10f, 0.10f, 0.16f, 1f);
            handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            handleImg.type   = Image.Type.Sliced;
            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect    = handleRt;

            sr.content  = contentRt;
            sr.viewport = vpRt;
            sr.verticalScrollbar = scrollbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            listComp.scrollRect = sr;
            listComp.content    = contentRt;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwCraftingView（制作主界面：模板页签 + 搜索/分组 + 排序/列表 + 详情）。</summary>
        static void BuildCraftingViewPrefab(GameObject listPrefab, UiwCraftingInputCell inputCellPrefab,
            GameObject detailPrefab, UiwInventoryItemSimple simplePrefab, UiwInventoryTab tabPrefab, UiwFoldTab foldTabPrefab,
            UiwNumberCounter counterPrefab)
        {
            string path = Pfb(KPfCraftingView);
            DeleteIfExists(path);

            var panelGo = NewGameObject(KPfCraftingView);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(900f, 560f);
            panelRt.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.97f);
            SetVlg(panelGo, new RectOffset(6, 6, 6, 6), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var view = panelGo.AddComponent<UiwCraftingView>();

            // 标题
            var headerRow = MakeRow("Header", panelGo.transform, 36f, Hex("0D0D17"));
            SetHlg(headerRow, new RectOffset(10, 10, 0, 0), 0f, TextAnchor.MiddleLeft, true, true, true, true);
            var titleGo = ChildGameObject("TitleText", headerRow.transform);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var titleTxt = AddText(titleGo, "制作  [编辑器测试 — Play 后自动打开]", 13,
                new Color(0.85f, 0.85f, 0.92f), TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(view, "titleLabel", titleTxt);

            // 模板页签行
            var tabRow = MakeRow("TemplateTabRow", panelGo.transform, 34f, new Color(0f, 0f, 0f, 0f));
            SetHlg(tabRow, new RectOffset(4, 4, 2, 2), 3f, TextAnchor.MiddleLeft, false, true, false, true);
            view.templateTabContainer = tabRow.transform;
            view.templateTabPrefab    = tabPrefab;

            // 主体三列
            var bodyGo = ChildGameObject("Body", panelGo.transform);
            bodyGo.AddComponent<RectTransform>();
            var bodyLe = bodyGo.AddComponent<LayoutElement>(); bodyLe.flexibleHeight = 1f; bodyLe.preferredHeight = 9000f;
            SetHlg(bodyGo, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.UpperLeft, true, true, false, true);

            // ── 左列：搜索 + 分组折叠 ──
            var leftCol = ChildGameObject("LeftColumn", bodyGo.transform);
            leftCol.AddComponent<RectTransform>();
            SetLayoutElement(leftCol, minW: 180, prefW: 180);
            leftCol.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            SetVlg(leftCol, new RectOffset(4, 4, 4, 4), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var searchInput = MakeInputField("SearchInput", leftCol.transform, "搜索蓝图名称…");
            SetLayoutElement(searchInput.gameObject, minH: 26, prefH: 26);
            view.searchInput = searchInput;

            var groupGo = ChildGameObject("GroupFilter", leftCol.transform);
            groupGo.AddComponent<RectTransform>();
            var groupLe = groupGo.AddComponent<LayoutElement>(); groupLe.flexibleHeight = 1f; groupLe.preferredHeight = 9000f;
            SetVlg(groupGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, true, true, true, false);
            var groupFilter = groupGo.AddComponent<UiwCraftingGroupFilter>();
            groupFilter.container      = groupGo.transform;
            groupFilter.uiwFoldTabPrefab = foldTabPrefab;
            view.groupFilter = groupFilter;

            // ── 中列：排序栏 + 蓝图列表 ──
            var midCol = ChildGameObject("MidColumn", bodyGo.transform);
            midCol.AddComponent<RectTransform>();
            SetLayoutElement(midCol, flexW: 1, prefW: 340);
            SetVlg(midCol, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var sortRow = MakeRow("SortRow", midCol.transform, 28f, new Color(0f, 0f, 0f, 0f));
            SetHlg(sortRow, new RectOffset(2, 2, 0, 0), 4f, TextAnchor.MiddleLeft, true, true, false, true);
            var sortDd = MakeDropdown("SortDropdown", sortRow.transform);
            sortDd.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var sdBtnGo = ChildGameObject("SortDirectionButton", sortRow.transform);
            sdBtnGo.AddComponent<RectTransform>();
            var sdLe = sdBtnGo.AddComponent<LayoutElement>(); sdLe.minWidth = 54f; sdLe.preferredWidth = 54f;
            var sdImg = sdBtnGo.AddComponent<Image>(); sdImg.color = Hex("2A2A4A");
            var sdBtn = sdBtnGo.AddComponent<Button>(); sdBtn.targetGraphic = sdImg;
            SetButtonColors(sdBtn, Hex("2A2A4A"), Hex("3A3A6A"), Hex("1E1E36"));
            var sdLblGo = ChildGameObject("Label", sdBtnGo.transform);
            Stretch(sdLblGo.AddComponent<RectTransform>());
            var sdLbl = AddText(sdLblGo, "降序", 11, Color.white);
            var sortTb = sortRow.AddComponent<UiwSortToolbar>();
            sortTb.sortDropdown        = sortDd;
            sortTb.sortDirectionButton = sdBtn;
            SetSerializedRef(sortTb, "sortDirectionLabel", sdLbl);

            if (listPrefab)
            {
                var listInst = (GameObject)PrefabUtility.InstantiatePrefab(listPrefab, midCol.transform);
                var listLe = listInst.GetComponent<LayoutElement>() ?? listInst.AddComponent<LayoutElement>();
                listLe.flexibleHeight = 1f; listLe.preferredHeight = 9000f;
                view.blueprintList = listInst.GetComponent<UiwCraftingBlueprintList>();
                // 排序栏引用配置在蓝图列表组件上（排序管线由 UiwInventoryListBase 内建）。
                if (view.blueprintList) view.blueprintList.sortToolbar = sortTb;
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwCraftingBlueprintList。");

            // ── 右列：详情 ──
            var rightCol = ChildGameObject("RightColumn", bodyGo.transform);
            rightCol.AddComponent<RectTransform>();
            SetLayoutElement(rightCol, minW: 320, prefW: 320);
            rightCol.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            SetVlg(rightCol, new RectOffset(6, 6, 6, 6), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var detail = rightCol.AddComponent<UiwCraftingDetail>();

            if (detailPrefab)
            {
                var mInst = (GameObject)PrefabUtility.InstantiatePrefab(detailPrefab, rightCol.transform);
                SetLayoutElement(mInst, minH: 80, prefH: 80);
                detail.mainOutputDetail = mInst.GetComponent<UiwInventoryItemDetail>();
            }

            var secRow = MakeRow("SecondaryOutputs", rightCol.transform, 40f, new Color(0f, 0f, 0f, 0f));
            SetHlg(secRow, new RectOffset(0, 0, 0, 0), 4f, TextAnchor.MiddleLeft, false, true, false, false);
            detail.secondaryOutputContainer = secRow.transform;
            detail.secondaryOutputPrefab    = simplePrefab;

            var inputsGo = ChildGameObject("Inputs", rightCol.transform);
            inputsGo.AddComponent<RectTransform>();
            var inputsLe = inputsGo.AddComponent<LayoutElement>(); inputsLe.flexibleHeight = 1f; inputsLe.preferredHeight = 9000f;
            SetVlg(inputsGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, true, true, true, false);
            detail.inputContainer  = inputsGo.transform;
            detail.inputCellPrefab = inputCellPrefab;

            // 可制作 / 持有
            var infoRow = MakeRow("InfoRow", rightCol.transform, 22f, new Color(0f, 0f, 0f, 0f));
            SetHlg(infoRow, new RectOffset(2, 2, 0, 0), 8f, TextAnchor.MiddleLeft, true, true, false, true);
            var craftableGo = ChildGameObject("CraftableText", infoRow.transform);
            craftableGo.AddComponent<RectTransform>();
            craftableGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var craftableTxt = AddText(craftableGo, "可制作：0", 12, new Color(0.8f, 0.9f, 0.8f), TextAnchor.MiddleLeft);
            SetSerializedRef(detail, "craftableCountText", craftableTxt);
            var ownedGo = ChildGameObject("OwnedText", infoRow.transform);
            ownedGo.AddComponent<RectTransform>();
            ownedGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var ownedTxt = AddText(ownedGo, "持有：0", 12, new Color(0.8f, 0.8f, 0.9f), TextAnchor.MiddleRight);
            SetSerializedRef(detail, "ownedCountText", ownedTxt);

            // 制作次数：实例化通用 PF_UiwNumberCounter（统一驱动 +/- 与显示）
            var countRow = MakeRow("CountRow", rightCol.transform, 30f, new Color(0f, 0f, 0f, 0f));
            SetHlg(countRow, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.MiddleCenter, false, true, false, true);
            if (counterPrefab)
            {
                var craftCounter = (UiwNumberCounter)PrefabUtility.InstantiatePrefab(counterPrefab, countRow.transform);
                detail.counter = craftCounter;
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwNumberCounter，制作界面无法调整制作次数。");

            // 制作 / 停止 按钮
            var craftRow = MakeRow("CraftRow", rightCol.transform, 34f, new Color(0f, 0f, 0f, 0f));
            SetHlg(craftRow, new RectOffset(0, 0, 0, 0), 0f, TextAnchor.MiddleCenter, true, true, true, true);
            var craftGo = ChildGameObject("CraftButton", craftRow.transform);
            craftGo.AddComponent<RectTransform>();
            craftGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var craftImg = craftGo.AddComponent<Image>(); craftImg.color = Hex("2A4A2A");
            var craftBtn = craftGo.AddComponent<Button>(); craftBtn.targetGraphic = craftImg;
            SetButtonColors(craftBtn, Hex("2A4A2A"), Hex("3A6A3A"), Hex("1E361E"));
            detail.craftButton = craftBtn;
            var craftLblGo = ChildGameObject("Label", craftGo.transform);
            Stretch(craftLblGo.AddComponent<RectTransform>());
            var craftLbl = AddText(craftLblGo, "制作", 14, Color.white);
            SetSerializedRef(detail, "craftButtonLabel", craftLbl);

            // 进度条
            var progRow = MakeRow("ProgressRow", rightCol.transform, 10f, new Color(0f, 0f, 0f, 0.3f));
            var progFillGo = ChildGameObject("Fill", progRow.transform);
            var progRt = progFillGo.AddComponent<RectTransform>();
            progRt.anchorMin = Vector2.zero; progRt.anchorMax = Vector2.one;
            progRt.offsetMin = Vector2.zero; progRt.offsetMax = Vector2.zero;
            var progImg = progFillGo.AddComponent<Image>();
            progImg.color       = Hex("66CC66");
            progImg.sprite      = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            progImg.type        = Image.Type.Filled;
            progImg.fillMethod  = Image.FillMethod.Horizontal;
            progImg.fillOrigin  = (int)Image.OriginHorizontal.Left;
            progImg.fillAmount  = 0f;
            detail.progressFill = progImg;

            view.detail = detail;

            // 编辑器测试：进入 Play 自动打开
            var vso = new SerializedObject(view);
            var autoOpenProp = vso.FindProperty("autoOpenOnStart");
            if (autoOpenProp != null) autoOpenProp.boolValue = true;
            vso.ApplyModifiedPropertiesWithoutUndo();

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(panelGo);
#endif

            MovePrimaryUiwToTop(panelGo);
            PrefabUtility.SaveAsPrefabAsset(panelGo, path);
            Object.DestroyImmediate(panelGo);
            Debug.Log("[InventoryDemoWizard] 制作主界面 Prefab 已保存：" + path);
        }

        /// <summary>创建一个标准 UI InputField（文本组件为 UnityEngine.UI.Text，与 UiwCraftingView.searchInput 类型一致）。</summary>
        static InputField MakeInputField(string name, Transform parent, string placeholder)
        {
            var go = ChildGameObject(name, parent);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = Hex("1C2533");
            var input = go.AddComponent<InputField>();
            input.targetGraphic = img;

            var txtGo = ChildGameObject("Text", go.transform);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(6f, 2f); txtRt.offsetMax = new Vector2(-6f, -2f);
            var txt = txtGo.AddComponent<Text>();
            txt.fontSize = 11; txt.color = new Color(0.9f, 0.9f, 0.95f);
            txt.alignment = TextAnchor.MiddleLeft; txt.supportRichText = false;

            var phGo = ChildGameObject("Placeholder", go.transform);
            var phRt = phGo.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(6f, 2f); phRt.offsetMax = new Vector2(-6f, -2f);
            var ph = phGo.AddComponent<Text>();
            ph.fontSize = 11; ph.color = new Color(0.6f, 0.6f, 0.65f);
            ph.alignment = TextAnchor.MiddleLeft; ph.fontStyle = FontStyle.Italic; ph.text = placeholder;

            input.textComponent = txt;
            input.placeholder   = ph;
            return input;
        }

        #endregion

        #region UI预制体 装备系统（叶子格子：Slot / CandidateCell / BonusEntry）

        /// <summary>构建 PF_UiwEquipmentSlot（装备槽：品质背景 + 图标 + 名称 + 悬停 + 选中指示 + 绿/红有效性叠加）。</summary>
        static void BuildEquipmentSlotPrefab(NumberFormatConfig numFmt)
        {
            string path = Pfb(KPfEquipSlot);
            DeleteIfExists(path);

            const float slotSize = 72f;
            var root = NewGameObject(KPfEquipSlot);
            SetRectSize(root.AddComponent<RectTransform>(), slotSize, slotSize);

            // QualityBackground（全覆盖底层，兼作射线接收）
            var qualityGo  = ChildGameObject("QualityBackground", root.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = new Color(1f, 1f, 1f, 0.10f);

            // Icon（四边缩进 6px）
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(6f, 6f); iconRt.offsetMax = new Vector2(-6f, -6f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.49f, 0.49f, 0.49f, 1f);
            iconImg.preserveAspect = true;

            var comp = root.AddComponent<UiwEquipmentSlot>();
            comp.numberFormat      = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            comp.iconImage         = iconImg;
            comp.qualityBackground = qualityImg;

            // NameText（顶部居中，空槽显示槽位名）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f); nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.sizeDelta = new Vector2(0f, 16f);
            nameRt.anchoredPosition = new Vector2(0f, -4f);
            var nameTxt = AddText(nameGo, "槽位", 9, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperCenter);
            SetSerializedRef(comp, "nameText", nameTxt);

            // CountText（右下角；装备槽恒 1 件，留空）
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(1f, 0f); qRt.anchorMax = new Vector2(1f, 0f);
            qRt.pivot = new Vector2(1f, 0f);
            qRt.sizeDelta = new Vector2(50f, 16f);
            qRt.anchoredPosition = new Vector2(-8f, 4f);
            var qTxt = AddText(qGo, string.Empty, 10, new Color(1f, 1f, 1f, 0.9f), TextAnchor.LowerRight);
            SetSerializedRef(comp, "countText", qTxt);

            // HoverBorder（全覆盖，alpha=0）
            var hoverGo = ChildGameObject("HoverBorder", root.transform);
            Stretch(hoverGo.AddComponent<RectTransform>());
            var hoverImg = hoverGo.AddComponent<Image>();
            hoverImg.color = new Color(1f, 1f, 1f, 0f);
            hoverImg.raycastTarget = false;
            comp.hoverBorder = hoverImg;

            // SelectedIndicator（选中边框，默认隐藏）
            var selGo = ChildGameObject("SelectedIndicator", root.transform);
            Stretch(selGo.AddComponent<RectTransform>());
            var selImg = selGo.AddComponent<Image>();
            selImg.color = new Color(1f, 0.85f, 0.2f, 0.5f);
            selImg.raycastTarget = false;
            selGo.SetActive(false);
            comp.selectedIndicator = selGo;

            // ValidityOverlay（拖拽悬停绿/红，默认禁用）
            var valGo = ChildGameObject("ValidityOverlay", root.transform);
            Stretch(valGo.AddComponent<RectTransform>());
            var valImg = valGo.AddComponent<Image>();
            valImg.color = comp.validColor;
            valImg.raycastTarget = false;
            valImg.enabled = false;
            comp.validityOverlay = valImg;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentCandidateCell（可装备道具格子：UiwInventoryItemCell + GridCellDragHandler；品质背景 + 图标 + 名称 + 数量 + 悬停；右键快速装备 / 左键拖拽装备）。</summary>
        static void BuildEquipmentCandidateCellPrefab(NumberFormatConfig numFmt)
        {
            string path = Pfb(KPfEquipCandidateCell);
            DeleteIfExists(path);

            const float slotSize = 64f;
            var root = NewGameObject(KPfEquipCandidateCell);
            SetRectSize(root.AddComponent<RectTransform>(), slotSize, slotSize);

            // QualityBackground
            var qualityGo  = ChildGameObject("QualityBackground", root.transform);
            Stretch(qualityGo.AddComponent<RectTransform>());
            var qualityImg = qualityGo.AddComponent<Image>();
            qualityImg.color = new Color(1f, 1f, 1f, 0.10f);

            // Icon
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(5f, 5f); iconRt.offsetMax = new Vector2(-5f, -5f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(0.49f, 0.49f, 0.49f, 1f);
            iconImg.preserveAspect = true;

            // 候选格子复用仓库格子 UiwInventoryItemCell（显示）+ GridCellDragHandler（未接入网格列表时驱动装备拖拽）。
            var comp = root.AddComponent<UiwInventoryItemCell>();
            comp.dragHandler       = root.AddComponent<GridCellDragHandler>();
            comp.numberFormat      = numFmt?.locales?.Count > 0 ? numFmt.locales[0] : null;
            comp.iconImage         = iconImg;
            comp.qualityBackground = qualityImg;

            // NameText（顶部）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f); nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.sizeDelta = new Vector2(0f, 14f);
            nameRt.anchoredPosition = new Vector2(0f, -3f);
            var nameTxt = AddText(nameGo, "道具名称", 9, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperCenter);
            SetSerializedRef(comp, "nameText", nameTxt);

            // CountText（右下角）
            var qGo = ChildGameObject("CountText", root.transform);
            var qRt = qGo.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(1f, 0f); qRt.anchorMax = new Vector2(1f, 0f);
            qRt.pivot = new Vector2(1f, 0f);
            qRt.sizeDelta = new Vector2(46f, 14f);
            qRt.anchoredPosition = new Vector2(-6f, 3f);
            var qTxt = AddText(qGo, "9", 10, new Color(1f, 1f, 1f, 0.9f), TextAnchor.LowerRight);
            SetSerializedRef(comp, "countText", qTxt);

            // HoverBorder
            var hoverGo = ChildGameObject("HoverBorder", root.transform);
            Stretch(hoverGo.AddComponent<RectTransform>());
            var hoverImg = hoverGo.AddComponent<Image>();
            hoverImg.color = new Color(1f, 1f, 1f, 0f);
            hoverImg.raycastTarget = false;
            comp.hoverBorder = hoverImg;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentBonusEntry（属性加成条目：左标签 + 右数值，一行）。亦用作分组标题行（数值留空）。</summary>
        static void BuildEquipmentBonusEntryPrefab()
        {
            string path = Pfb(KPfEquipBonusEntry);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipBonusEntry);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 22f);
            SetLayoutElement(root, minH: 22f, prefH: 22f, flexW: 1f);

            var comp = root.AddComponent<UiwEquipmentBonusEntry>();

            // Label（左 60%）
            var lblGo = ChildGameObject("Label", root.transform);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f); lblRt.anchorMax = new Vector2(0.6f, 1f);
            lblRt.offsetMin = new Vector2(8f, 0f); lblRt.offsetMax = Vector2.zero;
            var lblTxt = AddText(lblGo, "属性", 12, new Color(0.85f, 0.85f, 0.92f), TextAnchor.MiddleLeft);
            SetSerializedRef(comp, "labelText", lblTxt);

            // Value（右 40%）
            var valGo = ChildGameObject("Value", root.transform);
            var valRt = valGo.AddComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.6f, 0f); valRt.anchorMax = new Vector2(1f, 1f);
            valRt.offsetMin = Vector2.zero; valRt.offsetMax = new Vector2(-8f, 0f);
            var valTxt = AddText(valGo, "0", 12, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            SetSerializedRef(comp, "valueText", valTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentSlotList（槽位列表：名称 + HorizontalLayoutGroup 装备槽容器）。</summary>
        static void BuildEquipmentSlotListPrefab(UiwEquipmentSlot slotPrefab)
        {
            string path = Pfb(KPfEquipSlotList);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipSlotList);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 100f);
            SetVlg(root, new RectOffset(4, 4, 4, 4), 4f, TextAnchor.UpperLeft, true, false, true, false);
            SetContentSizeFitter(root, ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize);

            var comp = root.AddComponent<UiwEquipmentSlotList>();

            // 名称
            var nameGo = ChildGameObject("NameText", root.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 18f, prefH: 18f, flexW: 1f);
            var nameTxt = AddText(nameGo, "槽位列表", 13, new Color(0.9f, 0.9f, 0.95f), TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(comp, "nameText", nameTxt);

            // 装备槽容器（HorizontalLayoutGroup）
            var contGo = ChildGameObject("SlotContainer", root.transform);
            contGo.AddComponent<RectTransform>();
            SetHlg(contGo, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.MiddleLeft, false, false, false, false);
            SetLayoutElement(contGo, minH: 78f, prefH: 78f, flexW: 1f);

            comp.slotPrefab    = slotPrefab;
            comp.slotContainer = contGo.transform;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentCandidateList（可装备道具列表：ScrollRect + 虚拟滚动 UiwEquipmentCandidateList，纵向滚动·自动列数）。</summary>
        static void BuildEquipmentCandidateListPrefab(UiwInventoryItemCell cellPrefab)
        {
            string path = Pfb(KPfEquipCandidateList);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipCandidateList);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 160f);
            root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
            SetLayoutElement(root, minH: 120f, flexW: 1f, flexH: 1f);

            var comp = root.AddComponent<UiwEquipmentCandidateList>();
            comp.cellPrefab      = cellPrefab;
            comp.bufferCount     = 1;
            comp.scrollDirection = EListScrollDirection.Vertical;
            comp.spacing         = new Vector2(6f, 6f);
            comp.padding         = new Vector2(6f, 6f);

            // ScrollRect（纵向；虚拟滚动手动定位，不用 GridLayoutGroup）
            var srGo = ChildGameObject("ScrollRect", root.transform);
            var srRt = srGo.AddComponent<RectTransform>();
            srRt.anchorMin = Vector2.zero; srRt.anchorMax = Vector2.one;
            srRt.offsetMin = Vector2.zero; srRt.offsetMax = Vector2.zero;
            var sr = srGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f;

            // Viewport（占满，带遮罩）
            var vpGo = ChildGameObject("Viewport", srGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.pivot = new Vector2(0.5f, 0.5f);
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content（顶部对齐；格子由虚拟滚动手动定位，不挂 GridLayoutGroup）
            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;

            sr.content  = contentRt;
            sr.viewport = vpRt;

            comp.scrollRect = sr;
            comp.content    = contentRt;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentGroupPanel（装备组面板：名称 + VerticalLayoutGroup 槽位列表容器）。</summary>
        static void BuildEquipmentGroupPanelPrefab(UiwEquipmentSlotList slotListPrefab)
        {
            string path = Pfb(KPfEquipGroupPanel);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipGroupPanel);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320f, 400f);
            root.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.6f);
            SetVlg(root, new RectOffset(8, 8, 8, 8), 8f, TextAnchor.UpperLeft, true, false, true, false);

            var comp = root.AddComponent<UiwEquipmentGroupPanel>();

            // 装备组名称
            var nameGo = ChildGameObject("GroupName", root.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 22f, prefH: 22f, flexW: 1f);
            var nameTxt = AddText(nameGo, "装备组", 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(comp, "groupNameText", nameTxt);

            // 槽位列表容器（VerticalLayoutGroup）
            var contGo = ChildGameObject("SlotListContainer", root.transform);
            contGo.AddComponent<RectTransform>();
            SetVlg(contGo, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.UpperLeft, true, false, true, false);
            SetLayoutElement(contGo, flexW: 1f, flexH: 1f);

            comp.slotListPrefab    = slotListPrefab;
            comp.slotListContainer = contGo.transform;

            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwEquipmentBonusPanel（属性加成面板：标题 + VerticalLayoutGroup 条目容器；分组标题复用条目预制体）。</summary>
        static void BuildEquipmentBonusPanelPrefab(UiwEquipmentBonusEntry entryPrefab)
        {
            string path = Pfb(KPfEquipBonusPanel);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipBonusPanel);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 400f);
            root.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.6f);
            SetVlg(root, new RectOffset(8, 8, 8, 8), 6f, TextAnchor.UpperLeft, true, false, true, false);

            var comp = root.AddComponent<UiwEquipmentBonusPanel>();

            // 标题
            var titleGo = ChildGameObject("Title", root.transform);
            titleGo.AddComponent<RectTransform>();
            SetLayoutElement(titleGo, minH: 22f, prefH: 22f, flexW: 1f);
            AddText(titleGo, "属性加成", 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);

            // 条目容器（VerticalLayoutGroup）
            var contGo = ChildGameObject("EntryContainer", root.transform);
            contGo.AddComponent<RectTransform>();
            SetVlg(contGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, true, false, true, false);
            SetLayoutElement(contGo, flexW: 1f, flexH: 1f);

            comp.entryPrefab       = entryPrefab;
            comp.entryContainer    = contGo.transform;
            comp.groupHeaderPrefab = entryPrefab;   // 复用同一预制体作分组标题行（数值留空）

            SavePrefab(root, path);
        }

        /// <summary>装备 UI 用的简易文本按钮（Image + Button + 居中文本）。</summary>
        static Button MakeEquipButton(string name, Transform parent, string label, Color bg)
        {
            var go = ChildGameObject(name, parent);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            SetButtonColors(btn, bg, bg * 1.2f, bg * 0.8f);

            var lblGo = ChildGameObject("Text", go.transform);
            Stretch(lblGo.AddComponent<RectTransform>());
            AddText(lblGo, label, 13, Color.white);
            return btn;
        }

        /// <summary>
        /// 构建 PF_UiwEquipmentSelectPanel（装备选择面板：切换栏 prev/name/pos/next + 中间槽位列表实例 +
        /// 底部候选道具列表实例 + 退出按钮；根 Image 作右键空白退出的射线接收）。
        /// </summary>
        static void BuildEquipmentSelectPanelPrefab(GameObject slotListPrefab, GameObject candidateListPrefab)
        {
            string path = Pfb(KPfEquipSelectPanel);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipSelectPanel);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(360f, 460f);
            root.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.96f); // 根射线背景（右键空白退出）

            var comp = root.AddComponent<UiwEquipmentSelectPanel>();

            // ── 顶部切换栏 ──
            var barGo = ChildGameObject("SwitchBar", root.transform);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f); barRt.anchorMax = new Vector2(1f, 1f); barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(-16f, 32f); barRt.anchoredPosition = new Vector2(0f, -8f);

            var prevBtn = MakeEquipButton("PrevButton", barGo.transform, "<", Hex("2C3D50"));
            var prevRt  = (RectTransform)prevBtn.transform;
            prevRt.anchorMin = new Vector2(0f, 0f); prevRt.anchorMax = new Vector2(0f, 1f); prevRt.pivot = new Vector2(0f, 0.5f);
            prevRt.sizeDelta = new Vector2(30f, 0f); prevRt.anchoredPosition = Vector2.zero;
            comp.prevButton = prevBtn;

            var nextBtn = MakeEquipButton("NextButton", barGo.transform, ">", Hex("2C3D50"));
            var nextRt  = (RectTransform)nextBtn.transform;
            nextRt.anchorMin = new Vector2(1f, 0f); nextRt.anchorMax = new Vector2(1f, 1f); nextRt.pivot = new Vector2(1f, 0.5f);
            nextRt.sizeDelta = new Vector2(30f, 0f); nextRt.anchoredPosition = Vector2.zero;
            comp.nextButton = nextBtn;

            var nameGo = ChildGameObject("SlotListName", barGo.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.4f); nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(34f, 0f); nameRt.offsetMax = new Vector2(-34f, 0f);
            var nameTxt = AddText(nameGo, "槽位组", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetSerializedRef(comp, "slotListNameText", nameTxt);

            var posGo = ChildGameObject("Position", barGo.transform);
            var posRt = posGo.AddComponent<RectTransform>();
            posRt.anchorMin = new Vector2(0f, 0f); posRt.anchorMax = new Vector2(1f, 0.4f);
            posRt.offsetMin = new Vector2(34f, 0f); posRt.offsetMax = new Vector2(-34f, 0f);
            var posTxt = AddText(posGo, "1/1", 9, new Color(0.7f, 0.7f, 0.78f));
            SetSerializedRef(comp, "positionText", posTxt);

            // ── 中间：当前槽位列表（实例化；关掉其 ContentSizeFitter 改用固定锚定）──
            var slView = (GameObject)PrefabUtility.InstantiatePrefab(slotListPrefab, root.transform);
            var slCsf  = slView.GetComponent<ContentSizeFitter>(); if (slCsf) slCsf.enabled = false;
            var slRt   = (RectTransform)slView.transform;
            slRt.anchorMin = new Vector2(0f, 1f); slRt.anchorMax = new Vector2(1f, 1f); slRt.pivot = new Vector2(0.5f, 1f);
            slRt.sizeDelta = new Vector2(-16f, 110f); slRt.anchoredPosition = new Vector2(0f, -48f);
            comp.slotListView = slView.GetComponent<UiwEquipmentSlotList>();

            // ── 底部：可装备道具列表（实例化，填充中部到退出按钮之间）──
            var candView = (GameObject)PrefabUtility.InstantiatePrefab(candidateListPrefab, root.transform);
            var candRt   = (RectTransform)candView.transform;
            candRt.anchorMin = new Vector2(0f, 0f); candRt.anchorMax = new Vector2(1f, 1f);
            candRt.offsetMin = new Vector2(8f, 46f); candRt.offsetMax = new Vector2(-8f, -166f);
            comp.candidateList = candView.GetComponent<UiwEquipmentCandidateList>();

            // ── 退出按钮（底部）──
            var exitBtn = MakeEquipButton("ExitButton", root.transform, "返回", Hex("3A2530"));
            var exitRt  = (RectTransform)exitBtn.transform;
            exitRt.anchorMin = new Vector2(0f, 0f); exitRt.anchorMax = new Vector2(1f, 0f); exitRt.pivot = new Vector2(0.5f, 0f);
            exitRt.sizeDelta = new Vector2(-16f, 30f); exitRt.anchoredPosition = new Vector2(0f, 8f);
            comp.exitButton = exitBtn;

            SavePrefab(root, path);
        }

        /// <summary>
        /// 构建 PF_UiwEquipmentView（装备主界面：标题 + 左装备组面板 / 选择面板（叠加，选择面板初始隐藏） + 右属性加成面板）。
        /// </summary>
        static void BuildEquipmentViewPrefab(GameObject groupPanelPrefab, GameObject bonusPanelPrefab, GameObject selectPanelPrefab)
        {
            string path = Pfb(KPfEquipView);
            DeleteIfExists(path);

            var root = NewGameObject(KPfEquipView);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(720f, 500f);
            root.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.92f);

            var comp = root.AddComponent<UiwEquipmentView>();

            // 标题
            var titleGo = ChildGameObject("Title", root.transform);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(1f, 1f); titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(-16f, 30f); titleRt.anchoredPosition = new Vector2(0f, -6f);
            var titleTxt = AddText(titleGo, "装备", 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(comp, "titleLabel", titleTxt);

            // 左：装备组面板（实例化）
            var groupInst = (GameObject)PrefabUtility.InstantiatePrefab(groupPanelPrefab, root.transform);
            var groupRt   = (RectTransform)groupInst.transform;
            groupRt.anchorMin = new Vector2(0f, 0f); groupRt.anchorMax = new Vector2(0.62f, 1f);
            groupRt.offsetMin = new Vector2(8f, 8f); groupRt.offsetMax = new Vector2(-4f, -42f);
            comp.groupPanel = groupInst.GetComponent<UiwEquipmentGroupPanel>();

            // 左（叠加）：装备选择面板（实例化，初始隐藏；运行时由 View 打开/还原）
            var selInst = (GameObject)PrefabUtility.InstantiatePrefab(selectPanelPrefab, root.transform);
            var selRt   = (RectTransform)selInst.transform;
            selRt.anchorMin = new Vector2(0f, 0f); selRt.anchorMax = new Vector2(0.62f, 1f);
            selRt.offsetMin = new Vector2(8f, 8f); selRt.offsetMax = new Vector2(-4f, -42f);
            selInst.SetActive(false);
            comp.selectPanel = selInst.GetComponent<UiwEquipmentSelectPanel>();

            // 右：属性加成面板（实例化）
            var bonusInst = (GameObject)PrefabUtility.InstantiatePrefab(bonusPanelPrefab, root.transform);
            var bonusRt   = (RectTransform)bonusInst.transform;
            bonusRt.anchorMin = new Vector2(0.62f, 0f); bonusRt.anchorMax = new Vector2(1f, 1f);
            bonusRt.offsetMin = new Vector2(4f, 8f); bonusRt.offsetMax = new Vector2(-8f, -42f);
            comp.bonusPanel = bonusInst.GetComponent<UiwEquipmentBonusPanel>();

            SavePrefab(root, path);
        }

        #endregion

        #region UI预制体 技能系统（条目：Cell / Detail；列表：Grid / Order；Tooltip；View）

        /// <summary>构建 PF_UiwSkillCell（技能网格条目：位阶背景框 + 图标 + 名称，支持悬停弹窗）。</summary>
        static void BuildSkillCellPrefab()
        {
            string path = Pfb(KPfSkillCell);
            DeleteIfExists(path);

            var root = NewGameObject(KPfSkillCell);
            SetRectSize(root.AddComponent<RectTransform>(), 72f, 72f);
            root.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.20f, 0.90f); // 兼作悬停射线目标

            var entry = root.AddComponent<UiwSkillEntry>();
            entry.rankAttrId           = "位阶";
            entry.rankBackgroundAttrId = "背景框";
            entry.fallbackToId         = true;
            entry.showTooltip          = true;

            // 位阶背景框（铺满整格，位于图标之下；无位阶数据时运行时自动隐藏）
            var rankGo = ChildGameObject("RankBackground", root.transform);
            Stretch(rankGo.AddComponent<RectTransform>());
            var rankImg = rankGo.AddComponent<Image>();
            rankImg.color = Color.white; rankImg.raycastTarget = false; rankImg.enabled = false;
            entry.rankBackground = rankImg;

            // 图标（顶部居中，略内缩）
            var iconGo = ChildGameObject("Icon", root.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -6f);
            iconRt.sizeDelta = new Vector2(44f, 44f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            entry.iconImage = iconImg;

            // 名称（底部）
            var nameGo = ChildGameObject("NameText", root.transform);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f); nameRt.anchorMax = new Vector2(1f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.sizeDelta = new Vector2(-4f, 18f);
            nameRt.anchoredPosition = new Vector2(0f, 2f);
            var nameTxt = AddText(nameGo, "技能名", 10, Color.white);
            SetSerializedRef(entry, "nameText", nameTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwSkillDetail（技能列表条目：图标(含位阶背景框) + 名称 + 描述，支持悬停弹窗）。</summary>
        static void BuildSkillDetailPrefab()
        {
            string path = Pfb(KPfSkillDetail);
            DeleteIfExists(path);

            var root = NewGameObject(KPfSkillDetail);
            SetRectSize(root.AddComponent<RectTransform>(), 320f, 60f);
            SetLayoutElement(root, minH: 60, prefH: 60, flexW: 1);
            root.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.20f, 0.85f);
            SetHlg(root, new RectOffset(6, 6, 4, 4), 8f, TextAnchor.MiddleLeft, true, true, false, false);

            var entry = root.AddComponent<UiwSkillEntry>();
            entry.rankAttrId           = "位阶";
            entry.rankBackgroundAttrId = "背景框";
            entry.fallbackToId         = true;
            entry.showTooltip          = true;

            // 图标容器（位阶背景框 + 图标）
            var iconRoot = ChildGameObject("IconRoot", root.transform);
            iconRoot.AddComponent<RectTransform>();
            SetLayoutElement(iconRoot, minW: 48, prefW: 48, minH: 48, prefH: 48);

            var rankGo = ChildGameObject("RankBackground", iconRoot.transform);
            Stretch(rankGo.AddComponent<RectTransform>());
            var rankImg = rankGo.AddComponent<Image>();
            rankImg.color = Color.white; rankImg.raycastTarget = false; rankImg.enabled = false;
            entry.rankBackground = rankImg;

            var iconGo = ChildGameObject("Icon", iconRoot.transform);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4f, 4f); iconRt.offsetMax = new Vector2(-4f, -4f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            entry.iconImage = iconImg;

            // 文本块（名称 + 描述，纵向）
            var textCol = ChildGameObject("TextColumn", root.transform);
            textCol.AddComponent<RectTransform>();
            SetLayoutElement(textCol, flexW: 1, minH: 48, prefH: 48);
            SetVlg(textCol, new RectOffset(0, 0, 2, 2), 2f, TextAnchor.UpperLeft, true, true, true, false);

            var nameGo = ChildGameObject("NameText", textCol.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, minH: 20, prefH: 20);
            var nameTxt = AddText(nameGo, "技能名", 14, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(entry, "nameText", nameTxt);

            var descGo = ChildGameObject("DescText", textCol.transform);
            descGo.AddComponent<RectTransform>();
            SetLayoutElement(descGo, flexH: 1, minH: 20, prefH: 22);
            var descTxt = AddText(descGo, "技能描述", 11, new Color(0.72f, 0.72f, 0.80f), TextAnchor.UpperLeft);
            SetSerializedRef(entry, "descText", descTxt);

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwSkillGridList（技能网格列表：ScrollRect + 虚拟滚动 UiwSkillGridList，纵向滚动·自动列数）。</summary>
        static void BuildSkillGridListPrefab(UiwSkillEntry cellPrefab)
        {
            string path = Pfb(KPfSkillGridList);
            DeleteIfExists(path);

            var root = NewGameObject(KPfSkillGridList);
            Stretch(root.AddComponent<RectTransform>());
            var comp = root.AddComponent<UiwSkillGridList>();
            comp.cellPrefab      = cellPrefab;
            comp.bufferCount     = 1;
            comp.scrollDirection = EListScrollDirection.Vertical;
            comp.spacing         = new Vector2(6f, 6f);
            comp.padding         = new Vector2(6f, 6f);
            if (!cellPrefab) Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillCell，技能网格列表条目为空。");

            // ScrollRect（竖向）
            var srGo = ChildGameObject("ScrollRect", root.transform);
            Stretch(srGo.AddComponent<RectTransform>());
            srGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            var sr = srGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.elasticity = 0.06f;
            sr.scrollSensitivity = 40f;

            // Viewport（右留 20px 给滚动条）
            var vpGo = ChildGameObject("Viewport", srGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.pivot = new Vector2(0.5f, 0.5f);
            vpRt.anchoredPosition = new Vector2(-10f, 0f);
            vpRt.sizeDelta = new Vector2(-20f, 0f);
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content（顶部对齐；格子由虚拟滚动手动定位，不挂 GridLayoutGroup / SizeFitter）
            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;

            // Scrollbar Vertical
            var sbGo = ChildGameObject("Scrollbar Vertical", srGo.transform);
            var sbRt = sbGo.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f); sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 1f); sbRt.sizeDelta = new Vector2(16f, 0f);
            var sbImg = sbGo.AddComponent<Image>();
            sbImg.color  = new Color(0.38f, 0.38f, 0.49f, 1f);
            sbImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sbImg.type   = Image.Type.Sliced;
            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var saGo = ChildGameObject("Sliding Area", sbGo.transform);
            var saRt = saGo.AddComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero; saRt.anchorMax = Vector2.one; saRt.sizeDelta = new Vector2(-2f, -2f);
            var handleGo = ChildGameObject("Handle", saGo.transform);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero; handleRt.anchorMax = Vector2.zero; handleRt.sizeDelta = new Vector2(-4f, -4f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color  = new Color(0.10f, 0.10f, 0.16f, 1f);
            handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            handleImg.type   = Image.Type.Sliced;
            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect    = handleRt;

            sr.content  = contentRt;
            sr.viewport = vpRt;
            sr.verticalScrollbar = scrollbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            comp.content    = contentRt;
            comp.scrollRect = sr;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwSkillOrderList（技能顺序列表：ScrollRect + 虚拟滚动 UiwSkillOrderList）。</summary>
        static void BuildSkillOrderListPrefab(UiwSkillEntry detailPrefab)
        {
            string path = Pfb(KPfSkillOrderList);
            DeleteIfExists(path);

            var root = NewGameObject(KPfSkillOrderList);
            Stretch(root.AddComponent<RectTransform>());
            var comp = root.AddComponent<UiwSkillOrderList>();
            comp.cellPrefab  = detailPrefab;
            comp.bufferCount = 1;
            if (!detailPrefab) Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillDetail，技能顺序列表条目为空。");

            // ScrollRect
            var srGo = ChildGameObject("ScrollRect", root.transform);
            Stretch(srGo.AddComponent<RectTransform>());
            srGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
            var sr = srGo.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.elasticity = 0.06f;
            sr.scrollSensitivity = 40f;

            // Viewport（右留 20px 给滚动条）
            var vpGo = ChildGameObject("Viewport", srGo.transform);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.pivot = new Vector2(0.5f, 0.5f);
            vpRt.anchoredPosition = new Vector2(-10f, 0f);
            vpRt.sizeDelta = new Vector2(-20f, 0f);
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content（顶部对齐；条目由虚拟滚动手动定位，不挂 VerticalLayoutGroup / SizeFitter）
            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;

            // Scrollbar Vertical
            var sbGo = ChildGameObject("Scrollbar Vertical", srGo.transform);
            var sbRt = sbGo.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f); sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 1f); sbRt.sizeDelta = new Vector2(16f, 0f);
            var sbImg = sbGo.AddComponent<Image>();
            sbImg.color  = new Color(0.38f, 0.38f, 0.49f, 1f);
            sbImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sbImg.type   = Image.Type.Sliced;
            var scrollbar = sbGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var saGo = ChildGameObject("Sliding Area", sbGo.transform);
            var saRt = saGo.AddComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero; saRt.anchorMax = Vector2.one; saRt.sizeDelta = new Vector2(-2f, -2f);
            var handleGo = ChildGameObject("Handle", saGo.transform);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero; handleRt.anchorMax = Vector2.zero; handleRt.sizeDelta = new Vector2(-4f, -4f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color  = new Color(0.10f, 0.10f, 0.16f, 1f);
            handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            handleImg.type   = Image.Type.Sliced;
            scrollbar.targetGraphic = handleImg;
            scrollbar.handleRect    = handleRt;

            sr.content  = contentRt;
            sr.viewport = vpRt;
            sr.verticalScrollbar = scrollbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            comp.content    = contentRt;
            comp.scrollRect = sr;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>构建 PF_UiwSkillTooltip（技能悬停弹窗：图标 + 名称 + 位阶名 + 描述；由 InventoryManager 全局实例化）。</summary>
        static void BuildSkillTooltipPrefab()
        {
            string path = Pfb(KPfSkillTooltip);
            DeleteIfExists(path);

            var root = NewGameObject(KPfSkillTooltip);
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 150f);
            rt.pivot     = new Vector2(0f, 1f);   // 左上角，便于按光标定位
            root.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.96f);
            var cg = root.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            SetVlg(root, new RectOffset(10, 10, 8, 8), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var tip = root.AddComponent<UiwSkillTooltip>();
            tip.rankAttrId     = "位阶";
            tip.rankNameAttrId = "名称";
            tip.panel          = rt;
            tip.canvasGroup    = cg;

            // 顶部行：图标 + 名称 + 位阶名
            var headRow = ChildGameObject("Header", root.transform);
            headRow.AddComponent<RectTransform>();
            SetLayoutElement(headRow, minH: 32, prefH: 32);
            SetHlg(headRow, new RectOffset(0, 0, 0, 0), 6f, TextAnchor.MiddleLeft, true, true, false, false);

            var iconGo = ChildGameObject("Icon", headRow.transform);
            iconGo.AddComponent<RectTransform>();
            SetLayoutElement(iconGo, minW: 30, prefW: 30, minH: 30, prefH: 30);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            tip.iconImage = iconImg;

            var nameGo = ChildGameObject("NameText", headRow.transform);
            nameGo.AddComponent<RectTransform>();
            SetLayoutElement(nameGo, flexW: 1, minH: 28, prefH: 28);
            var nameTxt = AddText(nameGo, "技能名", 15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(tip, "nameText", nameTxt);

            var rankGo = ChildGameObject("RankNameText", headRow.transform);
            rankGo.AddComponent<RectTransform>();
            SetLayoutElement(rankGo, minW: 72, prefW: 72, minH: 28, prefH: 28);
            var rankTxt = AddText(rankGo, "位阶", 12, Hex("FFD24D"), TextAnchor.MiddleRight);
            SetSerializedRef(tip, "rankNameText", rankTxt);

            // 描述
            var descGo = ChildGameObject("DescText", root.transform);
            descGo.AddComponent<RectTransform>();
            SetLayoutElement(descGo, flexH: 1, minH: 40, prefH: 60);
            var descTxt = AddText(descGo, "技能描述", 12, new Color(0.80f, 0.80f, 0.88f), TextAnchor.UpperLeft);
            SetSerializedRef(tip, "descText", descTxt);

            // 自定义属性字段容器（预留：在组件上填 customFieldKeys + customFieldLinePrefab 即逐行显示；此处默认空，不显示）
            var fieldsGo = ChildGameObject("CustomFields", root.transform);
            fieldsGo.AddComponent<RectTransform>();
            SetLayoutElement(fieldsGo, minH: 0, prefH: 0);
            SetVlg(fieldsGo, new RectOffset(0, 0, 0, 0), 2f, TextAnchor.UpperLeft, true, true, true, false);
            tip.customFieldContainer = fieldsGo.transform;

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(root);
#endif
            SavePrefab(root, path);
        }

        /// <summary>
        /// 构建一个横向可滚动的过滤页签栏：<see cref="UiwFilterTabBar"/> 的按钮排入横向 ScrollView 的 Content；
        /// 标签总宽未超出时不滚动（Clamped），超出时可横向拖动 / 滚动，避免溢出界面。
        /// </summary>
        static void BuildFilterTabScroll(string name, Transform parent, Button filterButtonPrefab,
            float height, out UiwFilterTabBar bar)
        {
            var rowGo = ChildGameObject(name, parent);
            rowGo.AddComponent<RectTransform>();
            SetLayoutElement(rowGo, minH: height, prefH: height);
            var sr = rowGo.AddComponent<ScrollRect>();
            sr.horizontal = true; sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Clamped;   // 内容未超出时不滚动
            sr.scrollSensitivity = 20f;

            // Viewport（裁剪 + 拖拽射线目标）
            var vpGo = ChildGameObject("Viewport", rowGo.transform);
            Stretch(vpGo.AddComponent<RectTransform>());
            vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            sr.viewport = vpGo.GetComponent<RectTransform>();

            // Content（左对齐、高度铺满；横向布局 + 宽度自适应，撑开后可横向滚动）
            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f); contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.sizeDelta = Vector2.zero; contentRt.anchoredPosition = Vector2.zero;
            var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false; hlg.childForceExpandWidth = false;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = true;
            hlg.childScaleWidth = false; hlg.childScaleHeight = false;
            hlg.spacing = 3f; hlg.padding = new RectOffset(2, 2, 2, 2);
            SetContentSizeFitter(contentGo, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.Unconstrained);
            sr.content = contentRt;

            // 过滤页签栏（按钮实例化到 Content）
            bar = rowGo.AddComponent<UiwFilterTabBar>();
            bar.filterContainer    = contentGo.transform;
            bar.filterButtonPrefab = filterButtonPrefab;
        }

        /// <summary>构建 PF_UiwSkillView（技能主界面：标题 + 视图切换 + 搜索 + 主/副分组页签 + 网格/顺序列表）。</summary>
        static void BuildSkillViewPrefab(GameObject gridListPrefab, GameObject orderListPrefab, Button filterButtonPrefab)
        {
            string path = Pfb(KPfSkillView);
            DeleteIfExists(path);

            var panelGo = NewGameObject(KPfSkillView);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(420f, 560f);
            panelRt.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.97f);
            SetVlg(panelGo, new RectOffset(6, 6, 6, 6), 4f, TextAnchor.UpperLeft, true, true, true, false);

            var view = panelGo.AddComponent<UiwSkillView>();
            view.source         = ESkillSource.InventoryDatabase;
            view.titleText      = "技能";
            view.skillRefAttrId = "技能";
            view.showAllTab     = true;

            // 标题行（标题 + 视图切换按钮）
            var headerRow = MakeRow("Header", panelGo.transform, 36f, Hex("0D0D17"));
            SetHlg(headerRow, new RectOffset(10, 6, 0, 0), 6f, TextAnchor.MiddleLeft, true, true, false, true);
            var titleGo = ChildGameObject("TitleText", headerRow.transform);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var titleTxt = AddText(titleGo, "技能  [编辑器测试 — Play 后自动打开]", 13,
                new Color(0.85f, 0.85f, 0.92f), TextAnchor.MiddleLeft, FontStyle.Bold);
            SetSerializedRef(view, "titleLabel", titleTxt);

            var toggleGo = ChildGameObject("ViewModeToggle", headerRow.transform);
            toggleGo.AddComponent<RectTransform>();
            SetLayoutElement(toggleGo, minW: 56, prefW: 56, minH: 26, prefH: 26);
            var toggleImg = toggleGo.AddComponent<Image>(); toggleImg.color = Hex("2A2A4A");
            var toggleBtn = toggleGo.AddComponent<Button>(); toggleBtn.targetGraphic = toggleImg;
            SetButtonColors(toggleBtn, Hex("2A2A4A"), Hex("3A3A6A"), Hex("1E1E36"));
            var toggleLblGo = ChildGameObject("Label", toggleGo.transform);
            Stretch(toggleLblGo.AddComponent<RectTransform>());
            var toggleLbl = AddText(toggleLblGo, "列表", 12, Color.white);
            view.viewModeToggleButton = toggleBtn;
            SetSerializedRef(view, "viewModeToggleLabel", toggleLbl);

            // 搜索行
            var searchInput = MakeInputField("SearchInput", panelGo.transform, "搜索技能名称…");
            SetLayoutElement(searchInput.gameObject, minH: 26, prefH: 26);
            view.searchInput = searchInput;

            // 主 / 副分组页签行：各一横向 ScrollView（标签过多可横向滚动，不超范围不滚动），按钮复用 PF_FilterTabBtn。
            // 主分组、副分组为两个 AND 筛选条件（两者都满足的技能才显示）。
            BuildFilterTabScroll("PrimaryGroupTabScroll", panelGo.transform, filterButtonPrefab, 30f, out var primaryBar);
            BuildFilterTabScroll("SecondaryGroupTabScroll", panelGo.transform, filterButtonPrefab, 30f, out var secondaryBar);
            if (!filterButtonPrefab) Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_FilterTabBtn，技能分组页签无按钮。");

            // 列表容器（网格 + 顺序两个列表实例叠放，由视图切换显示其一）
            var listHost = ChildGameObject("ListHost", panelGo.transform);
            listHost.AddComponent<RectTransform>();
            var listHostLe = listHost.AddComponent<LayoutElement>();
            listHostLe.flexibleHeight = 1f; listHostLe.preferredHeight = 9000f;

            if (orderListPrefab)
            {
                var orderInst = (GameObject)PrefabUtility.InstantiatePrefab(orderListPrefab, listHost.transform);
                Stretch((RectTransform)orderInst.transform);
                view.orderList = orderInst.GetComponent<UiwSkillOrderList>();
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillOrderList。");

            if (gridListPrefab)
            {
                var gridInst = (GameObject)PrefabUtility.InstantiatePrefab(gridListPrefab, listHost.transform);
                Stretch((RectTransform)gridInst.transform);
                view.gridList = gridInst.GetComponent<UiwSkillGridList>();
            }
            else Debug.LogWarning("[InventoryDemoWizard] 缺少 PF_UiwSkillGridList。");

            // 主 / 副分组过滤栏引用配置在技能列表组件上（网格 + 顺序两列表各引用同一对栏；仅激活的列表响应其事件）。
            if (view.orderList) { view.orderList.filterBar = primaryBar; view.orderList.secondaryFilterBar = secondaryBar; }
            if (view.gridList)  { view.gridList.filterBar  = primaryBar; view.gridList.secondaryFilterBar  = secondaryBar; }

#if IS_TMP && IS_LOCALIZATION
            AttachFontEvent(panelGo);
#endif
            MovePrimaryUiwToTop(panelGo);
            PrefabUtility.SaveAsPrefabAsset(panelGo, path);
            Object.DestroyImmediate(panelGo);
            Debug.Log("[InventoryDemoWizard] 技能主界面 Prefab 已保存：" + path);
        }

        #endregion

        #region 工具方法

        // 创建不带 Transform 的 GameObject（自动拥有 Transform）
        static GameObject NewGameObject(string name)
        {
            var go = new GameObject(name);
            return go;
        }

        // 创建子节点（自动 AddComponent<RectTransform> 不在此处，外部显式 Add）
        static GameObject ChildGameObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        // 固定尺寸（锚点中心）
        static void SetRectSize(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
        }

        // 四边拉伸（零偏移）
        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // 创建固定高度的行节点（带 LayoutElement）
        static GameObject MakeRow(string name, Transform parent, float height, Color bgColor)
        {
            var go = ChildGameObject(name, parent);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, height);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height; le.flexibleWidth = 1f;
            if (bgColor.a > 0.001f)
            {
                var img = go.AddComponent<Image>();
                img.color = bgColor;
            }
            return go;
        }

        // Button 颜色状态
        static void SetButtonColors(Button btn, Color normal, Color highlight, Color pressed)
        {
            var colors = btn.colors;
            colors.normalColor      = normal;
            colors.highlightedColor = highlight;
            colors.pressedColor     = pressed;
            colors.selectedColor    = highlight;
            btn.colors = colors;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 创建带完整模板的标准 UI Dropdown。
        // Dropdown.captionText / itemText 必须是 UnityEngine.UI.Text（与 IS_TMP 无关），
        // 因为 UiwSortToolbar.sortDropdown 使用的是 UnityEngine.UI.Dropdown。
        // ─────────────────────────────────────────────────────────────────────
        static Dropdown MakeDropdown(string goName, Transform parent)
        {
            // ── 根节点 ────────────────────────────────────────────────────────
            var go = ChildGameObject(goName, parent);
            go.AddComponent<RectTransform>();
            var bgImg = go.AddComponent<Image>();
            bgImg.color = Hex("1C2533");
            var dropdown = go.AddComponent<Dropdown>();

            // Caption 文本（显示当前选中项）
            var captionGo = ChildGameObject("Label", go.transform);
            var captionRt = captionGo.AddComponent<RectTransform>();
            captionRt.anchorMin = Vector2.zero;
            captionRt.anchorMax = Vector2.one;
            captionRt.offsetMin = new Vector2(6f,  2f);
            captionRt.offsetMax = new Vector2(-6f, -2f);
            var captionTxt       = captionGo.AddComponent<Text>();
            captionTxt.fontSize  = 11;
            captionTxt.color     = new Color(0.85f, 0.85f, 0.92f);
            captionTxt.alignment = TextAnchor.MiddleLeft;
            dropdown.captionText       = captionTxt;

            // ── 下拉模板（弹出列表） ──────────────────────────────────────────
            // Unity 要求 Template 默认关闭；打开下拉时框架会自动激活它
            var templateGo = ChildGameObject("Template", go.transform);
            var templateRt = templateGo.AddComponent<RectTransform>();
            templateRt.anchorMin        = new Vector2(0f, 0f);
            templateRt.anchorMax        = new Vector2(1f, 0f);
            templateRt.pivot            = new Vector2(0.5f, 1f);
            templateRt.sizeDelta        = new Vector2(0f, 120f);
            templateRt.anchoredPosition = Vector2.zero;
            dropdown.template = templateRt; // 必须赋值，否则打开下拉时报 "template is not assigned"
            templateGo.AddComponent<Image>().color = Hex("1C2533");
            var templateSr = templateGo.AddComponent<ScrollRect>();
            templateSr.horizontal        = false;
            templateSr.vertical          = true;
            templateSr.scrollSensitivity = 20f;
            templateGo.SetActive(false);    // 必须初始为 inactive

            // Viewport
            var vpGo = ChildGameObject("Viewport", templateGo.transform);
            Stretch(vpGo.AddComponent<RectTransform>());
            vpGo.AddComponent<Image>().color     = new Color(0f, 0f, 0f, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            templateSr.viewport = vpGo.GetComponent<RectTransform>();

            // Content
            var contentGo = ChildGameObject("Content", vpGo.transform);
            var contentRt  = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin        = new Vector2(0f, 1f);
            contentRt.anchorMax        = new Vector2(1f, 1f);
            contentRt.pivot            = new Vector2(0.5f, 1f);
            contentRt.sizeDelta        = new Vector2(0f, 28f);
            contentRt.anchoredPosition = Vector2.zero;
            templateSr.content = contentRt;

            // Item 模板（Toggle）
            var itemGo = ChildGameObject("Item", contentGo.transform);
            var itemRt  = itemGo.AddComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 26f);
            var itemToggle = itemGo.AddComponent<Toggle>();
            var itemBg     = itemGo.AddComponent<Image>();
            itemBg.color            = new Color(0f, 0f, 0f, 0f);
            itemToggle.targetGraphic = itemBg;

            // Item Background（选中高亮）
            var itemBgGo = ChildGameObject("Item Background", itemGo.transform);
            Stretch(itemBgGo.AddComponent<RectTransform>());
            var itemBgImg    = itemBgGo.AddComponent<Image>();
            itemBgImg.color  = Hex("2C3D50");
            itemToggle.graphic = itemBgImg;

            // Item Label
            var itemLblGo = ChildGameObject("Item Label", itemGo.transform);
            var itemLblRt  = itemLblGo.AddComponent<RectTransform>();
            itemLblRt.anchorMin = Vector2.zero;
            itemLblRt.anchorMax = Vector2.one;
            itemLblRt.offsetMin = new Vector2(6f, 0f);
            itemLblRt.offsetMax = Vector2.zero;
            var itemLbl       = itemLblGo.AddComponent<Text>();
            itemLbl.fontSize  = 11;
            itemLbl.color     = new Color(0.85f, 0.85f, 0.92f);
            itemLbl.alignment = TextAnchor.MiddleLeft;
            dropdown.itemText       = itemLbl;

            return dropdown;
        }

        // 通过 SerializedObject 设置 objectReference 字段（兼容 IS_TMP 类型差异）
        static void SetSerializedRef(Component comp, string fieldName, Object value)
        {
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 保存 Prefab 到指定路径并销毁临时 GameObject
        static void SavePrefab(GameObject root, string path)
        {
            MovePrimaryUiwToTop(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log("[InventoryDemoWizard] 预制体已保存：" + path);
        }

        /// <summary>
        /// 把根节点上的主 Uiw 组件移到组件列表顶部（紧随 Transform/RectTransform），
        /// 这样在 Inspector 中能第一眼看到核心脚本。多数 builder 在添加完 Image/Button/LayoutGroup
        /// 等组件「之后」才 AddComponent&lt;UiwXxx&gt;()，故默认排在靠后位置，这里统一上移到顶部。
        /// </summary>
        static void MovePrimaryUiwToTop(GameObject root)
        {
            if (!root) return;
            var uiw = root.GetComponents<MonoBehaviour>()
                          .FirstOrDefault(c => c && c.GetType().Name.StartsWith("Uiw"));
            if (!uiw) return;
            // 反复上移直到无法再上移（Transform 始终占据首位，不会被越过）
            while (UnityEditorInternal.ComponentUtility.MoveComponentUp(uiw)) { }
        }

        // 十六进制颜色（"RRGGBB" 或 "RRGGBBAA"）
        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        // 确保文件夹存在（递归创建父链，支持多级子目录）
        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            int sep = path.LastIndexOf('/');
            if (sep < 0) return;
            string parent = path.Substring(0, sep);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(sep + 1));
        }

        // 若资产已存在则删除，保证后续用最新版本重新生成
        static void DeleteIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path))
                AssetDatabase.DeleteAsset(path);
        }

        // ── 对齐辅助（精灵 / 依赖加载 / 布局组件复制）──────────────────────────

        /// <summary>从指定资产路径加载 Sprite（把 Demo 静态精灵赋给 Image）；缺失时告警，不再静默留空。</summary>
        static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (!sprite)
                Debug.LogWarning($"[InventoryDemoWizard] 未找到精灵资产：{assetPath}（对应 Image 将留空）。");
            return sprite;
        }

        /// <summary>加载预制体根节点上的指定组件（用于依赖引用）。</summary>
        static T LoadPrefabComp<T>(string path) where T : Component
            => AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<T>();

        /// <summary>添加并设置 LayoutElement。</summary>
        static void SetLayoutElement(GameObject go, float minW = -1, float minH = -1,
            float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1, bool ignore = false)
        {
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = minW; le.minHeight = minH;
            le.preferredWidth = prefW; le.preferredHeight = prefH;
            le.flexibleWidth = flexW; le.flexibleHeight = flexH;
            le.ignoreLayout = ignore;
        }

        /// <summary>添加并设置 ContentSizeFitter。</summary>
        static void SetContentSizeFitter(GameObject go,
            ContentSizeFitter.FitMode h, ContentSizeFitter.FitMode v)
        {
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = h; csf.verticalFit = v;
        }

        /// <summary>添加并设置 HorizontalLayoutGroup。</summary>
        static void SetHlg(GameObject go, RectOffset padding, float spacing,
            TextAnchor align, bool controlW, bool controlH, bool expandW, bool expandH)
        {
            var g = go.AddComponent<HorizontalLayoutGroup>();
            g.padding = padding; g.spacing = spacing; g.childAlignment = align;
            g.childControlWidth = controlW; g.childControlHeight = controlH;
            g.childForceExpandWidth = expandW; g.childForceExpandHeight = expandH;
            g.childScaleWidth = false; g.childScaleHeight = false;
        }
        #endregion
    }
}

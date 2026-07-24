using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.UI;
using static Ale.Inventory.Editor.InventoryEditorL10n;

#if  IS_TMP
using TMPro;
#endif

namespace Ale.Inventory.Editor
{
    // 类型 Inventory 与命名空间段 Ale.Inventory 同名，此处显式别名消歧义（否则 CS0118）。
    using Inventory = global::Ale.Inventory.Runtime.Inventory;

    /// <summary>生成项编排：一键生成入口、按需生成的依赖闭包与条目目录。</summary>
    public static partial class InventoryDemoWizard
    {
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
        // 构建 _items 时所用的编辑器语言；语言切换后需重建，否则 DisplayName 停留在旧语言。
        private static EditorLanguage _itemsLang;

        /// <summary>全部可生成项（拓扑有序：依赖先于被依赖者）。</summary>
        public static IReadOnlyList<GenItem> Items
        {
            get
            {
                if (_items == null || _itemsLang != InventoryEditorL10n.Current)
                {
                    _itemsLang = InventoryEditorL10n.Current;
                    _items     = BuildItems();
                }
                return _items;
            }
        }

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
            // 局部名 NumFmt：避免与本文件 using static 引入的 InventoryEditorL10n.Fmt 冲突。
            NumberFormatConfig NumFmt() => GetOrCreateNumberFormat();
            return new List<GenItem>
            {
                new GenItem { Category = CatCommon, Key = "DB",       DisplayName = Fmt("数据库 {0}", "InventoryDatabase"), AssetPath = DatabasePath,             DepKeys = new string[0],
                    Build = () => GetOrCreateDatabase() },
                new GenItem { Category = CatInventory, Key = "Tab",      DisplayName = Fmt("仓库页签 {0}", KPfInventoryTab), AssetPath = Pfb(KPfInventoryTab),           DepKeys = new string[0],
                    Build = () => BuildTabPrefab() },
                new GenItem { Category = CatInventory, Key = "Filter",   DisplayName = Fmt("过滤按钮 {0}", KPfFilterButton), AssetPath = Pfb(KPfFilterButton),           DepKeys = new string[0],
                    Build = () => BuildFilterButtonPrefab() },
                new GenItem { Category = CatInventory, Key = "FoldTab",  DisplayName = Fmt("折叠页签 {0}", KPfFoldTab),      AssetPath = Pfb(KPfFoldTab),                DepKeys = new string[0],
                    Build = () => BuildFoldTabPrefab() },
                new GenItem { Category = CatItem, Key = "Simple",   DisplayName = Fmt("简易格子 {0}", KPfItemSimple),   AssetPath = Pfb(KPfItemSimple),             DepKeys = new string[0],
                    Build = () => BuildItemSimplePrefab(NumFmt()) },
                new GenItem { Category = CatItem, Key = "Cell",     DisplayName = Fmt("网格格子 {0}", KPfItemCell),     AssetPath = Pfb(KPfItemCell),               DepKeys = new string[0],
                    Build = () => BuildItemCellPrefab(NumFmt()) },
                new GenItem { Category = CatItem, Key = "Label",    DisplayName = Fmt("功能标签 {0}", KPfItemLabel),    AssetPath = Pfb(KPfItemLabel),              DepKeys = new string[0],
                    Build = () => BuildItemLabelPrefab() },
                new GenItem { Category = CatItem, Key = "Price",    DisplayName = Fmt("价格货币 {0}", KPfItemPrice),    AssetPath = Pfb(KPfItemPrice),              DepKeys = new string[0],
                    Build = () => BuildItemPricePrefab(NumFmt()) },
                new GenItem { Category = CatItem, Key = "Detail",   DisplayName = Fmt("列表格子 {0}", KPfItemDetail),   AssetPath = Pfb(KPfItemDetail),             DepKeys = new[] { "Label", "Price" },
                    Build = () => BuildItemDetailPrefab(NumFmt(),
                        LoadPrefabComp<UiwTextLabel>(Pfb(KPfItemLabel)),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemPrice))) },
                // 悬停弹窗：预制体由 InventoryManager 持有，运行时全局实例化一次（见 BuildInventoryManagerPrefab）。
                new GenItem { Category = CatItem, Key = "Tooltip",  DisplayName = Fmt("道具悬停弹窗 {0}", KPfItemTooltip), AssetPath = Pfb(KPfItemTooltip),         DepKeys = new[] { "Detail" },
                    Build = () => BuildItemTooltipPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfItemDetail))) },
                new GenItem { Category = CatInventory, Key = "ListPanel",DisplayName = Fmt("列表面板 {0}", KPfInventoryOrderList), AssetPath = Pfb(KPfInventoryOrderList), DepKeys = new[] { "Detail" },
                    Build = () => BuildInventoryListPanelPrefab(LoadPrefabComp<UiwInventoryItemDetail>(Pfb(KPfItemDetail))) },
                new GenItem { Category = CatInventory, Key = "Grid",     DisplayName = Fmt("网格面板 {0}", KPfInventoryGridList),  AssetPath = Pfb(KPfInventoryGridList),  DepKeys = new[] { "Cell" },
                    Build = () => BuildInventoryGridPrefab(LoadPrefabComp<UiwInventoryItemCell>(Pfb(KPfItemCell))) },
                new GenItem { Category = CatInventory, Key = "Panel",    DisplayName = Fmt("仓库面板 {0}", KPfInventoryPanel),     AssetPath = Pfb(KPfInventoryPanel),     DepKeys = new[] { "Tab", "Filter", "Simple", "ListPanel", "Grid" },
                    Build = () => BuildInventoryPanelPrefab(
                        LoadPrefabComp<UiwInventoryTab>(Pfb(KPfInventoryTab)),
                        LoadPrefabComp<Button>(Pfb(KPfFilterButton)),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemSimple)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfInventoryOrderList)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfInventoryGridList))) },
                new GenItem { Category = CatShop, Key = "ShopTab",  DisplayName = Fmt("商店组页签 {0}", KPfShopGroupTab), AssetPath = Pfb(KPfShopGroupTab),           DepKeys = new string[0],
                    Build = () => BuildShopGroupTabPrefab() },
                new GenItem { Category = CatShop, Key = "Counter",  DisplayName = Fmt("数量计数器 {0}", KPfNumberCounter), AssetPath = Pfb(KPfNumberCounter),         DepKeys = new string[0],
                    Build = () => BuildNumberCounterPrefab() },
                new GenItem { Category = CatShop, Key = "ShopCell", DisplayName = Fmt("商店商品条目 {0}", KPfShopItemDetail), AssetPath = Pfb(KPfShopItemDetail),  DepKeys = new[] { "Price", "Counter" },
                    Build = () => BuildShopItemDetailPrefab(NumFmt(),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemPrice)),
                        LoadPrefabComp<UiwNumberCounter>(Pfb(KPfNumberCounter))) },
                new GenItem { Category = CatShop, Key = "ShopPanel",DisplayName = Fmt("商店面板 {0}", KPfShopPanel),      AssetPath = Pfb(KPfShopPanel),              DepKeys = new[] { "ShopTab", "ShopCell", "Simple" },
                    Build = () => BuildShopPanelPrefab(
                        LoadPrefabComp<UiwShopGroupTab>(Pfb(KPfShopGroupTab)),
                        LoadPrefabComp<UiwShopItemDetail>(Pfb(KPfShopItemDetail)),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemSimple))) },
                new GenItem { Category = CatCrafting, Key = "CraftInput", DisplayName = Fmt("制作消耗行 {0}", KPfCraftingInputCell), AssetPath = Pfb(KPfCraftingInputCell), DepKeys = new string[0],
                    Build = () => BuildCraftingInputCellPrefab(NumFmt()) },
                new GenItem { Category = CatCrafting, Key = "CraftCell",  DisplayName = Fmt("蓝图条目 {0}", KPfCraftingBlueprintCell), AssetPath = Pfb(KPfCraftingBlueprintCell), DepKeys = new[] { "Label" },
                    Build = () => BuildCraftingBlueprintCellPrefab(NumFmt(),
                        LoadPrefabComp<UiwTextLabel>(Pfb(KPfItemLabel))) },
                new GenItem { Category = CatCrafting, Key = "CraftList",  DisplayName = Fmt("蓝图列表 {0}", KPfCraftingBlueprintList), AssetPath = Pfb(KPfCraftingBlueprintList), DepKeys = new[] { "CraftCell" },
                    Build = () => BuildCraftingBlueprintListPrefab(
                        LoadPrefabComp<UiwCraftingBlueprintCell>(Pfb(KPfCraftingBlueprintCell))) },
                new GenItem { Category = CatCrafting, Key = "CraftView",  DisplayName = Fmt("制作主界面 {0}", KPfCraftingView), AssetPath = Pfb(KPfCraftingView),      DepKeys = new[] { "CraftList", "CraftInput", "Detail", "Simple", "Tab", "FoldTab", "Counter" },
                    Build = () => BuildCraftingViewPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfCraftingBlueprintList)),
                        LoadPrefabComp<UiwCraftingInputCell>(Pfb(KPfCraftingInputCell)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfItemDetail)),
                        LoadPrefabComp<UiwInventoryItemSimple>(Pfb(KPfItemSimple)),
                        LoadPrefabComp<UiwInventoryTab>(Pfb(KPfInventoryTab)),
                        LoadPrefabComp<UiwFoldTab>(Pfb(KPfFoldTab)),
                        LoadPrefabComp<UiwNumberCounter>(Pfb(KPfNumberCounter))) },
                new GenItem { Category = CatEquipment, Key = "EquipSlot",     DisplayName = Fmt("装备槽 {0}", KPfEquipSlot),   AssetPath = Pfb(KPfEquipSlot),          DepKeys = new string[0],
                    Build = () => BuildEquipmentSlotPrefab(NumFmt()) },
                new GenItem { Category = CatEquipment, Key = "EquipCandidate",DisplayName = Fmt("候选道具格子 {0}", KPfEquipCandidateCell), AssetPath = Pfb(KPfEquipCandidateCell), DepKeys = new string[0],
                    Build = () => BuildEquipmentCandidateCellPrefab(NumFmt()) },
                new GenItem { Category = CatEquipment, Key = "EquipBonusEntry",DisplayName = Fmt("属性加成条目 {0}", KPfEquipBonusEntry), AssetPath = Pfb(KPfEquipBonusEntry), DepKeys = new string[0],
                    Build = () => BuildEquipmentBonusEntryPrefab() },
                new GenItem { Category = CatEquipment, Key = "EquipSlotList",     DisplayName = Fmt("槽位列表 {0}", KPfEquipSlotList), AssetPath = Pfb(KPfEquipSlotList),      DepKeys = new[] { "EquipSlot" },
                    Build = () => BuildEquipmentSlotListPrefab(LoadPrefabComp<UiwEquipmentSlot>(Pfb(KPfEquipSlot))) },
                new GenItem { Category = CatEquipment, Key = "EquipCandidateList",DisplayName = Fmt("候选道具列表 {0}", KPfEquipCandidateList), AssetPath = Pfb(KPfEquipCandidateList), DepKeys = new[] { "EquipCandidate" },
                    Build = () => BuildEquipmentCandidateListPrefab(LoadPrefabComp<UiwInventoryItemCell>(Pfb(KPfEquipCandidateCell))) },
                new GenItem { Category = CatEquipment, Key = "EquipGroupPanel",   DisplayName = Fmt("装备组面板 {0}", KPfEquipGroupPanel), AssetPath = Pfb(KPfEquipGroupPanel), DepKeys = new[] { "EquipSlotList" },
                    Build = () => BuildEquipmentGroupPanelPrefab(LoadPrefabComp<UiwEquipmentSlotList>(Pfb(KPfEquipSlotList))) },
                new GenItem { Category = CatEquipment, Key = "EquipBonusPanel",   DisplayName = Fmt("属性加成面板 {0}", KPfEquipBonusPanel), AssetPath = Pfb(KPfEquipBonusPanel), DepKeys = new[] { "EquipBonusEntry" },
                    Build = () => BuildEquipmentBonusPanelPrefab(LoadPrefabComp<UiwEquipmentBonusEntry>(Pfb(KPfEquipBonusEntry))) },
                new GenItem { Category = CatEquipment, Key = "EquipSelectPanel",  DisplayName = Fmt("装备选择面板 {0}", KPfEquipSelectPanel), AssetPath = Pfb(KPfEquipSelectPanel), DepKeys = new[] { "EquipSlotList", "EquipCandidateList" },
                    Build = () => BuildEquipmentSelectPanelPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipSlotList)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipCandidateList))) },
                new GenItem { Category = CatEquipment, Key = "EquipView",         DisplayName = Fmt("装备主界面 {0}", KPfEquipView), AssetPath = Pfb(KPfEquipView),          DepKeys = new[] { "EquipGroupPanel", "EquipBonusPanel", "EquipSelectPanel" },
                    Build = () => BuildEquipmentViewPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipGroupPanel)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipBonusPanel)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfEquipSelectPanel))) },
                new GenItem { Category = CatSkill, Key = "SkillCell",   DisplayName = Fmt("技能网格条目 {0}", KPfSkillCell),   AssetPath = Pfb(KPfSkillCell),   DepKeys = new string[0],
                    Build = () => BuildSkillCellPrefab() },
                new GenItem { Category = CatSkill, Key = "SkillDetail", DisplayName = Fmt("技能列表条目 {0}", KPfSkillDetail), AssetPath = Pfb(KPfSkillDetail), DepKeys = new string[0],
                    Build = () => BuildSkillDetailPrefab() },
                new GenItem { Category = CatSkill, Key = "SkillGridList", DisplayName = Fmt("技能网格列表 {0}", KPfSkillGridList), AssetPath = Pfb(KPfSkillGridList), DepKeys = new[] { "SkillCell" },
                    Build = () => BuildSkillGridListPrefab(LoadPrefabComp<UiwSkillEntry>(Pfb(KPfSkillCell))) },
                new GenItem { Category = CatSkill, Key = "SkillOrderList", DisplayName = Fmt("技能顺序列表 {0}", KPfSkillOrderList), AssetPath = Pfb(KPfSkillOrderList), DepKeys = new[] { "SkillDetail" },
                    Build = () => BuildSkillOrderListPrefab(LoadPrefabComp<UiwSkillEntry>(Pfb(KPfSkillDetail))) },
                new GenItem { Category = CatSkill, Key = "SkillTooltip", DisplayName = Fmt("技能悬停弹窗 {0}", KPfSkillTooltip), AssetPath = Pfb(KPfSkillTooltip), DepKeys = new string[0],
                    Build = () => BuildSkillTooltipPrefab() },
                new GenItem { Category = CatSkill, Key = "SkillView", DisplayName = Fmt("技能主界面 {0}", KPfSkillView), AssetPath = Pfb(KPfSkillView), DepKeys = new[] { "SkillGridList", "SkillOrderList", "Filter" },
                    Build = () => BuildSkillViewPrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfSkillGridList)),
                        AssetDatabase.LoadAssetAtPath<GameObject>(Pfb(KPfSkillOrderList)),
                        LoadPrefabComp<Button>(Pfb(KPfFilterButton))) },
                new GenItem { Category = CatCommon, Key = "Manager",  DisplayName = Fmt("管理器 {0}", KPfInventoryManager), AssetPath = Pfb(KPfInventoryManager),  DepKeys = new[] { "DB", "Panel", "ShopPanel", "CraftView", "EquipView", "SkillView", "Tooltip", "SkillTooltip" },
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
            EditorUtility.DisplayDialog(Tr("生成完成"),
                Fmt("已生成全部资产：\n{0}/\n{1}/\n\n" +
                    "将 InventoryManager.prefab 拖入场景，点击 Play 即可验证。", DataDir, PrefabRoot), "OK");
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
                int c = EditorUtility.DisplayDialogComplex(Tr("依赖提示"),
                    Fmt("「{0}」依赖以下子项：\n\n{1}\n\n是否一并生成这些依赖？",
                        item.DisplayName,
                        string.Join("\n", onlyDeps.Select(d => "· " + d.DisplayName))),
                    Tr("一并生成"), Tr("取消"), Tr("仅生成此项"));
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
            string msg = Fmt("以下资产已存在，将被覆盖：\n\n{0}",
                             string.Join("\n", existing.Select(e => "· " + e.DisplayName)));
            return EditorUtility.DisplayDialog(Tr("覆盖确认"), msg, Tr("覆盖"), Tr("取消"));
        }

        /// <summary>按给定顺序构建（带进度条），末尾保存刷新。</summary>
        private static void BuildSubset(IList<GenItem> toGen)
        {
            try
            {
                for (int i = 0; i < toGen.Count; i++)
                {
                    EditorUtility.DisplayProgressBar(Tr("生成测试资产"),
                        toGen[i].DisplayName, (float)i / toGen.Count);
                    toGen[i].Build();
                }
                EditorUtility.DisplayProgressBar(Tr("生成测试资产"), Tr("保存并刷新资产数据库..."), 0.97f);
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
    }
}

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
    public static partial class InventoryDemoWizard
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
    }
}

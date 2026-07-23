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

    /// <summary>单例预制体 InventoryManager（管理器 + Canvas + 各视图装配）的构建。</summary>
    public static partial class InventoryDemoWizard
    {
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
    }
}

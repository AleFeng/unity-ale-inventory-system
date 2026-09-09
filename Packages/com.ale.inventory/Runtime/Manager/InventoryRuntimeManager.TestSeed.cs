// 测试道具自动填充：仅在编辑器与开发版构建中参与编译，避免随发布包一起出。
#if UNITY_EDITOR || DEVELOPMENT_BUILD

using Ale.Effect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ale.Inventory.Runtime
{
    /// <summary>
    /// <see cref="InventoryRuntimeManager"/> 的测试功能分部：启动时向指定仓库自动填入道具，
    /// 便于在没有游戏流程的情况下直接查看各 UI 界面。
    ///
    /// <para>整份文件由 <c>UNITY_EDITOR || DEVELOPMENT_BUILD</c> 门控 ——
    /// 发布构建里这些字段与逻辑都不存在。<see cref="Init"/> 中的调用点同样带门控。</para>
    /// </summary>
    public partial class InventoryRuntimeManager
    {
        /// <summary>
        /// 测试道具条目。
        /// </summary>
        [Serializable]
        public class TestItemEntry
        {
            [Tooltip("道具 ID，须与 InventoryDatabase 中的道具 ID 完全一致。")]
            public string itemId;
            [Tooltip("填入的数量（最小 1）。")]
            [Min(1)] public int count = 1;
        }
        
        [Header("测试功能")]
        [Tooltip("自动向 测试仓库填入道具。")]
        [SerializeField] private bool autoPopulateOnStart = true;
        [Tooltip("测试目标仓库ID（需与数据库中的 Inventory.id 一致）。")]
        [SerializeField] private string testInventoryId = "玩家背包";
        [Tooltip("测试目标仓库 自动填入的道具列表。")]
        [SerializeField] private TestItemEntry[] testItems;

        [Tooltip("额外把所有数据库（Databases）里配置的道具，各添加若干个到测试仓库。" +
                 "已在上面「测试道具列表」中配置的道具会跳过（保留其指定数量，不重复添加）。")]
        [SerializeField] private bool addAllConfiguredItems;
        [Tooltip("「添加所有配置表道具」时，每种道具添加的数量（最小 1）。")]
        [Min(1)] [SerializeField] private int addAllItemCount = 1;

        /// <summary>
        /// 编辑器测试：向 <see cref="testInventoryId"/> 填入 <see cref="testItems"/>。
        /// 由 <see cref="Init"/> 在 Awake 时机调用；仅填充数据，界面由各视图自行打开。
        /// </summary>
        private void TestFunction()
        {
            SetupDemoUseContext(); // 先备好「使用」所需的效果库与目标上下文
            AddTestItems(); // 先添加测试道具列表
            AddAllConfiguredItems(); // 再添加所有配置表道具（跳过已在测试列表的道具）
        }
        

        #region 测试功能：道具「使用」的效果库与目标上下文

        [Header("测试功能 · 道具使用")]
        [Tooltip("演示用效果库：配置后于启动时登记到 toolkit 效果库管理器，并为右键菜单的「使用」提供一个演示目标上下文。" +
                 "正式工程不要依赖这里——本包不认识任何领域系统，应由宿主自行注册效果库，" +
                 "并注入 InventoryRuntimeManager.UseTargetContextProvider 返回真实角色的效果上下文。")]
        [SerializeField] private EffectDatabase demoEffectDatabase;

        // 演示用的效果容器与上下文：主体固定为本管理器，一次建好后复用（Subject 不变，无需每次施加都新建）。
        private IEffectContext _demoUseContext;

        /// <summary>
        /// 登记演示用效果库，并注入「使用」的目标上下文提供者。未配置效果库时什么都不做
        /// （此时有使用效果的道具会得到 <c>NoContext</c>，不施加也不扣减）。
        ///
        /// <para>已由宿主注入过 <see cref="UseTargetContextProvider"/> 时<b>不覆盖</b>——
        /// 演示接线不该盖掉真实工程的接线。</para>
        /// </summary>
        private void SetupDemoUseContext()
        {
            if (!demoEffectDatabase) return;

            // 登记后，效果可按 id 经全局注册表解析（EffectDataManager 注册时会把自己登记为默认来源）。
            EffectDataManager.Instance.Register(demoEffectDatabase);

            if (UseTargetContextProvider != null) return;

            // EffectApplier 需要「目标容器 + 效果定义」两样：定义来自上面登记的效果库；
            // 容器经上下文服务 IEffectContainerSource 解析——这也是真实宿主该走的形状（多角色时按主体查表）。
            var container = new EffectContainer(this);
            var ctx = new EffectContext { Subject = this };
            ctx.RegisterService<IEffectContainerSource>(new DemoContainerSource(container));
            _demoUseContext = ctx;

            UseTargetContextProvider = _ => _demoUseContext;
        }

        /// <summary>演示用容器来源：Demo 只有一个主体，故恒返回同一个容器。</summary>
        private sealed class DemoContainerSource : IEffectContainerSource
        {
            private readonly EffectContainer _container;

            public DemoContainerSource(EffectContainer container) => _container = container;

            public EffectContainer GetContainer(object subject) => _container;
        }

        #endregion

        /// <summary>
        /// 测试功能：添加 测试用道具列表
        /// </summary>
        private void AddTestItems()
        {
            if (!autoPopulateOnStart || testItems == null) return;

            if (string.IsNullOrEmpty(testInventoryId))
            {
                Debug.LogWarning("[InventoryTest] testInventoryId 为空，跳过。");
                return;
            }

            foreach (var entry in testItems)
            {
                if (string.IsNullOrEmpty(entry.itemId)) continue;
                if (!TryAddItem(testInventoryId, entry.itemId, Mathf.Max(1, entry.count)))
                    Debug.LogWarning($"[InventoryTest] 添加失败：{entry.itemId} × {entry.count}" +
                                     "（仓库已满、道具 ID 不存在或参数无效）");
            }
        }
        
        /// <summary>
        /// 测试功能：把所有数据库（<see cref="databases"/>）中配置的道具，各按 <see cref="addAllItemCount"/>
        /// 的数量添加到 <see cref="testInventoryId"/>。已在 <see cref="testItems"/> 中配置的道具会跳过
        /// （保留其指定数量，不重复添加）；同一道具 ID 跨库仅添加一次。由 <see cref="Init"/> 调用。
        /// </summary>
        private void AddAllConfiguredItems()
        {
            if (!autoPopulateOnStart || !addAllConfiguredItems || databases == null) return;

            if (string.IsNullOrEmpty(testInventoryId))
            {
                Debug.LogWarning("[InventoryTest] testInventoryId 为空，跳过「添加所有配置表道具」。");
                return;
            }

            // 预置「已在 testItems 中配置的道具 ID」；添加时 HashSet 同时承担 跨库去重
            // （skip.Add 返回 false = 已在 testItems 或已处理过 → 跳过）。
            var skip = new HashSet<string>();
            if (testItems != null)
                foreach (var e in testItems)
                    if (e != null && !string.IsNullOrEmpty(e.itemId)) skip.Add(e.itemId);

            int count = Mathf.Max(1, addAllItemCount);
            foreach (var db in databases)
            {
                if (!db) continue;
                foreach (var item in db.Items)
                {
                    if (item == null || string.IsNullOrEmpty(item.id)) continue;
                    if (!skip.Add(item.id)) continue;   // 已在 testItems 或已处理过 → 跳过
                    if (!TryAddItem(testInventoryId, item.id, count))
                        Debug.LogWarning($"[InventoryTest] 添加失败：{item.id} × {count}" +
                                         "（仓库已满、道具 ID 不存在或参数无效）");
                }
            }
        }

    }
}

#endif

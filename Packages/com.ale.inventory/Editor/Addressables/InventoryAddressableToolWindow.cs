using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// Addressable工具窗口（仅 IS_ADDRESSABLE 编译）。
    /// 在「直接 Object 引用」与「AssetReference 授权（GUID）」
    /// 两种存储之间批量互转某个 <see cref="InventoryDatabase"/> 的<b>全部</b>资源字段
    /// （属性系统对象值 + 固定资源字段 Skill.icon / SkillTemplate.icon / FunctionTag.backgroundSprite）——
    /// 因二者磁盘格式不同、无法靠同名字段自动共用，需本工具做一次性转换。
    ///
    /// <list type="bullet">
    ///   <item><b>Object → GUID</b>（采用 Addressables）：把实时引用经 <see cref="AddressableAssetRefResolver"/> 取其 GUID
    ///   写入授权地址、清空硬引用（此后加载数据库不再一并载入资源）。<b>本工具不自动把资源加入 Addressable 分组</b>，
    ///   分组由用户在 Addressables 窗口自行管理，转换完成后弹窗提醒。</item>
    ///   <item><b>GUID → Object</b>（还原）：把授权 GUID 解析回实时引用、清空地址（用于关闭 IS_ADDRESSABLE）。</item>
    /// </list>
    ///
    /// 「选数据库 + 逐帧步进 + 进度条 + 日志 + 取消 + 完成收尾」等通用能力继承自 <see cref="InventoryToolWindowBase"/>；
    /// 本类只提供 Addressable 专有的操作按钮、步骤构建与完成/取消文案。转换逐帧步进执行，实时刷新进度条与日志。
    /// 通过反射遍历数据库对象图收集所有 <see cref="AttributeValue"/>（遇 <see cref="UnityEngine.Object"/> 引用即止），
    /// 覆盖全部承载属性的集合，对未来新增集合稳健。
    /// </summary>
    [InitializeOnLoad]
    public class InventoryAddressableToolWindow : InventoryToolWindowBase
    {
        // 注入钩子：让 core 欢迎窗口（对 Addressables 零依赖、看不见本类型）得以打开本窗口。
        // 仅在本程序集参与编译（IS_ADDRESSABLE 启用）时执行，故宏关闭时欢迎窗口对应按钮自动隐藏。
        static InventoryAddressableToolWindow()
        {
            InventoryWelcomeWindow.OpenAddressableMigration = Open;
        }

        /// <summary>窗口支持的批量操作。</summary>
        private enum Op { ToGuid, ToObject, ClearLive, ClearGuid }

        private Op _op;   // 本次操作（用于进度 / 日志 / 完成提醒文案）

        #region 打开窗口

        [MenuItem("Tools/Inventory System/Addressable/Addressable工具窗口", priority = 200)]
        public static void Open()
        {
            var win = GetWindow<InventoryAddressableToolWindow>(true, "资源引用迁移（Addressables）", true);
            win.minSize = new Vector2(460f, 420f);
            win.Show();
        }

        // ── 绘制（基类骨架的钩子）─────────────────────────────────────────────────────

        protected override string DoneVerb => IsClearing(_op) ? "已清空" : "已转换";

        #endregion

        #region 绘制与启动

        protected override void DrawHeader()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("在「Object 引用 ↔ AssetReference(GUID)」间批量转换数据库的全部资源字段。",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
        }

        protected override void DrawOperations()
        {
            using (new EditorGUI.DisabledScope(IsRunning || !database))
            {
                // 迁移：两种存储互转
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("资源直接引用 → AssetReference(GUID)", GUILayout.Height(28)))
                        StartOp(Op.ToGuid);
                    if (GUILayout.Button("AssetReference(GUID) → 资源直接引用", GUILayout.Height(28)))
                        StartOp(Op.ToObject);
                }

                // 清空（破坏性）：无条件清空对应存储的全部值，逐处打印到日志。
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("清空 资源直接引用", GUILayout.Height(24)))
                        ConfirmAndStart(Op.ClearLive);
                    if (GUILayout.Button("清空 AssetReference(GUID)", GUILayout.Height(24)))
                        ConfirmAndStart(Op.ClearGuid);
                }
            }
        }

        // ── 迁移流程 ─────────────────────────────────────────────────────────────────

        /// <summary>清空类操作（破坏性）先弹二次确认，确认后再执行。</summary>
        private void ConfirmAndStart(Op op)
        {
            if (!database) { Log("⚠ 请先选择一个 InventoryDatabase。"); return; }
            string what = op == Op.ClearLive ? "资源直接引用" : "AssetReference(GUID)";
            bool ok = EditorUtility.DisplayDialog("危险操作确认",
                $"即将无条件清空「{database.name}」的全部{what}。\n\n" +
                "⚠ 这是危险操作：此操作可撤销（Ctrl+Z），但若对应条目另一侧（GUID / 实时引用）为空，将彻底失去该资源引用。\n\n是否执行？",
                "执行", "取消");
            if (ok) StartOp(op);
        }

        private void StartOp(Op op)
        {
            if (!database) { Log("⚠ 请先选择一个 InventoryDatabase。"); return; }
            if (IsRunning) return;

            _op = op;
            Undo.RegisterCompleteObjectUndo(database, UndoLabel(op));

            var steps = BuildSteps(database, op);
            RunSteps(steps, $"—— 开始{OpStartText(op)}：「{database.name}」，共 {steps.Count} 项 ——");
        }

        private static bool IsClearing(Op op) => op == Op.ClearLive || op == Op.ClearGuid;

        private static string UndoLabel(Op op)
        {
            switch (op)
            {
                case Op.ToGuid:    return "迁移资源引用为 GUID";
                case Op.ToObject:  return "还原资源引用为 Object";
                case Op.ClearLive: return "清空资源直接引用";
                default:           return "清空 AssetReference(GUID)";
            }
        }

        private static string OpStartText(Op op)
        {
            switch (op)
            {
                case Op.ToGuid:    return "迁移为 AssetReference(GUID)";
                case Op.ToObject:  return "还原为 Object 引用";
                case Op.ClearLive: return "清空资源直接引用";
                default:           return "清空 AssetReference(GUID)";
            }
        }

        #endregion

        #region 运行回调

        /// <summary>处理完成收尾：保存数据库并打印汇总日志（基类在进度条推到 100% 前调用）。</summary>
        protected override void OnRunComplete()
        {
            if (!database) return;

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            bool clearing = IsClearing(_op);
            Log(clearing
                ? $"✔ 完成：共清空 {Changed} 处。"
                : $"✔ 完成：共转换 {Changed} 处资源引用。");
        }

        /// <summary>操作完成 信息弹窗（基类在进度条重绘到 100% 后经 delayCall 调用）。</summary>
        protected override void OnRunFinished()
        {
            if (!database) return;

            // 完成提醒。转 GUID 后本工具不自动把资源加入 Addressable 分组，需用户自行标记。
            if (_op == Op.ToGuid)
            {
                Log("⚠ 提醒：本工具未自动把资源加入 Addressable 分组，请在 Addressables Groups 窗口中将相关资源标记为 Addressable，否则运行时无法按 GUID 加载。");
                EditorUtility.DisplayDialog("迁移完成",
                    $"已把「{database.name}」的 {Changed} 处资源引用转换为 GUID。\n\n" +
                    "本工具不会自动把资源加入 Addressable 分组。\n请在 Addressables Groups 窗口中把相关资源标记为 Addressable，" +
                    "否则运行时无法按 GUID 加载。",
                    "知道了");
            }
            else if (_op == Op.ToObject)
            {
                EditorUtility.DisplayDialog("还原完成",
                    $"已把「{database.name}」的 {Changed} 处 GUID 还原为直接资源引用。",
                    "知道了");
            }
            else
            {
                string what = _op == Op.ClearLive ? "资源直接引用" : "AssetReference(GUID)";
                EditorUtility.DisplayDialog("清空完成",
                    $"已清空「{database.name}」的 {Changed} 处{what}。",
                    "知道了");
            }
        }

        /// <summary>取消收尾：保存已改动并打印进度。</summary>
        protected override void OnRunCanceled()
        {
            if (database)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
            }
            Log($"■ 已取消：已{(IsClearing(_op) ? "清空" : "转换")} {Changed} 处（进度 {StepIndex}/{StepCount}）。");
        }

        // ── 构建转换步骤 ─────────────────────────────────────────────────────────────

        private List<Func<string>> BuildSteps(InventoryDatabase db, Op op)
        {
            var steps = new List<Func<string>>();

            // 属性系统对象值（反射遍历全库）
            foreach (var av in CollectObjectValues(db))
            {
                var cap = av;
                steps.Add(() => ProcessAttributeValue(cap, op));
            }

            // 固定资源字段（机制A）
            foreach (var s in db.Skills)
            {
                var c = s;
                steps.Add(() => ProcessFixed(() => c.icon, v => c.icon = v,
                    () => c.iconAddress, v => c.iconAddress = v, op, $"技能「{c.id}」图标"));
            }
            foreach (var t in db.SkillTemplates)
            {
                var c = t;
                steps.Add(() => ProcessFixed(() => c.icon, v => c.icon = v,
                    () => c.iconAddress, v => c.iconAddress = v, op, $"技能模板「{c.name}」图标"));
            }
            foreach (var ft in db.FunctionTags)
            {
                var c = ft;
                steps.Add(() => ProcessFixed(() => c.backgroundSprite, v => c.backgroundSprite = v,
                    () => c.backgroundSpriteAddress, v => c.backgroundSpriteAddress = v, op, $"功能标签「{c.name}」背景图"));
            }

            return steps;
        }

        /// <summary>按操作处理单个对象类 <see cref="AttributeValue"/> 的全部元素，返回一条汇总日志（无变化返回 null）。</summary>
        private string ProcessAttributeValue(AttributeValue av, Op op)
        {
            var raw   = av.RawObjects;
            var names = new List<string>();
            for (int i = 0; i < raw.Count; i++)
            {
                switch (op)
                {
                    case Op.ToGuid:
                    {
                        var obj = av.GetObject(i);
                        if (!obj) continue;
                        string guid = AddressableAssetRefResolver.Instance.ToGuid(obj, warnIfUnregistered: false);
                        if (string.IsNullOrEmpty(guid)) continue;
                        av.SetObjAddress(i, guid);
                        av.SetObject(i, null);
                        names.Add(obj.name);
                        Changed++;
                        break;
                    }
                    case Op.ToObject:
                    {
                        if (av.GetObject(i)) continue;
                        string key = av.GetObjAddress(i);
                        if (string.IsNullOrEmpty(key)) continue;
                        var obj = AddressableAssetRefResolver.Instance.FromGuid(key);
                        if (!obj) continue;
                        av.SetObject(i, obj);
                        av.SetObjAddress(i, string.Empty);
                        names.Add(obj.name);
                        Changed++;
                        break;
                    }
                    case Op.ClearLive:
                    {
                        var obj = av.GetObject(i);
                        if (!obj) continue;
                        av.SetObject(i, null);
                        names.Add(obj.name);
                        Changed++;
                        break;
                    }
                    case Op.ClearGuid:
                    {
                        string key = av.GetObjAddress(i);
                        if (string.IsNullOrEmpty(key)) continue;
                        av.SetObjAddress(i, string.Empty);
                        names.Add(DescribeGuid(key));
                        Changed++;
                        break;
                    }
                }
            }
            if (names.Count == 0) return null;
            return $"属性资源：{string.Join("、", names)} {OpArrowText(op)}";
        }

        #endregion

        #region 辅助

        /// <summary>各操作在属性资源汇总日志里的箭头 / 结果文案。</summary>
        private static string OpArrowText(Op op)
        {
            switch (op)
            {
                case Op.ToGuid:    return "→ GUID";
                case Op.ToObject:  return "← 引用";
                case Op.ClearLive: return "✖ 已清空直接引用";
                default:           return "✖ 已清空 GUID";
            }
        }

        /// <summary>尽量把被清除的授权 GUID 描述为可读文本：能解析回资源则显示资源名，否则显示 GUID 原文。</summary>
        private static string DescribeGuid(string key)
        {
            var obj = AddressableAssetRefResolver.Instance.FromGuid(key);
            return obj ? obj.name : key;
        }

        /// <summary>按操作处理单个固定 <see cref="Sprite"/> 资源字段（用 get/set 委托，避免闭包内 ref）。返回日志（无变化返回 null）。</summary>
        private string ProcessFixed(Func<Sprite> getLive, Action<Sprite> setLive,
            Func<string> getAddr, Action<string> setAddr, Op op, string label)
        {
            switch (op)
            {
                case Op.ToGuid:
                {
                    var live = getLive();
                    if (!live) return null;
                    string guid = AddressableAssetRefResolver.Instance.ToGuid(live, warnIfUnregistered: false);
                    if (string.IsNullOrEmpty(guid)) return null;
                    setAddr(guid);
                    setLive(null);
                    Changed++;
                    return $"{label}：{live.name} → GUID";
                }
                case Op.ToObject:
                {
                    if (getLive() || string.IsNullOrEmpty(getAddr())) return null;
                    var obj = AddressableAssetRefResolver.Instance.FromGuid(getAddr()) as Sprite;
                    if (!obj) return null;
                    setLive(obj);
                    setAddr(null);
                    Changed++;
                    return $"{label}：GUID → {obj.name}";
                }
                case Op.ClearLive:
                {
                    var live = getLive();
                    if (!live) return null;
                    setLive(null);
                    Changed++;
                    return $"{label}：清空直接引用（{live.name}）";
                }
                default: // Op.ClearGuid
                {
                    string addr = getAddr();
                    if (string.IsNullOrEmpty(addr)) return null;
                    setAddr(null);
                    Changed++;
                    return $"{label}：清空 GUID（{DescribeGuid(addr)}）";
                }
            }
        }

        // ── 反射遍历：收集数据库内全部对象类 AttributeValue ─────────────────────────────

        private static List<AttributeValue> CollectObjectValues(InventoryDatabase db)
        {
            var sink    = new List<AttributeValue>();
            var visited = new HashSet<object>(RefComparer.Instance);
            // 从数据库自身字段起步（数据库本身是 UnityEngine.Object，直接下钻其序列化字段）
            foreach (var f in typeof(InventoryDatabase).GetFields(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Walk(f.GetValue(db), sink, visited);
            return sink;
        }

        private static void Walk(object node, List<AttributeValue> sink, HashSet<object> visited)
        {
            if (node == null) return;

            // 命中属性值：收集对象类的，停止下钻
            if (node is AttributeValue av)
            {
                if (av.Type.IsObjectBacked()) sink.Add(av);
                return;
            }

            // 外部资源引用 / 值类型（基元 / 枚举 / Vector* / Color 等结构体，均不含 AttributeValue）/ 字符串：不下钻
            if (node is Object) return;
            var type = node.GetType();
            if (type.IsValueType || node is string) return;
            if (!visited.Add(node)) return;

            // 集合逐元素下钻
            if (node is System.Collections.IEnumerable en)
            {
                foreach (var e in en) Walk(e, sink, visited);
                return;
            }

            // 普通 [Serializable] 数据类：逐字段下钻
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var ft = f.FieldType;
                if (ft.IsPrimitive || ft.IsEnum || ft == typeof(string)) continue;
                if (typeof(Object).IsAssignableFrom(ft)) continue;   // 跳过外部资源引用字段
                Walk(f.GetValue(node), sink, visited);
            }
        }

        /// <summary>引用相等比较器（避免值类型 / 重写 Equals 干扰访问过标记）。</summary>
        private sealed class RefComparer : IEqualityComparer<object>
        {
            public static readonly RefComparer Instance = new RefComparer();
            bool IEqualityComparer<object>.Equals(object a, object b) => ReferenceEquals(a, b);
            int IEqualityComparer<object>.GetHashCode(object o) => RuntimeHelpers.GetHashCode(o);
        }
        #endregion

    }
}

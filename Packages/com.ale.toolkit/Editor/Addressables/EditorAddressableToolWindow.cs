using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ale.Toolkit.Runtime;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 一个「固定资源字段」的授权 GUID ↔ 直接引用 互转描述：一对 get/set（实时 <see cref="Sprite"/> 引用与授权地址）
    /// + 日志标签。由 <see cref="EditorAddressableToolWindow{TDb}.FixedFields"/> 提供（如 Skill.icon / Tag.backgroundSprite）。
    /// </summary>
    public struct AddressableFixedField
    {
        public readonly Func<Sprite>   GetLive;
        public readonly Action<Sprite> SetLive;
        public readonly Func<string>   GetAddr;
        public readonly Action<string> SetAddr;
        public readonly string         Label;

        public AddressableFixedField(Func<Sprite> getLive, Action<Sprite> setLive,
            Func<string> getAddr, Action<string> setAddr, string label)
        {
            GetLive = getLive; SetLive = setLive;
            GetAddr = getAddr; SetAddr = setAddr;
            Label   = label;
        }
    }

    /// <summary>
    /// Addressable 资源引用迁移工具窗口（泛型基类，仅 IS_ADDRESSABLE 编译）。
    /// 在「直接 Object 引用」与「AssetReference 授权（GUID）」两种存储之间批量互转某数据库的<b>全部</b>资源字段：
    /// 属性系统对象值（经 <see cref="AttributeValueWalker"/> 反射遍历全库）+ 固定资源字段（由子类经 <see cref="FixedFields"/> 提供）。
    ///
    /// <para>「选数据库 + 逐帧步进 + 进度条 + 日志 + 取消 + 完成收尾」继承自 <see cref="EditorToolWindowBase{TDb}"/>。
    /// 子类只需：① 提供 <see cref="FixedFields"/>（本库属性系统之外的具名 Sprite 资源字段）；
    /// ② 提供打开窗口的 <c>[MenuItem]</c> 入口。</para>
    /// </summary>
    /// <typeparam name="TDb">数据库资产类型。</typeparam>
    public abstract class EditorAddressableToolWindow<TDb> : EditorToolWindowBase<TDb> where TDb : ScriptableObject
    {
        /// <summary>窗口支持的批量操作。</summary>
        private enum Op { ToGuid, ToObject, ClearLive, ClearGuid }

        private Op _op;   // 本次操作（用于进度 / 日志 / 完成提醒文案）

        /// <summary>本库属性系统之外的具名 Sprite 资源字段（如 Skill.icon / Tag.backgroundSprite）。</summary>
        protected abstract IEnumerable<AddressableFixedField> FixedFields(TDb db);

        // ── 绘制（基类骨架的钩子）─────────────────────────────────────────────────────

        protected override string DoneVerb => IsClearing(_op) ? "已清空" : "已转换";

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
            if (!database) { Log("⚠ 请先选择一个数据库。"); return; }
            string what = op == Op.ClearLive ? "资源直接引用" : "AssetReference(GUID)";
            bool ok = EditorUtility.DisplayDialog("危险操作确认",
                $"即将无条件清空「{database.name}」的全部{what}。\n\n" +
                "⚠ 这是危险操作：此操作可撤销（Ctrl+Z），但若对应条目另一侧（GUID / 实时引用）为空，将彻底失去该资源引用。\n\n是否执行？",
                "执行", "取消");
            if (ok) StartOp(op);
        }

        private void StartOp(Op op)
        {
            if (!database) { Log("⚠ 请先选择一个数据库。"); return; }
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

        // ── 运行回调 ─────────────────────────────────────────────────────────────────

        /// <summary>处理完成收尾：保存数据库并打印汇总日志（基类在进度条推到 100% 前调用）。</summary>
        protected override void OnRunComplete()
        {
            if (!database) return;

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            bool clearing = IsClearing(_op);
            Log(clearing
                ? $"✔ 完成：共清空 {changed} 处。"
                : $"✔ 完成：共转换 {changed} 处资源引用。");
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
                    $"已把「{database.name}」的 {changed} 处资源引用转换为 GUID。\n\n" +
                    "本工具不会自动把资源加入 Addressable 分组。\n请在 Addressables Groups 窗口中把相关资源标记为 Addressable，" +
                    "否则运行时无法按 GUID 加载。",
                    "知道了");
            }
            else if (_op == Op.ToObject)
            {
                EditorUtility.DisplayDialog("还原完成",
                    $"已把「{database.name}」的 {changed} 处 GUID 还原为直接资源引用。",
                    "知道了");
            }
            else
            {
                string what = _op == Op.ClearLive ? "资源直接引用" : "AssetReference(GUID)";
                EditorUtility.DisplayDialog("清空完成",
                    $"已清空「{database.name}」的 {changed} 处{what}。",
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
            Log($"■ 已取消：已{(IsClearing(_op) ? "清空" : "转换")} {changed} 处（进度 {StepIndex}/{StepCount}）。");
        }

        // ── 构建转换步骤 ─────────────────────────────────────────────────────────────

        private List<Func<string>> BuildSteps(TDb db, Op op)
        {
            var steps = new List<Func<string>>();

            // 属性系统对象值（反射遍历全库，通用遍历见 AttributeValueWalker）
            foreach (var av in AttributeValueWalker.Collect(db))
            {
                var cap = av;
                steps.Add(() => ProcessAttributeValue(cap, op));
            }

            // 固定资源字段（由子类提供）
            foreach (var ff in FixedFields(db))
            {
                var f = ff;
                steps.Add(() => ProcessFixed(f.GetLive, f.SetLive, f.GetAddr, f.SetAddr, op, f.Label));
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
                        changed++;
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
                        changed++;
                        break;
                    }
                    case Op.ClearLive:
                    {
                        var obj = av.GetObject(i);
                        if (!obj) continue;
                        av.SetObject(i, null);
                        names.Add(obj.name);
                        changed++;
                        break;
                    }
                    case Op.ClearGuid:
                    {
                        string key = av.GetObjAddress(i);
                        if (string.IsNullOrEmpty(key)) continue;
                        av.SetObjAddress(i, string.Empty);
                        names.Add(DescribeGuid(key));
                        changed++;
                        break;
                    }
                }
            }
            if (names.Count == 0) return null;
            return $"属性资源：{string.Join("、", names)} {OpArrowText(op)}";
        }

        // ── 辅助 ─────────────────────────────────────────────────────────────────────

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
                    changed++;
                    return $"{label}：{live.name} → GUID";
                }
                case Op.ToObject:
                {
                    if (getLive() || string.IsNullOrEmpty(getAddr())) return null;
                    var obj = AddressableAssetRefResolver.Instance.FromGuid(getAddr()) as Sprite;
                    if (!obj) return null;
                    setLive(obj);
                    setAddr(null);
                    changed++;
                    return $"{label}：GUID → {obj.name}";
                }
                case Op.ClearLive:
                {
                    var live = getLive();
                    if (!live) return null;
                    setLive(null);
                    changed++;
                    return $"{label}：清空直接引用（{live.name}）";
                }
                default: // Op.ClearGuid
                {
                    string addr = getAddr();
                    if (string.IsNullOrEmpty(addr)) return null;
                    setAddr(null);
                    changed++;
                    return $"{label}：清空 GUID（{DescribeGuid(addr)}）";
                }
            }
        }
    }
}

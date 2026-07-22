using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// PlayerSettings 脚本宏定义与程序集反射工具。
    /// 供 InventorySystem 编辑器脚本内部使用，不依赖外部插件。
    /// </summary>
    internal static class InventoryDefineUtils
    {
        // ── 宏定义 ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 向所有有效的 <see cref="BuildTargetGroup"/> 添加或移除指定的脚本宏定义。
        /// </summary>
        /// <param name="add"><c>true</c> 添加；<c>false</c> 移除。</param>
        /// <param name="define">宏名称（例如 "IS_TMP"）。</param>
        public static void ApplyDefine(bool add, string define)
        {
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown) continue;

#if UNITY_2021_2_OR_NEWER
                NamedBuildTarget named;
                try
                {
                    named = NamedBuildTarget.FromBuildTargetGroup(group);
                }
                catch (Exception)
                {
                    continue;
                }

                if (named == NamedBuildTarget.Unknown) continue;

                string defines = PlayerSettings.GetScriptingDefineSymbols(named);
                var list = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                  .ToList();

                bool contains = list.Contains(define);
                if (add && !contains)
                {
                    list.Add(define);
                    PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", list));
                    Debug.Log($"[InventorySystem] Added define '{define}' to {named}.");
                }
                else if (!add && contains)
                {
                    list.RemoveAll(d => d == define);
                    PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", list));
                    Debug.Log($"[InventorySystem] Removed define '{define}' from {named}.");
                }
#else
                string definesForGroup =
                    PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                var listForGroup =
                    definesForGroup.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .ToList();

                bool containsForGroup = listForGroup.Contains(define);
                if (add && !containsForGroup)
                {
                    listForGroup.Add(define);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        group, string.Join(";", listForGroup));
                    Debug.Log($"[InventorySystem] Added define '{define}' to {group}.");
                }
                else if (!add && containsForGroup)
                {
                    listForGroup.RemoveAll(d => d == define);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        group, string.Join(";", listForGroup));
                    Debug.Log($"[InventorySystem] Removed define '{define}' from {group}.");
                }
#endif
            }
        }

        // ── 程序集反射 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 检测当前已加载的程序集中是否存在指定命名空间（或以其为前缀的命名空间）。
        /// 可用于判断某个 Unity 包（如 com.unity.localization / TMPro）是否已安装。
        /// </summary>
        /// <param name="namespaceName">命名空间全名，例如 "UnityEngine.Localization"。</param>
        public static bool HasNamespace(string namespaceName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException ex)
                    { types = ex.Types.Where(t => t != null).ToArray(); }

                    foreach (var t in types)
                    {
                        var ns = t.Namespace;
                        if (ns == null) continue;
                        if (ns == namespaceName ||
                            ns.StartsWith(namespaceName + ".", StringComparison.Ordinal))
                            return true;
                    }
                }
                catch { /* 忽略单个程序集的反射错误 */ }
            }
            return false;
        }

        /// <summary>
        /// 检测当前已加载的程序集中是否存在指定类（支持全限定名或简单类名）。
        /// </summary>
        /// <param name="className">类名，例如 "TMPro.TMP_Text" 或 "TMP_Text"。</param>
        public static bool HasClass(string className)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.GetType(className, false, false) != null) return true;

                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException ex)
                    { types = ex.Types.Where(t => t != null).ToArray(); }

                    if (types.Any(t => t.Name == className)) return true;
                }
                catch { /* 忽略单个程序集错误 */ }
            }
            return false;
        }
    }
}

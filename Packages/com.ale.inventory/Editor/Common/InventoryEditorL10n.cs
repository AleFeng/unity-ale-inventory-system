using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>编辑器 UI 显示语言。</summary>
    public enum EditorLanguage
    {
        /// <summary>简体中文（源语言，恒等返回）。</summary>
        ChineseSimplified = 0,
        /// <summary>English。</summary>
        English = 1,
        /// <summary>日本語。</summary>
        Japanese = 2,
    }

    /// <summary>
    /// 编辑器 UI 三语（中 / 英 / 日）本地化服务。仅作用于 <see cref="InventoryWelcomeWindow"/> 与
    /// <see cref="InventoryEditorWindow"/> 等编辑器窗口的界面文本，与运行时内容本地化
    /// （<c>IS_LOCALIZATION</c> / Unity Localization）完全无关。
    ///
    /// <para><b>以中文原文为键：</b>调用点直接传入中文字面量（如 <c>Tr("快捷操作")</c>）。
    /// 当前语言为中文时原样返回；英 / 日语言查各自译表，缺条目则回退中文——
    /// 缺翻译只会退化为中文显示，绝不报错或留空。</para>
    ///
    /// <para><b>译表按区域分部：</b>各 <c>InventoryEditorL10n.Table.*.cs</c> 通过实现对应的
    /// <c>RegisterXxx()</c> 分部方法登记本区域译文；未实现的分部方法在编译期被消除，
    /// 因此可分步增量补充译表而无需改动本文件。</para>
    ///
    /// <para>延迟初始化（首次访问时读取 <see cref="EditorPrefs"/> 并登记译表），
    /// 仿 <see cref="InventoryEditorStyles"/> 的风格，避免静态构造期或非 GUI 线程出错。</para>
    /// </summary>
    public static partial class InventoryEditorL10n
    {
        private static bool _initialized;
        private static EditorLanguage _current;
        private static bool _translateEnums;

        // 中文原文 → 目标语言译文（仅登记与中文不同的条目）。
        private static readonly Dictionary<string, string> _en = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _ja = new Dictionary<string, string>();

        // 枚举显示名。键为 "类型名.值名"（如 "EFieldType.Sprite"）。中文缺条目时回退枚举原名。
        private static readonly Dictionary<string, string> _enumZh = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _enumEn = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _enumJa = new Dictionary<string, string>();

        // ── 初始化与译表登记 ──────────────────────────────────────────────────────

        private static void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;

            _current        = (EditorLanguage)EditorPrefs.GetInt(
                                  InventoryEditorPrefs.EditorLanguage, (int)EditorLanguage.ChineseSimplified);
            _translateEnums = EditorPrefs.GetBool(InventoryEditorPrefs.EditorTranslateEnums, false);

            RegisterTables();
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

        private static void RegisterTables()
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
        }

        /// <summary>登记一条译文。<paramref name="en"/> / <paramref name="ja"/> 为空则该语言回退中文。</summary>
        private static void Add(string zh, string en, string ja)
        {
            if (!string.IsNullOrEmpty(en)) _en[zh] = en;
            if (!string.IsNullOrEmpty(ja)) _ja[zh] = ja;
        }

        /// <summary>登记一个枚举值的显示名。<paramref name="zh"/> 为空则中文回退枚举原名。</summary>
        private static void AddEnum(Enum value, string en, string ja, string zh = null)
        {
            string key = EnumKey(value);
            if (!string.IsNullOrEmpty(zh)) _enumZh[key] = zh;
            if (!string.IsNullOrEmpty(en)) _enumEn[key] = en;
            if (!string.IsNullOrEmpty(ja)) _enumJa[key] = ja;
        }

        private static string EnumKey(Enum value) => value.GetType().Name + "." + value.ToString();

        // ── 当前语言与开关 ────────────────────────────────────────────────────────

        /// <summary>当前编辑器 UI 语言。赋值时持久化并刷新所有相关窗口。</summary>
        public static EditorLanguage Current
        {
            get { EnsureInit(); return _current; }
            set
            {
                EnsureInit();
                if (_current == value) return;
                _current = value;
                EditorPrefs.SetInt(InventoryEditorPrefs.EditorLanguage, (int)value);
                RepaintAll();
            }
        }

        /// <summary>
        /// 是否翻译枚举下拉值（<c>EFieldType</c> / <c>ShopType</c> 等）。
        /// 关闭（默认）时枚举一律显示代码中的原名。赋值时持久化并刷新。
        /// </summary>
        public static bool TranslateEnums
        {
            get { EnsureInit(); return _translateEnums; }
            set
            {
                EnsureInit();
                if (_translateEnums == value) return;
                _translateEnums = value;
                EditorPrefs.SetBool(InventoryEditorPrefs.EditorTranslateEnums, value);
                RepaintAll();
            }
        }

        // ── 翻译入口 ──────────────────────────────────────────────────────────────

        /// <summary>翻译一段中文文本。缺译文时回退中文原文。</summary>
        public static string Tr(string zh)
        {
            if (string.IsNullOrEmpty(zh)) return zh;
            EnsureInit();
            switch (_current)
            {
                case EditorLanguage.English:  return _en.TryGetValue(zh, out var e) ? e : zh;
                case EditorLanguage.Japanese: return _ja.TryGetValue(zh, out var j) ? j : zh;
                default:                      return zh;
            }
        }

        /// <summary>翻译中文模板并套入参数。模板以中文原文为键（如 <c>Fmt("删除{0}", name)</c>）。</summary>
        public static string Fmt(string zhTemplate, params object[] args)
        {
            return string.Format(Tr(zhTemplate), args);
        }

        /// <summary>
        /// 枚举值的显示名。<see cref="TranslateEnums"/> 关闭时返回枚举原名（<c>value.ToString()</c>）；
        /// 开启时查显示名映射，缺条目回退枚举原名。
        /// </summary>
        public static string TrEnum(Enum value)
        {
            if (value == null) return string.Empty;
            EnsureInit();
            if (!_translateEnums) return value.ToString();

            string key = EnumKey(value);
            switch (_current)
            {
                case EditorLanguage.English:  return _enumEn.TryGetValue(key, out var e) ? e : value.ToString();
                case EditorLanguage.Japanese: return _enumJa.TryGetValue(key, out var j) ? j : value.ToString();
                default:                      return _enumZh.TryGetValue(key, out var z) ? z : value.ToString();
            }
        }

        /// <summary>
        /// 枚举下拉框，显示名经 <see cref="TrEnum"/> 解析（受 <see cref="TranslateEnums"/> 门控）。
        /// 语义等同 <c>EditorGUILayout.EnumPopup</c>，但显示名可随语言切换；
        /// 同时使 <c>[InspectorName]</c> 在此路径下失效的问题不再影响显示
        /// （<c>[InspectorName]</c> 仅在 <c>SerializedProperty</c> 路径生效）。
        /// </summary>
        public static TEnum TrEnumPopup<TEnum>(string label, TEnum current) where TEnum : struct, Enum
        {
            var values = (TEnum[])Enum.GetValues(typeof(TEnum));
            var names  = new string[values.Length];
            int idx    = 0;
            for (int i = 0; i < values.Length; i++)
            {
                names[i] = TrEnum(values[i]);
                if (EqualityComparer<TEnum>.Default.Equals(values[i], current)) idx = i;
            }
            int picked = EditorGUILayout.Popup(label, idx, names);
            return (picked >= 0 && picked < values.Length) ? values[picked] : current;
        }

        // ── 刷新 ──────────────────────────────────────────────────────────────────

        /// <summary>刷新所有已打开的相关编辑器窗口，使语言 / 开关变更即时可见。</summary>
        public static void RepaintAll()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w is InventoryWelcomeWindow || w is InventoryEditorWindow)
                    w.Repaint();
            }
        }
    }
}

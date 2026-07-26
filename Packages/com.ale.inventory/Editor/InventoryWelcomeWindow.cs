using Ale.Toolkit.Runtime.UI;
using Ale.Toolkit.Runtime;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Editor;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

#if ATK_TMP
using TMPro;
#endif

#if ATK_TMP && ATK_LOCALIZATION
using Ale.Inventory.Runtime.UI;
#endif

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统欢迎窗口。提供快捷入口、数据模板配置和插件宏开关。
    /// 每次 Unity 会话启动时自动弹出一次（可通过「不再自动显示」关闭）。
    /// 通过菜单 Tools > InventorySystem > Welcome 手动打开。
    /// </summary>
    public class InventoryWelcomeWindow : EditorWindow
    {
        private const string Version = "1.0.0";

        private static readonly Vector2 WindowSize = new Vector2(520f, 800f);

        /// <summary>
        /// 打开「Addressable 资源引用迁移窗口」的注入钩子。由受 ATK_ADDRESSABLE 约束的
        /// Addressable 编辑器程序集在 <c>[InitializeOnLoad]</c> 时赋值；为 null（宏未启用 / 包未装）
        /// 时对应按钮隐藏。core 编辑器程序集对 Addressables 零依赖，故不能直接引用迁移窗口类型，
        /// 与 <see cref="EditorExportResolver.AddressableProvider"/> 等钩子同构。
        /// </summary>
        public static Action OpenAddressableMigration;

        // 内部 UI 状态
        private InventoryDatabase _templateDb;
        private bool _autoShow;
        private bool _initialized;

        // Logo 纹理缓存（从磁盘加载，FilterMode.Point 保持像素锐利）
        private Texture2D _logoTexture;
        private bool _logoLoadAttempted;

        // 测试工具：预制体生成列表 折叠状态（默认折叠）与滚动位置
        private bool    _genFoldout;
        private Vector2 _genListScroll;
        // 预制体生成列表内：各子系统分类的折叠状态（默认折叠，按 InventoryDemoWizard.Categories 分组）
        private readonly Dictionary<string, bool> _genCategoryFoldouts = new Dictionary<string, bool>();

        // ── 向导字体设置 ──────────────────────────────────────────────────────────

#if ATK_TMP
        /// <summary>向导生成 Prefab 时应用于所有 TMP 文本节点的默认字体（EditorPrefs 持久化）。</summary>
        private TMP_FontAsset _wizardDefaultTmpFont;
        private bool _wizardFontFoldout;
#endif

#if ATK_TMP && ATK_LOCALIZATION
        /// <summary>向导生成 Prefab 时赋给 LocalizedFontEvent 的本地化字体引用。</summary>
        [SerializeField] private LocalizedTmpFont wizardLocalizedFont = new LocalizedTmpFont();
        private bool _wizardLocalizedFontFoldout;

        /// <summary>供 InventoryDemoWizard 读取当前窗口中配置的本地化字体引用。</summary>
        internal static LocalizedTmpFont WizardLocalizedFont => _active?.wizardLocalizedFont;
        private static InventoryWelcomeWindow _active;
#endif

        #region 打开窗口

        [MenuItem("Tools/Inventory System/Welcome Window", priority = 0)]
        public static void Open()
        {
            OpenWindow();
        }

        private static InventoryWelcomeWindow OpenWindow()
        {
            var window = GetWindow<InventoryWelcomeWindow>(true, Tr("Inventory 道具仓库系统"), true);
            window.minSize = WindowSize;
            window.maxSize = WindowSize;
            window.CenterOnMainWin();
            window.Show();
            window.Focus();
            return window;
        }

        private void CenterOnMainWin()
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            float x = main.x + (main.width  - WindowSize.x) * 0.5f;
            float y = main.y + (main.height - WindowSize.y) * 0.5f;
            position = new Rect(x, y, WindowSize.x, WindowSize.y);
        }

        #endregion

        #region 生命周期

        private void OnEnable()
        {
            _initialized = false;
            _logoTexture = null;
            _logoLoadAttempted = false;
#if ATK_TMP && ATK_LOCALIZATION
            _active = this;
#endif
        }

        private void OnDisable()
        {
            if (_logoTexture)
            {
                DestroyImmediate(_logoTexture);
                _logoTexture = null;
            }
#if ATK_TMP && ATK_LOCALIZATION
            if (_active == this) _active = null;
#endif
        }

        private void LoadPrefs()
        {
            if (_initialized) return;
            _initialized = true;

            _templateDb = InventoryEditorPrefs.LoadTemplateDatabase();
            _autoShow   = EditorPrefs.GetBool(InventoryEditorPrefs.WelcomeAutoShow, true);

#if ATK_TMP
            _wizardDefaultTmpFont = InventoryEditorPrefs.LoadWizardDefaultTmpFont();
#endif
        }

        #endregion

        #region UI界面

        private void OnGUI()
        {
            LoadPrefs();
            titleContent.text = Tr("Inventory 道具仓库系统");

            DrawHeader();
            EditorGUILayout.Space(8);

            DrawGlobalSettingsLink();
            DrawSeparator();

            DrawQuickActions();
            DrawSeparator();

            DrawTemplateSection();

#if ATK_TMP
            DrawSeparator();
            DrawWizardFontSection();
#endif

            DrawSeparator();

            DrawFooter();
        }

        private void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            var subStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            EditorGUILayout.BeginVertical(GUILayout.Height(56));
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.Space(20);
            var logo = GetLogoTexture();
            if (logo)
            {
                const int displaySize = 128;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Rect logoRect = GUILayoutUtility.GetRect(
                    displaySize, displaySize,
                    GUILayout.Width(displaySize), GUILayout.Height(displaySize));
                GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit, true);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(6);
            }

            EditorGUILayout.LabelField($"Inventory System  v{Version}", headerStyle);
            EditorGUILayout.LabelField(Tr("面向设计师的 道具与仓库 配置工具"), subStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 跳转到 Ale Toolkit 欢迎 / 全局设置窗口。界面语言、枚举翻译、插件宏（TMP / Localization / Addressables）
        /// 均为<b>项目级全局设定</b>，已统一下沉到 toolkit，在那里配置。
        /// </summary>
        private static void DrawGlobalSettingsLink()
        {
            if (GUILayout.Button(Tr("打开 Ale Toolkit 设置（语言 / 插件宏）"), GUILayout.Height(24)))
                ToolkitWelcomeWindow.Open();
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField(Tr("快捷操作"), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(Tr("创建新数据文件"), GUILayout.Height(28)))
                InventoryDatabaseCreateMenu.CreateDatabase();

            if (GUILayout.Button(Tr("打开 Inventory Editor"), GUILayout.Height(28)))
                InventoryEditorWindow.Open();

            // 仅在启用 ATK_ADDRESSABLE（迁移窗口已注入钩子）时显示。
            if (OpenAddressableMigration != null
                && GUILayout.Button(Tr("打开 Addressable工具窗口"), GUILayout.Height(28)))
                OpenAddressableMigration();

#if ATK_LOCALIZATION
            // 仅在启用 ATK_LOCALIZATION 时显示（本地化工具窗口同在本程序集，可直接调用）。
            if (GUILayout.Button(Tr("打开 本地化工具窗口"), GUILayout.Height(28)))
                InventoryLocalizationToolWindow.Open();
#endif

            if (GUILayout.Button(Tr("查看文档"), GUILayout.Height(28)))
                OpenDocumentation();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            _genFoldout = EditorGUILayout.Foldout(_genFoldout, Tr("预制体生成"), true);
            if (_genFoldout)
            {
                // 生成全部（列表最上方）
                var demoStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Normal,
                    normal    = { textColor = new Color(0.85f, 1f, 0.85f) }
                };
                if (GUILayout.Button(Tr("生成全部（数据库 + 全部 Prefab）"), demoStyle, GUILayout.Height(26)))
                    InventoryDemoWizard.GenerateAll();

                // 滚动列表：按子系统分类折叠，逐项「生成」
                _genListScroll = EditorGUILayout.BeginScrollView(_genListScroll, GUILayout.Height(200));
                foreach (var category in InventoryDemoWizard.Categories)
                {
                    // 统计该分类下的可生成项数量；为空则跳过该分组
                    int count = 0;
                    foreach (var it in InventoryDemoWizard.Items)
                        if (it.Category == category) count++;
                    if (count == 0) continue;

                    _genCategoryFoldouts.TryGetValue(category, out bool catOpen);
                    catOpen = EditorGUILayout.Foldout(catOpen, Fmt("{0}（{1}）", Tr(category), count), true);
                    _genCategoryFoldouts[category] = catOpen;
                    if (!catOpen) continue;

                    EditorGUI.indentLevel++;
                    foreach (var item in InventoryDemoWizard.Items)
                    {
                        if (item.Category != category) continue;
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        // DisplayName 已由 InventoryDemoWizard 目录按当前语言构建，此处无需再翻译。
                        EditorGUILayout.LabelField(item.DisplayName, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button(Tr("生成"), GUILayout.Width(64), GUILayout.Height(20)))
                            InventoryDemoWizard.GenerateItem(item.Key);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawTemplateSection()
        {
            EditorGUILayout.LabelField(Tr("数据模板"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                Tr("创建新数据文件时使用的模板（留空则使用默认数据）："),
                EditorStyles.wordWrappedMiniLabel);

            EditorGUI.BeginChangeCheck();
            var newTemplate = (InventoryDatabase)EditorGUILayout.ObjectField(
                _templateDb, typeof(InventoryDatabase), false);
            if (EditorGUI.EndChangeCheck())
            {
                _templateDb = newTemplate;
                InventoryEditorPrefs.SaveTemplateDatabase(_templateDb);
            }

            if (_templateDb)
            {
                var db = _templateDb;
                EditorGUILayout.LabelField(
                    Fmt("  包含：{0} 枚举类型  |  {1} 功能标签  |  {2} 道具模板  |  {3} 道具",
                        db.EnumTypes.Count, db.FunctionTags.Count, db.ItemTemplates.Count, db.Items.Count),
                    EditorStyles.miniLabel);
            }
        }

#if ATK_TMP
        /// <summary>
        /// 向导字体设置（仅 ATK_TMP 编译）。原挂在插件宏区域下方，宏开关下沉 toolkit 后独立成区。
        /// 这些是 <see cref="InventoryDemoWizard"/> 生成测试 Prefab 时使用的字体，属库存领域配置，故保留在本窗口。
        /// </summary>
        private void DrawWizardFontSection()
        {
            EditorGUILayout.LabelField(Tr("向导字体"), EditorStyles.boldLabel);
            DrawTmpFontFoldout();
            DrawLocalizationFontFoldout();
        }
#endif

        /// <summary>TMP 宏区域底部的"向导字体设置"折叠栏。</summary>
        private void DrawTmpFontFoldout()
        {
#if ATK_TMP
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _wizardFontFoldout = EditorGUILayout.Foldout(_wizardFontFoldout, Tr("TextMeshPro 设置"), true);
            if (_wizardFontFoldout)
            {
                EditorGUI.BeginChangeCheck();
                var newFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                    Tr("默认字体"), _wizardDefaultTmpFont, typeof(TMP_FontAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    _wizardDefaultTmpFont = newFont;
                    InventoryEditorPrefs.SaveWizardDefaultTmpFont(newFont);
                }
                EditorGUILayout.LabelField(
                    Tr("生成测试 Prefab 时将此字体应用于所有 TMP 文本节点（留空则使用 TMP 默认字体）。"),
                    EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
#endif
        }
        
        /// <summary>Localization 宏区域底部的"向导本地化字体设置"折叠栏。</summary>
        private void DrawLocalizationFontFoldout()
        {
#if ATK_TMP && ATK_LOCALIZATION
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _wizardLocalizedFontFoldout = EditorGUILayout.Foldout(
                _wizardLocalizedFontFoldout, Tr("Unity Localization 设置"), true);
            if (_wizardLocalizedFontFoldout)
            {
                // 使用 Unity.Localization 的内置 PropertyField 绘制表/条目选择器
                var so   = new SerializedObject(this);
                var prop = so.FindProperty("wizardLocalizedFont");
                if (prop != null)
                {
                    EditorGUILayout.PropertyField(prop, new GUIContent("Localized Asset Reference"));
                    so.ApplyModifiedProperties();
                }
                EditorGUILayout.LabelField(
                    Tr("生成测试 Prefab 时赋给 LocalizedFontEvent 组件的本地化字体资源。" +
                       "需同时启用 ATK_TMP 才生效。"),
                    EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
#endif
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            bool newAutoShow = EditorGUILayout.ToggleLeft(Tr("启动时自动显示"), _autoShow, GUILayout.Width(140));
            if (EditorGUI.EndChangeCheck())
            {
                _autoShow = newAutoShow;
                EditorPrefs.SetBool(InventoryEditorPrefs.WelcomeAutoShow, _autoShow);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            EditorGUILayout.Space(2);
        }

        /// <summary>
        /// FilterMode.Point 确保放大时像素边缘保持锐利清晰。结果缓存，避免每帧重复 I/O。
        /// </summary>
        private Texture2D GetLogoTexture()
        {
            if (_logoTexture) return _logoTexture;
            if (_logoLoadAttempted) return null;

            _logoLoadAttempted = true;

            string logoPath = System.IO.Path.GetFullPath(
                "Packages/com.ale.inventory/Docs~/Images/InventorySystem_Logo.png");

            if (!System.IO.File.Exists(logoPath)) return null;

            byte[] bytes = System.IO.File.ReadAllBytes(logoPath);
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,   // 像素锐利，无插值模糊
                wrapMode   = TextureWrapMode.Clamp
            };
            if (tex.LoadImage(bytes))
            {
                _logoTexture = tex;
            }
            else
            {
                DestroyImmediate(tex);
            }

            return _logoTexture;
        }

        #endregion

        #region 文档

        private static void OpenDocumentation()
        {
            // README.md 在 Packages/com.ale.inventory/ 根目录下。
            // .md 文件不被 AssetDatabase 索引，直接取绝对路径后用系统默认程序打开。
            string absolutePath = System.IO.Path.GetFullPath(
                "Packages/com.ale.inventory/README.md");

            if (System.IO.File.Exists(absolutePath))
            {
                Application.OpenURL("file:///" + absolutePath.Replace('\\', '/'));
            }
            else
            {
                EditorUtility.DisplayDialog(Tr("文档未找到"),
                    Tr("未能找到文档文件：\nPackages/com.ale.inventory/README.md"), Tr("确定"));
            }
        }

        #endregion
    }
}

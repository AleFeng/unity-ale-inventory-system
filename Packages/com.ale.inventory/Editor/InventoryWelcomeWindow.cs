using Ale.Toolkit.Runtime.UI;
using Ale.Toolkit.Runtime;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ale.Inventory.Runtime;
using Ale.Toolkit.Editor;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统欢迎窗口。提供快捷入口、数据模板配置和插件宏开关。
    /// 每次 Unity 会话启动时自动弹出一次（可通过「不再自动显示」关闭）。
    /// 通过菜单 Tools > Ale Toolkit > Inventory System > Welcome Window 手动打开。
    /// </summary>
    public class InventoryWelcomeWindow : EditorWindow
    {
        private const string Version = "1.0.0";

        private static readonly Vector2 WindowSize = new Vector2(520f, 800f);

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

        #region 打开窗口

        [MenuItem("Tools/Ale Toolkit/Inventory System/Welcome Window", priority = 1000)]
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
        }

        private void OnDisable()
        {
            if (_logoTexture)
            {
                DestroyImmediate(_logoTexture);
                _logoTexture = null;
            }
        }

        private void LoadPrefs()
        {
            if (_initialized) return;
            _initialized = true;

            _templateDb = InventoryEditorPrefs.LoadTemplateDatabase();
            _autoShow   = EditorPrefs.GetBool(InventoryEditorPrefs.WelcomeAutoShow, true);
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

#if ATK_LOCALIZATION
            // 仅在启用 ATK_LOCALIZATION 时显示；打开 toolkit 通用本地化窗口（拖入本库的 InventoryDatabase 使用）。
            if (GUILayout.Button(Tr("打开 本地化工具窗口"), GUILayout.Height(28)))
                ToolkitLocalizationToolWindow.Open();
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

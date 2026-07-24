using System.Collections.Generic;
using System.IO;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统配置编辑器主窗口（IMGUI）。顶部为系统页签（道具 / 仓库 / 商店 / 制作 / 装备 / 技能），
    /// 六个页签均已实现，各自由对应的 <c>*SystemTab</c> 承载。所有编辑基于 ScriptableObject，
    /// 支持 Undo/Redo；JSON / 二进制 仅用于单向导出。
    /// </summary>
    public class InventoryEditorWindow : EditorWindow, IInventoryEditorContext
    {
        private static readonly Vector2 WindowDefaultSize = new Vector2(1280f, 780f);
        private static readonly Vector2 WindowMinSize = new Vector2(900f, 520f);
        private const float ToolbarHeight = 26f;
        private const float TabRowHeight = 24f;
        private const float StatusBarHeight = 22f;

        private const string PrefKeyDbPath = "InventorySystem.DatabasePath";

        private static readonly string[] SystemTabs =
            { "道具系统", "仓库系统", "商店系统", "制作系统", "装备系统", "技能系统" };

        private InventoryDatabase _db;
        private SerializedObject _serialized;
        private int _systemTab;

        private readonly ItemSystemTab      _itemSystemTab      = new ItemSystemTab();
        private readonly InventorySystemTab _inventorySystemTab = new InventorySystemTab();
        private readonly ShopSystemTab      _shopSystemTab      = new ShopSystemTab();
        private readonly CraftingSystemTab  _craftingSystemTab  = new CraftingSystemTab();
        private readonly EquipmentSystemTab _equipmentSystemTab = new EquipmentSystemTab();
        private readonly SkillSystemTab     _skillSystemTab     = new SkillSystemTab();

        // 重复 ID 缓存。
        // 六类实体的重复 / 空 ID 缓存（种类 → 集合），由 EditorDuplicateIdScanner.ScanAll 整体刷新。
        private Dictionary<EInventoryEntityKind, HashSet<string>> _duplicateIds
            = EditorDuplicateIdScanner.ScanAll(null);
        private bool _needDuplicateCheck = true;

        // 模板属性字段变化后需对所有关联道具/仓库执行 RebuildAttributes 同步。
        private bool _rebuildPending = true;

        // Layout/Repaint 快照，保证控件数量一致。
        private string _snapStatusMessage;

        #region 打开窗口

        [MenuItem("Tools/Inventory System/Inventory Editor")]
        public static void Open()
        {
            OpenWindow().Show();
        }

        public static void Open(InventoryDatabase db)
        {
            var window = OpenWindow();
            if (db)
                window.SetDatabase(db);
            window.Show();
            window.Focus();
        }

        private static InventoryEditorWindow OpenWindow()
        {
            bool isNew = !HasOpenInstances<InventoryEditorWindow>();
            var window = GetWindow<InventoryEditorWindow>(Tr("Inventory Editor"));
            window.minSize = WindowMinSize;
            if (isNew)
            {
                var res = EditorGUIUtility.GetMainWindowPosition();
                float x = res.x + (res.width - WindowDefaultSize.x) * 0.5f;
                float y = res.y + (res.height - WindowDefaultSize.y) * 0.5f;
                window.position = new Rect(x, y, WindowDefaultSize.x, WindowDefaultSize.y);
            }
            return window;
        }

        #endregion

        #region 生命周期

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            string path = EditorPrefs.GetString(PrefKeyDbPath, string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                var db = AssetDatabase.LoadAssetAtPath<InventoryDatabase>(path);
                if (db) SetDatabase(db);
            }
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            _serialized?.UpdateIfRequiredOrScript();
            _itemSystemTab.OnUndoRedo();
            _inventorySystemTab.OnUndoRedo();
            _shopSystemTab.OnUndoRedo();
            _craftingSystemTab.OnUndoRedo();
            _equipmentSystemTab.OnUndoRedo();
            _skillSystemTab.OnUndoRedo();
            _needDuplicateCheck = true;
            _rebuildPending     = true;
            Repaint();
        }

        private void SetDatabase(InventoryDatabase db)
        {
            _db = db;
            _serialized = db ? new SerializedObject(db) : null;
            _needDuplicateCheck = true;
            _rebuildPending     = true;
            if (db)
                EditorPrefs.SetString(PrefKeyDbPath, AssetDatabase.GetAssetPath(db));
            _itemSystemTab.OnDatabaseChanged(this);
            _inventorySystemTab.OnDatabaseChanged(this);
            _shopSystemTab.OnDatabaseChanged(this);
            _craftingSystemTab.OnDatabaseChanged(this);
            _equipmentSystemTab.OnDatabaseChanged(this);
            _skillSystemTab.OnDatabaseChanged(this);
        }

        #endregion

        #region IInventoryEditorContext

        public InventoryDatabase Database => _db;
        public SerializedObject Serialized => _serialized;
        public IAssetRefResolver Resolver =>
            InventoryExportResolver.Resolve(InventoryEditorPrefs.IsAddressableEnabled());
        public HashSet<string> DuplicateIdsOf(EInventoryEntityKind kind) => _duplicateIds[kind];

        public void RecordUndo(string actionName)
        {
            if (_db) Undo.RecordObject(_db, actionName);
        }

        public void MarkDirty()
        {
            if (_db) EditorUtility.SetDirty(_db);
            _needDuplicateCheck = true;
            _rebuildPending     = true;
            _serialized?.Update();
        }

        #endregion

        #region 绘制

        private void OnGUI()
        {
            titleContent.text = Tr("Inventory Editor");

            // Layout 阶段刷新快照与缓存。
            if (Event.current.type == EventType.Layout)
            {
                // 1. 模板属性字段变动后，先同步所有道具/仓库的属性集合。
                //    RebuildAttributes 是幂等的：来源未变则不修改任何值，只补缺/删多余。
                //    MarkDirty 已调用 SetDirty，此处无需再调，避免无谓"已修改"标记。
                if (_rebuildPending)
                {
                    _rebuildPending = false;
                    RebuildAllAttributes(_db);

                    // 为商品组 / 商品补发缺失的稳定 guid（交易进度存档键）。
                    // 幂等；仅在确有补发时 SetDirty——首次打开旧数据走的是这条路径（无编辑动作、
                    // MarkDirty 未触发），不显式标脏补发结果就不会被保存。
                    if (_db && _db.EnsureShopEntryGuids())
                        EditorUtility.SetDirty(_db);
                }

                // 2. 刷新重复 ID 缓存。
                if (_needDuplicateCheck)
                {
                    _duplicateIds       = EditorDuplicateIdScanner.ScanAll(_db);
                    _needDuplicateCheck = false;
                }
                _snapStatusMessage = BuildStatusMessage();
            }

            DrawToolbar();
            DrawSystemTabRow();

            var bodyRect = new Rect(0, ToolbarHeight + TabRowHeight, position.width,
                position.height - ToolbarHeight - TabRowHeight - StatusBarHeight);

            if (!_db)
                DrawNoDatabase(bodyRect);
            else
                DrawBody(bodyRect);

            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            var rect = new Rect(0, 0, position.width, ToolbarHeight);
            GUILayout.BeginArea(rect, EditorStyles.toolbar);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(Tr("数据文件"), GUILayout.Width(56));
            var newDb = (InventoryDatabase)EditorGUILayout.ObjectField
                (_db, typeof(InventoryDatabase), false, GUILayout.Width(240));
            if (newDb != _db)
                SetDatabase(newDb);

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!_db))
            {
                // 仅当存在"非空重复 ID"时禁用导出；空 ID 条目在导出时自动跳过，不阻塞。
                // 六类实体一并检查——此前只看道具，仓库 / 商店 / 蓝图 / 装备组 / 技能的重复 ID
                // 只在状态栏出警告却不拦截导出。
                bool hasNonEmptyDups = false;
                if (_db)
                    foreach (var kind in EditorDuplicateIdScanner.AllKinds)
                        if (EditorDuplicateIdScanner.HasNonEmpty(_duplicateIds[kind]))
                        { hasNonEmptyDups = true; break; }

                using (new EditorGUI.DisabledScope(hasNonEmptyDups))
                {
                    if (GUILayout.Button(Tr("导出 JSON"), EditorStyles.toolbarButton, GUILayout.Width(90)))
                        ExportJson();
                    if (GUILayout.Button(Tr("导出二进制"), EditorStyles.toolbarButton, GUILayout.Width(90)))
                        ExportBinary();
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawSystemTabRow()
        {
            var rect = new Rect(0, ToolbarHeight, position.width, TabRowHeight);
            GUILayout.BeginArea(rect);
            var tabs = new string[SystemTabs.Length];
            for (int i = 0; i < tabs.Length; i++) tabs[i] = Tr(SystemTabs[i]);
            _systemTab = GUILayout.Toolbar(_systemTab, tabs, GUILayout.Height(TabRowHeight - 2));
            GUILayout.EndArea();
        }

        private void DrawBody(Rect rect)
        {
            GUILayout.BeginArea(rect);
            var inner = new Rect(0, 0, rect.width, rect.height);
            // 与 SystemTabs 一一对应；_systemTab 由 GUILayout.Toolbar 产出，恒在合法范围内。
            switch (_systemTab)
            {
                case 0: _itemSystemTab.OnGUI(inner, this);      break;
                case 1: _inventorySystemTab.OnGUI(inner, this); break;
                case 2: _shopSystemTab.OnGUI(inner, this);      break;
                case 3: _craftingSystemTab.OnGUI(inner, this);  break;
                case 4: _equipmentSystemTab.OnGUI(inner, this); break;
                case 5: _skillSystemTab.OnGUI(inner, this);     break;
            }
            GUILayout.EndArea();
        }

        private void DrawNoDatabase(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(360));
            EditorGUILayout.LabelField(Tr("请创建或选择一个 InventoryDatabase 数据文件"), InventoryEditorStyles.Placeholder);
            EditorGUILayout.Space(8);
            if (GUILayout.Button(Tr("创建新的数据文件"), GUILayout.Height(30)))
                CreateNewDatabase();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }

        private void DrawStatusBar()
        {
            var rect = new Rect(0, position.height - StatusBarHeight, position.width, StatusBarHeight);
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));
            var labelRect = new Rect(rect.x + 8, rect.y + 2, rect.width - 16, rect.height - 4);
            if (!string.IsNullOrEmpty(_snapStatusMessage))
                GUI.Label(labelRect, _snapStatusMessage, InventoryEditorStyles.StatusError);
        }

        private string BuildStatusMessage()
        {
            if (!_db) return string.Empty;

            var parts = new List<string>();

            foreach (var kind in EditorDuplicateIdScanner.AllKinds)
            {
                var set = _duplicateIds[kind];
                if (set.Count == 0) continue;

                var ids = new List<string>(set.Count);
                foreach (var id in set)
                    ids.Add(string.IsNullOrEmpty(id) ? Tr("(空ID)") : id);

                string noun = Tr(EditorDuplicateIdScanner.NounOf(kind));
                parts.Add(EditorDuplicateIdScanner.HasNonEmpty(set)
                    ? Fmt("⚠ {0}重复 ID：{1}（导出已禁用）", noun, string.Join(", ", ids))
                    : Fmt("⚠ {0}存在空 ID（导出时将跳过）", noun));
            }

            return string.Join("  |  ", parts);
        }

        /// <summary>
        /// 对数据库中所有道具和仓库调用 RebuildAttributes，使其与当前模板定义保持一致。
        /// 模板属性字段新增时补默认值条目，删除时移除对应条目，类型变更时重置值；已有条目值保留。
        /// </summary>
        private static void RebuildAllAttributes(InventoryDatabase db)
        {
            if (!db) return;
            foreach (var item in db.Items)
                item.RebuildAttributes(db);
            foreach (var inv in db.Inventories)
                inv.RebuildAttributes(db);
            foreach (var shop in db.Shops)
                shop.RebuildAttributes(db);
            foreach (var bp in db.CraftingBlueprints)
                bp.RebuildAttributes(db);
            foreach (var g in db.EquipmentGroups)
                g.RebuildAttributes(db);
            foreach (var s in db.Skills)
                s.RebuildAttributes(db);
        }

        #endregion

        #region 创建 / 导出

        private void CreateNewDatabase()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                Tr("创建仓库系统数据文件"), "InventoryDatabase", "asset",
                Tr("请选择数据文件保存位置"));
            if (string.IsNullOrEmpty(path)) return;

            var db = ScriptableObject.CreateInstance<InventoryDatabase>();

            // 配置了模板则深拷贝；否则保持空数据库（用户可导入 Demo 样本或手动配置）。
            var templateDb = InventoryEditorPrefs.LoadTemplateDatabase();
            if (templateDb)
                db.CloneFrom(templateDb);

            AssetDatabase.CreateAsset(db, path);
            AssetDatabase.SaveAssets();
            SetDatabase(db);
        }

        private void ExportJson()
        {
            if (!ValidateForExport()) return;
            string path = EditorUtility.SaveFilePanel(Tr("导出为 JSON"), Application.dataPath, _db.name, "json");
            if (string.IsNullOrEmpty(path)) return;

            string json = InventoryJsonSerializer.Export(_db, Resolver);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent(Tr("已导出 JSON")));
        }

        private void ExportBinary()
        {
            if (!ValidateForExport()) return;
            string path = EditorUtility.SaveFilePanel(Tr("导出为二进制"), Application.dataPath, _db.name, "bytes");
            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes = InventoryBinarySerializer.Export(_db, Resolver);
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent(Tr("已导出二进制")));
        }

        private bool ValidateForExport()
        {
            if (!_db) return false;
            if (!_db.Validate(out var errors))
            {
                EditorUtility.DisplayDialog(Tr("无法导出"), string.Join("\n", errors), Tr("确定"));
                return false;
            }
            return true;
        }

        #endregion
    }
}

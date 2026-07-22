using System.Collections.Generic;
using System.IO;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 仓库系统配置编辑器主窗口（IMGUI）。顶部为系统页签（道具/仓库/商店/装备/制作/技能），
    /// 第一期仅实现「道具系统」页签，其余为占位。所有编辑基于 ScriptableObject，支持 Undo/Redo，
    /// JSON / 二进制 仅用于单向导出。
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
        private HashSet<string> _duplicateIds          = new HashSet<string>();
        private HashSet<string> _inventoryDuplicateIds = new HashSet<string>();
        private HashSet<string> _shopDuplicateIds      = new HashSet<string>();
        private HashSet<string> _craftingDuplicateIds  = new HashSet<string>();
        private HashSet<string> _equipmentDuplicateIds = new HashSet<string>();
        private HashSet<string> _skillDuplicateIds     = new HashSet<string>();
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
            var window = GetWindow<InventoryEditorWindow>("Inventory Editor");
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
        public HashSet<string> DuplicateIds          => _duplicateIds;
        public HashSet<string> InventoryDuplicateIds => _inventoryDuplicateIds;
        public HashSet<string> ShopDuplicateIds      => _shopDuplicateIds;
        public HashSet<string> CraftingDuplicateIds  => _craftingDuplicateIds;
        public HashSet<string> EquipmentDuplicateIds => _equipmentDuplicateIds;
        public HashSet<string> SkillDuplicateIds     => _skillDuplicateIds;

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
                }

                // 2. 刷新重复 ID 缓存。
                if (_needDuplicateCheck)
                {
                    _duplicateIds          = DuplicateIdChecker.Scan(_db);
                    _inventoryDuplicateIds = ScanInventoryDuplicateIds(_db);
                    _shopDuplicateIds      = ScanShopDuplicateIds(_db);
                    _craftingDuplicateIds  = ScanCraftingDuplicateIds(_db);
                    _equipmentDuplicateIds = ScanEquipmentDuplicateIds(_db);
                    _skillDuplicateIds     = ScanSkillDuplicateIds(_db);
                    _needDuplicateCheck    = false;
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

            EditorGUILayout.LabelField("数据文件", GUILayout.Width(56));
            var newDb = (InventoryDatabase)EditorGUILayout.ObjectField
                (_db, typeof(InventoryDatabase), false, GUILayout.Width(240));
            if (newDb != _db)
                SetDatabase(newDb);

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!_db))
            {
                // 仅当存在"非空重复 ID"时禁用导出；空 ID 条目在导出时自动跳过，不阻塞。
                bool hasNonEmptyDups = false;
                if (_db)
                    foreach (var id in _duplicateIds)
                        if (!string.IsNullOrEmpty(id)) { hasNonEmptyDups = true; break; }

                using (new EditorGUI.DisabledScope(hasNonEmptyDups))
                {
                    if (GUILayout.Button("导出 JSON", EditorStyles.toolbarButton, GUILayout.Width(90)))
                        ExportJson();
                    if (GUILayout.Button("导出二进制", EditorStyles.toolbarButton, GUILayout.Width(90)))
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
            _systemTab = GUILayout.Toolbar(_systemTab, SystemTabs, GUILayout.Height(TabRowHeight - 2));
            GUILayout.EndArea();
        }

        private void DrawBody(Rect rect)
        {
            GUILayout.BeginArea(rect);
            var inner = new Rect(0, 0, rect.width, rect.height);
            if (_systemTab == 0)
                _itemSystemTab.OnGUI(inner, this);
            else if (_systemTab == 1)
                _inventorySystemTab.OnGUI(inner, this);
            else if (_systemTab == 2)
                _shopSystemTab.OnGUI(inner, this);
            else if (_systemTab == 3)
                _craftingSystemTab.OnGUI(inner, this);
            else if (_systemTab == 4)
                _equipmentSystemTab.OnGUI(inner, this);
            else if (_systemTab == 5)
                _skillSystemTab.OnGUI(inner, this);
            else
                DrawStub(inner, SystemTabs[_systemTab]);
            GUILayout.EndArea();
        }

        private static void DrawStub(Rect rect, string title)
        {
            GUI.Label(rect, $"「{title}」将在后续阶段实现", InventoryEditorStyles.Placeholder);
        }

        private void DrawNoDatabase(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(360));
            EditorGUILayout.LabelField("请创建或选择一个 InventoryDatabase 数据文件", InventoryEditorStyles.Placeholder);
            EditorGUILayout.Space(8);
            if (GUILayout.Button("创建新的数据文件", GUILayout.Height(30)))
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

            if (_duplicateIds.Count > 0)
            {
                bool hasNonEmpty = false;
                foreach (var id in _duplicateIds)
                    if (!string.IsNullOrEmpty(id)) { hasNonEmpty = true; break; }

                var ids = new List<string>();
                foreach (var id in _duplicateIds)
                    ids.Add(string.IsNullOrEmpty(id) ? "(空ID)" : id);

                string suffix = hasNonEmpty
                    ? "（道具重复 ID，导出已禁用）"
                    : "（空 ID 道具将在导出时跳过）";
                parts.Add("⚠ " + string.Join(", ", ids) + " " + suffix);
            }

            if (_inventoryDuplicateIds.Count > 0)
            {
                var ids = new List<string>();
                foreach (var id in _inventoryDuplicateIds)
                    ids.Add(string.IsNullOrEmpty(id) ? "(空ID)" : id);
                parts.Add("⚠ 仓库重复 ID：" + string.Join(", ", ids));
            }

            if (_shopDuplicateIds.Count > 0)
            {
                var ids = new List<string>();
                foreach (var id in _shopDuplicateIds)
                    ids.Add(string.IsNullOrEmpty(id) ? "(空ID)" : id);
                parts.Add("⚠ 商店重复 ID：" + string.Join(", ", ids));
            }

            if (_craftingDuplicateIds.Count > 0)
            {
                var ids = new List<string>();
                foreach (var id in _craftingDuplicateIds)
                    ids.Add(string.IsNullOrEmpty(id) ? "(空ID)" : id);
                parts.Add("⚠ 蓝图重复 ID：" + string.Join(", ", ids));
            }

            if (_equipmentDuplicateIds.Count > 0)
            {
                var ids = new List<string>();
                foreach (var id in _equipmentDuplicateIds)
                    ids.Add(string.IsNullOrEmpty(id) ? "(空ID)" : id);
                parts.Add("⚠ 装备组重复 ID：" + string.Join(", ", ids));
            }

            if (_skillDuplicateIds.Count > 0)
            {
                var ids = new List<string>();
                foreach (var id in _skillDuplicateIds)
                    ids.Add(string.IsNullOrEmpty(id) ? "(空ID)" : id);
                parts.Add("⚠ 技能重复 ID：" + string.Join(", ", ids));
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

        private static HashSet<string> ScanInventoryDuplicateIds(InventoryDatabase db)
        {
            var result = new HashSet<string>();
            if (!db) return result;

            var seen = new HashSet<string>();
            foreach (var inv in db.Inventories)
            {
                string id = string.IsNullOrWhiteSpace(inv.id) ? string.Empty : inv.id;
                if (!seen.Add(id))
                    result.Add(id);
            }
            return result;
        }

        private static HashSet<string> ScanShopDuplicateIds(InventoryDatabase db)
        {
            var result = new HashSet<string>();
            if (!db) return result;

            var seen = new HashSet<string>();
            foreach (var shop in db.Shops)
            {
                string id = string.IsNullOrWhiteSpace(shop.id) ? string.Empty : shop.id;
                if (!seen.Add(id))
                    result.Add(id);
            }
            return result;
        }

        private static HashSet<string> ScanCraftingDuplicateIds(InventoryDatabase db)
        {
            var result = new HashSet<string>();
            if (!db) return result;

            var seen = new HashSet<string>();
            foreach (var bp in db.CraftingBlueprints)
            {
                string id = string.IsNullOrWhiteSpace(bp.id) ? string.Empty : bp.id;
                if (!seen.Add(id))
                    result.Add(id);
            }
            return result;
        }

        private static HashSet<string> ScanEquipmentDuplicateIds(InventoryDatabase db)
        {
            var result = new HashSet<string>();
            if (!db) return result;

            var seen = new HashSet<string>();
            foreach (var g in db.EquipmentGroups)
            {
                string id = string.IsNullOrWhiteSpace(g.id) ? string.Empty : g.id;
                if (!seen.Add(id))
                    result.Add(id);
            }
            return result;
        }

        private static HashSet<string> ScanSkillDuplicateIds(InventoryDatabase db)
        {
            var result = new HashSet<string>();
            if (!db) return result;

            var seen = new HashSet<string>();
            foreach (var s in db.Skills)
            {
                string id = string.IsNullOrWhiteSpace(s.id) ? string.Empty : s.id;
                if (!seen.Add(id))
                    result.Add(id);
            }
            return result;
        }

        #endregion

        #region 创建 / 导出

        private void CreateNewDatabase()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "创建仓库系统数据文件", "InventoryDatabase", "asset",
                "请选择数据文件保存位置");
            if (string.IsNullOrEmpty(path)) return;

            var db = ScriptableObject.CreateInstance<InventoryDatabase>();

            // 优先使用配置的模板，无模板时填充默认数据。
            var templateDb = InventoryEditorPrefs.LoadTemplateDatabase();
            if (templateDb)
                db.CloneFrom(templateDb);
            else
                db.SeedDefaults();

            AssetDatabase.CreateAsset(db, path);
            AssetDatabase.SaveAssets();
            SetDatabase(db);
        }

        private void ExportJson()
        {
            if (!ValidateForExport()) return;
            string path = EditorUtility.SaveFilePanel("导出为 JSON", Application.dataPath, _db.name, "json");
            if (string.IsNullOrEmpty(path)) return;

            string json = InventoryJsonSerializer.Export(_db, Resolver);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("已导出 JSON"));
        }

        private void ExportBinary()
        {
            if (!ValidateForExport()) return;
            string path = EditorUtility.SaveFilePanel("导出为二进制", Application.dataPath, _db.name, "bytes");
            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes = InventoryBinarySerializer.Export(_db, Resolver);
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("已导出二进制"));
        }

        private bool ValidateForExport()
        {
            if (!_db) return false;
            if (!_db.Validate(out var errors))
            {
                EditorUtility.DisplayDialog("无法导出", string.Join("\n", errors), "确定");
                return false;
            }
            return true;
        }

        #endregion
    }
}

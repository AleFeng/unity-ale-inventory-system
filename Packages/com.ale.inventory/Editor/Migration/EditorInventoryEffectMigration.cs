using Ale.Effect;
using Ale.Effect.Editor;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 1.12.0 → 1.13.0 迁移窗口：把 <see cref="InventoryDatabase"/> 里 legacy 的效果 / Gameplay 标签搬进 toolkit 效果库 <see cref="EffectDatabase"/>
    /// （可就地新建）。同 id 冲突跳过并报告、不覆盖（冲突条目留在 legacy 字段，处理后可重跑）；两资产标脏保存，并刷新效果目录。
    /// 纯数据部分在运行时程序集的 <see cref="InventoryLegacyEffects"/>。
    /// </summary>
    public sealed class EditorInventoryEffectMigration : EditorWindow
    {
        private InventoryDatabase _source;
        private EffectDatabase    _target;
        private string            _report;
        private Vector2           _scroll;

        [MenuItem("Tools/Ale Toolkit/Inventory System/迁移效果到 Effect Database", priority = 1002)]
        public static void Open() => Open(Selection.activeObject as InventoryDatabase);

        /// <summary>打开迁移窗口并预填来源库（为空时取当前选中 / 库存编辑器最近打开的库）。</summary>
        public static void Open(InventoryDatabase source)
        {
            var w = GetWindow<EditorInventoryEffectMigration>(true, Tr("迁移效果到 Effect Database"));
            w.minSize = new Vector2(560f, 380f);
            if (source) w._source = source;
            else if (!w._source)
                w._source = AssetDatabase.LoadAssetAtPath<InventoryDatabase>(EditorPrefs.GetString("InventorySystem.DatabasePath", string.Empty));
            w._report = null;
            w.Show();
        }

        private void OnGUI()
        {
            titleContent.text = Tr("迁移效果到 Effect Database");
            EditorGUILayout.HelpBox(Tr("1.13.0 起效果与 Gameplay 标签由 toolkit 效果库（EffectDatabase）承载，所有上层系统共用；库存库只保留道具的效果 id 引用。本工具把 1.12.0 存在库存库里的效果 / 标签逐条搬入目标效果库：定义原样保留（显示名取定义的 displayName，不挂模板）；目标库已有同 id 的效果跳过并报告（不覆盖，条目留在 legacy 字段）；标签按名称去重并入。"), MessageType.Info);

            EditorGUILayout.Space(4);
            _source = (InventoryDatabase)EditorGUILayout.ObjectField(Tr("来源（库存库）"), _source, typeof(InventoryDatabase), false);
            if (_source)
            {
#pragma warning disable 618
                int fx = _source.LegacyEffects.Count, tags = _source.LegacyGameplayTags.Count;
#pragma warning restore 618
                EditorGUILayout.LabelField(Fmt("legacy 效果 {0} 条，legacy Gameplay 标签 {1} 条", fx, tags), EditorStyles.miniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            _target = (EffectDatabase)EditorGUILayout.ObjectField(Tr("目标（效果库）"), _target, typeof(EffectDatabase), false);
            if (GUILayout.Button(Tr("新建…"), GUILayout.Width(60)))
            {
                var created = EffectEditorWindow.CreateDatabaseAsset();
                if (created) _target = created;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            bool canRun = _source && _target && _source.HasLegacyEffectData;
            using (new EditorGUI.DisabledScope(!canRun))
            {
                if (GUILayout.Button(Tr("迁移"), GUILayout.Height(28))) Run();
            }
            if (_source && !_source.HasLegacyEffectData)
                EditorGUILayout.LabelField(Tr("来源库没有 legacy 效果 / 标签数据，无需迁移。"), EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(_report))
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(Tr("结果"), EditorStyles.boldLabel);
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(80f));
                EditorGUILayout.LabelField(_report, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();
                if (_target && GUILayout.Button(Tr("在 Effect Editor 中打开目标库")))
                    EffectEditorWindow.Open(_target);
            }
        }

        private void Run()
        {
            Undo.RecordObject(_target, "迁移效果到 Effect Database");
            Undo.RecordObject(_source, "迁移效果到 Effect Database");
            var report = InventoryLegacyEffects.MigrateInto(_source, _target, removeFromLegacy: true);
            EditorUtility.SetDirty(_target);
            EditorUtility.SetDirty(_source);
            AssetDatabase.SaveAssets();
            EffectEditorCatalog.Rebuild();
            _report = report.ToString();
            if (report.HasConflicts)
                _report += "\n\n" + Tr("存在同 id 冲突：请在目标库中重命名 / 删除对应效果后重跑（冲突条目仍留在来源库的 legacy 字段）。");
            Debug.Log("[InventoryEffectMigration] " + _report.Replace('\n', ' '));
        }
    }
}

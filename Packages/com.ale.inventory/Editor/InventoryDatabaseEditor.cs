using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEngine;
using static Ale.Toolkit.Editor.ToolkitEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// InventoryDatabase 的自定义 Inspector。
    /// 在默认 Inspector 顶部添加一个按钮，快速在 Inventory Editor 窗口中打开并编辑该数据文件；
    /// 资产仍含 1.12.0 legacy 效果 / Gameplay 标签时给出提示与「迁移效果到 Effect Database」入口。
    /// </summary>
    [CustomEditor(typeof(InventoryDatabase))]
    public class InventoryDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var db = (InventoryDatabase)target;

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 32f
            };
            if (GUILayout.Button("在 Inventory Editor 中编辑", btnStyle))
                InventoryEditorWindow.Open(db);

            if (db && db.HasLegacyEffectData)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(Tr("本资产仍含 1.12.0 存于库存库的效果 / Gameplay 标签（legacy）。1.13.0 起效果由 toolkit 效果库（EffectDatabase）承载，运行时不再读取这些数据；请迁移到效果库。"), MessageType.Warning);
                if (GUILayout.Button(Tr("迁移效果到 Effect Database…"), GUILayout.Height(24f)))
                    EditorInventoryEffectMigration.Open(db);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            EditorGUILayout.Space(2f);

            DrawDefaultInspector();
        }
    }
}

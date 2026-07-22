using InventorySystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// InventoryDatabase 的自定义 Inspector。
    /// 在默认 Inspector 顶部添加一个按钮，快速在 Inventory Editor 窗口中打开并编辑该数据文件。
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

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            EditorGUILayout.Space(2f);

            DrawDefaultInspector();
        }
    }
}

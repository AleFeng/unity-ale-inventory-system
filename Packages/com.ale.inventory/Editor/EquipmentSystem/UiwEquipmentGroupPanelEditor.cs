using UnityEditor;
using InventorySystem.Runtime.UI;

namespace InventorySystem.Editor
{
    /// <summary>
    /// <see cref="UiwEquipmentGroupPanel"/> 的自定义 Inspector：按「槽位列表布局方式」开关，
    /// 仅显示当前模式（自动 / 手动）需要配置的字段，隐藏另一模式的无关字段。
    /// </summary>
    [CustomEditor(typeof(UiwEquipmentGroupPanel))]
    public class UiwEquipmentGroupPanelEditor : UnityEditor.Editor
    {
        private SerializedProperty _groupNameText;
        private SerializedProperty _displayMode;
        private SerializedProperty _slotListPrefab;
        private SerializedProperty _slotListContainer;
        private SerializedProperty _manualSlotLists;
        private SerializedProperty _groupId;
        private SerializedProperty _bindOnStart;

        private void OnEnable()
        {
            _groupNameText     = serializedObject.FindProperty("groupNameText");
            _displayMode       = serializedObject.FindProperty("displayMode");
            _slotListPrefab    = serializedObject.FindProperty("slotListPrefab");
            _slotListContainer = serializedObject.FindProperty("slotListContainer");
            _manualSlotLists   = serializedObject.FindProperty("manualSlotLists");
            _groupId           = serializedObject.FindProperty("groupId");
            _bindOnStart       = serializedObject.FindProperty("bindOnStart");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Script 条目（C# 脚本来源，只读；自定义 Inspector 默认不显示，此处手动补上）
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            EditorGUILayout.PropertyField(_groupNameText);
            EditorGUILayout.PropertyField(_displayMode);

            EditorGUILayout.Space(4);
            var mode = (UiwEquipmentGroupPanel.DisplayMode)_displayMode.enumValueIndex;
            if (mode == UiwEquipmentGroupPanel.DisplayMode.Manual)
            {
                EditorGUILayout.LabelField("手动模式", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "在本物体层级下手动摆放槽位列表物体，再在下方逐条指定「槽位列表 ID → 槽位列表」。\n" +
                    "槽位列表 ID 须与装备组配置中某槽位列表的 ID 一致。",
                    MessageType.Info);
                EditorGUILayout.PropertyField(_manualSlotLists, true);
            }
            else
            {
                EditorGUILayout.LabelField("自动模式", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_slotListPrefab);
                EditorGUILayout.PropertyField(_slotListContainer);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_groupId);
            EditorGUILayout.PropertyField(_bindOnStart);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

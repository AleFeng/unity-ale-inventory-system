using UnityEditor;
using UnityEngine;
using Ale.Inventory.Runtime.UI;
using static Ale.Inventory.Editor.InventoryEditorL10n;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// <see cref="UiwEquipmentSlotList"/> 的自定义 Inspector：按「装备槽布局方式」开关，
    /// 仅显示当前模式（自动 / 手动）需要配置的字段，隐藏另一模式的无关字段。
    /// </summary>
    [CustomEditor(typeof(UiwEquipmentSlotList))]
    public class UiwEquipmentSlotListEditor : UnityEditor.Editor
    {
        private SerializedProperty _nameText;
        private SerializedProperty _displayMode;
        private SerializedProperty _slotPrefab;
        private SerializedProperty _slotContainer;
        private SerializedProperty _manualSlots;

        private void OnEnable()
        {
            _nameText      = serializedObject.FindProperty("nameText");
            _displayMode   = serializedObject.FindProperty("displayMode");
            _slotPrefab    = serializedObject.FindProperty("slotPrefab");
            _slotContainer = serializedObject.FindProperty("slotContainer");
            _manualSlots   = serializedObject.FindProperty("manualSlots");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Script 条目（C# 脚本来源，只读；自定义 Inspector 默认不显示，此处手动补上）
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            EditorGUILayout.PropertyField(_nameText);
            EditorGUILayout.PropertyField(_displayMode);

            EditorGUILayout.Space(4);
            var mode = (UiwEquipmentSlotList.DisplayMode)_displayMode.enumValueIndex;
            if (mode == UiwEquipmentSlotList.DisplayMode.Manual)
            {
                EditorGUILayout.LabelField(Tr("手动模式"), EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    Tr("在本物体层级下手动摆放装备槽物体，再在下方逐条指定「槽位 ID → 装备槽」。\n" +
                       "槽位 ID 须与槽位列表配置中某装备槽的 ID 一致。"),
                    MessageType.Info);
                EditorGUILayout.PropertyField(_manualSlots, true);
            }
            else
            {
                EditorGUILayout.LabelField(Tr("自动模式"), EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_slotPrefab);
                EditorGUILayout.PropertyField(_slotContainer);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

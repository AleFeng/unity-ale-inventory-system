using UnityEditor;
using UnityEngine;
using Ale.Inventory.Runtime;
using Ale.Inventory.Runtime.UI;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// <see cref="UiwSkillView"/> 的自定义 Inspector：按当前「技能来源」<see cref="ESkillSource"/>，
    /// 仅显示该来源需要配置的 ID 字段（Equipment→装备组 ID、Inventory→仓库 ID、Character→角色 ID），
    /// 隐藏其它来源的无关字段；InventoryDatabase 来源显示全部技能、无需 ID。
    /// 其余字段（标题 / 列表 / 视图切换 / 分组页签 / 搜索 / 打开）照常绘制。
    /// </summary>
    [CustomEditor(typeof(UiwSkillView))]
    public class UiwSkillViewEditor : UnityEditor.Editor
    {
        private SerializedProperty _source;
        private SerializedProperty _equipmentGroupId;
        private SerializedProperty _inventoryId;
        private SerializedProperty _characterId;
        private SerializedProperty _skillRefAttrId;

        private void OnEnable()
        {
            _source           = serializedObject.FindProperty("source");
            _equipmentGroupId = serializedObject.FindProperty("equipmentGroupId");
            _inventoryId      = serializedObject.FindProperty("inventoryId");
            _characterId      = serializedObject.FindProperty("characterId");
            _skillRefAttrId   = serializedObject.FindProperty("skillRefAttrId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Script 条目（只读；自定义 Inspector 默认不显示，此处手动补上）
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            // 技能来源 + 按来源仅显示对应的 ID / 技能引用属性
            EditorGUILayout.PropertyField(_source);
            switch ((ESkillSource)_source.enumValueIndex)
            {
                case ESkillSource.Equipment:
                    EditorGUILayout.PropertyField(_equipmentGroupId,
                        new GUIContent("装备组 ID", "从该装备组所有装备槽的已装备道具采集技能。"));
                    EditorGUILayout.PropertyField(_skillRefAttrId);
                    break;
                case ESkillSource.Inventory:
                    EditorGUILayout.PropertyField(_inventoryId,
                        new GUIContent("仓库 ID", "从该仓库所有道具采集技能。"));
                    EditorGUILayout.PropertyField(_skillRefAttrId);
                    break;
                case ESkillSource.Character:
                    EditorGUILayout.PropertyField(_characterId,
                        new GUIContent("角色 ID", "显示该角色（SkillRuntimeManager）当前已学会的技能。"));
                    break;
                // InventoryDatabase：显示数据库全部技能，无需 ID 与技能引用属性。
            }

            // 其余字段（含继承的 titleLabel）照常绘制，排除已在上方按来源单独处理的字段。
            DrawPropertiesExcluding(serializedObject,
                "m_Script", "source", "equipmentGroupId", "inventoryId", "characterId", "skillRefAttrId");

            serializedObject.ApplyModifiedProperties();
        }
    }
}

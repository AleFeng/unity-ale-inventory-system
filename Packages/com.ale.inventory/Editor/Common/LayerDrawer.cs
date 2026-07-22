using InventorySystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace InventorySystem.Editor
{
    /// <summary>
    /// <see cref="LayerAttribute"/> 的绘制器：把 <c>int</c> 字段以 Layer 单选下拉呈现，
    /// 与 GameObject 右上角的 Layer 下拉一致（单选、返回 Layer 索引）。
    /// </summary>
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 仅对 int 字段生效；其它类型回退到默认绘制，避免误用报错。
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            int newLayer = EditorGUI.LayerField(position, label, property.intValue);
            if (newLayer != property.intValue)
                property.intValue = newLayer;
            EditorGUI.EndProperty();
        }
    }
}

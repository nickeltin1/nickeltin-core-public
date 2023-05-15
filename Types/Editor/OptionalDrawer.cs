using nickeltin.Core.Runtime;
using UnityEditor;
using UnityEngine;


namespace nickeltin.Core.Editor
{
    [CustomPropertyDrawer(typeof(Optional<>), true)]
    internal class OptionalDrawer : PropertyDrawer
    {
        private const int TOGGLE_WIDTH = 24;
        private const int INDENT_STEP = 15;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative(nameof(Optional<int>.Value));
            var enabled = property.FindPropertyRelative(nameof(Optional<int>.Enabled));

            position.width -= TOGGLE_WIDTH;
            EditorGUI.BeginDisabledGroup(!enabled.boolValue);
            EditorGUI.PropertyField(position, value, label, true);
            EditorGUI.EndDisabledGroup();

            position.x += position.width + TOGGLE_WIDTH;
            position.width = position.height = EditorGUI.GetPropertyHeight(enabled);
            position.x -= position.width + (EditorGUI.indentLevel * INDENT_STEP);
            EditorGUI.PropertyField(position, enabled, GUIContent.none);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative(nameof(Optional<int>.Value));
            return EditorGUI.GetPropertyHeight(value);
        }
    }

}
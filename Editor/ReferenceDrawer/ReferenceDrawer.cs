using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Allows to draw field with popup to select type of value in form of enum.
    /// </summary>
    public abstract class ReferenceDrawer : PropertyDrawer
    {
        private static class Defaults
        {
            public static readonly GUIStyle popupStyle;

            static Defaults()
            {
                popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"))
                {
                    imagePosition = ImagePosition.ImageOnly
                };
            }
        }
        protected readonly struct PropertyData
        {
            private static readonly GUIContent OBSOLETE = new GUIContent("OBSOLETE");
        
            public delegate void DrawerFunc(Rect rect, GUIContent label);

            public readonly SerializedProperty Property;
            public readonly bool Disabled;

            private DrawerFunc Drawer { get; }
            
            public PropertyData(SerializedProperty property) : this(property, false) { }

            public PropertyData(SerializedProperty property, bool disabled) : this()
            {
                this.Property = property;
                this.Disabled = disabled;
                this.Drawer = DefaultDrawer;
            }
            
            public PropertyData(SerializedProperty property, bool disabled, DrawerFunc customDrawer) : this(property, disabled)
            {
                this.Drawer = customDrawer;
            }
            
            
            public static implicit operator PropertyData (SerializedProperty source) => new PropertyData(source);

            private void DefaultDrawer(Rect rect, GUIContent label) => EditorGUI.PropertyField(rect, Property, label, true);
            private static void ObsoleteDrawer(Rect rect, GUIContent label) => EditorGUI.LabelField(rect, label, OBSOLETE, EditorStyles.boldLabel);

            public void Draw(Rect rect, GUIContent label) => Drawer(rect, label);

            public static PropertyData Obsolete => new PropertyData(null, true, ObsoleteDrawer);
        }
        
        protected abstract string ReferenceTypePropName { get; }

       
        private SerializedProperty _referenceType;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            
            _referenceType = property.FindPropertyRelative(ReferenceTypePropName);
            
            DrawProperty(position, label, GetPropertiesToDraw(property));
            
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUI.EndProperty();
        }

        protected abstract IEnumerable<PropertyData> GetPropertiesToDraw(SerializedProperty property);
        
        private void DrawPopup(Rect position)
        {
            var buttonRect = new Rect(position);
            
            var indent = EditorGUI.indentLevel;
            var style = Defaults.popupStyle;
            var popupWidth = style.fixedWidth + style.margin.right;
            var popupHeight = style.fixedHeight + style.margin.top;
            buttonRect.width = popupWidth + (indent * popupWidth);
            buttonRect.height = popupHeight;
            buttonRect.x += ((EditorGUIUtility.labelWidth - (indent * (popupWidth - 1))) - popupWidth);
            
            
            _referenceType.enumValueIndex = EditorGUI.Popup(buttonRect, _referenceType.enumValueIndex, 
                _referenceType.enumDisplayNames, style);
        }

        private void DrawProperty(Rect position, GUIContent label, IEnumerable<PropertyData> propertiesData)
        {
            DrawPopup(position);
            
            var propId = 0;
            foreach (var propertyData in propertiesData)
            {
                if (propId == _referenceType.enumValueIndex)
                {
                    using (new EditorGUI.DisabledScope(propertyData.Disabled))
                    {
                        propertyData.Draw(position, label);
                    }
                }
                propId++;
            }
        }
    }
}
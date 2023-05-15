using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using nickeltin.Core.Editor;
using nickeltin.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    [CustomPropertyDrawer(typeof(SerializableType))]
    internal class SerializableTypeDrawer : PropertyDrawer
    {
        private static readonly GUIContent none = new GUIContent("<none>", "Click to select type");
        private static readonly GUIContent missing = new GUIContent("<missing>", "Last values: ");
        private static readonly GUIContent regular = new GUIContent("FILL TEXT BEFORE DRAWING", "FILL TOOLTIP BEFORE DRAWING");
        private static Texture2D missingTex;
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            missingTex = (Texture2D)EditorGUIUtility.IconContent("Invalid").image;
            missing.image = missingTex;
            none.image = missingTex;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var serializableType = (SerializableType)property.GetObjectValue();
            var asm = serializableType.AssemblyString;
            var tp = serializableType.TypeString;
            var type = serializableType.Get();
            var hasType = type != null;
            var typeMissing = !hasType && (!string.IsNullOrEmpty(asm) || !string.IsNullOrEmpty(tp));

            EditorGUI.BeginProperty(position, label, property);
            var rect = EditorGUI.PrefixLabel(position, label);

            GUIContent content = null;
            if (hasType)
            {
                content = regular;
                content.text = type.Name;
                content.image = TypeSearchWindow.GetIconForType(type);
                content.tooltip = type.FullName;
            }
            else if (typeMissing)
            {
                content = missing;
                content.tooltip = "Last values: " + tp + "," + asm;
            }
            else
            {
                content = none;
            }

            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive))
            {
                var searchWindowRect = TypeSearchWindow.CalcualteFieldRect(rect);
                TypeSearchWindow.Open(TypeSearchWindow.CreateEntries(GetValidTypes()), entry =>
                {
                    var attr = fieldInfo.GetCustomAttribute<ShowTypeChangeWarningAttribute>();
                    var newType = (Type)entry.GetData();
                    if (attr != null && type != null)
                    {
                        if(!EditorUtility.DisplayDialog(
                               "Warning", $"Type will be changed from \"{type}\" to \"{newType}\". " + 
                                          $"This might break some already existing references, are you sure?", "Change type", "Cancel"))
                        {
                            return;
                        }
                    }
                    var assemblyStr = property.FindPropertyRelative(nameof(SerializableType._assembly));
                    var typeStr = property.FindPropertyRelative(nameof(SerializableType._type));
                    if (newType != null)
                    {
                        assemblyStr.stringValue = newType.Assembly.FullName;
                        typeStr.stringValue = newType.FullName;
                    }
                    else
                    {
                        assemblyStr.stringValue = "";
                        typeStr.stringValue = "";
                    }

                    serializableType._isDirty = true;
                    property.serializedObject.ApplyModifiedProperties();
                }, position: searchWindowRect.position, size: searchWindowRect.size);
            }
            
            EditorGUI.EndProperty();
        }

        private IEnumerable<Type> GetValidTypes()
        {
            bool Validate(Type t) => !t.IsAbstract && t.IsPublic;
            var type = typeof(object);
            var attr = fieldInfo.GetCustomAttribute<TypeConstraintAttribute>();
            if (attr != null) type = attr.constrainedBaseType;
            if (Validate(type)) yield return type;
            var types = TypeCache.GetTypesDerivedFrom(type);
            foreach (var t in types.Where(Validate)) yield return t;
        }
    }
}
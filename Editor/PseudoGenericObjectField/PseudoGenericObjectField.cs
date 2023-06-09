using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Some static functions to mimic Unity Object field, for custom referencing methods (not trough object picker)
    /// </summary>
    public static class PseudoGenericObjectField
    {
        public delegate bool ObjectValidator(Object obj);
        public delegate IEnumerable<ISearchWindowEntry> ValidObjectsGetter();

        public delegate void StringConstructor(StringBuilder stringBuilder);

        public static class Defaults
        {
            public static readonly GUIStyle buttonStyle;
            public static readonly Texture2D invalidIcon;
            public static readonly Texture2D noneIcon;
            public static readonly GUIContent labelContent;
            public static readonly GUIStyle objectField;
            public static readonly GUIStyle textLabelStyle;

            static Defaults()
            {
                var editorStylesType = typeof(EditorStyles);
                var fieldInfo = editorStylesType.GetProperty( "objectFieldButton", BindingFlags.NonPublic | BindingFlags.Static);
                buttonStyle = (GUIStyle)fieldInfo?.GetValue(null);
                invalidIcon = (Texture2D)EditorGUIUtility.IconContent("Invalid").image;
                noneIcon = (Texture2D)EditorGUIUtility.IconContent("console.warnicon").image;
                labelContent = new GUIContent(GUIContent.none);
                objectField = new GUIStyle(EditorStyles.objectField)
                {
                    imagePosition = ImagePosition.ImageLeft,
                };
                textLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    clipping = TextClipping.Clip,
                };
            }
        }

        private static readonly StringBuilder _stringBuilder = new StringBuilder();

        public static void DrawObjectField(Rect position,
            GUIContent label,
            SerializedProperty mainProperty,
            SerializedProperty objectReferenceSource,
            bool isValid,
            ValidObjectsGetter getValidObjects,
            ObjectValidator objectValidator,
            StringConstructor objectName,
            string objectTooltip)
        {
            EditorGUI.BeginProperty(position, label, mainProperty);
            position.height = Mathf.Min(position.height, EditorGUIUtility.singleLineHeight);
            var objRef = objectReferenceSource.objectReferenceValue;
            var hasObjRef = objRef != null;
            position = EditorGUI.PrefixLabel(position, label, EditorStyles.label);
            _stringBuilder.Clear();
            _stringBuilder.Append(hasObjRef ? objRef.name : " None");
            if (!isValid)
            {
                _stringBuilder.Append("<invalid>");
            }

            objectName ??= DefaultStringConstructorMethod;
            objectName(_stringBuilder);

            var icon = !isValid ? Defaults.invalidIcon 
                    : EditorGUIUtility.ObjectContent(objRef, null).image;


            var content = Defaults.labelContent;
            content.text = _stringBuilder.ToString();
            content.tooltip = objectTooltip;
            GUI.BeginGroup(position);
            var localPosition = position;
            localPosition.position = Vector2.zero;
            GUI.Label(localPosition, "", Defaults.objectField);
            
            var overlayPos = localPosition;
            if (hasObjRef)
            {
                overlayPos.x += localPosition.height;
            }
            
            GUI.Label(overlayPos, content, Defaults.textLabelStyle);
            

            if (hasObjRef)
            {
                overlayPos.x -= localPosition.height;
                overlayPos.width = localPosition.height;
                GUI.Label(overlayPos, icon);
            }
            
            var fieldPos = ObjectSearchWindow.CalcualteFieldRect(localPosition);
            var width = EditorGUIUtility.singleLineHeight;
            localPosition.width -= width;
            position.width -= width;
            
            var fieldPosition = position;
            
            var e = Event.current;

            if (hasObjRef)
            {
                if (e.rawType == EventType.MouseDown && localPosition.Contains(e.mousePosition))
                {
                    if (e.clickCount == 1)
                    {
                        EditorGUIUtility.PingObject(objRef);
                        e.Use();
                    }
                    else if (e.clickCount == 2)
                    {
                        AssetDatabase.OpenAsset(objRef);
                        e.Use();
                    }
                }
            }

            localPosition.height -= 2;
            localPosition.x += localPosition.width - 0.5f; 
            localPosition.y += 1;
            localPosition.width = width;
            if (GUI.Button(localPosition, GUIContent.none, Defaults.buttonStyle))
            {
                var objSource = objectReferenceSource;
                ObjectSearchWindow.Open(getValidObjects(), entry =>
                {
                    var obj = entry.GetData() as Object;
                    objSource.objectReferenceValue = obj;
                    objSource.serializedObject.ApplyModifiedProperties();
                }, position: fieldPos.position, size: fieldPos.size);
            }
            
            GUI.EndGroup();
            
            EditorGUI.EndProperty();
            
            HandleDropdown(fieldPosition, objectValidator, objectReferenceSource);
        }

        
        
        public static void DrawObjectField(Rect position, 
            GUIContent label, 
            SerializedProperty mainProperty, 
            SerializedProperty objectReferenceSource, 
            bool isValid, 
            ValidObjectsGetter getValidObjects, 
            ObjectValidator objectValidator,
            string objectTypeString,
            string genericTypeString,
            string objectTooltip)
        {
            DrawObjectField(position, label, mainProperty, objectReferenceSource, isValid, getValidObjects, objectValidator, 
                builder =>
                {
                    builder.Append(" (");
                    builder.Append(objectTypeString);
                    builder.Append("<");
                    builder.Append(genericTypeString);
                    builder.Append(">)");
                }, objectTooltip);
        }
        
        private static void HandleDropdown(Rect fieldPosition, ObjectValidator objectValidator, SerializedProperty objectReferenceSource)
        {
            var e = Event.current;
            if (!fieldPosition.Contains(e.mousePosition))
            {
                return;
            }

            if (DragAndDrop.objectReferences.Length == 0 || DragAndDrop.objectReferences[0] == null)
            {
                return;
            }

            var obj = DragAndDrop.objectReferences[0];
            
            if (objectValidator(obj))
            {
                if (e.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    e.Use();
                }   
                else if (e.type == EventType.DragPerform)
                {
                    e.Use();

                    objectReferenceSource.objectReferenceValue = obj;
                    objectReferenceSource.serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        private static void DefaultStringConstructorMethod(StringBuilder builder) { }
    }
}
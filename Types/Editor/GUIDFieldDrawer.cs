using System.Text;
using nickeltin.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    [CustomPropertyDrawer(typeof(GUIDField))]
    public class GUIDFieldDrawer : PropertyDrawer
    {
        private static class Defaults
        {
            public static readonly GUIStyle paneOptions;
            public const string defaultTooltip = "(Using manual id is not recommended)";
            public static readonly string[] idOptions;
            public static readonly GUIContent idContent;
            public static readonly StringBuilder tooltipBuilder;
            
            static Defaults()
            {
                paneOptions = "PaneOptions";
                paneOptions.imagePosition = ImagePosition.ImageOnly;
                
                idOptions = new[] { "GUID", "Manual ID" };
                idContent = new GUIContent("ID", defaultTooltip);

                tooltipBuilder = new StringBuilder();
            }
        }

        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.contextualPropertyMenu += ContextualPropertyMenu;
        }

        private static void ContextualPropertyMenu(GenericMenu menu, SerializedProperty property)
        {
            property.GetFieldInfoAndElementStaticTypeFromProperty(out var type);
            if (type == typeof(GUIDField))
            {
                menu.AddItem(new GUIContent("Generate GUID"), false, prop =>
                {
                    var property1 = (SerializedProperty)prop;
                    var guid = property1.FindPropertyRelative(nameof(GUIDField._guid));
                    guid.stringValue = GUIDField.NewGUID();
                    guid.serializedObject.ApplyModifiedProperties();
                    ((GUIDField)property1.GetValue()).DirtyHash();
                }, property);
                menu.AddItem(new GUIContent("Print Hash"), false, prop =>
                {
                    var field = (GUIDField)property.GetValue();
                    Debug.Log($"Hash code for GUID: {field.Value} is: {field.GetHashedGUID()}");
                }, property);
            }
        }

        public static bool ShowHashCollision;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Draw(position, property, label);
        }

        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            EditorGUI.BeginChangeCheck();
            
            var buttonRect = new Rect(position);
            
            var indent = EditorGUI.indentLevel;
            var style = Defaults.paneOptions;
            var popupWidth = style.fixedWidth + style.margin.right;
            var popupHeight = style.fixedHeight + style.margin.top;
            buttonRect.width = popupWidth + (indent * popupWidth);
            buttonRect.height = popupHeight;
            buttonRect.x += ((EditorGUIUtility.labelWidth - (indent * (popupWidth - 1))) - popupWidth);

            var useUserDefId = property.FindPropertyRelative(nameof(GUIDField._usingUserDefinedId));

            var i = useUserDefId.boolValue ? 1 : 0;
            i = EditorGUI.Popup(buttonRect, i, Defaults.idOptions, style);

            switch (i)
            {
                case 0: useUserDefId.boolValue = false; break;
                case 1: useUserDefId.boolValue = true; break;
            }

           
            Defaults.idContent.text = label.text;
            var tooltipSB = Defaults.tooltipBuilder;
            tooltipSB.Clear();
            if (!string.IsNullOrEmpty(label.tooltip))
            {
                tooltipSB.AppendLine(label.tooltip);
                tooltipSB.AppendLine();
            }

            if (ShowHashCollision)
            {
                tooltipSB.AppendLine(Defaults.defaultTooltip);
                tooltipSB.AppendLine();
                tooltipSB.Append("Has hash collisions. Current GUID produced same hash as the other.");
            }
            else
            {
                tooltipSB.Append(Defaults.defaultTooltip);
            }
            
            Defaults.idContent.tooltip = tooltipSB.ToString();
            
            position = EditorGUI.PrefixLabel(position, Defaults.idContent);

            var color = GUI.color;
            GUI.color = ShowHashCollision ? Color.red : color;
            if (useUserDefId.boolValue)
            {
                var userDefId = property.FindPropertyRelative(nameof(GUIDField._userDefinedId));
                EditorGUI.PropertyField(position, userDefId, GUIContent.none);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    var guid = property.FindPropertyRelative(nameof(GUIDField._guid));
                    EditorGUI.PropertyField(position, guid, GUIContent.none);
                }
            }

            GUI.color = color;

            if (EditorGUI.EndChangeCheck())
            {
                var guidField = (GUIDField)property.GetValue();
                guidField.DirtyHash();
            }
            EditorGUI.EndProperty();
        }
    }
}
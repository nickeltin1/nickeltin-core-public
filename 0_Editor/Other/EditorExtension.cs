using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    public static class EditorExtension
    {
        public static ScriptableObject CreateScriptableObjectInAsset(this Object parent, Type type,
            HideFlags hideFlags = HideFlags.None)
        {
            var child = ScriptableObject.CreateInstance(type);
            Undo.RegisterCreatedObjectUndo(child, $"{parent}: creating child");
            child.name = type.Name;
            AssetDatabase.AddObjectToAsset(child, parent);
            return child;
        }

        [Obsolete]
        public static IEnumerable<SerializedObject> GetAllInspectedObjects(this UnityEditor.Editor editor)
        {
            foreach (var editorTarget in editor.targets)
            {
                yield return new SerializedObject(editorTarget);
            }
        }

        public static void SearchInProjectWindow(string searchText)
        {
            EditorUtility.FocusProjectWindow();
            var projectBrowserType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
            var projectBrowser = EditorWindow.GetWindow(projectBrowserType);
            var serachMethod = projectBrowserType.GetMethod("SetSearch", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
                null, new[]{ typeof(string) }, null);
            serachMethod!.Invoke(projectBrowser, new object[] { searchText });
        }

        public static bool TryGetActiveFolderPath(out string path)
        {
            var _tryGetActiveFolderPath = typeof(ProjectWindowUtil).GetMethod("TryGetActiveFolderPath", 
                BindingFlags.Static | BindingFlags.NonPublic);
            object[] args = { null };
            var found = (bool)_tryGetActiveFolderPath!.Invoke( null, args );
            path = (string)args[0];
            return found;
        }

        /// <summary>
        /// Validates is <see cref="EditorGUIUtility.systemCopyBuffer"/> has serialized with JSON object,
        /// and targeted type has any of serialized fields name.
        /// To fill this buffer you can use <see cref="EditorJsonUtility.ToJson(object)"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool ValidatePasteBuffer(Type type)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return fields.Any(field => EditorGUIUtility.systemCopyBuffer.Contains(field.Name));
        }

        public static Rect GetPropertyRect(float addHeight = 2)
        {
            return GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight + addHeight);
        }
    }
}
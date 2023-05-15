using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor.EmbeddedPackages
{
    [CustomEditor(typeof(EmbeddedPackage))]
    internal class EmbeddedPackageEditor : UnityEditor.Editor
    {
        private SerializedProperty _author;
        private SerializedProperty _desc;
        private SerializedProperty _name;
        private SerializedProperty _path;
        private SerializedProperty _configPath;
        private SerializedProperty _deprecated;
        
        private void OnEnable()
        {
            _author = serializedObject.FindProperty(nameof(EmbeddedPackage._author));
            _desc = serializedObject.FindProperty(nameof(EmbeddedPackage._description));
            _name = serializedObject.FindProperty(nameof(EmbeddedPackage._displayName));
            _path = serializedObject.FindProperty(nameof(EmbeddedPackage._createdFromPath));
            _configPath = serializedObject.FindProperty(nameof(EmbeddedPackage._configCreatedFromPath));
            _deprecated = serializedObject.FindProperty(nameof(EmbeddedPackage._deprecated));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            
            // this.DrawScriptField();
            
            EditorGUILayout.PropertyField(_name);
            EditorGUILayout.PropertyField(_author);
            EditorGUILayout.PropertyField(_desc);
            EditorGUILayout.PropertyField(_deprecated);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_path);
                EditorGUILayout.PropertyField(_configPath);
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var texture = ((EmbeddedPackage)target)._icon;
            if (texture == null) return null;
            var previewTex = new Texture2D(texture.width, texture.height);
            EditorUtility.CopySerialized(texture, previewTex);
            return previewTex;
        }
    }
}
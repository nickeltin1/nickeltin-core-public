using System.Collections.Generic;
using nickeltin.Runtime.Utility;
using UnityEditor;
using UnityEngine;

namespace nickeltin.CodeGeneration.Editor
{
    internal class GeneratedCodeObserverWindow : EditorWindow
    {
        private const string EDITOR_EXPAND_KEY = nameof(GeneratedCodeObserverWindow) + ".editorExpanded";
        private const string RUNTIME_EXPAND_KEY = nameof(GeneratedCodeObserverWindow) + ".runtimeExpanded";
        private const string WINDOW_TITLE = "Generated Code";

        private bool _editorExpanded;
        private bool _runtimeExpanded;
        
        // [MenuItem(MenuPathsUtility.internalMenu + WINDOW_TITLE + " Window")]
        private static void ShowWindow() => ShowWindow_Internal();

        private static GeneratedCodeObserverWindow ShowWindow_Internal()
        {
            var window = GetWindow<GeneratedCodeObserverWindow>();
            window.minSize = new Vector2(1000, 400);
            window.Show();
            return window;
        }

        private GeneratedScriptsSelection _selection;
        private static Texture2D _defaultIcon;
        
        public void OnEnable()
        {
            _defaultIcon = (Texture2D)EditorGUIUtility.IconContent("d_cs Script Icon").image;
            titleContent = new GUIContent(WINDOW_TITLE);

            _selection = new GeneratedScriptsSelection();


            _editorExpanded = EditorPrefs.GetBool(EDITOR_EXPAND_KEY);
            _runtimeExpanded = EditorPrefs.GetBool(RUNTIME_EXPAND_KEY);
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool(EDITOR_EXPAND_KEY, _editorExpanded);
            EditorPrefs.GetBool(RUNTIME_EXPAND_KEY, _runtimeExpanded);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            DrawSripts(_selection.GetEditorScripts(), "Editor", ref _editorExpanded);
            DrawSripts(_selection.GetRuntimeScripts(), "Runtime", ref _runtimeExpanded);
            EditorGUILayout.EndVertical();
        }

        private void DrawSripts(IList<GeneratedScriptFile> files, string name, ref bool expanded)
        {
            EditorGUILayout.BeginVertical();
            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, name + " (files: " + files.Count + ")");
            if (expanded)
            {
                foreach (var file in files)
                {
                    DrawScriptFile(file);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawScriptFile(GeneratedScriptFile file)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
            GUILayout.Label(new GUIContent(file.fileInfo.Name, _defaultIcon), GUILayout.MaxHeight(20), 
                GUILayout.ExpandWidth(false));

            GUILayout.Label(new GUIContent(file.projectRelativePath, file.fileInfo.FullName), 
                EditorStyles.linkLabel, GUILayout.ExpandWidth(true));
            
            EditorGUILayout.EndVertical();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using nickeltin.ProjectSettings.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.ProjectSettings.Editor
{
    internal class ProjectsSettingsProvider : SettingsProvider
    {
        private readonly Type _managerType;
        private UnityEditor.Editor _editor;
        private ProjectSettingsAsset _asset;
            
        private ProjectsSettingsProvider(string path, SettingsScope scopes, Type managerType) : base(path, scopes)
        {
            _managerType = managerType;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _asset = ProjectSettingsHandle.GetInstance(_managerType);
            _editor = UnityEditor.Editor.CreateEditor(_asset);
            this.keywords = GetSearchKeywordsFromSerializedObject(_editor.serializedObject);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            _editor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                ProjectSettingsHandle.IO.Save(_asset);
            }
            
        }

        [SettingsProviderGroup]
        public static SettingsProvider[] CreateSettingsProvider()
        {
            var list = new List<SettingsProvider>();

            foreach (var manager in ProjectSettingsHandle.GetAllInstances())
            {
                var type = manager.GetType();
                var attribute = type.GetCustomAttribute<ProjectSettingsAttribute>();
                if (attribute != null && attribute.HideInEditor)
                {
                    continue;
                }
                ProjectSettingsAsset.Log("Creating settings provider for " + manager);
                list.Add(new ProjectsSettingsProvider(manager.GetProjectPath(), SettingsScope.Project, type));
            }

            return list.ToArray();
        }
        
    }

}
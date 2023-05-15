using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditorInternal;
#endif

namespace nickeltin.ProjectSettings.Runtime
{
#if UNITY_EDITOR
    /// <summary>
    /// Editor only class to keep references for <see cref="ProjectSettingsAsset"/>
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSettingsHandle
    {
        private static readonly List<Type> _types;
        private static readonly Dictionary<Type, ProjectSettingsAsset> _settingsAssets;

        static ProjectSettingsHandle()
        {
            _types = new List<Type>();
            _settingsAssets = new Dictionary<Type, ProjectSettingsAsset>();
            var scrManagersType = TypeCache.GetTypesDerivedFrom<ProjectSettingsAsset>();
            foreach (var type in scrManagersType.Where(type => !type.IsAbstract && !type.IsGenericType))
            {
                _types.Add(type);
            }
            _settingsAssets = LoadAll().ToDictionary(p => p.GetType());
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeRecompile;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeRecompile;

            ProjectSettingsAsset.Log("Project settings assets loaded");
        }
        
        /// <summary>
        /// Creates new or loads existing manager;
        /// </summary>
        private static ProjectSettingsAsset Load(Type type)
        {
            if (_settingsAssets.ContainsKey(type) && _settingsAssets[type] != null)
            {
                return _settingsAssets[type];
            }

            if (!IO.Load(type, out ProjectSettingsAsset manager))
            {
                manager = Create(type);
            }

            if (_settingsAssets.ContainsKey(type)) _settingsAssets[type] = manager;
            else _settingsAssets.Add(type, manager);
                
            manager.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
            
            return manager;
        }

        private static void OnBeforeRecompile()
        {
            foreach (var asset in _settingsAssets.Values)
            {
                Object.DestroyImmediate(asset, true);
            }
            
            _settingsAssets.Clear();
            ProjectSettingsAsset.Log("Project settings assets unloaded");
        }

        private static ProjectSettingsAsset Create(Type type)
        {
            var asset = ScriptableObject.CreateInstance(type) as ProjectSettingsAsset;
            asset.name = type.Name;
            IO.Save(asset);
            ProjectSettingsAsset.Log($"New manager {asset.name} created");
            return asset;
        }
        
        internal static T GetInstance<T>() where T : ProjectSettingsAsset
        {
            return Load(typeof(T)) as T;
        }
        
        internal static ProjectSettingsAsset GetInstance(Type settingsType)
        {
            return Load(settingsType);
        }

        private static IEnumerable<ProjectSettingsAsset> LoadAll() => _types.Select(Load);

        internal static IEnumerable<ProjectSettingsAsset> GetAllInstances() => LoadAll();

        public static void Save(this ProjectSettingsAsset target) => IO.Save(target);

        internal static class IO
        {
            private static string PathFor(Type type)
            {
                return Paths.libRoot + type.Name + ".asset";
            }

            public static bool Load(Type type, out ProjectSettingsAsset manager)
            {
                string path = PathFor(type);
                bool exist = File.Exists(path);
                if (exist)
                {
                    ProjectSettingsAsset.Log($"File for type {type.Name} found, loading...");
                    manager = InternalEditorUtility.LoadSerializedFileAndForget(path).FirstOrDefault() as ProjectSettingsAsset;
                    return true;
                }

                manager = null;
                return false;
            }

            public static void Save(ProjectSettingsAsset asset)
            {
                asset.OnSave();
                var path = PathFor(asset.GetType());
                Directory.CreateDirectory(Paths.libRoot);
                InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] {asset}, path, true);
                ProjectSettingsAsset.Log($"{asset.name} saved to {path}");
            }
        }
        
        internal static class Paths
        {
            public const string lib = "nickeltin/";
            public const string projectRoot = "ProjectSettings/";
            public const string libRoot = projectRoot + lib;
            public const string buildAssetsRoot = "Assets/ProjectSettingsBuildCache";
            public const string projectSettingsRoot = "Project/" + lib;
        }
    }
#endif
}
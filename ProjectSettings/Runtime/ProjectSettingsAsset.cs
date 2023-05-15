using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
#endif

namespace nickeltin.ProjectSettings.Runtime
{
    public abstract class ProjectSettingsAsset : ScriptableObject
    {
        private const bool LOG = false;

        public static void Log(object message)
        {
            if (LOG)
            {
                Debug.Log(message);
            }
        }
        
#if UNITY_EDITOR
        public abstract string GetProjectPath();

        /// <summary>
        /// Invoked only in editor, after asset was saved.
        /// Similar to on OnAfterDeserialize call.
        /// Data from input fields might be cached here.
        /// Consider using #if UNITY_EDITOR to prevent build errors if overwritten.
        /// </summary>
        public virtual void OnSave()
        {
            
        }

        
        /// <summary>
        /// Invoked only in editor, during build process, while asset is in memory but not written to disk (Not exist as 'Asset') 
        /// Some editor data might be transferred here into different form that will work in build.
        /// Consider using #if UNITY_EDITOR to prevent build errors if overwritten.
        /// </summary>
        public virtual void OnBuild(BuildReport buildReport)
        {
            
        }
#endif
    }
    
    public abstract class ProjectSettingsAsset<T> : ProjectSettingsAsset where T : ProjectSettingsAsset
    {
#if !UNITY_EDITOR
        private static T instance;
#endif
        
        public static T Get()
        {
#if UNITY_EDITOR
            return ProjectSettingsHandle.GetInstance<T>();
#else
            return instance;
#endif
        }
        

        private void OnEnable()
        {
#if !UNITY_EDITOR
            Log($"Assigning instance for {name}");
            instance = this as T;
#endif
            
            OnLoad();
        }

        
        protected virtual void OnLoad() { }

#if UNITY_EDITOR
        public override string GetProjectPath()
        {
            var type = typeof(T);
            string name = type.Name;
            var attribute = type.GetCustomAttribute<ProjectSettingsAttribute>();
            if (attribute != null) name = attribute.name;
            return ProjectSettingsHandle.Paths.projectSettingsRoot + name;
        }
#endif
    }
}
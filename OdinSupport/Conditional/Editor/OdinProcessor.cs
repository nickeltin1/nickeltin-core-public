using System.Reflection;
using nickeltin.OdinSupport.Runtime;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace nickeltin.OdinSupport.Editor
{
    internal static class OdinProcessor 
    {
        [InitializeOnLoadMethod]
        private static void Process()
        {
            var excludedClasses = TypeCache.GetTypesWithAttribute<ExcludeEditorFromOdinAttribute>();
            var config = InspectorConfig.Instance;
            foreach (var excludedClass in excludedClasses)
            {
                if (!typeof(Object).IsAssignableFrom(excludedClass)) continue;
            
                var attr = excludedClass.GetCustomAttribute<ExcludeEditorFromOdinAttribute>();
                if (attr.Exclude)
                {
                    config.DrawingConfig.SetEditorType(excludedClass, null);
                }
                else
                {
                    config.DrawingConfig.ClearEditorEntryForDrawnType(excludedClass);
                }
            }
        }
    }
}
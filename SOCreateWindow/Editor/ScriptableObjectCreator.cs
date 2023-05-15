using System.Collections.Generic;
using System.Reflection;
using nickeltin.Core.Editor;
using nickeltin.SOCreateWindow.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SOCreateWindow.Editor
{
    internal static class ScriptableObjectCreator
    {
        private static bool _cacheValid;
        private static List<ISearchWindowEntry> _entries;
        private static MenuCommand _lastCommand;
       
        private static void ValidateCache()
        {
            if (_cacheValid) return;
            
            _cacheValid = true;
            _entries = new List<ISearchWindowEntry>();
            var typeCollection = TypeCache.GetTypesWithAttribute<CreateAssetWindowAttribute>();
            foreach (var type in typeCollection)
            {
                if (!ScriptableObjectCreateWindow.IsTypeValid(type))
                {
                    continue;
                }

                var attr = type.GetCustomAttribute<CreateAssetWindowAttribute>();
                var entryData = new ScriptableObjectCreateWindow.ObjectData(attr, type);
                var entry = new ScriptableObjectCreateWindow.Entry(entryData);
                _entries.Add(entry);
            }

            var methodCollection = TypeCache.GetMethodsWithAttribute<CustomCreateAssetWindowAttribute>();
            foreach (var method in methodCollection)
            {
                if (!method.IsStatic) continue;
                
                var attr = method.GetCustomAttribute<CustomCreateAssetWindowAttribute>();
                var entryData = new ScriptableObjectCreateWindow.ObjectData(attr, method);
                var entry = new ScriptableObjectCreateWindow.Entry(entryData);
                _entries.Add(entry);
            }
        }

        private const string PATH = "Assets/Create/<Scriptable Objects>";

        [MenuItem(PATH, priority = 10)]
        private static void ShowDropdown(MenuCommand command)
        {
            ValidateCache();
            EditorExtension.TryGetActiveFolderPath(out var path);
            ScriptableObjectCreateWindow.Open(_entries, entry =>
            {
                var data = (ScriptableObjectCreateWindow.ObjectData)entry.GetData();
                if (data.CustomAssetCreateHandler != null)
                {
                    data.CustomAssetCreateHandler(path);
                }
                else
                {
                    path = AssetDatabase.GenerateUniqueAssetPath(path + "/" + data.FileName + ".asset");
                    var instance = ScriptableObject.CreateInstance(data.Type);
                    ProjectWindowUtil.CreateAsset(instance, path);
                }
                
            }, false, true, size: new Vector2(400, 600), topLabel: "Create Scriptable Object");
        }
        
    }
}
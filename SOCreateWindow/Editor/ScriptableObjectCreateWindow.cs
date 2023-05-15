using System;
using System.Collections.Generic;
using System.Reflection;
using nickeltin.Core.Editor;
using nickeltin.SOCreateWindow.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.SOCreateWindow.Editor
{
    public class ScriptableObjectCreateWindow : SearchWindowBase<ScriptableObjectCreateWindow>
    {
        private static Texture2D _defaultIcon;
        private static Dictionary<Type, Texture2D> _previewsCache;

        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            _defaultIcon = (Texture2D)EditorGUIUtility.IconContent("ScriptableObject Icon").image;
            _previewsCache = new Dictionary<Type, Texture2D>();
        }

        public readonly struct ObjectData
        {
            public readonly CreateAssetWindowAttributeBase Attribute;
            public readonly Type Type;
            public readonly string FileName;
            public readonly CustomCreateAssetWindowAttribute.AssetCreateHandler CustomAssetCreateHandler;

            public ObjectData(CustomCreateAssetWindowAttribute customAttribute, MethodInfo assetCreateHandler)
            {
                Attribute = customAttribute;
                Type = customAttribute.ProducedType;
                FileName = customAttribute.FileName;
                if (string.IsNullOrEmpty(FileName))
                {
                    FileName = Type != null ? Type.Name : assetCreateHandler.Name;
                }
                
                CustomAssetCreateHandler = (CustomCreateAssetWindowAttribute.AssetCreateHandler)Delegate.CreateDelegate(
                    typeof(CustomCreateAssetWindowAttribute.AssetCreateHandler),
                    assetCreateHandler, false);

                if (CustomAssetCreateHandler == null)
                {
                    throw new Exception($"Custom asset create delegate at {assetCreateHandler.DeclaringType}.{assetCreateHandler.Name} not creatable, see signature in " + 
                                   typeof(CustomCreateAssetWindowAttribute.AssetCreateHandler).FullName); ;
                }
            }
            
            public ObjectData(CreateAssetWindowAttribute attribute, Type type)
            {
                Attribute = attribute;
                Type = type;
                FileName = Attribute.FileName;
                if (string.IsNullOrEmpty(FileName))
                {
                    FileName = Type.Name;
                }
                CustomAssetCreateHandler = null;
            }
        }
        
    
        public readonly struct Entry : ISearchWindowEntry
        {
            private readonly ObjectData _data;
            private readonly string[] _path;
            private readonly string[] _pathAlias;

            public Entry(ObjectData data) : this()
            {
                _data = data;
                var path = data.Attribute.Path;
                var lastEntry = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(path))
                {
                    _path = new string[] { "Uncategorized", lastEntry };
                }
                else
                {
                    _path = path.Split("/", StringSplitOptions.RemoveEmptyEntries);
                    ArrayUtility.Add(ref _path, lastEntry);
                }
                _pathAlias = new string[_path.Length];
                Array.Copy(_path, _pathAlias, _path.Length);
                _pathAlias[^1] = data.FileName;
            }
            
            public object GetData() => _data;
            public string[] GetPathAlias() => _pathAlias;
            public string[] GetPath() => _path;
        }

        public static bool IsTypeValid(Type type)
        {
            return type is { IsAbstract: false, IsGenericType: false } && typeof(ScriptableObject).IsAssignableFrom(type);
        }
        
        protected override Texture2D GetIconForEntry(ISearchWindowEntry entry)
        {
            var data = entry.GetData();
            var type = entry switch
            {
                Entry => ((ObjectData)data).Type,
                TypeSearchWindow.Entry => (Type)data,
                _ => null
            };

            return GetPreview(type);
        }
        
        private static Texture2D CachePreview(Type type)
        {
            var instance = CreateInstance(type);
            var icon = (Texture2D)EditorGUIUtility.ObjectContent(instance, type).image;
            DestroyImmediate(instance);
            _previewsCache.Add(type, icon);
            return icon;
        }
        
        public static Texture2D GetPreview(Type type)
        {
            if (type != null && _previewsCache.TryGetValue(type, out var preview))
            {
                return preview;
            }

            if (IsTypeValid(type))
            {
                return CachePreview(type);
            }

            return _defaultIcon;
        }
        
        
    }
}
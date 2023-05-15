using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    public class TypeSearchWindow : SearchWindowBase<TypeSearchWindow>
    {
        public readonly struct Entry : ISearchWindowEntry
        {
            private readonly Type type;
            private readonly string[] path;
            public Entry(Type type)
            {
                this.type = type;
                path = type.FullName?.Split('.');
            }

            public object GetData() => type;
            public string[] GetPathAlias() => path;
            public string[] GetPath() => path;
        }
        
        private static Texture2D _defaultIcon;
        private static Texture2D _systemObjectIcon;
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            _defaultIcon = (Texture2D)EditorGUIUtility.IconContent("d_cs Script Icon").image;
            _systemObjectIcon = (Texture2D)EditorGUIUtility.IconContent("dll Script Icon").image;
        }
        
        protected override Texture2D GetIconForEntry(ISearchWindowEntry entry)
        {
            return GetIconForType((Type)entry.GetData());
        }
        

        public static IEnumerable<ISearchWindowEntry> CreateEntries(IEnumerable<Type> types)
        {
            foreach (var type in types) yield return new Entry(type);
        }

        public static Texture2D GetIconForType(Type type)
        {
            var unityObj = typeof(Object);
            if (type == null) return _defaultIcon;
            if (type == unityObj || type.IsSubclassOf(unityObj) && type != typeof(object))
            {
                var icon = AssetPreview.GetMiniTypeThumbnail(type);
                if (icon == null) icon = _defaultIcon;
                return icon;
            }
            
            return _systemObjectIcon;
        }
    }
}

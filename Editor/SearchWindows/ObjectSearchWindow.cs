using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    public class ObjectSearchWindow : SearchWindowBase<ObjectSearchWindow>
    {
        public readonly struct BaseEntry : ISearchWindowEntry
        {
            private readonly Object obj;
            private readonly string[] path;
            
            public BaseEntry(Object obj)
            {
                this.obj = obj;
                this.path = new string[] { obj.name };
            }

            public object GetData() => obj;
            public string[] GetPathAlias() => path;

            public string[] GetPath() => path;
        }
        
        public readonly struct UniqueEntry : ISearchWindowEntry
        {
            private readonly Object obj;
            private readonly string[] path;
            private readonly string[] pathAlias;
            
            public UniqueEntry(Object obj)
            {
                this.obj = obj;
                var guid = Guid.NewGuid().ToString();
                this.path = new string[] { guid };
                this.pathAlias = new string[] {  obj.name };
                
            }

            public object GetData() => obj;
            public string[] GetPathAlias() => pathAlias;
            public string[] GetPath() => path;
        }
        
        public readonly struct NestedEntry : ISearchWindowEntry
        {
            private readonly Object obj;
            private readonly string[] path;
            private readonly string[] pathAlias;
            
            public NestedEntry(Object obj, Object parent, string objGuid, string parentGuid)
            {
                this.obj = obj;
                path = new[] { parentGuid, objGuid };
                pathAlias = new[] { parent.name, obj.name };
            }

            public object GetData() => obj;
            public string[] GetPathAlias() => pathAlias;
            public string[] GetPath() => path;
        }
        
        protected override Texture2D GetIconForEntry(ISearchWindowEntry entry)
        {
            var obj = (Object)entry.GetData();
            return (Texture2D)EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;;
        }
        
        public static IEnumerable<ISearchWindowEntry> CreateEntries(IEnumerable<Object> objects)
        {
            foreach (var obj in objects) yield return new BaseEntry(obj);
        }
        
        public static IEnumerable<ISearchWindowEntry> CreateUniqueEntries(IEnumerable<Object> objects)
        {
            foreach (var obj in objects) yield return new UniqueEntry(obj);
        }
    }
}
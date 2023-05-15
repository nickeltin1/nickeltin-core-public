using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor.EmbeddedPackages
{
    [PreferBinarySerialization]
    public class EmbeddedPackage : ScriptableObject
    {
        private const string DEFAULT_NAME = "name";
        private const string DEFAULT_DESC = "description";
        private const string DEFAULT_AUTHOR = "unknown author";
        private const string DEFAULT_PATH = "unknown path";
        
        [SerializeField] internal TextAsset _nativeData;
        [SerializeField] internal TextAsset _nativeConfigData;
        [SerializeField] internal Texture2D _icon;
        [SerializeField] internal string _displayName = DEFAULT_NAME;
        [SerializeField] internal string _author = DEFAULT_AUTHOR;
        [SerializeField, TextArea(5, 20)] internal string _description = DEFAULT_DESC;
        [SerializeField] internal bool _deprecated;

        [SerializeField] internal string _createdFromPath = DEFAULT_PATH;
        [SerializeField] internal string _configCreatedFromPath = DEFAULT_PATH;
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_displayName)) _displayName = DEFAULT_NAME;
            if (string.IsNullOrEmpty(_author)) _author = DEFAULT_AUTHOR;
        }


        public static IEnumerable<EmbeddedPackage> FindAll()
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(EmbeddedPackage).FullName);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                yield return AssetDatabase.LoadAssetAtPath<EmbeddedPackage>(path);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    public class ImportSettingsUserData
    {
        [Serializable]
        private struct Entry
        {
            public string key;
            public string data;

            public Entry(string key, string data)
            {
                this.key = key;
                this.data = data;
            }
        }
        
        [Serializable]
        private class RawData
        {
            public Entry[] entries;

            public RawData()
            {
                entries = Array.Empty<Entry>();
            }

            public RawData(int capacity)
            {
                entries = new Entry[capacity];
            }
        }

        private readonly AssetImporter _importer;
        private readonly Dictionary<string, string> _keyToData;

        private ImportSettingsUserData(AssetImporter importer)
        {
            _keyToData = new Dictionary<string, string>();
            _importer = importer;
        }
        
        public T Read<T>(string key, T defaultObject)
        {
            if (_keyToData.ContainsKey(key))
            {
                return JsonUtility.FromJson<T>(_keyToData[key]);
            }

            return defaultObject;
        }
        
        public void Write(string key, object objectToWrite)
        {
            var serializedData = JsonUtility.ToJson(objectToWrite);
            if (_keyToData.ContainsKey(key)) _keyToData[key] = serializedData;
            else _keyToData.Add(key, serializedData);
        }

        public static ImportSettingsUserData Load(AssetImporter importer)
        {
            var instance = new ImportSettingsUserData(importer);
            var rawData = new RawData();
            if (importer != null)
            {
                JsonUtility.FromJsonOverwrite(importer.userData, rawData);
            }

            
            foreach (var entry in rawData.entries)
            {
                instance._keyToData.Add(entry.key, entry.data);
            }
            

            return instance;
        }
        
        public void Save()
        {
            var rawData = new RawData(_keyToData.Count);
            var i = 0;
            foreach (var entry in _keyToData)
            {
                rawData.entries[i] = new Entry(entry.Key, entry.Value);
                i++;
            }
            
            var serializedData = JsonUtility.ToJson(rawData);
            _importer.userData = serializedData;
        }
    }
}
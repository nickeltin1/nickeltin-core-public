using System;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace nickeltin.Core.Editor
{
    internal static class NickeltinCoreUpdater
    {
        [Serializable]
        private class PackageData
        {
            public string version = "0.0.0";

            public override string ToString()
            {
                return $"PackageData (version: {version})";
            }
        }
    
        private const string PACKAGE_JSON_URL = "https://raw.githubusercontent.com/nickeltin1/nickeltin-core-public/prod/package.json";
        
        private static AddRequest _addRequest;
        private static Version _currentVersion;

        
        /// <summary>
        /// TODO: fetch current version of core, if it installed from git check for updates
        /// TODO: Show popup about update and save preferences to skip update if user wants to
        /// TODO: Context menus to download update
        /// TODO: Progress bar to show update
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Init()
        {
            _currentVersion = new Version(1,0,0);
            
        }

        private static void SendVersionValidationRequest()
        {
            var www = UnityWebRequest.Get(PACKAGE_JSON_URL);
            Debug.Log("Fetching nickeltin-core package manifest from git repo");
            var requestAsyncOperation = www.SendWebRequest();
            requestAsyncOperation.completed += RequestCompletedCallback;
        }

        private static void RequestCompletedCallback(AsyncOperation operation)
        {
            var www = ((UnityWebRequestAsyncOperation)operation).webRequest;
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                var packageData = new PackageData();
                var packageJson = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data, 3, www.downloadHandler.data.Length - 3);
                Debug.Log(packageJson);
                JsonUtility.FromJsonOverwrite(packageJson, packageData);
                var version = packageData.version;
                if (Version.TryParse(version, out var parsedVersion))
                {
                    if (parsedVersion > _currentVersion)
                    {
                        Debug.Log("Found a higher version: " + parsedVersion.ToString());
                    }
                    else
                    {
                        Debug.Log("No higher version found.");
                    }
                }
                else
                {
                    Debug.LogError("Failed to parse version: " + version);
                }
            }
            else if (www.result != UnityWebRequest.Result.InProgress)
            {
                Debug.LogError("Failed to fetch package.json content: " + www.error);
            }

            www.Dispose();
        }
    }
}
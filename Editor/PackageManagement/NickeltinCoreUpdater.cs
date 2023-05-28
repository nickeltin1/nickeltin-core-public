using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

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


        private const string SKIPPED_VERSION_KEY = NickeltinCoreDefines.Name + ".SKIPPED_VERSION";
        private const string PROD_BRANCH = "prod";
        private static string PACKAGE_JSON_URL(string usernameAndRepoName) => $"https://raw.githubusercontent.com/{usernameAndRepoName}/{PROD_BRANCH}/package.json";
        
        private static AddRequest _addRequest;
        private static ListRequest _listRequest;
        private static PackageInfo _packageInfo;


        /// <summary>
        /// TODO: Context menus to download update
        /// TODO: Progress bar to show update
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Init()
        {
            _listRequest = Client.List(true);
            EditorApplication.update += CheckPackageFetchRequest;
        }
        
        private static void CheckPackageFetchRequest()
        {
            if (!_listRequest.IsCompleted) return;
            
            EditorApplication.update -= CheckPackageFetchRequest;
            
            if (_listRequest.Status == StatusCode.Success)
            {
                _packageInfo = _listRequest.Result.FirstOrDefault(p => p.name == NickeltinCoreDefines.Name);
                if (_packageInfo is { source: PackageSource.Git })
                {
                    SendVersionValidationRequest(_packageInfo, false);
                }
            }

            _listRequest = null;
        }

        private static void SendVersionValidationRequest(PackageInfo packageInfo, bool forceShowUpdatePopup)
        {
            var currentVersion = new Version(packageInfo.version);
            var match = Regex.Match(packageInfo.packageId, @"@(.*?)/(.*?)(?:\.git(?:#|$)|$)");

            if (match.Success && match.Groups.Count >= 3)
            {
                var username = match.Groups[1].Value;
                var repository = match.Groups[2].Value;
                var repositoryPath = $"{repository}";

                Debug.Log(repositoryPath);
            }
            
            var www = UnityWebRequest.Get(PACKAGE_JSON_URL("nickeltin1/nickeltin-core-public"));
            var requestAsyncOperation = www.SendWebRequest();
            requestAsyncOperation.completed += operation =>
            {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    var packageData = new PackageData();
                    var packageJson = Encoding.UTF8.GetString(www.downloadHandler.data, 3, www.downloadHandler.data.Length - 3);
                    JsonUtility.FromJsonOverwrite(packageJson, packageData);
                    var newVersion = Version.Parse(packageData.version);
                    if (newVersion > currentVersion)
                    {
                        Debug.Log("New core version avaliable: " + newVersion);
                        TryDisplayPackageUpdateDialog(packageInfo, currentVersion, newVersion, forceShowUpdatePopup);
                    }
                }
                
                Debug.Log(www.result + " " + www.error);

                www.Dispose();
            };
        }

        private static void TryDisplayPackageUpdateDialog(PackageInfo packageInfo, Version currentVersion, Version newVersion, bool forceShowPopup)
        {
            if (!forceShowPopup)
            {
                var skippedVersionStr = EditorPrefs.GetString(SKIPPED_VERSION_KEY, "");
                if (!string.IsNullOrEmpty(skippedVersionStr))
                {
                    var skippedVersion = new Version(skippedVersionStr);
                    if (skippedVersion == newVersion)
                    {
                        Debug.Log($"Not showing update dialog because version were skipped by user, dialog will be shown for next version avaliable.");
                        return;
                    }
                }
            }
           
        
            var result = EditorUtility.DisplayDialogComplex($"{NickeltinCoreDefines.Name} version {newVersion} available!", 
                $"New version of {NickeltinCoreDefines.Name} is avaliable. Current version: {currentVersion}, new version: {newVersion}. " +
                $"Package that uses {NickeltinCoreDefines.Name} might require the latest version to function properly.",
                "Install", "No, thanks", $"Don't show this message for version {newVersion}");


            switch (result)
            {
                case 0:
                    EditorPrefs.DeleteKey(SKIPPED_VERSION_KEY);
                    Client.Add(packageInfo.packageId);
                    break;
                case 1:
                    // Do nothing
                    break;
                case 2:
                    EditorPrefs.SetString(SKIPPED_VERSION_KEY, newVersion.ToString());
                    break;
            }
        }
    }
}
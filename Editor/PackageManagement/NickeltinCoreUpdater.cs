using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using nickeltin.Core.Runtime;
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


        private const string SKIPPED_VERSION_KEY = NickeltinCore.Name + ".SKIPPED_VERSION";
        private const string PROD_BRANCH = "prod";
        private static string PACKAGE_JSON_URL(string usernameAndRepoName) => $"https://raw.githubusercontent.com/{usernameAndRepoName}/{PROD_BRANCH}/package.json";
        
        // private static PMRequest<AddRequest> _addRequest;
        private static PMRequest<ListRequest> _packageFetchRequest;
        
        private static PackageInfo _packageInfo;


        /// <summary>
        /// TODO: Progress bar to show update
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Init()
        {
            CheckForUpdates(false);
        }
        
        private const string CHECK_FOR_UPDATES = MenuPathsUtility.baseMenu + "Check for nickletin-core updates";
        
        [MenuItem(CHECK_FOR_UPDATES)]
        private static void CheckForUpdates_Context()
        {
            CheckForUpdates(true);
        }

        private static void CheckForUpdates(bool forceCheck)
        {
            if (_packageInfo != null)
            {
                if (forceCheck)
                {
                    NickeltinCore.Log("Package info already fetched, checking for updates.");
                }
                TrySendVersionValidationRequest(_packageInfo, forceCheck);
            }
            else
            {
                if (_packageFetchRequest == null)
                {
                    if (forceCheck)
                    {
                        NickeltinCore.Log("Fetching local package info");
                    }
                    
                    _packageFetchRequest = new PMRequest<ListRequest>(Client.List(true));
                    _packageFetchRequest.Completed += (request, status) =>
                    {
                        _packageFetchRequest = null;
                        if (status == StatusCode.Success)
                        {
                            _packageInfo = request.Result.FirstOrDefault(p => p.name == NickeltinCore.Name);
                            if (_packageInfo != null)
                            {
                                if (forceCheck)
                                {
                                    NickeltinCore.Log("Local package info fetched");
                                }
                                
                                TrySendVersionValidationRequest(_packageInfo, forceCheck);
                            }
                        }
                        
                        if (forceCheck && _packageInfo == null)
                        {
                            NickeltinCore.Log($"Can't fetch current {NickeltinCore.Name} pacakge. " +
                                                     $"Error code: {request.Error.errorCode}, message: {request.Error.message}", LogType.Error);
                        }
                    };
                }
                else
                {
                    NickeltinCore.Log("Check for updates already queried");
                }
            }
            
        }

        private static void TrySendVersionValidationRequest(PackageInfo packageInfo, bool forceCheck)
        {
            if (packageInfo.source != PackageSource.Git)
            {
                if (forceCheck)
                {
                    NickeltinCore.Log("Package installed not from git", LogType.Error);
                }
                return;
            }
            
            var currentVersion = new Version(packageInfo.version);
            var match = Regex.Match(packageInfo.packageId, @"@(.*?)/(.*?)(?:\.git(?:#|$)|$)");

            Debug.Log(packageInfo.packageId);
            //TODO:
            // if (match.Success && match.Groups.Count >= 3)
            // {
            //     var username = match.Groups[1].Value;
            //     var repository = match.Groups[2].Value;
            //     var repositoryPath = $"{repository}";
            //
            //     Debug.Log(repositoryPath);
            // }
            
            var www = UnityWebRequest.Get(PACKAGE_JSON_URL("nickeltin1/nickeltin-core-public"));
            var requestAsyncOperation = www.SendWebRequest();
            requestAsyncOperation.completed += operation =>
            {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    var packageData = new PackageData();
                    // Removing first 3 bytes known as UTF-8 BOM. It causes problem for JsonUtility
                    var packageJson = Encoding.UTF8.GetString(www.downloadHandler.data, 3, www.downloadHandler.data.Length - 3);
                    JsonUtility.FromJsonOverwrite(packageJson, packageData);
                    var newVersion = Version.Parse(packageData.version);
                    if (newVersion > currentVersion)
                    {
                        TryDisplayPackageUpdateDialog(packageInfo, currentVersion, newVersion, forceCheck);
                    }
                    else if (forceCheck)
                    {
                        NickeltinCore.Log($"Current version: {currentVersion}, version on the remote: {newVersion}. " +
                                          $"No newer version is available.");
                    }
                }
                else if (forceCheck)
                {
                    NickeltinCore.Log($"Can't fetch version from remote. Request result: {www.result}, error: {www.error}", LogType.Error);
                }
                

                www.Dispose();
            };
        }

        private static void TryDisplayPackageUpdateDialog(PackageInfo packageInfo, Version currentVersion, Version newVersion, bool forceCheck)
        {
            if (!forceCheck)
            {
                var skippedVersionStr = EditorPrefs.GetString(SKIPPED_VERSION_KEY, "");
                if (!string.IsNullOrEmpty(skippedVersionStr))
                {
                    var skippedVersion = new Version(skippedVersionStr);
                    if (skippedVersion == newVersion)
                    {
                        return;
                    }
                }
            }
           
        
            var result = EditorUtility.DisplayDialogComplex($"{NickeltinCore.Name} version {newVersion} available!", 
                $"New version of {NickeltinCore.Name} is avaliable. Current version: {currentVersion}, new version: {newVersion}. " +
                $"Package that uses {NickeltinCore.Name} might require the latest version to function properly.",
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
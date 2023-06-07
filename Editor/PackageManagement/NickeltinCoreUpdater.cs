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
        }


        private abstract class PackageRemote
        {
            public abstract PackageSource Source { get; }

            public abstract string GetURL();

            public abstract Version GetLatestVersion(UnityWebRequest request);
        }

        private const string SKIPPED_VERSION_KEY = NickeltinCore.Name + ".SKIPPED_VERSION";
        private const string PROD_BRANCH = "prod";
        
        private static string PACKAGE_JSON_GITHUB_URL(string usernameAndRepoName)
        {
            return $"https://raw.githubusercontent.com/{usernameAndRepoName}/{PROD_BRANCH}/package.json";
        }

        private static string PACKAGE_REGISTRY_URL(PackageInfo packageInfo)
        {
            return "https://registry.npmjs.org/com.nickeltin.core/latest";
            // return $"{packageInfo.registry.url}/{packageInfo.name}/latest";
        }

        private static PMRequest<AddRequest> _addRequest;
        private static PMRequest<ListRequest> _packageFetchRequest;

        private static readonly Regex _repoGitLocalAddressRegex = new Regex(@"\/([^\/]+\/[^\/]+?)(?:\.git)?(?:#.*)?$");

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

        [MenuItem(MenuPathsUtility.baseMenu + "Check for nickletin-core updates [Registry]")]
        private static void CheckForUpdatesRegistry_Context()
        {
            const bool forceCheck = true;
            if (_packageFetchRequest == null)
            {
                NickeltinCore.Log("Fetching local package info");

                _packageFetchRequest = new PMRequest<ListRequest>(Client.List(true));
                _packageFetchRequest.Completed += (request, status) =>
                {
                    _packageFetchRequest = null;
                    PackageInfo packageInfo = null;
                    if (status == StatusCode.Success)
                    {
                        packageInfo = request.Result.FirstOrDefault(p => p.name == NickeltinCore.Name);
                        if (packageInfo != null)
                        {
                            NickeltinCore.Log("Local package info fetched");

                            Debug.Log(packageInfo.packageId);
                            var currentVersion = new Version(packageInfo.version);
                            var www = UnityWebRequest.Get(PACKAGE_REGISTRY_URL(packageInfo));
                            var requestAsyncOperation = www.SendWebRequest();
                            requestAsyncOperation.completed += operation =>
                            {
                                if (www.result == UnityWebRequest.Result.Success)
                                {
                                    Debug.Log(www.downloadHandler.text);
                    
                                    var packageData = new PackageData();
                                    // Removing first 3 bytes known as UTF-8 BOM. It causes problem for JsonUtility
                                    var packageJson = www.downloadHandler.text;//Encoding.UTF8.GetString(www.downloadHandler.data, 3, www.downloadHandler.data.Length - 3);
                                    JsonUtility.FromJsonOverwrite(packageJson, packageData);
                                    var newVersion = Version.Parse(packageData.version);
                                    
                                    Debug.Log(newVersion);
                                    // if (newVersion > currentVersion)
                                    // {
                                    //     TryDisplayPackageUpdateDialog(packageInfo, currentVersion, newVersion, forceCheck);
                                    // }
                                    // else
                                    // {
                                    //     NickeltinCore.Log($"Current version: {currentVersion}, version on the remote: {newVersion}. " +
                                    //                       $"No newer version is available.");
                                    // }
                                }
                                else
                                {
                                    NickeltinCore.Log($"Can't fetch version from remote. Request result: {www.result}, error: {www.error}", LogType.Error);
                                }


                                www.Dispose();
                            };
                        }
                    }
                    
                    if (packageInfo == null)
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

        private static void CheckForUpdates(bool forceCheck)
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
                    PackageInfo packageInfo = null;
                    if (status == StatusCode.Success)
                    {
                        packageInfo = request.Result.FirstOrDefault(p => p.name == NickeltinCore.Name);
                        if (packageInfo != null)
                        {
                            if (forceCheck)
                            {
                                NickeltinCore.Log("Local package info fetched");
                            }
                            
                            TrySendVersionValidationRequest(packageInfo, forceCheck);
                        }
                    }
                    
                    if (forceCheck && packageInfo == null)
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
        
        private static void TrySendVersionValidationRequest(PackageInfo packageInfo, bool forceCheck)
        {
            var url = "";
            switch (packageInfo.source)
            {
                case PackageSource.Unknown:
                    if (forceCheck)
                    {
                        NickeltinCore.Log("Local package info unknown", LogType.Error);
                    }
                    return;
                case PackageSource.Registry:
                    url = PACKAGE_REGISTRY_URL(packageInfo);
                    break;
                case PackageSource.Git:
                    url = PACKAGE_JSON_GITHUB_URL(
                        _repoGitLocalAddressRegex.Match(packageInfo.packageId).Groups[1].Value);
                    break;
                default:
                    if (forceCheck)
                    {
                        NickeltinCore.Log("Package installed not from git or registry", LogType.Error);
                    }
                    return;
            }
            
            
            var currentVersion = new Version(packageInfo.version);
            var www = UnityWebRequest.Get(url);
            var requestAsyncOperation = www.SendWebRequest();
            requestAsyncOperation.completed += operation =>
            {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.downloadHandler.text);
                    
                    Debug.Log(packageInfo.packageId);
                    var packageData = new PackageData();
                    var packageJson = packageInfo.source == PackageSource.Git
                        // Removing first 3 bytes known as UTF-8 BOM. It causes problem for JsonUtility. Case only for git.
                        ? Encoding.UTF8.GetString(www.downloadHandler.data, 3, www.downloadHandler.data.Length - 3)
                        : www.downloadHandler.text;
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
                    if (_addRequest != null)
                    {
                        NickeltinCore.Log("Package update is installing");
                        break;
                    }
                    
                    NickeltinCore.Log($"Installing update {newVersion}");
                    
                    _addRequest = new PMRequest<AddRequest>(Client.Add(packageInfo.packageId));
                    _addRequest.Completed += (request, status) =>
                    {
                        _addRequest = null;
                        if (status == StatusCode.Success)
                        {
                            NickeltinCore.Log($"Package updated successfully to version {newVersion}");
                        }
                        else
                        {
                            NickeltinCore.Log($"Package update failure. Error code {request.Error.errorCode}, message: {request.Error.message}", LogType.Error);
                        }
                    };
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
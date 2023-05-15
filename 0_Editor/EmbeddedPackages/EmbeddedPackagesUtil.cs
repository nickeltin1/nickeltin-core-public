using System;
using System.IO;
using System.Linq;
using System.Reflection;
using nickeltin.Core.Runtime;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor.EmbeddedPackages
{
    [InitializeOnLoad]
    internal static class EmbeddedPackagesUtil
    {
        public delegate void EmbeddedPackageImportCallback();
        
        public enum PackageDataChannel
        {
            MainData,
            ConfigData
        }
        
        private static string _currentlyImportingUnityPackagePath = "";
        private static string _currentlyImportingUnityPackageName = "";

        private static readonly MethodInfo _extractAndPrepareAssetList;
        private static readonly FieldInfo _importPackageItemIsFolder;
        private static readonly FieldInfo _importPackageItemAssetChanged;
        
        public static bool embeddedPackageImportStarted { get; private set; }
        public static event EmbeddedPackageImportCallback onEmbeddedPackageImportEnded;
        
        static EmbeddedPackagesUtil()
        {
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;

            var editorAssmebly = typeof(UnityEditor.Editor).Assembly;
            
            var packageUtil = editorAssmebly.GetType("UnityEditor.PackageUtility");
            _extractAndPrepareAssetList = packageUtil.GetMethod("ExtractAndPrepareAssetList",
                new[]{ typeof(string), typeof(string).MakeByRefType(), typeof(string).MakeByRefType()});

            var importPackageItem = editorAssmebly.GetType("UnityEditor.ImportPackageItem");
            _importPackageItemIsFolder = importPackageItem.GetField("isFolder");
            _importPackageItemAssetChanged = importPackageItem.GetField("assetChanged");
        }
        
        #region Logging

        
        private static void Log(object message, LogType logType = LogType.Log)
        {
            Debug.LogFormat(logType, LogOption.NoStacktrace, null, "<b>EMBEDDED PACKAGES</b>: {0}", message);
        }

        private static void LogPackageHasNothingNewToImport(string packageName)
        {
            Log($"\"{packageName.FormatPackageName()}\" package has " + "nothing new to import".FormatWarning());
        }
        private static void LogInvalidPath(string path) => Log(path + " invalid path".FormatError());

        private static string FormatPackageName(this string str) => str.Italic().Bold().Size(14).Color(nameof(Color.cyan));
        private static string FormatError(this string str) => str.Bold().Color(Color.red);
        private static string FormatSuccess(this string str) => str.Bold().Color(new Color(0.18f, 1f, 0f));
        private static string FormatWarning(this string str) => str.Bold().Color(Color.yellow);

        #endregion


        #region Creating package
        
        /// <summary>
        /// Creates new embedded package out of folder in Assets
        /// </summary>
        public static EmbeddedPackage ExportFolderAsEmbeddedPackage(string folderPath, string packageName, out string unityPackagePath)
        {
            unityPackagePath = CreateTempFile();
            Log($"Exporting assets at path: {folderPath} as package. Destination: {unityPackagePath}");
            ExportFolderAsUnityPackage(folderPath, unityPackagePath);
            var embeddedPackage = ScriptableObject.CreateInstance<EmbeddedPackage>();

            var embeddedPath = "Assets/" + packageName + ".asset"; 
            embeddedPath = AssetDatabase.GenerateUniqueAssetPath(embeddedPath);
            AssetDatabase.CreateAsset(embeddedPackage, embeddedPath);

            WriteDataToEmbeddedPackage(embeddedPackage, unityPackagePath, true, folderPath);
            
            AssetDatabase.SaveAssets();
            Selection.activeObject = embeddedPackage;
            EditorGUIUtility.PingObject(embeddedPackage);
            return embeddedPackage;
        }

        /// <summary>
        /// Displays dialog for folder selection, then creates new embedded package out of it.
        /// </summary>
        public static EmbeddedPackage CreateEmbeddedPackage()
        {
            var defaultPath = "Assets/";
            if (EditorExtension.TryGetActiveFolderPath(out var tempPath))
            {
                defaultPath = tempPath;
            }
            
            if (TrySelectFolder($"Select source folder for \"NEW\" package", defaultPath, out var packageSourceFolder))
            {
                var fileName = Path.GetFileNameWithoutExtension(packageSourceFolder);
                return ExportFolderAsEmbeddedPackage(packageSourceFolder, fileName, out _);
            }

            LogInvalidPath(packageSourceFolder);
            return null;
        }
        
        public static void ExportFolderAsUnityPackage(string sourceFolderPath, string destinationFilePath)
        {
            AssetDatabase.ExportPackage(sourceFolderPath, destinationFilePath, ExportPackageOptions.Recurse);
        }

        #endregion
        
        /// <summary>
        /// Writes unity package data to embedded package.
        /// If already data exist it will be deleted.
        /// </summary>
        /// <param name="package">Embedded package</param>
        /// <param name="unityPackagePath">Path to unitypackage on disk</param>
        /// <param name="deleteUnityPackage">If true unity package will be deleted form diks, after performing write</param>
        /// <param name="createdFromFolder">Source folder that packages was generated from, just for informational purposes</param>
        /// <param name="dataChannel">Defines which data variables will be filled</param>
        private static void WriteDataToEmbeddedPackage(EmbeddedPackage package, string unityPackagePath, 
            bool deleteUnityPackage, string createdFromFolder = "", PackageDataChannel dataChannel = PackageDataChannel.MainData)
        {
            var bytes = File.ReadAllBytes(unityPackagePath);
            var packageData = new TextAsset(Convert.ToBase64String(bytes));
            Object oldData = null;
            
            switch (dataChannel)
            {
                case PackageDataChannel.MainData:
                    packageData.name = "NativePackageData";
                    oldData = package._nativeData;
                    package._nativeData = packageData;
                    package._createdFromPath = createdFromFolder;
                    break;
                case PackageDataChannel.ConfigData:
                    packageData.name = "NativeConfigData";
                    oldData = package._nativeConfigData;
                    package._nativeConfigData = packageData; 
                    package._configCreatedFromPath = createdFromFolder;
                    break;
            }

            if (oldData != null) AssetDatabase.RemoveObjectFromAsset(oldData);
            Object.DestroyImmediate(oldData, true);
            
            AssetDatabase.AddObjectToAsset(packageData, package);
            
            if (deleteUnityPackage) File.Delete(unityPackagePath);
        }

        /// <summary>
        /// Displays dialog to select package update source folder
        /// After selection creates new unitpackage out of that folder, and replaces its with old package data.
        /// </summary>
        public static void WriteDataToEmbeddedPackage(EmbeddedPackage package, 
            PackageDataChannel dataChannel = PackageDataChannel.MainData)
        {
            var message = "";
            var defaultPath = "";
            var packageName = package.name;
            switch (dataChannel)
            {
                case PackageDataChannel.MainData:
                    message = $"Select source folder for \"{packageName}\" package";
                    defaultPath = package._createdFromPath;
                    break;
                case PackageDataChannel.ConfigData:
                    packageName += "Config";
                    message = $"Select config folder for \"{packageName}\" package";
                    defaultPath = package._configCreatedFromPath;
                    break;
            }
            if (TrySelectFolder(message, defaultPath, out var packageSourceFolder))
            {
                var unityPackagePath = CreateTempFile();
                ExportFolderAsUnityPackage(packageSourceFolder, unityPackagePath);
                WriteDataToEmbeddedPackage(package, unityPackagePath, true, packageSourceFolder, dataChannel);
                
                Log($"\"{packageName.FormatPackageName()}\" updated " + "successfully".FormatSuccess());
                AssetDatabase.SaveAssets();
            }
            else
            {
                LogInvalidPath(packageSourceFolder);
            }
        }

        public static void DeleteConfigData(EmbeddedPackage package)
        {
            if (package._nativeConfigData != null)
            {
                AssetDatabase.RemoveObjectFromAsset(package._nativeConfigData);
                Object.DestroyImmediate(package._nativeConfigData, true);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Unpacks embedded package, if it has any changes imports it.
        /// </summary>
        /// <returns>
        /// True if package installation started successfully
        /// </returns>
        public static bool ImportEmbeddedPackage(EmbeddedPackage embeddedPackage, 
            PackageDataChannel dataChannel = PackageDataChannel.MainData)
        {
            
            var packageName = embeddedPackage.name;
            var packageValid = false;
            switch (dataChannel)
            {
                case PackageDataChannel.MainData:
                    packageValid = embeddedPackage._nativeData != null;
                    break;
                case PackageDataChannel.ConfigData:
                    packageValid = embeddedPackage._nativeConfigData != null;
                    packageName += "Config";
                    break;
            }
            
            if (embeddedPackageImportStarted)
            {
                Log("Can't install package " + packageName.FormatPackageName() + ", while " + "importing other package".FormatError());
                return false;
            }

            if (!packageValid)
            {
                Log($"{packageName.FormatPackageName()} package has no data", LogType.Error);
                return false;
            }
            

            var packagePath = CreateTempFile();

            ExtractUnityPackage(embeddedPackage, packagePath, dataChannel);

            var packageHasSomethingToImport = IsUnityPackageHasSomethingToImport(packagePath);

            //var packageName = Path.GetFileNameWithoutExtension(packagePath);
            if (packageHasSomethingToImport)
            {
                _currentlyImportingUnityPackagePath = packagePath;
                _currentlyImportingUnityPackageName = packageName;
                embeddedPackageImportStarted = true;
                
                Log($"\"{packageName.FormatPackageName()}\" package import started");
                
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.DisallowAutoRefresh();
                AssetDatabase.ImportPackage(packagePath, false);
            }
            else
            {
                LogPackageHasNothingNewToImport(packageName);
                File.Delete(packagePath);
            }

            return packageHasSomethingToImport;
        }

        /// <summary>
        /// Extracts unity package form embedded package, and writes it to dist at specified path.
        /// Afterwards refreshes AssetDatabase
        /// </summary>
        private static void ExtractUnityPackage(EmbeddedPackage embeddedPackage, string extractedPackagePath, 
            PackageDataChannel dataChannel = PackageDataChannel.MainData)
        {
            TextAsset dataSource = null;
            switch (dataChannel)
            {
                case PackageDataChannel.MainData:
                {
                    dataSource = embeddedPackage._nativeData;
                } break;
                case PackageDataChannel.ConfigData:
                {
                    dataSource = embeddedPackage._nativeConfigData;
                    
                } break;
            }

            if (dataSource == null)
            {
                throw new Exception("Data source is null");
            }
            var bytes = Convert.FromBase64String(dataSource.text);
            using (var stream = File.Create(extractedPackagePath, 4096))
            {
                stream.Write(bytes);
            }
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        public static string CreateTempFile()
        {
            var path = Path.GetTempFileName();
            path = Path.ChangeExtension(path, ".unitypackage");
            return path;
        }

        /// <summary>
        /// Exports unity package, validates its content, then deletes it.
        /// Returns true if theres is any changes to the project in unity package.
        /// </summary>
        public static bool ValidateEmbeddedPackageUpdates(EmbeddedPackage embeddedPackage, bool logResults = true)
        {
            var tempPath = CreateTempFile();
            ExtractUnityPackage(embeddedPackage, tempPath);
            var packageHasSomethingToImport = IsUnityPackageHasSomethingToImport(tempPath);
            File.Delete(tempPath);

            if (logResults)
            {
                if (packageHasSomethingToImport)
                {
                    Log($"\"{embeddedPackage.name.FormatPackageName()}\" package " + "has new content to import".FormatSuccess());
                }
                else LogPackageHasNothingNewToImport(embeddedPackage.name);
            }

            return packageHasSomethingToImport;
        }

        /// <summary>
        /// If package installed with default path, folder will be returned.
        /// </summary>
        public static bool TryGetPackgeDefaultInstallFolder(EmbeddedPackage package, out Object folder, 
            bool logIfNotFound = true, PackageDataChannel dataChannel = PackageDataChannel.MainData)
        {
            var path = GetPackageDefaultInstallPath(package, dataChannel);
            var packageName = package.name;
            if (dataChannel == PackageDataChannel.ConfigData)
            {
                packageName += "Config";
            }
            folder = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            var found = folder != null;
            if (logIfNotFound && !found)
            {
                Log($"Default installation folder for package \"{packageName.FormatPackageName()}\" " + "not found".FormatError());
            }
            return folder;
        }


        
        #region Utils

        /// <summary>
        /// Size in bytes of package
        /// </summary>
        public static long GetEmbeddedPackageSize(EmbeddedPackage package)
        {
            var path = AssetDatabase.GetAssetPath(package);
            var info = new FileInfo(path);
            return info.Length;
        }
        
        public static string GetPackageDefaultInstallPath(EmbeddedPackage package, PackageDataChannel dataChannel = PackageDataChannel.MainData)
        {
            switch (dataChannel)
            {
                case PackageDataChannel.MainData: return package._createdFromPath;
                case PackageDataChannel.ConfigData: return package._configCreatedFromPath;
            }

            return "";
        }
        
        // public static string CreateUnityPackagePathInAssets(string packageName)
        // {
        //     var unityPackagePath = "Assets/" + packageName + ".unitypackage";
        //     unityPackagePath = AssetDatabase.GenerateUniqueAssetPath(unityPackagePath);
        //     return unityPackagePath;
        // }
        
        private static object GetUnityPackageContent(string unityPackagePath)
        {
            var parameters = new object[] { unityPackagePath, "", "" };
            return _extractAndPrepareAssetList.Invoke(null, parameters);
        }

        /// <summary>
        /// Returns true if theres is any changes to the project in unity package.
        /// </summary>
        private static bool IsUnityPackageHasSomethingToImport(string unityPackagePath)
        {
            var array = ((Array)GetUnityPackageContent(unityPackagePath));
            var length = array.GetLength(0);
            if (length == 0) return false;
            
            for (var i = 0; i < length; i++)
            {
                var importPackageItem = array.GetValue(i);
                var isFolder = (bool)_importPackageItemIsFolder.GetValue(importPackageItem);
                var assetChanged = (bool)_importPackageItemAssetChanged.GetValue(importPackageItem);
                if (!isFolder && assetChanged)
                {
                    return true;
                }
            }

            return false;
        }
        
        private static bool TrySelectFolder(string message, string defaultPath, out string selectedPath)
        {
            var path = EditorUtility.OpenFolderPanel(message, defaultPath, "");

            selectedPath = path.Replace(Application.dataPath, "Assets");
            return !string.IsNullOrEmpty(path);
        }
        
        private static bool TrySelectTexture(out string selectedTexturePath)
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Select icon source", "Assets/", new[]{ "Image files", "png,jpg,jpeg" });

            selectedTexturePath = path;
            return !string.IsNullOrEmpty(path);
        }
        
        #endregion
        
        #region Icon

        /// <summary>
        /// Show dialog for texture selection.
        /// </summary>
        public static void AddEmbeddedPackagedIconWithDialog(EmbeddedPackage embeddedPackage)
        {
            if (TrySelectTexture(out var texturePath))
            {
                var icon = new Texture2D(2, 2);
                var bytes = File.ReadAllBytes(texturePath);
                icon.LoadImage(bytes);
                AddEmbeddedPackagedIcon(embeddedPackage, icon);
            }
            else
            {
                LogInvalidPath(texturePath);
            }
        }
        
        public static void AddEmbeddedPackagedIcon(EmbeddedPackage embeddedPackage, Texture2D icon)
        {
            if (embeddedPackage._icon != null)
            {
                RemoveEmbeddedPackageIcon(embeddedPackage, false);
            }

            icon.name = "Icon";
            AssetDatabase.AddObjectToAsset(icon, embeddedPackage);
            embeddedPackage._icon = icon;
            AssetDatabase.SaveAssets();
        }

        public static void RemoveEmbeddedPackageIcon(EmbeddedPackage embeddedPackage, bool saveAssets = true)
        {
            AssetDatabase.RemoveObjectFromAsset(embeddedPackage._icon);
            Object.DestroyImmediate(embeddedPackage._icon, true);
            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
            }
        }

        #endregion
        
        
        #region Package import callbacks

        private static void OnImportPackageCompleted(string packagename)
        {
            OnPackageImportEnded($"\"{_currentlyImportingUnityPackageName.FormatPackageName()}\" package imported " + "successfully".FormatSuccess(), LogType.Log);
        }

        private static void OnImportPackageFailed(string packagename, string errormessage)
        {
            OnPackageImportEnded($"\"{_currentlyImportingUnityPackageName.FormatPackageName()}\" package import " + "failed".FormatError() + $"Error: {errormessage}", LogType.Error);
        }

        private static void OnPackageImportEnded(string message, LogType logType)
        {
            if (embeddedPackageImportStarted)
            {
                Log(message, logType);
                
                File.Delete(_currentlyImportingUnityPackagePath);
                
                EditorApplication.UnlockReloadAssemblies();
                AssetDatabase.AllowAutoRefresh();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                _currentlyImportingUnityPackagePath = "";
                _currentlyImportingUnityPackageName = "";
                
                embeddedPackageImportStarted = false;
                
                onEmbeddedPackageImportEnded?.Invoke();
            }
        }
        
        #endregion


        #region Other
        
#if !NICKELTIN_READONLY
        private static PackRequest _packRequest; 
        
        [MenuItem(MenuPathsUtility.packageMenu + "Pack Gzip")]
        private static void PackGzip()
        {
            if (EditorExtension.TryGetActiveFolderPath(out var path))
            {
                // var outputPath = AssetDatabase.GenerateUniqueAssetPath(path + "/" + path.Split("/").Last() + ".gzip");
                // Debug.Log(outputPath);
                _packRequest = Client.Pack(path, path);
                EditorApplication.update += WaitForGZipBuild;
            }
        }
        
        private static void WaitForGZipBuild()
        {
            if (!_packRequest.IsCompleted) return;

            EditorApplication.update -= WaitForGZipBuild;
            Log("Package builded, status: " + _packRequest.Status);
            if (_packRequest.Status == StatusCode.Failure)
            {
                Log(_packRequest.Error.message.Red());
            }
            else
            {
                var identifer = "file:" + _packRequest.Result.tarballPath;
                Log("Package created " + identifer);
            }
            AssetDatabase.Refresh();
        }
#endif

        #endregion
    }
}
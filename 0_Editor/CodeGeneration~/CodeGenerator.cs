using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using nickeltin.Editor;
using nickeltin.Extensions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace nickeltin.CodeGeneration.Editor
{
    /// <summary>
    /// Class that manages generated scripts lifecycle
    /// After script generation in "Source" folder, generator will expose this folder to Unity, to create *.meta files
    /// When meta files created, "Source" folder became hiiden again, and all its content (*.cs/*.meta) copied to "Generated" folder
    /// This folder than exposed to Unity to generate meta files for *.asmdef and package.json
    /// From exposed folder PackageManager builds *.tgz pacakge, hiding "Generated" folder afterwards
    /// Now *.tgz pacakge installs, it contains all original scripts and meta files from "Source" folder, but now its readonly precompiled assembly 
    /// </summary>
    internal static class CodeGenerator
    {
        private const string AUTOGEN_MESSAGE = 
            "//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
            "//!!!This code is generated from nickeltin-lib code generator!!!\n" +
            "//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
            "\n";

        private const string PACKAGE_NAME = "com.nickeltin.nickeltin-generated";

        private static string PACKAGE_MANIFEST_PATH => GENERATED_FOLDER + "package.json";
        
        internal const string NICKELTIN_GENERATED = "nickeltin.Generated";

        //private const string SOURCE_FOLDER = NICKELTIN_GENERATED + "/Source/";
        // private const string EXPOSED_SOURCE_FOLDER = "Assets/" + NICKELTIN_GENERATED + ".Exposed/";
        // private const string SOURCE_FOLDER_EDITOR = SOURCE_FOLDER + "Editor/";
        // private const string SOURCE_FOLDER_RUNTIME = SOURCE_FOLDER + "Runtime/";

        // private static string PROJECT => Application.dataPath.Replace("Assets", "");
        
        public static string GENERATED_FOLDER => NICKELTIN_GENERATED + "/" + PACKAGE_NAME + "/";
        public static string GENERATED_FOLDER_EDITOR => GENERATED_FOLDER + "Editor/";
        public static string GENERATED_FOLDER_RUNTIME => GENERATED_FOLDER + "Runtime/";

        private static string EXPOSED_GENERATED_FOLDER => "Assets/" + PACKAGE_NAME + "/";

        public const string CS_FILTER = "*.cs";

        private const string LOG_HEAD = "<b>CODE GENERATOR</b>:"; 
        
        // private const string TEMP_PACKAGE_LOCATION_IN_ASSETS = "Assets/" + PACKAGE_NAME + "/";
        

        private static FileSystemWatcher _sourceFolderWatcher;
        private static SynchronizationContext _unitySynchronizationContext;
        private static PackRequest _packRequest; 
        private static AddRequest _addRequest;
        private static bool _lockPacakgeRebuild;

        public static event FileSystemEventHandler onGeneratedFolderStateChanged;
        
        // private static bool _packageRebuildQueued;
        
        private static void Log(object message, LogType logType = LogType.Log)
        {
            Debug.LogFormat(logType, LogOption.NoStacktrace, null,  LOG_HEAD + " {0}", message);
        }
        
        private static void LogException(Exception exc)
        {
            Debug.LogException(new Exception(LOG_HEAD, exc));
        }
       
        
        // [InitializeOnLoadMethod]
        private static void Init()
        {
            Directory.CreateDirectory(GENERATED_FOLDER_EDITOR);
            Directory.CreateDirectory(GENERATED_FOLDER_RUNTIME);
            
            _sourceFolderWatcher = CreateWatcher(GENERATED_FOLDER, OnGeneratedFolderChange);

            _unitySynchronizationContext = SynchronizationContext.Current;
        }
        
        
        private static FileSystemWatcher CreateWatcher(string atPath, FileSystemEventHandler onChange)
        {
            var instnace = new FileSystemWatcher(atPath, CS_FILTER);
            instnace.EnableRaisingEvents = true;
            instnace.IncludeSubdirectories = true;
            
            instnace.Changed += onChange;
            instnace.Created += onChange;
            instnace.Deleted += onChange;

            return instnace;
        }

        private static void OnGeneratedFolderChange(object sender, FileSystemEventArgs e)
        {
            onGeneratedFolderStateChanged?.Invoke(sender, e);
            
            if (_lockPacakgeRebuild)
            {
                // _packageRebuildQueued = true;
                return;
            }
            
            // FileSystemWatcher sends callbacks from another thread, so making call to rebuild pacakge from unity sync context
            _unitySynchronizationContext.Post(state => StartPacakgeBuild(), null);
        }
        
        /// <summary>
        /// Locks *.tgz pacakge build
        /// </summary>
        public static void LockPackageRebuild() => _lockPacakgeRebuild = true;

        /// <summary>
        /// Unlocks *.tgz pacakge build
        /// </summary>
        public static void UnlockPackageRebuild() => _lockPacakgeRebuild = false;

        /// <summary>
        /// Generates script at inner folder, afterwards from all generated scripts *.tgz pacakge is builded, and added to project.
        /// Package is splited into two parts: Editor, Runtime
        /// </summary>
        /// <param name="scriptName">Script name is script adress, can be "Characters/Data/Inventory, where last element if .cs file name, and elements before is path in folders"</param>
        /// <param name="scriptContent">Text of script, usings, namespace, classes</param>
        /// <param name="isEditorScript">If true script will go into Editor assembly, eles in Runtime</param>
        public static void GenerateScript(string scriptName, string scriptContent, bool isEditorScript)
        {
            WriteToScript(scriptName, scriptContent, isEditorScript);
        }
        
        
        
        // public static void GenerateScript()
        
        private static void WriteToScript(string scriptName, string scriptContent, bool isEditorScript)
        {
            scriptContent = AUTOGEN_MESSAGE + scriptContent;
            var path = GetScriptPath(scriptName, isEditorScript);
            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, scriptContent);
        }
        

        public static string GetGeneratedFolder(bool isEditor) => isEditor ? GENERATED_FOLDER_EDITOR : GENERATED_FOLDER_RUNTIME;
        private static string GetScriptPath(string scriptName, bool isEditorScript) => GetGeneratedFolder(isEditorScript) + scriptName + ".cs";

        /// <summary>
        /// Copies scripts from source folder to pacakge folder
        /// Creates manifest file and assembly definitions
        /// </summary>
        // [MenuItem(MenuPathsUtility.baseMenu + "Rebuild generated pacakge")]
        private static void StartPacakgeBuild()
        {
            Directory.CreateDirectory(GENERATED_FOLDER_EDITOR);
            Directory.CreateDirectory(GENERATED_FOLDER_RUNTIME);
            
            CreatePackageParts();
            
            
            EditorApplication.LockReloadAssemblies();
            LockPackageRebuild();


            // Copying all package content into assets folder to generate missing *.meta for *.asmdef and pacakge.json files
            if (Directory.Exists(EXPOSED_GENERATED_FOLDER))
            {
                AssetDatabase.DeleteAsset(EXPOSED_GENERATED_FOLDER);
            }
            Directory.Move(GENERATED_FOLDER, EXPOSED_GENERATED_FOLDER);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.Move(EXPOSED_GENERATED_FOLDER, GENERATED_FOLDER);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.DeleteAsset(EXPOSED_GENERATED_FOLDER);
      

            _packRequest = Client.Pack(GENERATED_FOLDER, NICKELTIN_GENERATED);
            EditorApplication.update += WaitForPackageBuild;
        }
        
        private static void CreatePackageParts()
        {
            PackagePartsGenerator.CreatePackageManifest(PACKAGE_MANIFEST_PATH, PACKAGE_NAME, "1.0.0", 
                "nickeltin-generated", "Auto-generated part of nickeltin-lib defined by current project", 
                "Valentyn Romanyshyn");
            
            PackagePartsGenerator.CreateAssemblyDefinition(GENERATED_FOLDER_EDITOR + PACKAGE_NAME + ".Editor.asmdef",
                new []{ "Editor"}, null, new [] { "nickeltin.Editor"});
            
            PackagePartsGenerator.CreateAssemblyDefinition(GENERATED_FOLDER_RUNTIME + PACKAGE_NAME + ".Runtime.asmdef",
                null, null, new [] { "nickeltin.Runtime"});
        }
        
        
        /// <summary>
        /// Waiting for *.tgz pacakage to build
        /// </summary>
        private static void WaitForPackageBuild()
        {
            if (!_packRequest.IsCompleted) return;
        
            EditorApplication.UnlockReloadAssemblies();
            UnlockPackageRebuild();
            
            EditorApplication.update -= WaitForPackageBuild;
            Log("Pacakge builded, status: " + _packRequest.Status);
            if (_packRequest.Status == StatusCode.Failure)
            {
                Log(_packRequest.Error.message.Red());
            }
            else
            {
                var identifer = "file:" + _packRequest.Result.tarballPath;
                Log("Package created " + identifer);
                _addRequest = Client.Add(identifer);
                EditorApplication.update += WaitForPackageInstall;
            }
        }

        /// <summary>
        /// After building *.tgz pacakge, waiting for it to install
        /// </summary>
        private static void WaitForPackageInstall()
        {
            if (!_addRequest.IsCompleted) return;
            
            EditorApplication.update -= WaitForPackageInstall;
            Log("Package install status: " + _addRequest.Status);
            
            if (_addRequest.Status == StatusCode.Failure)
            {
                Log(_addRequest.Error.message.Red());
            }
            else
            {
                Log("Package installed successfully" + _addRequest.Result.name);
            }
        }

        public static IEnumerable<FileInfo> EnumerateScriptFiles(bool isEditor)
        {
            var path = GetGeneratedFolder(isEditor);
            foreach (var filePath in Directory.EnumerateFiles(path, CS_FILTER, SearchOption.AllDirectories))
                yield return new FileInfo(filePath);
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using nickeltin.Core.Runtime;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEditor.PackageManager.Requests;

using Debug = UnityEngine.Debug;
using PackageManagerClient = UnityEditor.PackageManager.Client;

namespace nickeltin.Core.Editor
{
    internal static class ContextMenuProvider
    {
        #region UTILS

        [MenuItem(MenuPathsUtility.utilsMenu + "Open DataPath")]
        private static void PresistentDataPath_Context() => EditorUtility.RevealInFinder(Application.persistentDataPath);


        [MenuItem(MenuPathsUtility.utilsMenu + "Copy Blue Chromakey color")]
        private static void CopyChromakeyColorB_Context() => PasetColorInSystemBuffer(new Color(0, 71f / 255f, 187f / 255f));

        [MenuItem(MenuPathsUtility.utilsMenu +  "Copy Green Chromakey color")]
        private static void CopyChromakeyColorG_Context() => PasetColorInSystemBuffer(new Color(0, 177f / 255f, 64f / 255f));

        private const string RECOMPILE = MenuPathsUtility.utilsMenu + "Recompile";
        
        // [MenuItem(RECOMPILE, true)]
        private static bool Recompile_Validator() => !EditorApplication.isCompiling;

        
        [MenuItem(RECOMPILE)]
        private static void Recompile_Context()
        {
            EditorApplication.UnlockReloadAssemblies();
            CompilationPipeline.RequestScriptCompilation();
        }

        private static void PasetColorInSystemBuffer(Color color) => EditorGUIUtility.systemCopyBuffer = '#' + ColorUtility.ToHtmlStringRGB(color);

        #endregion
        
        #region PACKAGE

        private static ListRequest _packageFindRequest;
        private static Action<UnityEditor.PackageManager.PackageInfo> _requestCallback;

        private static void StartPackageFetchRequest(Action<UnityEditor.PackageManager.PackageInfo> callback)
        {
            _packageFindRequest = PackageManagerClient.List(true);
            _requestCallback = callback;
            EditorApplication.update -= CheckRequest;
            EditorApplication.update += CheckRequest;
        }
        
        private static void CheckRequest()
        {
            if (_packageFindRequest.IsCompleted)
            {
                var package = _packageFindRequest.Result.First(p => p.name == Core.Editor.NickeltinPackageInfo.Name);
                if (_packageFindRequest.Status == StatusCode.Success)
                {
                    _requestCallback(package); 
                    _requestCallback = null;
                }
                else if (_packageFindRequest.Status >= StatusCode.Failure)
                    Debug.LogError(_packageFindRequest.Error.message);
                
                EditorApplication.update -= CheckRequest;
            }
        }

        private const string UPDATE = MenuPathsUtility.packageMenu + "Update";
        private static Stopwatch _stopwatch;
        private static AddRequest _addRequest;
        private static int _addProgressId;


        [MenuItem(UPDATE, true)]
        private static bool UpdatePackage_Validator() => _addRequest == null && !EditorApplication.isCompiling;

        [MenuItem(UPDATE)]
        private static void UpdatePackage_Context()
        {
            if (_addRequest != null)
            {
                Debug.LogError("Update request already started!");
                return;
            }

            StartPackageFetchRequest(UpdatePackage);
        }

        private static void UpdatePackage(UnityEditor.PackageManager.PackageInfo info)
        {
            if (info.source != PackageSource.Git)
            {
                Debug.LogError($"Package {NickeltinPackageInfo.Name} installed not from GIT!");
                return;
            }
            
            var message = "Updating package: " + info.name;
            Debug.Log(message);
            _stopwatch = Stopwatch.StartNew();
            _addRequest = PackageManagerClient.Add(info.packageId);
            _addProgressId = Progress.Start(message, options:Progress.Options.Indefinite);
            
            EditorApplication.update -= WaitForUpdate;
            EditorApplication.update += WaitForUpdate;
        }

        private static void WaitForUpdate()
        {
            if (_addRequest.IsCompleted)
            {
                EditorApplication.update -= WaitForUpdate;
                var status = Progress.Status.Succeeded;
                switch (_addRequest.Status)
                {
                    case StatusCode.Success:
                        status = Progress.Status.Succeeded; 
                        break;
                    case StatusCode.Failure:
                        status = Progress.Status.Failed;
                        break;
                }

                _stopwatch.Stop();
                var time = _stopwatch.ElapsedMilliseconds / 1000f;
                var message = $"Package updated with result {_addRequest.Status} ({time:0.000} s)";
                if (_addRequest.Status == StatusCode.Failure)
                {
                    message += "\nError code: " + _addRequest.Error.message;
                }
                
                Progress.Finish(_addProgressId, status);
                Debug.Log(message);

                _addRequest = null;
            }
        }

        [MenuItem(MenuPathsUtility.packageMenu + "View Changelog")]
        private static void OpenChangelog_Context() => StartPackageFetchRequest(OpenChangelog);

        private static void OpenChangelog(UnityEditor.PackageManager.PackageInfo info)
        {
            var files = Directory.EnumerateFiles(info.resolvedPath, "CHANGELOG.*");
            EditorUtility.RevealInFinder(files.FirstOrDefault());
        }

        [MenuItem(MenuPathsUtility.packageMenu + "Open Git Repo")]
        private static void OpenGitRepo_Context() => StartPackageFetchRequest(OpenGitRepo);
        
        private static void OpenGitRepo(UnityEditor.PackageManager.PackageInfo info)
        {
            if (info.source != PackageSource.Git)
            {
                Debug.LogError($"Package {NickeltinPackageInfo.Name} installed not from GIT!");
                return;
            }
            
            var url = info.packageId.Split("@").Last();
            Debug.Log("Opening URL: " + url);
            Application.OpenURL(url);
        }
            
        #endregion

        #region BUILD

        [MenuItem(MenuPathsUtility.buildMenu + "Build And Run (Ignore Exceptions)")]
        private static void BuildAndRun_Context()
        {
            BuildPlayerWindow.ShowBuildPlayerWindow();
             var type = typeof(BuildPlayerWindow);
             var method = type.GetMethod("CallBuildMethods", BindingFlags.Static | BindingFlags.NonPublic, 
                 null, new []{typeof(bool), typeof(BuildOptions)}, null);
             method?.Invoke(null, new object[]{true, BuildOptions.AutoRunPlayer});
        }

        #endregion

        #region COLLIDER CONTEXT

        [MenuItem("CONTEXT/BoxCollider/Encapsulate Renderers")]
        [MenuItem("CONTEXT/CapsuleCollider/Encapsulate Renderers Min")]
        [MenuItem("CONTEXT/SphereCollider/Encapsulate Renderers Min")]
        private static void EncapsulateRenderers(MenuCommand command)
        {
            EncapsulateCollider(command, false);
        }
        
        [MenuItem("CONTEXT/CapsuleCollider/Encapsulate Renderers Max")]
        [MenuItem("CONTEXT/SphereCollider/Encapsulate Renderers Max")]
        private static void EncapsulateRenderersOutside(MenuCommand command)
        {
            EncapsulateCollider(command, true);
        }

        private static void EncapsulateCollider(MenuCommand command, bool wrapOutsideNotInside)
        {
            static int FindAxisIndex(Vector3 vector3, bool min)
            {
                int current = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (min) { if (vector3[i] < vector3[current]) current = i; }
                    else if (vector3[i] > vector3[current]) current = i;
                }
                return current;
            }

            var collider = command.context as Collider;
            if (collider == null) return;

            Undo.RecordObject(collider, "Collider bounds update");
            
            var rot = collider.transform.rotation;
            collider.transform.rotation = Quaternion.identity;
            
            var renderers = collider.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.Log("0 renderers found");
                return;
            }

            var first = renderers.First().bounds;
            var bounds = new Bounds(first.center, first.size);
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }
            
            var localCenter = collider.transform.InverseTransformPoint(bounds.center);
            var localSize = bounds.size;
            int maxAxisIndex = FindAxisIndex(localSize, false);
            int minAxisIndex = FindAxisIndex(localSize, true);
            var axisIndexes = new[] {0, 1, 2};
            collider.transform.rotation = rot;

            switch (collider)
            {
                case BoxCollider boxCollider:
                    boxCollider.center = localCenter;
                    boxCollider.size = localSize;
                    break;
                case SphereCollider sphereCollider:
                    sphereCollider.center = localCenter;
                    sphereCollider.radius = localSize[wrapOutsideNotInside ? maxAxisIndex : minAxisIndex] / 2f;
                    break;
                case CapsuleCollider capsuleCollider:
                    capsuleCollider.center = localCenter;
                    capsuleCollider.height = localSize[maxAxisIndex];
                    capsuleCollider.direction = maxAxisIndex;
                    if (wrapOutsideNotInside)
                    {
                        var axisExcept = axisIndexes.Where(i => i != maxAxisIndex).ToArray();
                        capsuleCollider.radius = Mathf.Max(localSize[axisExcept[0]], localSize[axisExcept[1]]) / 2f;
                    }
                    else 
                        capsuleCollider.radius = localSize[minAxisIndex] / 2f;
                    break;
                default:
                    throw new Exception($"Unsuported collider type {collider.GetType().Name}");
            }
        }
        
        #endregion
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nickeltin.ProjectSettings.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.ProjectSettings.Editor
{
    internal class ProjectSettingsBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private string dirPath => ProjectSettingsHandle.Paths.buildAssetsRoot;

        private HashSet<string> buildPaths;
        private List<ProjectSettingsAsset> buildInstances;
        private HashSet<Object> preloadedAssets;
        
        private string FilePath(Object file) => dirPath + '/' + file.name + ".asset";

        public int callbackOrder => -200;
       

        public void OnPreprocessBuild(BuildReport report)
        {
            //Debug.Log("Build preprocess");
            buildPaths = new HashSet<string>();
            preloadedAssets = new HashSet<Object>(PlayerSettings.GetPreloadedAssets().Where(s => s != null));
            buildInstances = new List<ProjectSettingsAsset>();
            
            try
            {
                Directory.CreateDirectory(dirPath);

                foreach (var currentInstance in ProjectSettingsHandle.GetAllInstances())
                {
                    var copy = Object.Instantiate(currentInstance);
                    copy.hideFlags = HideFlags.None;
                    copy.name = currentInstance.name;
                    copy.OnBuild(report);
                    string path = FilePath(copy);
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(copy, path);
                    buildPaths.Add(path);
                    var asset = AssetDatabase.LoadAssetAtPath<ProjectSettingsAsset>(path);
                    preloadedAssets.Add(asset);
                    buildInstances.Add(asset);
                    ProjectSettingsAsset.Log($"{path} was created for build.");
                }
            
                PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());

                AssetDatabase.SaveAssets();
            }
            catch
            {
                OnPostprocessBuild(null);
            }
        }
        
        public void OnPostprocessBuild(BuildReport report)
        {
            foreach (var buildInstance in buildInstances)
            {
                preloadedAssets.Remove(buildInstance);
            }

            preloadedAssets.RemoveWhere(asset => asset == null);
            
            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
            
            foreach (var path in buildPaths)
            {
                AssetDatabase.DeleteAsset(path);
                ProjectSettingsAsset.Log($"{path} was deleted after build.");
            }
        
            AssetDatabase.SaveAssets();
            Directory.Delete(dirPath);
            File.Delete(dirPath + ".meta");
            AssetDatabase.Refresh();
        
            buildPaths = null;
            preloadedAssets = null;
            buildInstances = null;
        }
    }
}
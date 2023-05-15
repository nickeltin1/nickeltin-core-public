using System.Reflection;
using nickeltin.Core.Runtime;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    internal static class ContextMenuProvider
    {
        [MenuItem(MenuPathsUtility.utilsMenu + "Open DataPath")]
        private static void PresistentDataPath_Context()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        private const string RECOMPILE = MenuPathsUtility.utilsMenu + "Recompile";


        [MenuItem(RECOMPILE)]
        private static void Recompile_Context()
        {
            EditorApplication.UnlockReloadAssemblies();
            CompilationPipeline.RequestScriptCompilation();
        }

        [MenuItem(MenuPathsUtility.utilsMenu + "Build And Run (Ignore Exceptions)")]
        private static void BuildAndRun_Context()
        {
            BuildPlayerWindow.ShowBuildPlayerWindow();
             var type = typeof(BuildPlayerWindow);
             var method = type.GetMethod("CallBuildMethods", BindingFlags.Static | BindingFlags.NonPublic, 
                 null, new []{typeof(bool), typeof(BuildOptions)}, null);
             method?.Invoke(null, new object[]{true, BuildOptions.AutoRunPlayer});
        }
    }
}
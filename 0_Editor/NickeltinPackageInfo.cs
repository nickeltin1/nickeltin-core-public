using UnityEditor;
using UnityEditor.Compilation;

namespace nickeltin.Core.Editor
{
    internal static class NickeltinPackageInfo
    {
        public static readonly System.Reflection.Assembly CoreEditorAssembly = typeof(NickeltinPackageInfo).Assembly;

        public static string CoreEditorAssemblyName => CoreEditorAssembly.GetName().Name;

        public static readonly string CoreEditorAssemblyDefinitionPath =
            CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(CoreEditorAssemblyName);
    
        public const string Name = "com.nickeltin.nickeltin-lib";

        /// <summary>
        /// If package installed from git (with package manager, meaning its readonly), this will be defined.
        /// </summary>
        public const string PACKAGE_READONLY = "NICKELTIN_READONLY";


        public static bool IsReadonly { get; private set; }

        [InitializeOnLoadMethod]
        private static void EnsureDefines()
        {
            // If asmdef file is open for edit it means it installed as local package.
            var isOpenForEdit = AssetDatabase.IsOpenForEdit(CoreEditorAssemblyDefinitionPath);
            
            IsReadonly = !isOpenForEdit;

            if (IsReadonly)
            {
                DefinesUtil.TryAddDefine(PACKAGE_READONLY);
            }
            else
            {
                DefinesUtil.TryRemoveDefine(PACKAGE_READONLY);
            }
        }
    }
}
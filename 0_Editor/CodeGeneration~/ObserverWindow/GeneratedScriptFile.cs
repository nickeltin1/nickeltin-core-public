using System.IO;
using UnityEditor;

namespace nickeltin.CodeGeneration.Editor
{
    internal class GeneratedScriptFile
    {
        private static string _sriptsEditorRoot;
        private static string _sriptsRuntimeRoot;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            string GetPath(bool isEditor)
            {
                var generatorFolder = CodeGenerator.GetGeneratedFolder(isEditor);
                generatorFolder = generatorFolder.Replace("/", "\\");
                return Directory.GetCurrentDirectory() + "\\" + generatorFolder;
            }

            _sriptsEditorRoot = GetPath(true);
            _sriptsRuntimeRoot = GetPath(false);
        }

        private static string FilterRootFolder(string path, bool isEditor)
        {
            var p = isEditor ? _sriptsEditorRoot : _sriptsRuntimeRoot;
            return path.Replace(p, "");
        }
        
        public readonly FileInfo fileInfo;

        public readonly bool isEditorAssembly;

        public readonly string projectRelativePath;
        
        public GeneratedScriptFile(FileInfo fileInfo, bool isEditorAssembly)
        {
            this.fileInfo = fileInfo;
            this.isEditorAssembly = isEditorAssembly;
            projectRelativePath = FilterRootFolder(fileInfo.FullName, isEditorAssembly);
        }
    }
}
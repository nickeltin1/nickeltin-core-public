using System;
using System.Collections.Generic;
using System.IO;


namespace nickeltin.CodeGeneration.Editor
{
    internal class GeneratedScriptsSelection
    {
        private readonly List<GeneratedScriptFile> _editorFiles;
        private readonly List<GeneratedScriptFile> _runtimeFiles;

        public event Action onSelectionChanged;
        
        public GeneratedScriptsSelection()
        {
            _editorFiles = new List<GeneratedScriptFile>();
            _runtimeFiles = new List<GeneratedScriptFile>();

            UpdateSelection();
            CodeGenerator.onGeneratedFolderStateChanged += OnGeneratedFolderStateChanged;
        }

        ~GeneratedScriptsSelection()
        {
            CodeGenerator.onGeneratedFolderStateChanged -= OnGeneratedFolderStateChanged;
        }
        
        private void OnGeneratedFolderStateChanged(object sender, FileSystemEventArgs e)
        {
            UpdateSelection();
        }
        
        public void UpdateSelection()
        {
            _editorFiles.Clear();
            _runtimeFiles.Clear();
            AddScripts(true);
            AddScripts(false);
            onSelectionChanged?.Invoke();
        }
        
        private void AddScripts(bool isEditor)
        {
            var list = isEditor ? _editorFiles : _runtimeFiles;
            foreach (var fileInfo in CodeGenerator.EnumerateScriptFiles(isEditor))
            {
                list.Add(new GeneratedScriptFile(fileInfo, isEditor));
            }
        }

        public IList<GeneratedScriptFile> GetEditorScripts() => _editorFiles;

        public IList<GeneratedScriptFile> GetRuntimeScripts() => _runtimeFiles;
    }
}
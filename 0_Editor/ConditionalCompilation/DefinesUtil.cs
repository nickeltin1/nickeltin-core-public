using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Pool;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Utility that helps with managing Scripting Define Symbols
    /// </summary>
    internal static class DefinesUtil
    {
        private static FileSystemWatcher _coreAsmDefWatcher;
        private static Dictionary<Type, ModuleDefinition> _modules;
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            _modules = new Dictionary<Type, ModuleDefinition>();
            
            var implTypes = TypeCache.GetTypesDerivedFrom<ModuleImplementation>();
            var defTypes = TypeCache.GetTypesDerivedFrom<ModuleDefinition>();
            
            var impls = DictionaryPool<Type, ModuleImplementation>.Get();
            var defSymbolsSet = HashSetPool<string>.Get();

            foreach (var implType in implTypes)
            {
                if (implType.IsAbstract) continue;
                
                var instance = (ModuleImplementation)Activator.CreateInstance(implType);
                if (!instance.DefinitionType.IsSubclassOf(typeof(ModuleDefinition)))
                {
                    Debug.LogError($"{instance} linked with wrong definition type {instance.DefinitionType}. " +
                                   $"Type should be inherited from {nameof(ModuleDefinition)}");
                    continue;
                }
                
                if (!impls.TryAdd(instance.DefinitionType, instance))
                {
                    Debug.LogError($"Multiple {nameof(ModuleImplementation)} registered for the same " +
                                   $"{instance.DefinitionType} {nameof(ModuleDefinition)} type.");
                }
            }

            foreach (var defType in defTypes)
            {
                if (defType.IsAbstract) continue;

                impls.TryGetValue(defType, out var impl);
                var def = (ModuleDefinition)Activator.CreateInstance(defType, impl);
                if (defSymbolsSet.Add(def.DEFINE_SYMBOL))
                {
                    _modules.Add(defType, def);
                }
                else
                {
                    Debug.LogError($"Multiple {nameof(ModuleDefinition)} has the same " +
                                   $"{nameof(ModuleDefinition.DEFINE_SYMBOL)} of \"{def.DEFINE_SYMBOL}\"");
                }
            }
            
            DictionaryPool<Type, ModuleImplementation>.Release(impls);
            HashSetPool<string>.Release(defSymbolsSet);
            
            UpdateDefineSymbols();
            
            InitDeletionWatcher();
        }

        private static void InitDeletionWatcher()
        {
            var directory = Path.GetDirectoryName(NickeltinCoreInfo.CoreEditorAssemblyDefinitionPath)!;
            var file = Path.GetFileName(NickeltinCoreInfo.CoreEditorAssemblyDefinitionPath)!;
            _coreAsmDefWatcher = new FileSystemWatcher(directory, file);
            _coreAsmDefWatcher.EnableRaisingEvents = true;
            _coreAsmDefWatcher.Deleted += OnAsmDefDeleted;
        }

        private static void OnAsmDefDeleted(object sender, FileSystemEventArgs e)
        {
            Debug.Log(e.FullPath);
            Debug.Log(NickeltinCoreInfo.Name + " deleted");
        }

        public static void UpdateDefineSymbols()
        {
            if (!TryGetDefineSymbols(out var defines, out var buildTargetGroup))
            {
                Debug.LogError($"Can't fetch build target group, current group: {buildTargetGroup}");
                return;
            }

            var changed = false;

            foreach (var moduleDefinition in _modules.Values)
            {
                if (moduleDefinition.Defined)
                {
                    TryAddDefine(ref defines, moduleDefinition.DEFINE_SYMBOL, ref changed);
                }
                else
                {
                    TryRemoveDefine(ref defines, moduleDefinition.DEFINE_SYMBOL, ref changed);
                }
            }

            if (changed)
            {
                SetDefineSymbols(buildTargetGroup, defines);
            }
        }

        public static bool TryAddDefine(string defineSymbol)
        {
            var changed = false;
            if (TryGetDefineSymbols(out var defines, out var buildTargetGroup) 
                && TryAddDefine(ref defines, defineSymbol, ref changed))
            {
                SetDefineSymbols(buildTargetGroup, defines);
                return true;
            }

            return false;
        }
        
        public static bool TryRemoveDefine(string defineSymbol)
        {
            var changed = false;
            if (TryGetDefineSymbols(out var defines, out var buildTargetGroup) 
                && TryRemoveDefine(ref defines, defineSymbol, ref changed))
            {
                SetDefineSymbols(buildTargetGroup, defines);
                return true;
            }

            return false;
        }

        private static bool TryGetDefineSymbols(out string[] defines, out BuildTargetGroup group)
        {
            defines = null;
            group = EditorUserBuildSettings.selectedBuildTargetGroup;

            if (group == BuildTargetGroup.Unknown)
            {
                return false;
            }
            
            PlayerSettings.GetScriptingDefineSymbolsForGroup(group, out defines);
            return true;
        }

        private static void SetDefineSymbols(BuildTargetGroup currentTarget, string[] defines)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(currentTarget, defines);
        }
        
        private static bool TryAddDefine(ref string[] defines, string defineSymbol, ref bool changedFlag)
        {
            if (ArrayUtility.Contains(defines, defineSymbol))
            {
                return false;
            }
            
            ArrayUtility.Add(ref defines, defineSymbol);
            changedFlag = true;
            return true;
        }
        
        private static bool TryRemoveDefine(ref string[] defines, string defineSymbol, ref bool changedFlag)
        {
            if (ArrayUtility.Contains(defines, defineSymbol))
            {
                ArrayUtility.Remove(ref defines, defineSymbol);
                changedFlag = true;
                return true;
            }
            
            return false;
        }
        
        public static ModuleDefinition GetDefinition(Type definitionType)
        {
            if (_modules == null)
            {
                throw new Exception("Modules Definitions not yet initialized");
            }
        
            if (_modules.TryGetValue(definitionType, out var definition))
            {
                return definition;
            }

            return null;
        }
    }
}
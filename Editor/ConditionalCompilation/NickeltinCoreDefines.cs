using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Pool;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Manages Scripting Define Symbols fore nickeltin-core and its modules.
    /// </summary>
    internal static class NickeltinCoreDefines
    {
        /// <summary>
        /// If package installed from git (with package manager, meaning its readonly), this will be defined.
        /// </summary>
        public const string PACKAGE_READONLY = "NICKELTIN_READONLY";
        
        /// <summary>
        /// Project define whether core package installed.
        /// </summary>
        public const string PACKAGE_INSTALLED = "NICKELTIN_INSTALLED";

        public static readonly System.Reflection.Assembly CoreEditorAssembly = typeof(NickeltinCoreDefines).Assembly;

        public static string CoreEditorAssemblyName => CoreEditorAssembly.GetName().Name;

        public static readonly string CoreEditorAssemblyDefinitionPath =
            CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(CoreEditorAssemblyName);
    
        public const string Name = "com.nickeltin.core";
        
        public static bool IsReadonly { get; private set; }

        
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
                    Log($"{instance} linked with wrong definition type {instance.DefinitionType}. " +
                                   $"Type should be inherited from {nameof(ModuleDefinition)}", LogType.Error);
                    continue;
                }
                
                if (!impls.TryAdd(instance.DefinitionType, instance))
                {
                    Log($"Multiple {nameof(ModuleImplementation)} registered for the same " +
                                   $"{instance.DefinitionType} {nameof(ModuleDefinition)} type.", LogType.Error);
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
                    Log($"Multiple {nameof(ModuleDefinition)} has the same " +
                                   $"{nameof(ModuleDefinition.DEFINE_SYMBOL)} of \"{def.DEFINE_SYMBOL}\"", LogType.Error);
                }
            }
            
            DictionaryPool<Type, ModuleImplementation>.Release(impls);
            HashSetPool<string>.Release(defSymbolsSet);

            Events.registeringPackages += OnRegisteringPackages;
            
            UpdateDefineSymbols(false);
        }

        private static void OnRegisteringPackages(PackageRegistrationEventArgs obj)
        {
            foreach (var packageInfo in obj.removed)
            {
                if (packageInfo.name == Name)
                {
                    UpdateDefineSymbols(true);
                    return;
                }
            }
        }

        /// <summary>
        /// Will add defines for all modules as well as <see cref="PACKAGE_INSTALLED"/> and <see cref="PACKAGE_READONLY"/> defines.
        /// </summary>
        /// <param name="packageRemoved">If true update will be treated as core package were deletes, will remove default defines but keep modules defines</param>
        public static void UpdateDefineSymbols(bool packageRemoved)
        {
            if (!TryGetDefineSymbols(out var defines, out var buildTargetGroup))
            {
                Log($"Can't fetch build target group, current group: {buildTargetGroup}", LogType.Error);
                return;
            }

            var changed = false;

            foreach (var moduleDefinition in _modules.Values)
            {
                if (moduleDefinition.Defined)
                {
                    TryAddDefine(defines, moduleDefinition.DEFINE_SYMBOL, ref changed);
                }
                else
                {
                    TryRemoveDefine(defines, moduleDefinition.DEFINE_SYMBOL, ref changed);
                }
            }

            if (packageRemoved)
            {
                TryRemoveDefine(defines, PACKAGE_INSTALLED, ref changed);
            }
            else
            {
                TryAddDefine(defines, PACKAGE_INSTALLED, ref changed);
            }
            
            // If asmdef file is open for edit it means it installed as local package.
            var isOpenForEdit = AssetDatabase.IsOpenForEdit(CoreEditorAssemblyDefinitionPath);
            
            IsReadonly = !isOpenForEdit;

            if (IsReadonly && !packageRemoved)
            {
                TryAddDefine(defines, PACKAGE_READONLY, ref changed);
            }
            else
            {
                TryRemoveDefine(defines, PACKAGE_READONLY, ref changed);
            }

            if (changed)
            {
                Log("Defines changed, current:\n" + string.Join(",\n", defines));
                SetDefineSymbols(buildTargetGroup, defines.ToArray());
            }
        }

        private static void Log(object msg, LogType logType = LogType.Log)
        {
            Debug.LogFormat(logType, LogOption.NoStacktrace, null, "<b>[{0}]</b> {1}", Name, msg);
        }
        
        private static bool TryGetDefineSymbols(out HashSet<string> defines, out NamedBuildTarget target)
        {
            defines = new HashSet<string>();
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            target = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            if (buildTargetGroup == BuildTargetGroup.Unknown)
            {
                return false;
            }

            PlayerSettings.GetScriptingDefineSymbols(target, out var definesArray);
            defines = definesArray.ToHashSet();
            return true;
        }

        private static void SetDefineSymbols(NamedBuildTarget target, string[] defines)
        {
            PlayerSettings.SetScriptingDefineSymbols(target, defines);
        }
        
        private static bool TryAddDefine(HashSet<string> defines, string defineSymbol, ref bool changedFlag)
        {
            if (defines.Add(defineSymbol))
            {
                changedFlag = true;
                return true;
            }

            return false;
        }
        
        private static bool TryRemoveDefine(HashSet<string> defines, string defineSymbol, ref bool changedFlag)
        {
            if (defines.Remove(defineSymbol))
            {
                changedFlag = true;
                return true;
            }

            return false;
        }
        
        public static ModuleDefinition GetModuleDefinition(Type definitionType)
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
using System;
using System.Linq;
using nickeltin.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor.EmbeddedPackages
{
    internal class EmbeddedPackagesCreatorWindow : EditorWindow
    {
        private class Defaults
        {
            
        }

        
        private const string TITLE = "Embedded Packages Creator";


        private static Defaults _defaults;

        private EmbeddedPackage _currentPackage;
        
        /// <summary>
        /// Hidden if package installed from git
        /// </summary>
#if !NICKELTIN_READONLY
        [MenuItem(MenuPathsUtility.packageMenu + TITLE)]
#endif
        private static void ShowWindow()
        {
            var window = GetWindow<EmbeddedPackagesCreatorWindow>(true, TITLE,true);
            window.minSize = new Vector2(500, 150);
            window.maxSize = new Vector2(500, 150);
            window.Show();
        }
        
        private void OnEnable()
        {
            OnSelectionChange();
        }

        private void OnSelectionChange()
        {
            _currentPackage = Selection.GetFiltered<EmbeddedPackage>(SelectionMode.Assets).FirstOrDefault();
            Repaint();
        }

        private void OnGUI()
        {
            _defaults ??= new Defaults();

            _currentPackage = (EmbeddedPackage)EditorGUILayout.ObjectField("Selected package", _currentPackage, typeof(EmbeddedPackage), false);

            DrawButtons();
        }

        private void DrawButtons()
        {
            var packageIsValid = _currentPackage != null && AssetDatabase.IsOpenForEdit(_currentPackage);
            var packageHasConfig = packageIsValid && _currentPackage._nativeConfigData != null;

            BeginCategoryGroup("Base pacakge data");
            
            DrawButton("Create",  () => EmbeddedPackagesUtil.CreateEmbeddedPackage());
            
            using (new EditorGUI.DisabledScope(!packageIsValid))
            {
                DrawButton("Import", () =>
                    {
                        if (EditorUtility.DisplayDialog("Warning", "Pacakge will be imported additively, continue import?", "Yes", "No"))
                        {
                            EmbeddedPackagesUtil.ImportEmbeddedPackage(_currentPackage);
                        }

                    }, 
                    packageIsValid && _currentPackage._nativeData != null);
                
                DrawButton("Update", () => EmbeddedPackagesUtil.WriteDataToEmbeddedPackage(_currentPackage));
                
                EndCategoryGroup();
                BeginCategoryGroup("Icons");

                DrawButton("Add icon", () => EmbeddedPackagesUtil.AddEmbeddedPackagedIconWithDialog(_currentPackage));
                
                DrawButton("Remove icon", () =>
                    {
                        if (EditorUtility.DisplayDialog("Warning", "This action can't be undone, remove icon?", "Yes", "No"))
                        {
                            EmbeddedPackagesUtil.RemoveEmbeddedPackageIcon(_currentPackage);
                        }
                    }, 
                    packageIsValid && _currentPackage != null);
                
                EndCategoryGroup();
                
                BeginCategoryGroup("Config");

                DrawButton("Create", () =>
                {
                    EmbeddedPackagesUtil.WriteDataToEmbeddedPackage(_currentPackage, EmbeddedPackagesUtil.PackageDataChannel.ConfigData);
                });
                
                DrawButton("Import", () =>
                {
                    if (EditorUtility.DisplayDialog("Warning", "Import config?", "Yes", "No"))
                    {
                        EmbeddedPackagesUtil.ImportEmbeddedPackage(_currentPackage, EmbeddedPackagesUtil.PackageDataChannel.ConfigData);
                    }
                }, packageHasConfig);
                
                DrawButton("Delete", () =>
                {
                    if (EditorUtility.DisplayDialog("Warning", "Delete config?", "Yes", "No"))
                    {
                        EmbeddedPackagesUtil.DeleteConfigData(_currentPackage);
                    }
                }, packageHasConfig);

                EndCategoryGroup();
            }
            
        }

        private static void BeginCategoryGroup(string name)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawCategoryLabel(name);
            EditorGUILayout.BeginHorizontal();
        }

        private static void EndCategoryGroup()
        {
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        private static void DrawCategoryLabel(string text)
        {
            GUILayout.Label(text, EditorStyles.centeredGreyMiniLabel);
        }
        
        private static void DrawButton(string name, Action onClick, bool isActive = true)
        {
            using (new EditorGUI.DisabledScope(!isActive))
            {
                if (GUILayout.Button(name))
                {
                    onClick();
                }
            }
        }
    }
}
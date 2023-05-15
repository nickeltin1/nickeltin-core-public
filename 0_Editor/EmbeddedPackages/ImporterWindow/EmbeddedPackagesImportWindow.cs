using System.Diagnostics;
using System.Linq;
using nickeltin.Core.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace nickeltin.Core.Editor.EmbeddedPackages.ImportWindow
{
    internal class EmbeddedPackagesImportWindow : EditorWindow
    {
        public class Defaults
        {
            public readonly GUIContent upToDatePackageContent;
            public readonly GUIContent outdatedPackageContent;
            public readonly GUIContent notValidatedPackageContent;
            public readonly GUIContent deprecatedContent;
            
            public Defaults(Texture unvalidated, Texture outdated, Texture updated, Texture deprecated)
            {
                outdatedPackageContent = new GUIContent(outdated)
                {
                    tooltip = "Package has new content to import"
                };
                upToDatePackageContent = new GUIContent(updated)
                {
                    tooltip = "All package content imported"
                };

                notValidatedPackageContent = new GUIContent(unvalidated)
                {
                    tooltip = "Package state unknown, validate it"
                };
                
                deprecatedContent = new GUIContent(deprecated)
                {
                    tooltip = "Package is deprecated. This means its no longer supported and not recommended to use in new projects.\n" +
                              "It exist here only for backwards compatibility"
                };
            }
        }

        private const string TITLE = "Embedded Packages Importer";
        private const string AUTO_VALIDATE_KEY = nameof(EmbeddedPackagesImportWindow) + ".autoValidatePackages";
        private const string SORTER_ID_KEY = nameof(EmbeddedPackagesImportWindow) + ".sorterID";

        private static Defaults _defaults;
        
        public static Defaults defaults { get; private set; }
        

        [SerializeField] private VisualTreeAsset _mainDocument;
        [SerializeField] private Texture2D _unvalidatedPackage;
        [SerializeField] private Texture2D _packageHasContentToImport;
        [SerializeField] private Texture2D _packageUpdated;
        [SerializeField] private Texture2D _packageDeprecated;
        
        private PackagesSelection _selection;
        private bool _autoValidatePackages;
        private EmbeddedPackagesValidator.AsyncValidator _packagesValidator;
        private static EmbeddedPackagesImportWindow _instance;
        private Label _validationLabel;
        private IMGUIContainer _toolbarContainer;

        [MenuItem(MenuPathsUtility.packageMenu + TITLE)]
        private static void ShowWindow()
        {
            if (_instance != null)
            {
                _instance.Focus();
                return;
            }
            var window = GetWindow<EmbeddedPackagesImportWindow>(true, TITLE,true);
            window.minSize = new Vector2(400, 300);
            window.Show();
        }
        
        private void OnEnable()
        {
            _instance = this;
            
            _packagesValidator = new EmbeddedPackagesValidator.AsyncValidator
            {
                onAllPackagesValidation = OnAllPackagesValidation,
                onPackageValidation = OnPackageValidation,
                onValidationStateChanged = v => _validationLabel.visible = v
            };
            
            defaults ??= new Defaults(_unvalidatedPackage, _packageHasContentToImport, _packageUpdated, _packageDeprecated);
            _autoValidatePackages = EditorPrefs.GetBool(AUTO_VALIDATE_KEY, true);
            //EmbeddedPackagesUtil.onEmbeddedPackageImportEnded += OnEmbeddedPackageImportEnded;
        }
        
        private void OnDisable()
        {
            //EmbeddedPackagesUtil.onEmbeddedPackageImportEnded -= OnEmbeddedPackageImportEnded;
            EditorPrefs.SetBool(AUTO_VALIDATE_KEY, _autoValidatePackages);
            if (_selection.CurrentSorter != null)
            {
                EditorPrefs.SetInt(SORTER_ID_KEY, _selection.CurrentSorter.ID);
            }

            _instance = null;

            if (_packagesValidator.IsValidating)
            {
                _packagesValidator.Cancel();
            }
        }

        private void CreateGUI()
        {
            _mainDocument.CloneTree(rootVisualElement);
            _toolbarContainer = rootVisualElement.Q("toolbar").Q<IMGUIContainer>();
            _toolbarContainer.onGUIHandler = ToolbarGUI;

            var packages = rootVisualElement.Q("packages");

            _validationLabel = packages.Q<Label>("validation-label");
            _validationLabel.visible = false;

            var packagesList = packages.Q<ListView>();
            var searchField = packages.Q<ToolbarSearchField>();

            var sorterId = EditorPrefs.GetInt(SORTER_ID_KEY, 0);
            _selection = new PackagesSelection(packagesList, searchField, PackageSorter.GetSorterWithID(sorterId));
            
            if (_autoValidatePackages)
            {
                ValidateAllPackages(false);
            }

            var packageContainer = rootVisualElement.Q("package-inspector").Q<IMGUIContainer>();
            packageContainer.onGUIHandler = DrawSelectedPackage;

        }

        private void ToolbarGUI()
        {
            var button = EditorStyles.toolbarButton;
            var dropDown = EditorStyles.toolbarDropDown;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Validate all", button))
                {
                    ValidateAllPackages(false);
                }

                var newValue = GUILayout.Toggle(_autoValidatePackages, "Validate on open", button);
                if (newValue != _autoValidatePackages)
                {
                    _autoValidatePackages = newValue;
                    if (newValue)
                    {
                        ValidateAllPackages(false);
                    }
                }

                var rect = EditorGUILayout.BeginVertical();
                if (GUILayout.Button("Sort: " + _selection.CurrentSorter.Name, dropDown))
                {
                    var sorters = PackageSorter.GetAllSorters();
                    var menu = new GenericDropdownMenu();
                    foreach (var sorter in sorters)
                    {
                        menu.AddItem(sorter.Name, sorter == _selection.CurrentSorter, () =>
                        {
                            _selection.CurrentSorter = sorter;
                        });
                    }
                    menu.DropDown(rect, _toolbarContainer, true);
                }
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawSelectedPackage()
        {
            var selectedContainer = _selection.SelectedContainer;
            if (selectedContainer == null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Select package");
                    GUILayout.FlexibleSpace();
                }
            }
            else
            {
                DrawPackage(_selection.SelectedContainer);
            }
        }
        
        
        private static GUIStyle _title;
        
        private static void DrawPackageHeader(PackageContainer container)
        {
            _title ??= new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                fontSize = 20, richText = true
            };
            
            const int height = 30;
            var package = container.package;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (package._icon != null)
                {
                    GUI.DrawTexture(GUILayoutUtility.GetRect(height, height), package._icon, ScaleMode.ScaleToFit);
                }
                
                GUILayout.Label(package._displayName.Bold(), _title, GUILayout.Height(height));
                GUILayout.Label("by " + package._author, EditorStyles.label, GUILayout.Height(height));
                
                if (package._deprecated)
                {
                    var h = 20;
                    GUILayout.Label(defaults.deprecatedContent, GUILayout.Height(height), GUILayout.Width(h * 4));
                }
                
                GUILayout.FlexibleSpace();
            }
        }
        
        private void DrawPackage(PackageContainer container)
        {
            var package = container.package;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                DrawPackageHeader(container);
                
                if (GUILayout.Button("Size: " + container.sizeInBytes.FormatAsSize(), EditorStyles.largeLabel))
                {
                    Selection.activeObject = container.package;
                    EditorGUIUtility.PingObject(container.package);
                }
            
                GUILayout.Space(4);
            

                void DrawInstallLink(string prefix, EmbeddedPackagesUtil.PackageDataChannel dataChannel, bool active)
                {
                    const int height = 20;
                    var path = EmbeddedPackagesUtil.GetPackageDefaultInstallPath(package, dataChannel);
                    GUILayout.Label(prefix, EditorStyles.miniLabel, GUILayout.Height(height));
                    using (new EditorGUI.DisabledScope(!active))
                    {
                        if (EditorGUILayout.LinkButton(path, GUILayout.Height(height)))
                        {
                            if (EmbeddedPackagesUtil.TryGetPackgeDefaultInstallFolder(package, out var folder, true, dataChannel))
                            {
                                Selection.activeObject = folder;
                                EditorGUIUtility.PingObject(folder);
                            }
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
            
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawInstallLink("Default path: ", EmbeddedPackagesUtil.PackageDataChannel.MainData, container.defaultInstallFolderFound);
                }
            
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (package._nativeConfigData != null)
                    {
                        DrawInstallLink("Default config path: ", EmbeddedPackagesUtil.PackageDataChannel.ConfigData, container.defaultConfigInstallFolderFound);
                    }
                }
            }

            DrawPackageDescription(container);
            
            DrawPackageFooter(container);

            //EditorGUILayout.EndVertical();
        }

        private static void DrawPackageDescription(PackageContainer packageContainer)
        {
            var desc = packageContainer.package._description;
            if (string.IsNullOrEmpty(desc)) return;
            GUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("Description:", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Label(desc, EditorStyles.wordWrappedLabel);
                GUILayout.Space(4);
            }
        }

        private EmbeddedPackage _delayedPackage;
        
        private void DrawPackageFooter(PackageContainer container)
        {
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
           
            if (GUILayout.Button("Import/Update"))
            {
                var isAdditiveInstall =
                    EmbeddedPackagesUtil.TryGetPackgeDefaultInstallFolder(container.package, out _, false);
                
                var message =
                    $"{container.package.name} will be installed additively, this means that if there are deleted files in package update, " +
                    $"or some extra files that already in the folder, they will not be deleted. However already existing files can be modified, as well as newly added by update.\n" +
                    $"Are you sure you want to install package additively? " +
                    $"Maybe you want to Re-Install package?";

                if (isAdditiveInstall)
                {
                    if (EditorUtility.DisplayDialog("Package additive installation", message, "Yes, install additively", "Cancel"))
                    {
                        EmbeddedPackagesUtil.ImportEmbeddedPackage(container.package);
                    }
                }
                else
                {
                    if (container.package._nativeConfigData != null)
                    {
                        _delayedPackage = container.package;
                        EmbeddedPackagesUtil.onEmbeddedPackageImportEnded += DelayedPackageInstall;
                        if (!EmbeddedPackagesUtil.ImportEmbeddedPackage(container.package, EmbeddedPackagesUtil.PackageDataChannel.ConfigData))
                        {
                            DelayedPackageInstall();
                        }
                    }
                    else
                    {
                        EmbeddedPackagesUtil.ImportEmbeddedPackage(container.package);
                    }
                }
            }
            
            if (GUILayout.Button("", EditorStyles.popup, GUILayout.Width(20)))
            {
                ShowPackageAdditionalMenu(container);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DelayedPackageInstall()
        {
            EmbeddedPackagesUtil.onEmbeddedPackageImportEnded -= DelayedPackageInstall;
            EmbeddedPackagesUtil.ImportEmbeddedPackage(_delayedPackage); 
        }
        
        private void ShowPackageAdditionalMenu(PackageContainer container)
        {
            var menu = new GenericMenu();
                    
            menu.AddItem(new GUIContent("Validate content"), false, () => ValidateCallback(container));

            var deleteContent = new GUIContent("Delete");
            var reInstallContent = new GUIContent("Re-Install");
            if (EmbeddedPackagesUtil.TryGetPackgeDefaultInstallFolder(container.package, out _, false))
            {
                menu.AddItem(deleteContent, false, () => DeletePackage(container, EmbeddedPackagesUtil.PackageDataChannel.MainData));
                menu.AddItem(reInstallContent, false, () => ReInstallPackage(container, EmbeddedPackagesUtil.PackageDataChannel.MainData));
            }
            else
            {
                menu.AddDisabledItem(deleteContent);
                menu.AddDisabledItem(reInstallContent);
            }


            var importContent = new GUIContent("Import config");

            if (container.package._nativeConfigData != null)
            {
                menu.AddItem(importContent, false, () =>
                {
                    EmbeddedPackagesUtil.ImportEmbeddedPackage(container.package, EmbeddedPackagesUtil.PackageDataChannel.ConfigData);
                });
            }
            else
            {
                menu.AddDisabledItem(importContent);
            }

            deleteContent = new GUIContent("Delete Config");
            reInstallContent = new GUIContent("Re-Install Config");
            if (EmbeddedPackagesUtil.TryGetPackgeDefaultInstallFolder(container.package, out _, false, 
                    EmbeddedPackagesUtil.PackageDataChannel.ConfigData))
            {
                menu.AddItem(deleteContent, false, () => DeletePackage(container, EmbeddedPackagesUtil.PackageDataChannel.ConfigData));
                menu.AddItem(reInstallContent, false, () => ReInstallPackage(container, EmbeddedPackagesUtil.PackageDataChannel.ConfigData));
            }
            else
            {
                menu.AddDisabledItem(deleteContent);
                menu.AddDisabledItem(reInstallContent);
            }
            
            menu.ShowAsContext();
        }
        
        private void ValidateCallback(PackageContainer container)
        {
            container.validated = true;
            container.hasSomethigToImport = EmbeddedPackagesUtil.ValidateEmbeddedPackageUpdates(container.package);
            
            Repaint();
        }
        
        private static void DeletePackage(PackageContainer container, EmbeddedPackagesUtil.PackageDataChannel dataChannel)
        {
            if (EmbeddedPackagesUtil.TryGetPackgeDefaultInstallFolder(container.package, out _, true, dataChannel))
            {
                var path = EmbeddedPackagesUtil.GetPackageDefaultInstallPath(container.package, dataChannel);
                if (EditorUtility.DisplayDialog("WARNING",
                        $"{path} will be deleted. Are you sure?", 
                        "Yes, delete it", "Cancel"))
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
            }
        }
        
        private static void ReInstallPackage(PackageContainer container, EmbeddedPackagesUtil.PackageDataChannel dataChannel)
        {
            if (EmbeddedPackagesUtil.TryGetPackgeDefaultInstallFolder(container.package, out _, true, dataChannel))
            {
                var path = EmbeddedPackagesUtil.GetPackageDefaultInstallPath(container.package, dataChannel);
                if (EditorUtility.DisplayDialog("WARNING",
                        $"{path} will be deleted and re-installed. Are you sure?",
                        "Yes, re-install it", "Cancel"))
                {
                    AssetDatabase.DeleteAsset(path);
                    EmbeddedPackagesUtil.ImportEmbeddedPackage(container.package, dataChannel);
                }
            }
        }
        
        
        private void OnPackageValidation(EmbeddedPackagesValidator.ValidationData validationdata)
        {
            _selection.TryGetContainerForPackage(validationdata.package, out var container);
            container.validated = true;
            container.hasSomethigToImport = validationdata.hasSomethigToImport;
            if (_selection.CurrentSorter.GetType() == typeof(StateSorter))
            {
                _selection.UpdateSorting();
            }
            Repaint();
        }

        private void OnAllPackagesValidation(EmbeddedPackagesValidator.ValidationData[] validationdatas)
        {
            _stopwatch.Stop();
#if !NICKELTIN_READONLY
            //EmbeddedPackagesValidator.Log("Validation ended, time: " + _stopwatch.Elapsed.TotalMilliseconds);
#endif
        }


        private Stopwatch _stopwatch;
        
        private void ValidateAllPackages(bool skipAlreadyValidatedPackages)
        {
            if (_packagesValidator.IsValidating) return;
            
            var pacakgesToValidate = skipAlreadyValidatedPackages 
                ? _selection.GetUnvalidatedPackages() 
                : _selection.GetAllPackages();
            
                _stopwatch = Stopwatch.StartNew();
#if !NICKELTIN_READONLY
                //EmbeddedPackagesValidator.Log("Starting async validation");
#endif
            if (!_packagesValidator.Validate(pacakgesToValidate.ToArray()))
            {
                _stopwatch.Stop();
            }
        }
    }
}
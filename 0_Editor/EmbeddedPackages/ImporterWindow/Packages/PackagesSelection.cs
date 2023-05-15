using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace nickeltin.Core.Editor.EmbeddedPackages.ImportWindow
{
    internal class PackagesSelection : IEnumerable<PackageContainer>
    {
        private readonly List<PackageContainer> _packagesList;
        private readonly Dictionary<EmbeddedPackage, PackageContainer> _packagesDict;

        private readonly ListView _listView;
        private readonly List<PackageContainer> _packagesToShow;

        private PackageSorter _currentSorter;

        public PackagesSelection(ListView listView, ToolbarSearchField searchField, PackageSorter sorter)
        {
            _currentSorter = sorter;
            
            _listView = listView;
            
            _packagesList = new List<PackageContainer>();
            _packagesToShow = new List<PackageContainer>();
            _packagesDict = new Dictionary<EmbeddedPackage, PackageContainer>();
            Fill();
            
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            listView.itemsSource = _packagesToShow;
            listView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            listView.makeItem = () => new PackagePreview();
            listView.bindItem = (element, i) =>
            {
                ((PackagePreview)element).Bind(_packagesToShow[i]);
            };
            searchField.RegisterValueChangedCallback(evt => UpdateSearch(evt.newValue));

            var scrollView = listView.Q<ScrollView>();
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            UpdateSearch("");
        }

        
        public int Length => _packagesList.Count;
        

        public PackageContainer SelectedContainer
        {
            get
            {
                if (_listView.selectedIndex != -1)
                {
                    return _packagesToShow[_listView.selectedIndex];
                }

                return null;
            }
        }

        public PackageSorter CurrentSorter
        {
            get => _currentSorter;
            set
            {
                if (_currentSorter != value)
                {
                    _currentSorter = value;
                    UpdateSorting();
                }
            }
        }

        public void UpdateSorting()
        {
            _currentSorter.Sort(_packagesToShow);
            _listView.RefreshItems();
        }

        private void UpdateSearch(string filter)
        {
            bool Match(string s) => s.Contains(filter, StringComparison.OrdinalIgnoreCase);

            _packagesToShow.Clear();
            
            if (string.IsNullOrEmpty(filter))
            {
                _packagesToShow.AddRange(_packagesList);
            }
            else
            {
                foreach (var packageContainer in _packagesList)
                {
                    foreach (var field in GetPackageSearchFields(packageContainer.package))
                    {
                        if (Match(field))
                        {
                            _packagesToShow.Add(packageContainer);
                            break;
                        }   
                    }
                }
                
            }
            
            UpdateSorting();
        }

        private static IEnumerable<string> GetPackageSearchFields(EmbeddedPackage package)
        {
            yield return package._author;
            yield return package.name;
            yield return package._displayName;
        }

        public bool TryGetContainerForPackage(EmbeddedPackage package, out PackageContainer container)
        {
            return _packagesDict.TryGetValue(package, out container);
        }
        
        public IEnumerable<EmbeddedPackage> GetAllPackages() => _packagesDict.Keys;

        public IEnumerable<EmbeddedPackage> GetUnvalidatedPackages()
        {
            foreach (var packageContainer in _packagesList)
            {
                if (!packageContainer.validated)
                {
                    yield return packageContainer.package;
                }
            }
        }

        private void Fill()
        {
            var packages = EmbeddedPackage.FindAll();
            foreach (var package in packages)
            {
                var container = new PackageContainer(package);
                
                container.defaultInstallFolderFound =
                    EmbeddedPackagesUtil.TryGetPackgeDefaultInstallFolder(container.package, out _, false);
                
                container.defaultConfigInstallFolderFound =
                    EmbeddedPackagesUtil.TryGetPackgeDefaultInstallFolder(container.package, out _, false, 
                        EmbeddedPackagesUtil.PackageDataChannel.ConfigData);

                container.sizeInBytes = EmbeddedPackagesUtil.GetEmbeddedPackageSize(package);

                _packagesList.Add(container);
                _packagesDict.TryAdd(package, container);
            }
        }

        public IEnumerator<PackageContainer> GetEnumerator() => _packagesList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public PackageContainer this[int index] => _packagesList[index];
    }
}
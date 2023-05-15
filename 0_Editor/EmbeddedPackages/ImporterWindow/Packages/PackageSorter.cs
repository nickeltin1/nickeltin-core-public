using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor.EmbeddedPackages.ImportWindow
{
    internal abstract class PackageSorter : IComparer<PackageContainer>
    {
        private static List<PackageSorter> _sorters;

        [InitializeOnLoadMethod]
        private static void CreateSorters()
        {
            _sorters = new List<PackageSorter>();
            var types = TypeCache.GetTypesDerivedFrom<PackageSorter>();

            PackageSorter CreateSorter(Type type)
            {
                var instance = (PackageSorter)Activator.CreateInstance(type);
                instance.ID = _sorters.Count;
                _sorters.Add(instance);
                return instance;
            }
            
            foreach (var type in types)
            {
                if (!type.IsAbstract)
                {
                    var instance = CreateSorter(type);

                    if (instance.hasInvertedVersion)
                    {
                        var invertedInstance = CreateSorter(type);
                        invertedInstance.isInverted = true;
                    }
                }
            }
        }

        public static IEnumerable<PackageSorter> GetAllSorters() => _sorters;

        public static PackageSorter GetSorterWithID(int id)
        {
            if (id >= 0 && id < _sorters.Count)
            {
                return _sorters[id];
            }

            Debug.LogError("No sorter with id " + id);
            return null;
        }

        public int ID { get; private set; }
        
        public string Name
        {
            get
            {
                var n = name;
                if (hasInvertedVersion)
                {
                    n += isInverted ? " ↓" : " ↑";
                }
                return n;
            }
        }

        protected abstract string name { get; }

        protected abstract bool hasInvertedVersion { get; }

        private bool isInverted;

        public int Compare(PackageContainer x, PackageContainer y)
        {
            var result = ComparePackages(x, y);
            return isInverted ? -result : result;
        }

        protected abstract int ComparePackages(PackageContainer x, PackageContainer y);
        
        
        public void Sort(List<PackageContainer> packages) => packages.Sort(this);
    }

    internal class StateSorter : PackageSorter
    {
        protected override string name => "State";

        protected override bool hasInvertedVersion => true;

        protected override int ComparePackages(PackageContainer x, PackageContainer y)
        {
            int CalculateScore(PackageContainer p)
            {
                var score = 0;
                if (p.validated)
                {
                    score++;
                    if (!p.hasSomethigToImport)
                    {
                        score++;
                    }
                }

                return score;
            }

            var scoreX = CalculateScore(x);
            var scoreY = CalculateScore(y);
            
            return scoreY - scoreX;
        }
    }
    
    internal class NameSorter : PackageSorter
    {
        protected override string name => "Name";
        protected override bool hasInvertedVersion => true;
        protected override int ComparePackages(PackageContainer x, PackageContainer y)
        {
            return string.Compare(x.package._displayName, y.package._displayName, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal class SizeSorter : PackageSorter
    {
        protected override string name => "Size";
        protected override bool hasInvertedVersion => true;
        protected override int ComparePackages(PackageContainer x, PackageContainer y)
        {
            if (x.sizeInBytes > y.sizeInBytes) return -1;
            if (y.sizeInBytes > x.sizeInBytes) return 1;
            return 0;
        }
    }

    internal class DeprecatedSorter : PackageSorter
    {
        protected override string name => "Deprecated";
        protected override bool hasInvertedVersion => false;
        protected override int ComparePackages(PackageContainer x, PackageContainer y)
        {
            if (x.package._deprecated && !y.package._deprecated) return -1;
            if (y.package._deprecated && !x.package._deprecated) return 1;
            return 0;
        }
    }
}
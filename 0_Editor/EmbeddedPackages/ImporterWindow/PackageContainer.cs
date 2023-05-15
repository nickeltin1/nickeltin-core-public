using System;

namespace nickeltin.Core.Editor.EmbeddedPackages.ImportWindow
{
    internal class PackageContainer
    {
        private bool _validated;
        private bool _hasSomethigToImport;
        
        
        public readonly EmbeddedPackage package;
        public bool defaultInstallFolderFound;
        public bool defaultConfigInstallFolderFound;
        public long sizeInBytes;

        
        public bool hasSomethigToImport
        {
            get => _hasSomethigToImport;
            set
            {
                if (_hasSomethigToImport != value)
                {
                    _hasSomethigToImport = value;
                    stateChanged?.Invoke();
                }
            }
        }
        public bool validated
        {
            get => _validated;
            set
            {
                if (_validated != value)
                {
                    _validated = value;
                    stateChanged?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Invoked whenever <see cref="validated"/> or <see cref="hasSomethigToImport"/> variables changed.
        /// </summary>
        public Action stateChanged;

        
        public PackageContainer(EmbeddedPackage package)
        {
            this.package = package;
            validated = false;
            _hasSomethigToImport = false;
            sizeInBytes = -1;
            defaultInstallFolderFound = false;
            defaultConfigInstallFolderFound = false;
        }
    }
}
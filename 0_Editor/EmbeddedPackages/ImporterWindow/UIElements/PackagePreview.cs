using UnityEngine.UIElements;

namespace nickeltin.Core.Editor.EmbeddedPackages.ImportWindow
{
    internal class PackagePreview : VisualElement
    {
        private PackageContainer _packageContainer;

        private readonly Image _icon;
        private readonly Label _name;
        private readonly Label _author;
        private readonly Image _state;

        private readonly VisualElement _leftContainer;
        
        public PackagePreview()
        {
            _icon = new Image() { name = "icon" };
            _name = new Label("NAME") { name = "name" };
            _author = new Label("author") { name = "author" };
            _leftContainer = new VisualElement() { name = "left-container" };
            
            
            Add(_leftContainer);
            
            _leftContainer.Add(_icon);
            _leftContainer.Add(_name);
            _leftContainer.Add(_author);

            _state = new Image() { name = "state" };
            Add(_state);
        }

        public void Bind(PackageContainer container)
        {
            if (_packageContainer != null)
            {
                _packageContainer.stateChanged -= UpdateState;
            }
            
            _packageContainer = container;
            _packageContainer.stateChanged += UpdateState;
            
            UpdateState();
        }
        
        
        private void UpdateState()
        {
            var p = _packageContainer.package;
            _icon.image = p._icon;

            _name.text = p._displayName;

            _author.text = "by " + p._author;
            
            var defaults = EmbeddedPackagesImportWindow.defaults;
            
            var content = defaults.notValidatedPackageContent;
            if (_packageContainer.validated)
            {
                content = _packageContainer.hasSomethigToImport
                    ? defaults.outdatedPackageContent
                    : defaults.upToDatePackageContent;
            }

            _state.image = content.image;
            _state.tooltip = content.tooltip;

            const string DEPRECATED = "deprecated";
            if (p._deprecated) _leftContainer.AddToClassList(DEPRECATED);
            else _leftContainer.RemoveFromClassList(DEPRECATED);
        }
    }
}
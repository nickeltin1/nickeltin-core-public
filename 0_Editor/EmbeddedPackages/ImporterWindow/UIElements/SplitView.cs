using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace nickeltin.Core.Editor.EmbeddedPackages.ImportWindow
{
    [MovedFrom(true)]
    public class SplitView : TwoPaneSplitView
    {
        public new class UxmlFactory : UxmlFactory<SplitView, UxmlTraits> { }
    }
}
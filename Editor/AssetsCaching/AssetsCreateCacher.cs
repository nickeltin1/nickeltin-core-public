using UnityEditor;

namespace nickeltin.Core.Editor
{
    internal class AssetsCreateCacher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, 
            string[] movedFromAssetPaths)
        {
           AssetsCacher.OnAssetsCreated(importedAssets);
        }
    }
}
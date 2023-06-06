using System;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Inherit from this interface to recive callbacks when asset with specified type is created or deleted.
    /// Callbacks also will be recieved for inherited types.
    /// </summary>
    public interface IAssetCacher
    {
        Type TargetedType { get; }

        void OnAssetDeletion(Object deletedAsset);

        void OnAssetCreation(Object createdAsset);
    }
}
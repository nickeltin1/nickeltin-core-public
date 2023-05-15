using System;

namespace nickeltin.SOCreateWindow.Runtime
{
    public abstract class CreateAssetWindowAttributeBase : Attribute
    {
        /// <summary>
        /// The display name for this type shown in the search window
        /// <example>
        ///     "Game/Items" <see cref="FileName"/> will be appedned afterwards.
        /// </example>
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The default file name used by newly created instances of this type and how its displayed in search window.
        /// If null of empty Type.Name will be used instead.
        /// <example>
        ///     "Weapon"  
        ///     Then path in search window will look like "Game/Items/Weapon"
        /// </example>
        /// </summary>
        public string FileName { get; set; }
    }
}
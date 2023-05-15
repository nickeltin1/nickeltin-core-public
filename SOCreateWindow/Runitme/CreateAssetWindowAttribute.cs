using System;

namespace nickeltin.SOCreateWindow.Runtime
{
    /// <summary>
    /// Mark a ScriptableObject-derived type to be automatically listed in the Assets/Create/Scriptable Objects search window, like when creating mono behaviour,
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CreateAssetWindowAttribute : CreateAssetWindowAttributeBase
    {
        public CreateAssetWindowAttribute()
        {
        }

        public CreateAssetWindowAttribute(string path)
        {
            Path = path;
        }

        public CreateAssetWindowAttribute(string path, string fileName) : this(path)
        {
            FileName = fileName;
        }
    }
}
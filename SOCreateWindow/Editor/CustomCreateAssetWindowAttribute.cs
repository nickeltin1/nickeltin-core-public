using System;
using nickeltin.SOCreateWindow.Runtime;

namespace nickeltin.SOCreateWindow.Editor
{
    /// <summary>
    /// Mark static function with <see cref="AssetCreateHandler"/> delegate signature.
    /// This function will be invoked
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CustomCreateAssetWindowAttribute : CreateAssetWindowAttributeBase
    {
        public delegate void AssetCreateHandler(string atPath);
        
        private Type _producedType;

        /// <summary>
        /// Optional, if not defined (or defined with wrong type) than this entry will not have icon in create window.
        /// Scriptable object type that will be produced as result of this function, will be used for icon.
        /// </summary>
        public Type ProducedType
        {
            get => _producedType;
            set
            {
                if (value != null && !ScriptableObjectCreateWindow.IsTypeValid(value))
                {
                    throw new Exception(
                        "Only non abstract non generic types that inherits from ScriptableObject is supported.");
                }
                
                _producedType = value;
            }
        }

        public CustomCreateAssetWindowAttribute()
        {
        }

        public CustomCreateAssetWindowAttribute(Type producedType) : this()
        {
            ProducedType = producedType;
        }

        public CustomCreateAssetWindowAttribute(string path) : this()
        {
            Path = path;
        }

        public CustomCreateAssetWindowAttribute(string path, string fileName) : this(path)
        {
            FileName = fileName;
        }
    }
}
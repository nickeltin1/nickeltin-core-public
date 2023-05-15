using System;

namespace nickeltin.OdinSupport.Runtime
{
    /// <summary>
    /// Valid on class derived from <see cref="UnityEngine.Object"/>
    /// On recompile will change setting for Odin drawn types, if odin is presented in project
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExcludeEditorFromOdinAttribute : Attribute
    {
        public bool Exclude { get; }
        
        public ExcludeEditorFromOdinAttribute(bool exclude = true)
        {
            Exclude = exclude;
        }

    }
}
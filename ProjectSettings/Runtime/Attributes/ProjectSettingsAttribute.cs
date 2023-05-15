using System;

namespace nickeltin.ProjectSettings.Runtime
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ProjectSettingsAttribute : Attribute
    {
        public readonly string name;
        
        public bool HideInEditor { get; set; }

        public ProjectSettingsAttribute(string name)
        {
            this.name = name;
        }
    }
}
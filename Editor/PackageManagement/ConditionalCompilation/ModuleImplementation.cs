using System;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Used to register nickeltin module, if implementation exist in project appropriate module define will be added to project.
    /// </summary>
    public abstract class ModuleImplementation
    {
        /// <summary>
        /// Should return type that inherits from <see cref="ModuleDefinition"/>
        /// </summary>
        public abstract Type DefinitionType { get; }
    }
}
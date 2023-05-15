using System;

namespace nickeltin.Core.Editor
{
    public abstract class ModuleImplementation
    {
        /// <summary>
        /// Should return type that inherits from <see cref="ModuleDefinition"/>
        /// </summary>
        public abstract Type DefinitionType { get; }
    }
}
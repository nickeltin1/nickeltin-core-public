using System;

namespace nickeltin.Core.Editor
{
    internal class CoreModuleImplementation : ModuleImplementation
    {
        public override Type DefinitionType => typeof(CoreModuleDefinition);
    }
}
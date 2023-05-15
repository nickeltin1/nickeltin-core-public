namespace nickeltin.Core.Editor
{
    internal class CoreModuleDefinition : ModuleDefinition
    {
        public CoreModuleDefinition(ModuleImplementation implementation) : base(implementation) { }

        public override string DEFINE_SYMBOL => "NICKELTIN_INSTALLED";
    }
}
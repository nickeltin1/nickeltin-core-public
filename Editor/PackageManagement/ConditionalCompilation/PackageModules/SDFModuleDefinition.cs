namespace nickeltin.Core.Editor
{
    public class SDFModuleDefinition : ModuleDefinition
    {
        public SDFModuleDefinition(ModuleImplementation implementation) : base(implementation)
        {
        }

        public override string DEFINE_SYMBOL => "NICKELTIN_SDF";
    }
}
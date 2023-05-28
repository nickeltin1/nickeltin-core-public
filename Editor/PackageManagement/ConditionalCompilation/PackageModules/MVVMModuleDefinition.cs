namespace nickeltin.Core.Editor
{
    public class MVVMModuleDefinition : ModuleDefinition
    {
        public MVVMModuleDefinition(ModuleImplementation implementation) : base(implementation)
        {
        }

        public override string DEFINE_SYMBOL => "NICKELTIN_MVVM";
    }
}
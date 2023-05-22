namespace nickeltin.Core.Editor
{
    public class TweeningModuleDefinition : ModuleDefinition
    {
        public TweeningModuleDefinition(ModuleImplementation implementation) : base(implementation)
        {
        }

        public override string DEFINE_SYMBOL => "NICKELTIN_TWEENING";
    }
}
namespace nickeltin.Core.Editor
{
    public class SceneManagementModuleDefinition : ModuleDefinition
    {
        public SceneManagementModuleDefinition(ModuleImplementation implementation) : base(implementation)
        {
        }

        public override string DEFINE_SYMBOL => "NICKELTIN_SCENE_MANAGEMENT";
    }
}
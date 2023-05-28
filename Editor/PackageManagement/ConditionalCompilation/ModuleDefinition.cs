namespace nickeltin.Core.Editor
{
    /// <summary>
    /// TODO:
    /// </summary>
    public abstract class ModuleDefinition
    {
        public abstract string DEFINE_SYMBOL { get; }

        internal readonly ModuleImplementation Implementation;

        public bool Defined => Implementation != null;

        internal ModuleDefinition(ModuleImplementation implementation)
        {
            Implementation = implementation;
        }
    }
}
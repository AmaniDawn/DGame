using DGame;

namespace Procedure
{
    public abstract class ProcedureBase : DGame.ProcedureBase
    {
        public abstract bool UseNativeDialog { get; }

        protected readonly IResourceModule m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
    }
}
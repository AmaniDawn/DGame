using DGame;

namespace Procedure
{
    public abstract class ProcedureBase : DGame.ProcedureBase
    {
        public abstract bool UseNativeDialog { get; }

        protected readonly IResourceModule m_resourceModule = ModuleSystem.GetModule<IResourceModule>();

        protected void SetRemoteServerURL(string resDownloadPath, string fallbackResDownloadPath)
            => m_resourceModule?.SetRemoteServerURL(resDownloadPath, fallbackResDownloadPath);
        
        protected void SetPlayMode(YooAsset.EPlayMode playMode) => m_resourceModule.PlayMode = playMode;
        
        protected void SetDefaultPackageName(string packageName) => m_resourceModule.DefaultPackageName = packageName;
        
    }
}
namespace DGame
{
    public interface ISetAssetObject : IMemory
    {
        /// <summary>
        /// 资源地址
        /// </summary>
        string Location { get; }

        /// <summary>
        /// Unity资源对象
        /// </summary>
        public UnityEngine.Object TargetObject { get; set; }

        /// <summary>
        /// 设置资源
        /// </summary>
        /// <param name="asset"></param>
        void SetAsset(UnityEngine.Object asset);

        /// <summary>
        /// 是否可以回收释放资源
        /// </summary>
        /// <returns></returns>
        bool IsCanRelease();
    }
}
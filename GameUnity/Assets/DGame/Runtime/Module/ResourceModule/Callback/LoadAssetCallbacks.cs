namespace DGame
{
    public sealed class LoadAssetCallbacks
    {
        private readonly LoadAssetSuccessCallback m_loadAssetSuccessCallback;
        public LoadAssetSuccessCallback LoadAssetSuccessCallback => m_loadAssetSuccessCallback;
        private readonly LoadAssetFailureCallback m_loadAssetFailureCallback;
        public LoadAssetFailureCallback LoadAssetFailureCallback => m_loadAssetFailureCallback;
        private readonly LoadAssetUpdateCallback m_loadAssetUpdateCallback;
        public LoadAssetUpdateCallback LoadAssetUpdateCallback => m_loadAssetUpdateCallback;

        public LoadAssetCallbacks(LoadAssetSuccessCallback successCallback): this(successCallback, null, null)
        {
        }

        public LoadAssetCallbacks(LoadAssetSuccessCallback successCallback, LoadAssetFailureCallback failureCallback): this(successCallback, failureCallback, null)
        {
        }

        public LoadAssetCallbacks(LoadAssetSuccessCallback successCallback, LoadAssetUpdateCallback updateCallback): this(successCallback, null, updateCallback)
        {
        }

        /// <summary>
        /// 初始化加载资源回调函数集的新实例
        /// </summary>
        /// <param name="successCallback">加载资源成功回调函数</param>
        /// <param name="failureCallback">加载资源失败回调函数</param>
        /// <param name="updateCallback">加载资源更新回调函数</param>
        public LoadAssetCallbacks(LoadAssetSuccessCallback successCallback, LoadAssetFailureCallback failureCallback, LoadAssetUpdateCallback updateCallback)
        {
            if (successCallback == null)
            {
                throw new DGameException("加载资源成功回调函数不能为空");
            }
            m_loadAssetSuccessCallback = successCallback;
            m_loadAssetFailureCallback = failureCallback;
            m_loadAssetUpdateCallback = updateCallback;
        }
    }

    /// <summary>
    /// 加载资源成功回调函数
    /// </summary>
    /// <param name="assetName">要加载的资源名称</param>
    /// <param name="asset">已加载的资源</param>
    /// <param name="duration">加载持续时间</param>
    /// <param name="userData">用户自定义数据</param>
    public delegate void LoadAssetSuccessCallback(string assetName, object asset, float duration, object userData);

    /// <summary>
    /// 加载资源失败回调函数
    /// </summary>
    /// <param name="assetName">要加载的资源名称</param>
    /// <param name="status">加载资源状态</param>
    /// <param name="errorMsg">错误信息</param>
    /// <param name="userData">用户自定义数据</param>
    public delegate void LoadAssetFailureCallback(string assetName, LoadResourceStatus status, string errorMsg, object userData);

    /// <summary>
    /// 加载资源更新中回调函数
    /// </summary>
    /// <param name="assetName">要加载的资源名称</param>
    /// <param name="progress">加载资源进度</param>
    /// <param name="userData">用户自定义数据</param>
    public delegate void LoadAssetUpdateCallback(string assetName, float progress, object userData);
}
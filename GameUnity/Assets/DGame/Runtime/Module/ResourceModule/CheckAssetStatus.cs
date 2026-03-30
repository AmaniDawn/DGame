namespace DGame
{
    /// <summary>
    /// 检查资源是否存在的枚举
    /// </summary>
    public enum CheckAssetStatus : byte
    {
        /// <summary>
        /// 资源不存在
        /// </summary>
        NotExist = 0,

        /// <summary>
        /// 资源需要从远端更新下载
        /// </summary>
        AssetOnline = 1,

        /// <summary>
        /// 资源存储在磁盘上
        /// </summary>
        AssetOnDisk = 2,

        /// <summary>
        /// 资源存储在文件系统里
        /// </summary>
        AssetOnFileSystem = 3,

        /// <summary>
        /// 二进制资源存储在磁盘上
        /// </summary>
        BinaryOnDisk = 4,

        /// <summary>
        /// 二进制资源存储在文件系统里
        /// </summary>
        BinaryOnFileSystem = 5,

        /// <summary>
        /// 无效的资源地址
        /// </summary>
        Invalid = 6,
    }
}
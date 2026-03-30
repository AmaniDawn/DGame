using UnityEngine;

namespace DGame
{
    /// <summary>
    /// 资源模块的加密类型
    /// </summary>
    public enum EncryptionType : byte
    {
        /// <summary>
        /// 无加密
        /// </summary>
        [InspectorName("无加密")]
        None = 0,

        /// <summary>
        /// 文件偏移加密
        /// <remarks>通过在文件开头添加偏移量来隐藏真实文件内容的加密方式</remarks>
        /// </summary>
        [InspectorName("文件偏移加密")]
        FileOffset = 1,

        /// <summary>
        /// 文件流加密
        /// <remarks>使用加密流对文件内容进行加密处理的加密方式</remarks>
        /// </summary>
        [InspectorName("文件流加密")]
        FileStream = 2,
    }
}
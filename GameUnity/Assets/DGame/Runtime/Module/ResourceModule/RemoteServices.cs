using System.IO;
using UnityEngine;
using YooAsset;
using System;

namespace DGame
{
    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    public class RemoteServices : IRemoteServices
    {
        private readonly string m_defaultHostServer;
        private readonly string m_fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            m_defaultHostServer = defaultHostServer;
            m_fallbackHostServer = fallbackHostServer;
        }

        public string GetRemoteMainURL(string fileName)
        {
            return $"{m_defaultHostServer}/{fileName}";
        }

        public string GetRemoteFallbackURL(string fileName)
        {
            return $"{m_fallbackHostServer}/{fileName}";
        }
    }

    /// <summary>
    /// 文件流加密方式
    /// <remarks>使用加密流对文件内容进行加密处理的加密方式</remarks>
    /// </summary>
    public class FileStreamEncryption : IEncryptionServices
    {
        /// <summary>
        /// 偏移加密 异或加密
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public EncryptResult Encrypt(EncryptFileInfo fileInfo)
        {
            // 将整个文件读取为字节数组
            var fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
            // 对文件的每个字节与 BundleStream.KEY（一个常量密钥）进行异或操作
            for (int i = 0; i < fileData.Length; i++)
            {
                fileData[i] ^= BundleStream.KEY;
            }

            EncryptResult result = new EncryptResult();
            result.Encrypted = true; // 标记已加密
            result.EncryptedData = fileData; // 返回加密后的字节数据
            return result;
        }
    }

    /// <summary>
    /// 文件流解密方式
    /// <remarks>使用加密流对文件内容进行加密处理的加密方式</remarks>
    /// </summary>
    public class FileStreamDecryption : IDecryptionServices
    {
        /// <summary>
        /// 同步方式获取解密的资源包对象
        /// <remarks>注意：加载流对象在资源包对象释放的时候会自动释放</remarks>
        /// </summary>
        DecryptResult IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo)
        {
            BundleStream bundleStream =
                new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            DecryptResult decryptResult = new DecryptResult
            {
                // 将解密流对象保存到结果中，确保流在整个AssetBundle生命周期内保持打开状态
                // 注意：当AssetBundle被销毁或卸载时，Unity会自动关闭此流
                ManagedStream = bundleStream,
                Result = AssetBundle.LoadFromStream(bundleStream, 0, GetManagedReadBufferSize())
            };
            return decryptResult;
        }

        /// <summary>
        /// 异步方式获取解密的资源包对象
        /// 注意：加载流对象在资源包对象释放的时候会自动释放
        /// </summary>
        DecryptResult IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo)
        {
            BundleStream bundleStream =
                new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            DecryptResult decryptResult = new DecryptResult
            {
                ManagedStream = bundleStream,
                CreateRequest = AssetBundle.LoadFromStreamAsync(bundleStream, 0, GetManagedReadBufferSize())
            };
            return decryptResult;
        }

        /// <summary>
        /// 后备方式获取解密的资源包对象
        /// </summary>
        DecryptResult IDecryptionServices.LoadAssetBundleFallback(DecryptFileInfo fileInfo)
            => new DecryptResult();

        /// <summary>
        /// 获取解密的字节数据
        /// </summary>
        byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
            => throw new System.NotImplementedException();

        /// <summary>
        /// 获取解密的文本数据
        /// </summary>
        string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
            => throw new System.NotImplementedException();

        private static uint GetManagedReadBufferSize()
            => 1024;
    }

    /// <summary>
    /// 文件偏移加密
    /// <remarks>通过在文件开头添加偏移量来隐藏真实文件内容的加密方式</remarks>
    /// </summary>
    public class FileOffsetEncryption : IEncryptionServices
    {
        public EncryptResult Encrypt(EncryptFileInfo fileInfo)
        {
            // 定义偏移量，表示在原始数据前添加的空字节数
            // 这里固定为32字节，可以修改为动态值或从配置读取
            int offset = 32;
            // 读取原始文件的全部字节数据到内存中
            byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
            // 创建新的字节数组，大小为原始数据大小 + 偏移量
            // 新数组的前offset字节将保持默认值(0)，原始数据从offset位置开始存放
            var encryptData = new byte[fileData.Length + offset];
            // 将原始文件数据复制到加密数组的偏移位置
            Buffer.BlockCopy(fileData, 0, encryptData, offset, fileData.Length);
            var encryptResult = new EncryptResult
            {
                Encrypted = true,
                EncryptedData = encryptData
            };
            return encryptResult;
        }
    }

    /// <summary>
    /// 文件偏移解密
    /// <remarks>通过在文件开头添加偏移量来隐藏真实文件内容的加密方式</remarks>
    /// </summary>
    public class FileOffsetDecryption : IDecryptionServices
    {
        /// <summary>
        /// 同步方式获取解密的资源包对象
        /// 注意：加载流对象在资源包对象释放的时候会自动释放
        /// </summary>
        DecryptResult IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo)
        {
            DecryptResult decryptResult = new DecryptResult
            {
                // 由于直接使用LoadFromFile方法，不需要管理自定义的流对象
                // 因此ManagedStream设为null，Unity会管理自己的文件流
                ManagedStream = null,
                Result = AssetBundle.LoadFromFile(fileInfo.FileLoadPath, 0, GetFileOffset())
            };
            return decryptResult;
        }

        /// <summary>
        /// 异步方式获取解密的资源包对象
        /// 注意：加载流对象在资源包对象释放的时候会自动释放
        /// </summary>
        DecryptResult IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo)
        {
            DecryptResult decryptResult = new DecryptResult
            {
                ManagedStream = null,
                CreateRequest = AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, 0, GetFileOffset())
            };
            return decryptResult;
        }

        public DecryptResult LoadAssetBundleFallback(DecryptFileInfo fileInfo)
            => new DecryptResult();

        /// <summary>
        /// 获取解密的字节数据
        /// </summary>
        byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
            => throw new System.NotImplementedException();

        /// <summary>
        /// 获取解密的文本数据
        /// </summary>
        string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
            => throw new System.NotImplementedException();

        private static ulong GetFileOffset()
            => 32;
    }

    #region WebDecryptionServices

    /// <summary>
    /// 资源文件偏移加载解密类
    /// </summary>
    public class FileOffsetWebDecryption : IWebDecryptionServices
    {
        public WebDecryptResult LoadAssetBundle(WebDecryptFileInfo fileInfo)
        {
            int offset = GetFileOffset();
            byte[] decryptedData = new byte[fileInfo.FileData.Length - offset];
            Buffer.BlockCopy(fileInfo.FileData, offset, decryptedData, 0, decryptedData.Length);
            // 从内存中加载AssetBundle
            WebDecryptResult decryptResult = new WebDecryptResult
            {
                Result = AssetBundle.LoadFromMemory(decryptedData)
            };
            return decryptResult;
        }

        private static int GetFileOffset()
            => 32;
    }

    public class FileStreamWebDecryption : IWebDecryptionServices
    {
        public WebDecryptResult LoadAssetBundle(WebDecryptFileInfo fileInfo)
        {
            // 优化：使用Buffer批量操作替代逐字节异或
            byte[] decryptedData = new byte[fileInfo.FileData.Length];
            Buffer.BlockCopy(fileInfo.FileData, 0, decryptedData, 0, fileInfo.FileData.Length);

            for (int i = 0; i < decryptedData.Length; i++)
            {
                decryptedData[i] ^= BundleStream.KEY;
            }

            WebDecryptResult decryptResult = new WebDecryptResult
            {
                Result = AssetBundle.LoadFromMemory(decryptedData)
            };
            return decryptResult;
        }
    }

    #endregion
}

/// <summary>
/// 资源文件解密流
/// <remarks>继承自FileStream，在读取文件时实时进行异或解密操作</remarks>
/// </summary>
public class BundleStream : FileStream
{
    /// <summary>
    /// 解密密钥常量
    /// 使用异或运算进行加解密，值为64 (二进制: 01000000)
    /// 异或特性：A ^ KEY ^ KEY = A，即用同一个密钥加密和解密
    /// </summary>
    public const byte KEY = 64;

    /// <summary>
    /// 构造函数 - 支持完整文件流参数
    /// </summary>
    /// <param name="path">文件路径，指向加密的AssetBundle文件</param>
    /// <param name="mode">文件打开模式（如Open, Create等）</param>
    /// <param name="access">文件访问权限（如Read, Write等）</param>
    /// <param name="share">文件共享模式</param>
    public BundleStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access,
        share)
    {
    }

    /// <summary>
    /// 构造函数 - 只支持路径和打开模式
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="mode">文件打开模式</param>
    public BundleStream(string path, FileMode mode) : base(path, mode)
    {
    }

    /// <summary>
    /// 重写Read方法 - 核心解密逻辑
    /// <remarks>在读取文件数据后，对读取到的字节进行异或解密</remarks>
    /// </summary>
    /// <param name="array">字节数组，用于存储读取的数据</param>
    /// <param name="offset">数组中的起始位置</param>
    /// <param name="count">要读取的字节数</param>
    /// <returns>实际读取的字节数</returns>
    public override int Read(byte[] array, int offset, int count)
    {
        // 1. 首先调用基类的Read方法从文件中读取原始（加密）数据
        // index变量存储实际读取到的字节数
        var index = base.Read(array, offset, count);
        // 2. 对读取到的数据进行解密处理
        // 遍历整个数组，对每个字节执行异或运算
        // 注意：这里遍历了整个array.Length，而不是只处理index长度的数据
        // 这可能是一个bug - 应该只处理实际读取的部分（offset到offset+index）
        // for (int i = 0; i < array.Length; i++)
        // {
        //     array[i] ^= KEY;
        // }

        for (int i = offset; i < offset + index; i++)
        {
            array[i] ^= KEY;
        }
        // 3. 返回实际读取的字节数
        return index;
    }
}
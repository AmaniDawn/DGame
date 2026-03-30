using System.IO;

namespace DGame
{
    public static partial class Utility
    {
        public static class PathUtil
        {
            /// <summary>
            /// 获取规范的路径
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public static string GetRegularPath(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return string.Empty;
                }
                return path.Replace("\\", "/");
            }

            /// <summary>
            /// 获取远程格式路径 (带有 file:// 或 http:// 前缀)
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public static string GetRemotePath(string path)
            {
                var regularPath = GetRegularPath(path);
                if (string.IsNullOrEmpty(regularPath))
                {
                    return string.Empty;
                }
                return regularPath.Contains("://") ? regularPath : ("file:///" + regularPath).Replace("file:////", "file:///");
            }

            /// <summary>
            /// 移除空的文件夹
            /// </summary>
            /// <param name="directoryName"></param>
            /// <returns></returns>
            /// <exception cref="DGameException"></exception>
            public static bool RemoveEmptyDirectory(string directoryName)
            {
                if (string.IsNullOrEmpty(directoryName))
                {
                    throw new DGameException("文件夹名无效");
                }

                try
                {
                    if (!Directory.Exists(directoryName))
                    {
                        return false;
                    }

                    string[] subDirectories = Directory.GetDirectories(directoryName, "*");
                    int subDirectoriesCount = subDirectories.Length;
                    foreach (var subDirectory in subDirectories)
                    {
                        if (RemoveEmptyDirectory(subDirectory))
                        {
                            subDirectoriesCount--;
                        }
                    }

                    if (subDirectoriesCount > 0)
                    {
                        return false;
                    }

                    if (Directory.GetFiles(directoryName, "*").Length > 0)
                    {
                        return false;
                    }
                    Directory.Delete(directoryName);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
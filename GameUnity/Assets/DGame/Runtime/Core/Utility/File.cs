using System;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DGame
{
    public static partial class Utility
    {
        public static class File
        {
            public static bool CreateFile(string filePath, bool isCreateDir = true)
            {
                if (!System.IO.File.Exists(filePath))
                {
                    string dir = System.IO.Path.GetDirectoryName(filePath);

                    if (!System.IO.Directory.Exists(dir))
                    {
                        if (isCreateDir)
                        {
                            if (!string.IsNullOrEmpty(dir))
                            {
                                System.IO.Directory.CreateDirectory(dir);
                            }
                        }
                        else
                        {
                            DLogger.Error($"文件夹不存在: {dir}");
                            return false;
                        }
                    }
                    System.IO.File.Create(filePath).Close();
                }
                return true;
            }

            public static bool CreateFile(string filePath, string content, bool isCreateDir = true)
            {
                StreamWriter sw;
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    string dir = System.IO.Path.GetDirectoryName(filePath);

                    if (!System.IO.Directory.Exists(dir))
                    {
                        if (isCreateDir)
                        {
                            if (!string.IsNullOrEmpty(dir))
                            {
                                System.IO.Directory.CreateDirectory(dir);
                            }
                        }
                        else
                        {
#if UNITY_EDITOR
                            EditorUtility.DisplayDialog("CreateFileTips", $"文件夹{dir}不存在", "Cancel");
#endif
                            DLogger.Error($"文件夹不存在: {dir}");
                            return false;
                        }
                    }
                    sw = fileInfo.CreateText();
                }
                else
                {
                    sw = fileInfo.CreateText();
                }
                sw.WriteLine(content);
                sw.Close();
                sw.Dispose();
                return true;
            }

            public static string GetPersistentDataPlatformPath(string filePath)
            {
                return Path.Combine(Application.persistentDataPath, filePath);
            }

            public static string GetStreamingAssetsPathPlatformPath(string filePath)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Android: 使用 jar:file:// 协议访问 APK 内资源
                return $"jar:file://{Application.dataPath}!assets/{filePath}";
#elif UNITY_IOS
                // iOS: 使用 file:// 协议
                return $"file://{Path.Combine(Application.streamingAssetsPath, filePath)}";
#else
                // 其他平台：直接文件路径
                return Path.Combine(Application.streamingAssetsPath, filePath);
#endif
            }

            public static string GetPath(string path)
            {
                return Utility.PathUtil.GetRegularPath(path);
            }

            public static string Md5ByPathName(string pathName)
            {
                try
                {
                    FileStream fileStream = new FileStream(pathName, FileMode.Open);
                    System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                    byte[] retVal = md5.ComputeHash(fileStream);
                    fileStream.Close();
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < retVal.Length; i++)
                    {
                        sb.Append(retVal[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
                catch (Exception e)
                {
                    DLogger.Error("转换MD5失败: " + e.Message);
                    return "Error";
                }
            }

            public static string GetLengthString(long length)
            {
                const long KB = 1024;
                const long MB = KB * 1024;
                const long GB = MB * 1024;

                if (length < 0)
                {
                    return "0 Bytes";
                }
                if (length < KB)
                {
                    return $"{length} Bytes";
                }

                if (length < MB)
                {
                    return $"{(length / (float)KB):F2} KB";
                }

                if (length < GB)
                {
                    return $"{(length / (float)MB):F2} MB";
                }

                return $"{(length / (float)GB):F2} GB";
            }

            public static string GetByteLengthString(long byteLength)
            {
                if (byteLength < 0)
                {
                    return "0 Bytes";
                }

                string[] units = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB" };
                // 2 ^ 10  2 ^ 20  2 ^ 30  2 ^ 40  2 ^ 50  2 ^ 60
                long[] thresholds = { 1L << 10, 1L << 20, 1L << 30, 1L << 40, 1L << 50, 1L << 60 };

                if (byteLength < 1024)
                {
                    return Utility.StringUtil.Format("{0} Bytes", byteLength);
                }

                double size = byteLength / 1024.0; // Start from KB
                int unitIndex = 0;

                while (unitIndex < thresholds.Length - 1 && size >= 1024)
                {
                    size /= 1024;
                    unitIndex++;
                }

                return Utility.StringUtil.Format("{0} {1}", size.ToString("F2"), units[unitIndex + 1]);

                // if (byteLength < 1024L) // 2 ^ 10
                // {
                //     return Utility.StringUtil.Format("{0} Bytes", byteLength.ToString());
                // }
                //
                // if (byteLength < 1048576L) // 2 ^ 20
                // {
                //     return Utility.StringUtil.Format("{0} KB", (byteLength / 1024f).ToString("F2"));
                // }
                //
                // if (byteLength < 1073741824L) // 2 ^ 30
                // {
                //     return Utility.StringUtil.Format("{0} MB", (byteLength / 1048576f).ToString("F2"));
                // }
                //
                // if (byteLength < 1099511627776L) // 2 ^ 40
                // {
                //     return Utility.StringUtil.Format("{0} GB", (byteLength / 1073741824f).ToString("F2"));
                // }
                //
                // if (byteLength < 1125899906842624L) // 2 ^ 50
                // {
                //     return Utility.StringUtil.Format("{0} TB", (byteLength / 1099511627776f).ToString("F2"));
                // }
                //
                // if (byteLength < 1152921504606846976L) // 2 ^ 60
                // {
                //     return Utility.StringUtil.Format("{0} PB", (byteLength / 1125899906842624f).ToString("F2"));
                // }
                //
                // return Utility.StringUtil.Format("{0} EB", (byteLength / 1152921504606846976f).ToString("F2"));
            }

            public static string BinToUtf8(byte[] total)
            {
                if (total == null)
                {
                    throw new ArgumentNullException(nameof(total));
                }

                // 使用 UTF8Encoding 的 GetString 方法，它可以自动处理 BOM
                var encoding = new UTF8Encoding(false); // false 表示不包含 BOM
                return encoding.GetString(total);

                // if (total.Length == 0)
                // {
                //     return string.Empty;
                // }
                // byte[] result = total;
                // if (total[0] == 0xef && total[1] == 0xbb && total[2] == 0xbf)
                // {
                //     // utf8文件的前三个字节为特殊占位符，要跳过
                //     result = new byte[total.Length - 3];
                //     System.Array.Copy(total, 3, result, 0, total.Length - 3);
                // }
                // string utf8Str = System.Text.Encoding.UTF8.GetString(result);
                // return utf8Str;
            }

            public static string FormatData(long data)
            {
                string result;

                if (data < 0)
                {
                    data = 0;
                }
                const long KB = 1024;
                const long MB = KB * 1024;
                if (data > MB)
                {
                    result = $"{((int)(data / MB)).ToString()}MB";
                }
                else if (data > KB)
                {
                    result = $"{((int)(data / KB)).ToString()}KB";
                }
                else
                {
                    result = $"{data}B";
                }

                return result;
            }
        }
    }
}
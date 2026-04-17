#if UNITY_EDITOR

using System;
using System.IO;
using GameProto;
using Luban;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 编辑器模式下的配置表加载器，仅在编辑器下使用，避免触发运行时系统初始化
    /// </summary>
    public static class EditorConfigLoader
    {
        private const string DefaultConfigPath = "Assets/BundleAssets/Configs/Binary";

        private static Tables s_tables;

        /// <summary>
        /// 获取编辑器配置表实例
        /// </summary>
        public static Tables Tables
        {
            get
            {
                if (s_tables == null)
                {
                    LoadTables(DefaultConfigPath);
                }
                return s_tables;
            }
        }

        /// <summary>
        /// 加载配置表
        /// </summary>
        /// <param name="configPath">配置文件路径，默认使用 DefaultConfigPath</param>
        public static void LoadTables(string configPath = null)
        {
            if (string.IsNullOrEmpty(configPath))
            {
                configPath = DefaultConfigPath;
            }

            if (!Directory.Exists(configPath))
            {
                Debug.LogError($"[EditorConfigLoader] Config directory not found: {configPath}");
                return;
            }

            try
            {
                s_tables = new Tables(file =>
                {
                    string filePath = $"{configPath}/{file}.bytes";
                    if (!File.Exists(filePath))
                    {
                        Debug.LogWarning($"[EditorConfigLoader] File not found: {filePath}");
                        return new ByteBuf(Array.Empty<byte>());
                    }
                    byte[] bytes = File.ReadAllBytes(filePath);
                    return new ByteBuf(bytes);
                });

                Debug.Log($"[EditorConfigLoader] Tables loaded from: {configPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EditorConfigLoader] Failed to load tables: {e.Message}");
            }
        }

        /// <summary>
        /// 重新加载配置表（配置文件更新后调用）
        /// </summary>
        public static void ReloadTables()
        {
            s_tables = null;
            LoadTables();
        }

        /// <summary>
        /// 获取文本配置
        /// </summary>
        public static TextConfig GetTextConfig(int textID)
        {
            return Tables.TbTextConfig.GetOrDefaultNoStatic(textID);
        }

        /// <summary>
        /// 获取文本配置
        /// </summary>
        public static TextConfig GetTextConfig(TextDefine textDefine)
        {
            return GetTextConfig((int)textDefine);
        }

        /// <summary>
        /// 获取文本内容
        /// </summary>
        public static string GetTextContent(int textID, LocalAreaType language = LocalAreaType.CN)
        {
            TextConfig config = GetTextConfig(textID);
            if (config == null)
            {
                return $"[没有配置的文本ID: {textID}]";
            }

            int langIndex = (int)language;
            if (langIndex < 0 || langIndex >= config.Content.Length)
            {
                langIndex = 0;
            }

            return config.Content[langIndex];
        }

        public static bool TryGetTextDefineStr(string content, out string str)
        {
            for (int i = 0; i < TbTextConfig.DataList.Count; i++)
            {
                var cfg = TbTextConfig.DataList[i];
                // 一般都是中文开发，所以直接默认判断第0个索引
                if (cfg.Content[0] == content)
                {
                    return TryGetTextDefineStr(cfg.ID, out str);
                }
            }
            str = string.Empty;
            return false;
        }

        public static bool TryGetTextDefineStr(int id, out string str)
        {
            if (Enum.IsDefined(typeof(TextDefine), (TextDefine)id))
            {
                str = ((TextDefine)id).ToString();
                return true;
            }
            str = string.Empty;
            return false;
        }
    }
}

#endif
using System;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace DGame
{
    public class EditorScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T m_instance;

        public static T Instance
        {
            get
            {
                if (m_instance == null)
                {
                    LoadOrCreate();
                }
                return m_instance;
            }
        }

        private static T LoadOrCreate()
        {
            string filePath = GetFilePath();

            if (!string.IsNullOrEmpty(filePath))
            {
                var arr = InternalEditorUtility.LoadSerializedFileAndForget(filePath);
                m_instance = arr.Length > 0 ? arr[0] as T : m_instance == null ? CreateInstance<T>() : m_instance;
            }
            else
            {
                Debug.LogError($"save location of {nameof(EditorScriptableSingleton<T>)} is invalid");
            }
            return m_instance;
        }

        public static void Save(bool saveAsText = true)
        {
            if (!m_instance)
            {
                Debug.LogError($"该ScriptableSingleton不是一个实例 无法保存");
                return;
            }
            string path = GetFilePath();

            if (!string.IsNullOrEmpty(path))
            {
                string dir = Path.GetDirectoryName(path);

                if (!Directory.Exists(dir))
                {
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                UnityEngine.Object[] objects = { m_instance };
                InternalEditorUtility.SaveToSerializedFileAndForget(objects, path, saveAsText);
            }
        }

        protected static string GetFilePath()
        {
            return typeof(T).GetCustomAttributes(inherit: true)
                .Where(v => v is FilePathAttribute)
                .Cast<FilePathAttribute>()
                .FirstOrDefault()
                ?.FilePath;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FilePathAttribute : Attribute
    {
        internal readonly string FilePath;

        public FilePathAttribute(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("文件路径为空 无效");
            }

            if (path[0] == '/')
            {
                path = path.Substring(1);
            }
            this.FilePath = path;
        }
    }
}
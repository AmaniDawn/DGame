#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using static VFolders.Libs.VUtils;
using static VFolders.Libs.VGUI;
// using static VTools.VDebug;

namespace VFolders
{
    [FilePath("Library/vFolders Cache.asset", FilePathAttribute.Location.ProjectFolder)]
    public class VFoldersCache : ScriptableSingleton<VFoldersCache>
    {

        public Texture2D GetIcon(int key)
        {
            if (instance.iconTextures_byKey.ContainsKey(key))
                return instance.iconTextures_byKey[key];

            if (instance.iconTextureDatas_byKey.ContainsKey(key))
                return instance.iconTextures_byKey[key] = instance.iconTextureDatas_byKey[key].GetTexture();

            return null;

        }

        public void AddIcon(int key, Texture2D icon)
        {
            instance.iconTextures_byKey[key] = icon;
            instance.iconTextureDatas_byKey[key] = new TextureData(icon);

            instance.Save(true);

        }
        public bool HasIcon(int key) => instance.iconTextureDatas_byKey.ContainsKey(key);

        public Dictionary<int, Texture2D> iconTextures_byKey = new();
        public SerializableDictionary<int, TextureData> iconTextureDatas_byKey = new();

        [System.Serializable]
        public class TextureData
        {
            public byte[] rawData;
            public int width;
            public int height;
            public TextureFormat format;
            public int mipCount;
            public float pixelsPerPoint;

            public TextureData(Texture2D t)
            {
                rawData = t.GetRawTextureData();
                width = t.width;
                height = t.height;
                format = t.format;
                mipCount = t.mipmapCount;
                pixelsPerPoint = t.GetPropertyValue<float>("pixelsPerPoint");

            }

            public Texture2D GetTexture()
            {
                var t = new Texture2D(width, height, format, mipCount, false);
                t.LoadRawTextureData(rawData);
                t.SetPropertyValue("pixelsPerPoint", pixelsPerPoint);
                t.hideFlags = HideFlags.DontSave;
                t.Apply();

                return t;

            }

        }




        public SerializableDictionary<string, FolderState> folderStates_byGuid = new();

        [System.Serializable]
        public class FolderState
        {
            public bool isEmpty;

            public List<string> contentMinimapIconNames = new();

            public string autoIconName = "";

            public bool needsUpdate;

        }

    }
}
#endif

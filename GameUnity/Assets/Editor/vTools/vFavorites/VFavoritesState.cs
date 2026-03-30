#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System.Reflection;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Type = System.Type;
using static VFavorites.Libs.VUtils;
using static VFavorites.Libs.VGUI;


namespace VFavorites
{
    [FilePath("Library/vFavorites State.asset", FilePathAttribute.Location.ProjectFolder)]
    public class VFavoritesState : ScriptableSingleton<VFavoritesState>
    {
        public int curPageIndex;

        public SerializableDictionary<int, PageState> pageStates_byPageId = new SerializableDictionary<int, PageState>();

        public SerializableDictionary<int, ItemState> itemStates_byItemId = new SerializableDictionary<int, ItemState>();


        [System.Serializable]
        public class PageState
        {
            public long lastItemSelectTime_ticks;
            public long lastItemDragTime_ticks;

            public float scrollPos;

        }

        [System.Serializable]
        public class ItemState
        {
            public string _name;

            public string sceneGameObjectIconName;

            public long lastSelectTime_ticks;
            public bool isSelected;

        }


        public static void Save() => instance.Save(true);

    }
}
#endif
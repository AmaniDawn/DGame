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
using static VFolders.Libs.VUtils;
using static VFolders.Libs.VGUI;
// using static VTools.VDebug;


namespace VFolders
{
    public class VFoldersData : ScriptableObject, ISerializationCallbackReceiver
    {

        public SerializableDictionary<string, FolderData> folderDatas_byGuid = new();

        [System.Serializable]
        public class FolderData
        {
            public string iconNameOrGuid = "";
            public int colorIndex = 0;

            public bool isIconRecursive;
            public bool isColorRecursive;

        }

        public void OnBeforeSerialize() => VFolders.OnDataSerialization();
        public void OnAfterDeserialize() { }




        public List<Bookmark> bookmarks = new();

        [System.Serializable]
        public class Bookmark
        {

            public string name
            {
                get
                {
                    if (isDeleted)
                        return "Deleted";

                    var filename = guid.ToPath().GetFilename(withExtension: true);

                    if (filename.StartsWith("com."))
                        if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath(guid.ToPath()) is UnityEditor.PackageManager.PackageInfo packageInfo)
                            return packageInfo.displayName;

                    return filename;

                }
            }


            public bool isDeleted => !AssetDatabase.IsValidFolder(guid.ToPath());



            public Bookmark(Object o) => guid = o.GetGuid();

            public string guid;

        }








        [CustomEditor(typeof(VFoldersData))]
        class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };

                void normal()
                {
                    if (storeDataInMetaFiles) return;

                    SetGUIEnabled(false);
                    BeginIndent(0);

                    Space(10);
                    EditorGUILayout.LabelField("This file stores data about which icons and colors are assigned to folders, along with bookmarks from navigation bar.", style);

                    Space(6);
                    GUILayout.Label("If there are multiple people working on the project, it's better to store icon and color data in .meta files of folders to avoid merge conflicts. To do that, click the  ⋮  button at the top right corner and enable Team Mode.", style);

                    EndIndent(10);
                    ResetGUIEnabled();
                }
                void meta()
                {
                    if (!storeDataInMetaFiles) return;

                    SetGUIEnabled(false);
                    BeginIndent(0);

                    Space(10);
                    EditorGUILayout.LabelField("Icon and color data is currently stored in folders .meta files of folders, and this file only contains bookmarks from navigation bar.", style);

                    Space(6);
                    GUILayout.Label("If you want all data to be stored in this file, click the ⋮ button at the top right corner and disable Team Mode.", style);

                    EndIndent(10);
                    ResetGUIEnabled();
                }

                normal();
                meta();

            }
        }

        public static bool storeDataInMetaFiles { get => EditorPrefsCached.GetBool("vFolders-teamModeEnabled", false); set => EditorPrefsCached.SetBool("vFolders-teamModeEnabled", value); }



        [ContextMenu("Enable Team Mode", isValidateFunction: false, priority: 1)]
        public void EnableTeamMode()
        {
            var option = EditorUtility.DisplayDialogComplex("Licensing notice",
                                                            "To use vFolders 2 within a team, licenses must be purchased for each individual user as per the Asset Store EULA.\n\n Sharing one license across the team is illegal and considered piracy.",
                                                            "Acknowledge",
                                                            "Cancel",
                                                            "Purchase more seats");
            if (option == 0)
                storeDataInMetaFiles = true;

            if (option == 2)
                Application.OpenURL("https://prf.hn/click/camref:1100lGLBn/pubref:teammode/destination:https://assetstore.unity.com/packages/tools/utilities/vfolders-2-255470");
            // Application.OpenURL("https://assetstore.unity.com/packages/slug/255470");

        }

        [ContextMenu("Disable Team Mode", isValidateFunction: false, priority: 2)]
        public void DisableTeamMode() => storeDataInMetaFiles = false;

        [ContextMenu("Enable Team Mode", isValidateFunction: true, priority: 1)] bool asd() => !storeDataInMetaFiles;
        [ContextMenu("Disable Team Mode", isValidateFunction: true, priority: 2)] bool ads() => storeDataInMetaFiles;



    }
}
#endif
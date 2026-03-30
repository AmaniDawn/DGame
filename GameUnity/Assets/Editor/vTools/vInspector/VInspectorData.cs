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
using static VInspector.VInspectorState;
using static VInspector.Libs.VUtils;
using static VInspector.Libs.VGUI;
// using static VTools.VDebug;


namespace VInspector
{
    public class VInspectorData : ScriptableObject
    {

        public List<Bookmark> bookmarks = new();

        [System.Serializable]
        public class Bookmark
        {

            public Object obj
            {
                get
                {
                    if (isSceneGameObject && _obj == null)
                    {
                        if (!failedToLoadSceneObject) // to prevent continuous GlobalID.GetObjects() calls if object is deleted
                            VInspector.unloadedSceneBookmarks_sceneGuids.Add(globalId.guid);

                        return null;
                    }

                    return _obj ??= globalId.GetObject();

                }

            }
            public Object _obj;

            [System.NonSerialized]
            public bool failedToLoadSceneObject;



            public bool isLoadable => obj != null;

            public bool isDeleted
            {
                get
                {
                    if (!isSceneGameObject)
                        return !isLoadable;

                    if (isLoadable)
                        return false;

                    if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(globalId.guid.ToPath()))
                        return true;

                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                        if (EditorSceneManager.GetSceneAt(i).path == globalId.guid.ToPath())
                            return true;

                    return false;

                }
            }

            public string assetPath => globalId.guid.ToPath();

            public Type type => Type.GetType(_typeString) ?? typeof(DefaultAsset);

            public string name
            {
                get
                {
                    if (!obj) return "Can't load object";

                    if (assetPath.GetExtension() == ".cs")
                        return obj.name.Decamelcase();
                    else
                        return obj.name;

                }
            }



            public Bookmark(Object o)
            {
                globalId = o.GetGlobalID();

                isSceneGameObject = o is GameObject go && go.scene.rootCount != 0;
                isAsset = !isSceneGameObject;

                _typeString = o.GetType().AssemblyQualifiedName;

                _obj = o;

            }

            public GlobalID globalId;

            public bool isSceneGameObject;
            public bool isAsset;

            public string _typeString;

        }










        [CustomEditor(typeof(VInspectorData))]
        class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };


                SetGUIEnabled(false);
                BeginIndent(0);

                Space(10);
                EditorGUILayout.LabelField("This file stores bookmarks from vInspector's navigation bar", style);

                EndIndent(10);
                ResetGUIEnabled();

                // Space(15);
                // base.OnInspectorGUI();

            }
        }


    }
}
#endif
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class ClassReplaceHelper
{
#if UNITY_6000_1_OR_NEWER
    public static EntityId GetClassID(System.Type type)
#else
    public static int GetClassID(System.Type type)
#endif
    {
        GameObject go = EditorUtility.CreateGameObjectWithHideFlags("Temp", HideFlags.HideAndDontSave);
        Component uiSprite = go.AddComponent(type);
        SerializedObject ob = new SerializedObject(uiSprite);
#if UNITY_6000_4_OR_NEWER
        EntityId classID = ob.FindProperty("m_Script").objectReferenceEntityIdValue;
#else
        int classID = ob.FindProperty("m_Script").objectReferenceInstanceIDValue;
#endif
        Object.DestroyImmediate(go);
        return classID;
    }

#if UNITY_6000_1_OR_NEWER
    public static EntityId GetClassID<T>() where T : MonoBehaviour => GetClassID(typeof(T));
#else
    public static int GetClassID<T>() where T : MonoBehaviour => GetClassID(typeof(T));
#endif

    public static SerializedObject ReplaceClass(MonoBehaviour mb, System.Type type)
    {
        var id = GetClassID(type);
        SerializedObject ob = new SerializedObject(mb);
        ob.Update();
#if UNITY_6000_4_OR_NEWER
        ob.FindProperty("m_Script").objectReferenceEntityIdValue = id;
#else
        ob.FindProperty("m_Script").objectReferenceInstanceIDValue = id;
#endif
        ob.ApplyModifiedProperties();
        ob.Update();
        return ob;
    }
}

#endif

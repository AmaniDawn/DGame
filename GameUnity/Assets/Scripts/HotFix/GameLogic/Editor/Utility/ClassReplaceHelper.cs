#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class ClassReplaceHelper
{
    public static int GetClassID(System.Type type)
    {
        GameObject go = EditorUtility.CreateGameObjectWithHideFlags("Temp", HideFlags.HideAndDontSave);
        Component uiSprite = go.AddComponent(type);
        SerializedObject ob = new SerializedObject(uiSprite);
        int classID = ob.FindProperty("m_Script").objectReferenceInstanceIDValue;
        Object.DestroyImmediate(go);
        return classID;
    }

    public static int GetClassID<T>() where T : MonoBehaviour => GetClassID(typeof(T));

    public static SerializedObject ReplaceClass(MonoBehaviour mb, System.Type type)
    {
        int id = GetClassID(type);
        SerializedObject ob = new SerializedObject(mb);
        ob.Update();
        ob.FindProperty("m_Script").objectReferenceInstanceIDValue = id;
        ob.ApplyModifiedProperties();
        ob.Update();
        return ob;
    }
}

#endif
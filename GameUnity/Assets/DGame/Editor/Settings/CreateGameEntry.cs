#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DGame
{
    public class CreateGameEntry
    {
        [MenuItem("DGame Tools/Settings/Create GameEntry", priority = 2)]
        public static void CreateGameEntryObject()
        {
            GameObject obj = GameObject.Find("GameEntry");

            if (obj != null)
            {
                Debug.Log("场景中已经存在 [GameEntry]");
                return;
            }

            var guids = AssetDatabase.FindAssets("GameEntry t:Prefab");
            if (guids.Length <= 0)
            {
                Debug.LogError("没有找到 [GameEntry] 预制体，请检查相关资源");
                return;
            }
            string firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var gameEntryObj = AssetDatabase.LoadAssetAtPath<GameObject>(firstPath);
            if (gameEntryObj == null)
            {
                return;
            }

            var gameEntry = PrefabUtility.InstantiatePrefab(gameEntryObj) as GameObject;
            gameEntry?.transform.SetParent(null);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());
            Debug.Log("已成功创建 [GameEntry] 物体");
        }
    }
}

#endif
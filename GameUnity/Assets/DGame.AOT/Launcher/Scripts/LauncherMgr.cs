using System;
using System.Collections.Generic;
using DGame;
using UnityEngine;

namespace Launcher
{
    public static class LauncherMgr
    {
        private static string UI_ROOT_PATH = "UIRoot/UICanvas";
        private static string UI_WINDOW_PATH = "UIWindow/";
        private static Transform m_uiRoot;
        private static readonly Dictionary<string, UIBase> m_uiMapDict = new Dictionary<string, UIBase>(4);

        public static void Initialize()
        {
            m_uiRoot = GameObject.Find(UI_ROOT_PATH)?.transform;

            if (m_uiRoot == null)
            {
                DLogger.Error($"======== 找不到 UIRoot 节点 请检查资源路径或Hierarchy窗口中的游戏对象 ========");
                return;
            }

            // DLogger.Info("======== 初始化 LauncherMgr 完成 ========");
        }

        public static void ShowUI<T>(object param = null) where T : UIBase, new()
        {
            string uiName = typeof(T).Name;
            if (string.IsNullOrEmpty(uiName))
            {
                DLogger.Warning($"======== LauncherMgr.ShowUI UIName 为空 ========");
                return;
            }

            if (!m_uiMapDict.TryGetValue(uiName, out var uiBase))
            {
                UnityEngine.Object obj = Resources.Load(UI_WINDOW_PATH + uiName);
                if (obj != null)
                {
                    var uiWindow = UnityEngine.Object.Instantiate(obj) as GameObject;

                    if (uiWindow != null)
                    {
                        uiWindow.transform.SetParent(m_uiRoot.transform);
                        uiWindow.name = uiName;
                        uiWindow.transform.localScale = Vector3.one;
                        uiWindow.transform.localPosition = Vector3.zero;
                        uiWindow.transform.localRotation = Quaternion.identity;
                        RectTransform rectTransform = uiWindow.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = Vector2.zero;

                        uiBase = new T();
                        uiBase.gameObject = uiWindow;
                        uiBase.CallScriptGenerator();
                        m_uiMapDict[uiName] = uiBase;
                    }
                }
            }
            uiBase?.Show();
            uiBase?.OnInit(param);
        }

        public static void CloseUI(UIBase uiWindow)
        {
            CloseUI(uiWindow.GetType().Name);
        }

        public static void CloseUI<T>() where T : UIBase
        {
            CloseUI(typeof(T).Name);
        }

        public static void CloseUI(string uiName)
        {
            if (string.IsNullOrEmpty(uiName))
            {
                DLogger.Warning($"======== LauncherMgr.HideUI UIName 为空 ========");
                return;
            }

            if (!m_uiMapDict.TryGetValue(uiName, out var uiWindow))
            {
                return;
            }

            uiWindow?.Hide();
            UnityEngine.Object.DestroyImmediate(uiWindow?.gameObject);
            m_uiMapDict.Remove(uiName);
        }

        public static T GetActiveUI<T>() where T : UIBase
        {
            return GetActiveUI(typeof(T).Name) as T;
        }

        public static UIBase GetActiveUI(string uiName)
        {
            return m_uiMapDict.GetValueOrDefault(uiName);
        }

        public static void HideAllUI()
        {
            foreach (var ui in m_uiMapDict.Values)
            {
                ui?.Hide();
                UnityEngine.Object.Destroy(ui?.gameObject);
            }
            m_uiMapDict.Clear();
        }

        #region UI调用

        public static void ShowMessageBox(string desc, Action onConfirm = null,
            Action onCancel = null, Action onUpdate = null)
        {
            ShowUI<LoadTipsUI>(desc);
            var ui = GetActiveUI<LoadTipsUI>();
            ui?.SetAllCallback(onConfirm, onUpdate, onCancel);
        }

        public static void RefreshProgress(float progress)
        {
            ShowUI<LoadUpdateUI>();
            var ui = GetActiveUI<LoadUpdateUI>();
            ui?.RefreshProgress(progress);
        }

        #endregion
    }
}
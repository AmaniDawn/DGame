using DGame;
using Fantasy.Entitas;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace GameLogic
{
    /// <summary>
    /// 主相机管理组件。
    /// </summary>
    public sealed class CameraMgrComponent : Entity
    {
        private Camera m_mainCamera;
        private GameObject m_gameObject;
        private Transform m_transform;

        /// <summary>
        /// 获取主相机实例。
        /// </summary>
        public Camera MainCamera
        {
            get
            {
                EnsureCameraValid();
                return m_mainCamera;
            }
        }
        
        /// <summary>
        /// 获取主相机对应的游戏对象。
        /// </summary>
        public GameObject gameObject 
        {
            get
            {
                EnsureCameraValid();
                return m_gameObject;
            }
        }
        
        /// <summary>
        /// 获取主相机对应的变换组件。
        /// </summary>
        public Transform transform
        {
            get
            {
                EnsureCameraValid();
                return m_transform;
            }
        }
        
        /// <summary>
        /// 确保相机有效，如果无效则刷新。
        /// </summary>
        private void EnsureCameraValid()
        {
            if (!IsValidCamera)
            {
                RefreshCamera();
            }
        }

        /// <summary>
        /// 刷新相机引用，用于场景切换后重新获取。
        /// </summary>
        public void RefreshCamera()
        {
            m_mainCamera = Camera.main;
            DLogger.Assert(m_mainCamera != null, "找不到主相机");
            if (m_mainCamera == null)
            {
                Clear();
                return;
            }
            m_gameObject = m_mainCamera.gameObject;
            m_transform = m_mainCamera.transform;
        }

        private bool IsValidCamera => m_mainCamera != null && m_mainCamera.gameObject.activeInHierarchy;

        /// <summary>
        /// 清理当前缓存的相机引用。
        /// </summary>
        public void Clear()
        {
            m_mainCamera = null;
            m_gameObject = null;
            m_transform = null;
        }
    }
}
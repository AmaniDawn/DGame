using UnityEditor;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// Inspector抽象基类
    /// </summary>
    public abstract class DGameInspector : UnityEditor.Editor
    {
        private bool m_isCompiling = false;

        /// <summary>
        /// 绘制
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (m_isCompiling && !EditorApplication.isCompiling)
            {
                m_isCompiling = false;
                OnCompileComplete();
            }
            else if (!m_isCompiling && EditorApplication.isCompiling)
            {
                m_isCompiling = true;
                OnCompileStart();
            }
        }

        /// <summary>
        /// 编译开始
        /// </summary>
        protected virtual void OnCompileStart()
        {
        }

        /// <summary>
        /// 编译完成
        /// </summary>
        protected virtual void OnCompileComplete()
        {
        }

        protected bool IsPrefabInHierarchy(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return false;
            }

#if UNITY_2018_3_OR_NEWER
            return PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.Regular;
#else
            return PrefabUtility.GetPrefabType(obj) != PrefabType.Prefab;
#endif
        }
    }
}
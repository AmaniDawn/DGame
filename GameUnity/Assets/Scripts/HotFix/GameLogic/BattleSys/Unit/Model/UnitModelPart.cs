using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 单个模型部件封装。
    /// 当前主要用于主模型，后续也可以扩展到武器、阴影、挂件等部件。
    /// </summary>
    public class UnitModelPart
    {
        /// <summary>
        /// 当前实例化出的模型对象。
        /// </summary>
        private GameObject m_modelGo;

        /// <summary>
        /// 当前加载的资源地址。
        /// </summary>
        private string m_location;

        /// <summary>
        /// 当前父节点。
        /// </summary>
        private Transform m_parent;

        /// <summary>
        /// 当前模型对象。
        /// </summary>
        public GameObject ModelGo => m_modelGo;

        /// <summary>
        /// 当前模型节点的 Transform。
        /// </summary>
        public Transform Transform => m_modelGo != null ? m_modelGo.transform : null;

        /// <summary>
        /// 当前资源地址。
        /// </summary>
        public string Location => m_location;

        /// <summary>
        /// 当前部件是否已经加载出模型实例。
        /// </summary>
        public bool IsLoaded => m_modelGo != null;

        /// <summary>
        /// 修改模型部件父节点。
        /// 如果模型已经存在，会立即调整层级关系。
        /// </summary>
        /// <param name="parent">新的父节点。</param>
        /// <param name="worldPositionStays">是否保持世界坐标。</param>
        public void SetParent(Transform parent, bool worldPositionStays = false)
        {
            m_parent = parent;
            if (m_modelGo != null)
            {
                m_modelGo.transform.SetParent(parent, worldPositionStays);
            }
        }

        /// <summary>
        /// 加载指定资源地址对应的模型对象。
        /// 若与当前资源一致，则直接复用已有实例。
        /// </summary>
        /// <param name="location">资源地址。</param>
        /// <param name="parent">挂载父节点。</param>
        /// <returns>加载成功返回 true。</returns>
        public async UniTask<bool> LoadModelAsync(string location, Transform parent = null)
        {
            if (string.IsNullOrEmpty(location))
            {
                Destroy();
                return false;
            }

            if (m_modelGo != null && m_location == location)
            {
                if (parent != null && m_parent != parent)
                {
                    m_parent = parent;
                    m_modelGo.transform.SetParent(parent);
                }
                return true;
            }

            Destroy();

            m_parent = parent;
            m_location = location;
            m_modelGo = await GameModule.ResourceModule.LoadGameObjectAsync(location, parent);
            if (m_modelGo == null)
            {
                m_parent = null;
                m_location = string.Empty;
                return false;
            }

            m_modelGo.transform.ResetLocalPosScaleRot();
            return true;
        }

        /// <summary>
        /// 设置模型显隐。
        /// </summary>
        /// <param name="active">是否可见。</param>
        public void SetActive(bool active) => m_modelGo?.SetActive(active);

        /// <summary>
        /// 销毁当前模型实例并清空部件状态。
        /// </summary>
        public void Destroy()
        {
            if (m_modelGo != null)
            {
                Object.Destroy(m_modelGo);
                m_modelGo = null;
            }

            m_location = string.Empty;
            m_parent = null;
        }
    }
}
/* -------------------------------------------------------------------------
 * 这里处理的是单个部位模型的加载、挂接、复用与销毁，不负责整套显示逻辑编排。
 *
 * 当前版本主要用于主模型部件，但如果后期出现更多的部位模型，例如：
 * 武器模型、翅膀模型、挂件模型、阴影模型、特效承载模型，
 * 都应该继续以这个类为基类，在此基础上派生出对应的部位模型子类。
 *
 * 例如可以扩展为：
 * CharacterModelPart、WeaponModelPart、WingModelPart、AccessoryModelPart。
 * 各子类只关注自身部位的资源定位、挂点规则和附加行为，
 * 通用的模型加载、父节点切换、实例复用和销毁流程应尽量复用这里的基础能力。
 *
 * 不建议把不同部位的特殊规则直接堆叠进当前基类，
 * 否则后续部位一多，这里会演变成职责混杂的“大而全”模型部件管理器。
 * -------------------------------------------------------------------------
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 单位模型部位类型。
    /// </summary>
    public enum UnitModelType
    {
        /// <summary>
        /// 主模型部位。
        /// </summary>
        MainModelType,

        /// <summary>
        /// 武器模型部位。
        /// </summary>
        WeaponModelType,

        /// <summary>
        /// 阴影模型部位。
        /// </summary>
        ActorShadowModelType,

        /// <summary>
        /// 部位类型上限。
        /// </summary>
        MaxModelType
    }
    
    /// <summary>
    /// 单个模型部件封装。
    /// 当前主要用于主模型，后续也可以扩展到武器、阴影、挂件等部件。
    /// </summary>
    public abstract class UnitModelPart
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
        /// 当前部位类型。
        /// </summary>
        public virtual UnitModelType ModelType { get; }

        /// <summary>
        /// 模型创建完成后的回调。
        /// </summary>
        protected Action<GameObject, UnitModelType> m_onCreate;

        /// <summary>
        /// 模型销毁完成后的回调。
        /// </summary>
        protected Action<UnitModelType> m_onDestroy;

        /// <summary>
        /// 模型销毁前的回调。
        /// </summary>
        protected Action<UnitModelType> m_onBeforeDestroy;

        /// <summary>
        /// 修改模型部件父节点。
        /// 如果模型已经存在，会立即调整层级关系。
        /// </summary>
        /// <param name="parent">新的父节点。</param>
        /// <param name="worldPositionStays">是否保持世界坐标。</param>
        public virtual void SetParent(Transform parent, bool worldPositionStays = false)
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
        /// <param name="ct">模型加载时使用的取消令牌。</param>
        /// <returns>加载成功返回 true。</returns>
        public virtual async UniTask<bool> LoadModelAsync(string location, Transform parent = null, CancellationToken ct = default)
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
                    m_modelGo.transform.SetParent(parent, false);
                    m_modelGo.transform.ResetLocalPosScaleRot();
                }
                return true;
            }

            Destroy();

            m_parent = parent;
            m_location = location;
            m_modelGo = await GameModule.ResourceModule.LoadGameObjectAsync(location, parent, ct);
            if (m_modelGo == null)
            {
                m_parent = null;
                m_location = string.Empty;
                return false;
            }

            m_modelGo.transform.ResetLocalPosScaleRot();
            OnModelLoaded();
            m_onCreate?.Invoke(ModelGo, ModelType);
            return true;
        }

        /// <summary>
        /// 设置模型显隐。
        /// </summary>
        /// <param name="active">是否可见。</param>
        public virtual void SetActive(bool active) => m_modelGo?.SetActive(active);
        
        /// <summary>
        /// 模型加载完成后的扩展钩子。
        /// </summary>
        protected virtual void OnModelLoaded() { }

        /// <summary>
        /// 模型销毁前的扩展钩子。
        /// </summary>
        protected virtual void OnBeforeDestroy() { }

        /// <summary>
        /// 销毁当前模型实例并清空部件状态。
        /// </summary>
        public virtual void Destroy()
        {
            m_onBeforeDestroy?.Invoke(ModelType);
            OnBeforeDestroy();
            if (m_modelGo != null)
            {
                Object.Destroy(m_modelGo);
                m_modelGo = null;
            }
            m_onDestroy?.Invoke(ModelType);
            m_location = string.Empty;
            m_parent = null;
        }
    }
}
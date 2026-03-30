using GameProto;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 单位模型容器。
    /// 负责创建模型根节点、管理主模型部件，以及在模型切换后刷新挂点缓存。
    /// </summary>
    public sealed class UnitModel
    {
        /// <summary>
        /// 所属显示组件。
        /// 用于回写挂点缓存等显示层数据。
        /// </summary>
        private readonly UnitDisplayComponent m_owner;

        /// <summary>
        /// 模型容器根节点。
        /// 所有模型部件都挂在该节点下，便于整体替换和管理。
        /// </summary>
        public GameObject ModelRoot { get; private set; }

        /// <summary>
        /// 模型容器根节点 Transform。
        /// </summary>
        public Transform ModelRootTransform { get; private set; }

        /// <summary>
        /// 主模型部件。
        /// 当前版本先只实现主模型，后续可以继续扩展武器、阴影、特效部件。
        /// </summary>
        public UnitModelPart MainModelPart { get; } = new();

        /// <summary>
        /// 当前主模型配置。
        /// </summary>
        public ModelConfig MainModelCfg { get; private set; }

        /// <summary>
        /// 创建单位模型容器。
        /// </summary>
        /// <param name="owner">所属显示组件。</param>
        public UnitModel(UnitDisplayComponent owner)
        {
            m_owner = owner;
        }

        /// <summary>
        /// 初始化模型根节点，并挂到显示容器下。
        /// </summary>
        /// <param name="parent">显示层父节点。</param>
        public void Init(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            if (ModelRoot == null)
            {
                ModelRoot = new GameObject("UnitModelRoot");
                ModelRootTransform = ModelRoot.transform;
            }

            ModelRootTransform.SetParent(parent, false);
            ResetLocalTransform(ModelRootTransform);
            MainModelPart.SetParent(ModelRootTransform);
        }

        /// <summary>
        /// 根据模型 ID 刷新主模型。
        /// 会读取模型配置、加载模型资源，并刷新主模型挂点缓存。
        /// </summary>
        /// <param name="modelId">模型 ID。</param>
        /// <returns>刷新成功返回 true。</returns>
        public bool RefreshMainModel(int modelId)
        {
            MainModelCfg = modelId > 0 ? ModelConfigMgr.Instance.GetOrDefault(modelId) : null;
            if (MainModelCfg == null || string.IsNullOrEmpty(MainModelCfg.ModelLocation))
            {
                MainModelPart.Destroy();
                m_owner?.UnitDummy?.Clear();
                return false;
            }

            if (!MainModelPart.Load(MainModelCfg.ModelLocation, ModelRootTransform))
            {
                m_owner?.UnitDummy?.Clear();
                return false;
            }

            var mainTransform = MainModelPart.Transform;
            if (mainTransform != null)
            {
                var scale = MainModelCfg.ModelScale <= 0f ? 1f : MainModelCfg.ModelScale;
                mainTransform.localScale = Vector3.one * scale;
            }

            m_owner?.UnitDummy?.Refresh(mainTransform);
            return true;
        }

        /// <summary>
        /// 获取当前主模型对象。
        /// </summary>
        /// <returns>主模型对象；未加载时返回 null。</returns>
        public GameObject GetModelGo() => MainModelPart.ModelGo;

        /// <summary>
        /// 设置整套模型显隐。
        /// </summary>
        /// <param name="active">是否可见。</param>
        public void SetActive(bool active)
        {
            MainModelPart.SetActive(active);
        }

        /// <summary>
        /// 销毁模型容器及其所有部件。
        /// </summary>
        public void Destroy()
        {
            MainModelPart.Destroy();
            MainModelCfg = null;
            m_owner?.UnitDummy?.Clear();
            if (ModelRoot != null)
            {
                Object.Destroy(ModelRoot);
                ModelRoot = null;
                ModelRootTransform = null;
            }
        }

        /// <summary>
        /// 重置模型根节点局部姿态。
        /// </summary>
        /// <param name="trans">目标节点。</param>
        private static void ResetLocalTransform(Transform trans)
        {
            if (trans == null)
            {
                return;
            }

            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
        }
    }
}
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameProto;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 单位模型容器。
    /// <para>
    /// 负责创建模型根节点，并统一管理主模型、武器模型等部位对象。
    /// </para>
    /// <para>
    /// 当前实现中，主模型负责刷新挂点缓存；武器模型依赖该缓存绑定到主模型挂点。
    /// </para>
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
        public MainUnitModelPart MainModelPart { get; private set; }

        /// <summary>
        /// 当前主模型配置。
        /// </summary>
        public ModelConfig MainModelCfg { get; private set; }

        /// <summary>
        /// 武器模型部件。
        /// 武器部件通常依附主模型挂点工作。
        /// </summary>
        public UnitWeaponModelPart WeaponModelPart { get; private set; }

        /// <summary>
        /// 当前武器模型配置。
        /// </summary>
        public WeaponModelConfig WeaponModelCfg { get; private set; }

        /// <summary>
        /// 创建单位模型容器。
        /// </summary>
        /// <param name="owner">所属显示组件。</param>
        public UnitModel(UnitDisplayComponent owner)
        {
            m_owner = owner;
            Init();
        }

        /// <summary>
        /// 初始化模型根节点，并挂到显示容器下。
        /// </summary>
        private void Init()
        {
            var parent = m_owner.DisplayRootTransform;
            if (parent == null)
            {
                return;
            }

            if (ModelRoot == null)
            {
                ModelRoot = new GameObject(UnitHelper.ModelRootName);
                ModelRootTransform = ModelRoot.transform;
            }

            ModelRootTransform.SetParent(parent, false);
            ModelRootTransform.ResetLocalPosScaleRot();
        }

        /// <summary>
        /// 根据模型 ID 刷新主模型。
        /// 会读取模型配置、加载模型资源，并刷新主模型挂点缓存。
        /// </summary>
        /// <param name="modelId">模型 ID。</param>
        /// <param name="ct">模型加载取消令牌。</param>
        /// <returns>刷新成功返回 true。</returns>
        public async UniTask<bool> RefreshMainModelAsync(int modelId, CancellationToken ct = default)
        {
            MainModelCfg = modelId > 0 ? ModelConfigMgr.Instance.GetModelOrDefault(modelId) : null;
            if (MainModelCfg == null || string.IsNullOrEmpty(MainModelCfg.ModelLocation))
            {
                MainModelPart?.Destroy();
                MainModelPart = null;
                m_owner?.UnitDummy?.Clear();
                return false;
            }
            return await AddModelAsync(UnitModelType.MainModelType, ct);
        }

        /// <summary>
        /// 根据模型 ID 刷新武器模型。
        /// </summary>
        /// <param name="modelId">武器模型 ID。</param>
        /// <param name="ct">模型加载取消令牌。</param>
        /// <returns>刷新成功返回 <see langword="true"/>。</returns>
        public async UniTask<bool> RefreshWeaponModelAsync(int modelId, CancellationToken ct = default)
        {
            WeaponModelCfg = modelId > 0 ? ModelConfigMgr.Instance.GetWeaponModelCfgOrDefault(modelId) : null;
            if (WeaponModelCfg == null || string.IsNullOrEmpty(WeaponModelCfg.ModelLocation))
            {
                WeaponModelPart?.Destroy();
                WeaponModelPart = null;
                return false;
            }

            return await AddModelAsync(UnitModelType.WeaponModelType, ct);
        }
        
        /// <summary>
        /// 根据指定部位类型创建并加载模型部位。
        /// </summary>
        /// <param name="unitModelType">部位类型。</param>
        /// <param name="ct">模型加载取消令牌。</param>
        /// <returns>加载成功返回 <see langword="true"/>。</returns>
        public async UniTask<bool> AddModelAsync(UnitModelType unitModelType, CancellationToken ct = default)
        {
            switch (unitModelType)
            {
                case UnitModelType.MainModelType:
                    return await AddMainModelAsyncInternal(ct);

                case UnitModelType.WeaponModelType:
                    return await AddWeaponModelAsyncInternal(ct);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 创建并加载主模型部位。
        /// </summary>
        /// <param name="ct">模型加载取消令牌。</param>
        /// <returns>加载成功返回 <see langword="true"/>。</returns>
        private async UniTask<bool> AddMainModelAsyncInternal(CancellationToken ct = default)
        {
            MainModelPart?.Destroy();
            MainModelPart = CreatePart<MainUnitModelPart>(UnitModelType.MainModelType);
            if (MainModelPart == null)
            {
                m_owner?.UnitDummy?.Clear();
                return false;
            }

            var isSuccess = await MainModelPart.LoadModelAsync(MainModelCfg.ModelLocation, ModelRootTransform, ct);
            if (!isSuccess)
            {
                MainModelPart.Destroy();
                MainModelPart = null;
                m_owner?.UnitDummy?.Clear();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建并加载武器模型部位。
        /// 武器部位加载成功后会尝试绑定到主模型挂点。
        /// </summary>
        /// <param name="ct">模型加载取消令牌。</param>
        /// <returns>加载成功返回 <see langword="true"/>。</returns>
        private async UniTask<bool> AddWeaponModelAsyncInternal(CancellationToken ct = default)
        {
            WeaponModelPart?.Destroy();
            WeaponModelPart = CreatePart<UnitWeaponModelPart>(UnitModelType.WeaponModelType);
            if (WeaponModelPart == null)
            {
                return false;
            }

            var isSuccess = await WeaponModelPart.RefreshModel(WeaponModelCfg, ModelRootTransform, ct);
            if (!isSuccess)
            {
                WeaponModelPart.Destroy();
                WeaponModelPart = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 创建指定类型的模型部位实例。
        /// </summary>
        /// <typeparam name="TPart">模型部位类型。</typeparam>
        /// <param name="unitModelType">部位类型。</param>
        /// <returns>创建出的模型部位；不支持时返回 <see langword="null"/>。</returns>
        private TPart CreatePart<TPart>(UnitModelType unitModelType) where TPart : UnitModelPart
            => UnitModelPartFactory.Create(m_owner, unitModelType,
                OnModelCreated, OnModelDestroy, OnBeforeModelDestroy) as TPart;

        /// <summary>
        /// 模型销毁前回调。
        /// 主模型销毁前会先让武器回退到模型根节点，避免主模型删除时武器跟着丢失。
        /// </summary>
        /// <param name="unitModelType">销毁的部位类型。</param>
        private void OnBeforeModelDestroy(UnitModelType unitModelType)
        {
            if (unitModelType == UnitModelType.MainModelType)
            {
                WeaponModelPart?.SetParent(ModelRootTransform, false);
            }
        }

        /// <summary>
        /// 模型销毁完成回调。
        /// </summary>
        /// <param name="unitModelType">销毁的部位类型。</param>
        private void OnModelDestroy(UnitModelType unitModelType)
        {
        }

        /// <summary>
        /// 模型创建完成回调。
        /// 主模型创建完成后会驱动武器重新绑定到最新挂点。
        /// </summary>
        /// <param name="go">创建出的模型对象。</param>
        /// <param name="unitModelType">创建的部位类型。</param>
        private void OnModelCreated(GameObject go, UnitModelType unitModelType)
        {
            if (unitModelType == UnitModelType.MainModelType)
            {
                WeaponModelPart?.RebindToDummy(ModelRootTransform);
            }
        }

        /// <summary>
        /// 获取当前主模型对象。
        /// </summary>
        /// <returns>主模型对象；未加载时返回 null。</returns>
        public GameObject GetMainModelGo() => MainModelPart?.ModelGo;

        /// <summary>
        /// 获取当前武器模型对象。
        /// </summary>
        /// <returns>武器模型对象；未加载时返回 null。</returns>
        public GameObject GetWeaponModelGo() => WeaponModelPart?.ModelGo;

        /// <summary>
        /// 设置整套模型显隐。
        /// </summary>
        /// <param name="active">是否可见。</param>
        public void SetActive(bool active) => ModelRoot?.SetActive(active);

        /// <summary>
        /// 销毁模型容器及其所有部件。
        /// </summary>
        public void Destroy()
        {
            WeaponModelPart?.Destroy();
            WeaponModelPart = null;
            WeaponModelCfg = null;
            MainModelPart?.Destroy();
            MainModelPart = null;
            MainModelCfg = null;
            m_owner?.UnitDummy?.Clear();
            if (ModelRoot != null)
            {
                Object.Destroy(ModelRoot);
                ModelRoot = null;
                ModelRootTransform = null;
            }
        }
    }
}
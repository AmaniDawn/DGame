using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameBattle;
using GameProto;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 武器模型部位。
    /// <para>
    /// 负责武器模型资源加载、挂点重绑，以及武器局部位姿参数应用。
    /// </para>
    /// <para>
    /// 武器部位依赖主模型挂点缓存工作：主模型加载完成后刷新 <see cref="DummyPointCache"/>，
    /// 武器再根据 <see cref="WeaponModelConfig.DummyPoint"/> 绑定到对应挂点。
    /// </para>
    /// <para>
    /// 当主模型切换或销毁时，武器会先临时回退到模型根节点，待主模型新挂点就绪后再重新绑定。
    /// </para>
    /// </summary>
    public class UnitWeaponModelPart : UnitModelPart
    {
        private readonly UnitDisplayComponent m_owner;

        private string m_bindPointName;

        private DummyPointType m_bindPointType = DummyPointType.DM_NONE;

        /// <summary>
        /// 当前武器模型配置。
        /// </summary>
        public WeaponModelConfig ModelConfig { get; private set; }

        /// <summary>
        /// 当前武器绑定挂点类型。
        /// </summary>
        public DummyPointType BindPointType => m_bindPointType;

        /// <summary>
        /// 武器模型部位类型。
        /// </summary>
        public override UnitModelType ModelType => UnitModelType.WeaponModelType;

        /// <summary>
        /// 创建武器模型部位。
        /// </summary>
        /// <param name="owner">所属显示组件。</param>
        /// <param name="onCreate">模型创建回调。</param>
        /// <param name="onDestroy">模型销毁回调。</param>
        /// <param name="onBeforeDestroy">模型销毁前回调。</param>
        public UnitWeaponModelPart(
            UnitDisplayComponent owner,
            Action<GameObject, UnitModelType> onCreate,
            Action<UnitModelType> onDestroy,
            Action<UnitModelType> onBeforeDestroy)
        {
            m_owner = owner;
            m_onCreate = onCreate;
            m_onDestroy = onDestroy;
            m_onBeforeDestroy = onBeforeDestroy;
        }

        /// <summary>
        /// 根据配置刷新武器模型。
        /// </summary>
        /// <param name="modelConfig">武器模型配置。</param>
        /// <param name="fallbackParent">找不到挂点时的回退父节点。</param>
        /// <param name="ct">模型加载取消令牌。</param>
        /// <returns>刷新成功返回 <see langword="true"/>。</returns>
        public async UniTask<bool> RefreshModel(WeaponModelConfig modelConfig, Transform fallbackParent, CancellationToken ct = default)
        {
            ModelConfig = modelConfig;
            if (ModelConfig == null || string.IsNullOrEmpty(ModelConfig.ModelLocation))
            {
                Destroy();
                return false;
            }

            m_bindPointType = ResolveBindPointType(ModelConfig);
            m_bindPointName = ModelConfig.DummyPoint;
            var isSuccess = await LoadModelAsync(ModelConfig.ModelLocation, fallbackParent, ct);
            if (!isSuccess)
            {
                return false;
            }

            RebindToDummy(fallbackParent);
            return true;
        }

        /// <summary>
        /// 将武器重新挂接到指定挂点。
        /// 如果挂点不存在，则回退到模型根节点下。
        /// </summary>
        /// <param name="fallbackParent">找不到挂点时的回退父节点。</param>
        public void RebindToDummy(Transform fallbackParent)
        {
            var dummy = ResolveDummyTransform();
            var targetParent = dummy != null ? dummy : fallbackParent;
            SetParent(targetParent, false);
            ApplyLocalTransform();
        }

        /// <summary>
        /// 解析武器绑定挂点。
        /// 优先按武器配置中的挂点名称解析；解析失败时回退到角色右手武器挂点。
        /// </summary>
        /// <param name="modelConfig">模型配置。</param>
        /// <returns>绑定挂点类型。</returns>
        protected virtual DummyPointType ResolveBindPointType(WeaponModelConfig modelConfig)
        {
            if (modelConfig == null || string.IsNullOrWhiteSpace(modelConfig.DummyPoint))
            {
                return DummyPointType.DM_ACTOR_R_WEAPON;
            }

            return Enum.TryParse(modelConfig.DummyPoint, out DummyPointType pointType)
                ? pointType : DummyPointType.DM_ACTOR_R_WEAPON;
        }

        /// <summary>
        /// 获取武器本地偏移。
        /// 当前默认直接使用武器配置中的 UI 偏移参数。
        /// </summary>
        /// <returns>本地位置偏移。</returns>
        protected virtual Vector3 GetLocalPosition()
        {
            if (ModelConfig == null)
            {
                return Vector3.zero;
            }

            return new Vector3(ModelConfig.UIOffsetX, ModelConfig.UIOffsetY, 0f);
        }

        /// <summary>
        /// 获取武器本地旋转。
        /// 当前默认仅使用配置中的 Z 轴旋转。
        /// </summary>
        /// <returns>本地欧拉角。</returns>
        protected virtual Vector3 GetLocalEulerAngles()
        {
            if (ModelConfig == null)
            {
                return Vector3.zero;
            }

            return new Vector3(0f, 0f, ModelConfig.RotateZ);
        }

        /// <summary>
        /// 获取武器本地缩放。
        /// 当前默认使用配置中的 UI 缩放参数。
        /// </summary>
        /// <returns>本地缩放。</returns>
        protected virtual Vector3 GetLocalScale()
        {
            if (ModelConfig == null || ModelConfig.UIScale <= 0f)
            {
                return Vector3.one;
            }

            return Vector3.one * ModelConfig.UIScale;
        }

        /// <summary>
        /// 武器模型加载完成后的处理。
        /// 加载完成后立即应用一次局部位姿，保证首次显示位置正确。
        /// </summary>
        protected override void OnModelLoaded()
        {
            ApplyLocalTransform();
        }

        /// <summary>
        /// 武器模型销毁前的处理。
        /// 清空配置与挂点状态，避免后续复用时残留旧数据。
        /// </summary>
        protected override void OnBeforeDestroy()
        {
            ModelConfig = null;
            m_bindPointName = null;
            m_bindPointType = DummyPointType.DM_NONE;
        }

        /// <summary>
        /// 应用武器的本地位姿参数。
        /// 该位姿是相对于当前挂接父节点的局部参数。
        /// </summary>
        private void ApplyLocalTransform()
        {
            if (Transform == null)
            {
                return;
            }

            Transform.localPosition = GetLocalPosition();
            Transform.localEulerAngles = GetLocalEulerAngles();
            Transform.localScale = GetLocalScale();
        }

        /// <summary>
        /// 解析当前武器应绑定的挂点节点。
        /// 先按已解析的枚举挂点查找，未命中时再退回到原始挂点名查找。
        /// </summary>
        /// <returns>挂点节点；不存在时返回 <see langword="null"/>。</returns>
        private Transform ResolveDummyTransform()
        {
            if (m_owner?.UnitDummy == null)
            {
                return null;
            }

            var dummy = m_owner.UnitDummy.GetDummyPoint(m_bindPointType);
            if (dummy != null)
            {
                return dummy;
            }

            if (string.IsNullOrWhiteSpace(m_bindPointName))
            {
                return null;
            }

            return m_owner.UnitDummy.GetDummyPoint(m_bindPointName);
        }
    }
}
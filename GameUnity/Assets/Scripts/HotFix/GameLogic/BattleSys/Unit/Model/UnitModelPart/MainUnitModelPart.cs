using System;
using Cysharp.Threading.Tasks;
using GameProto;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 主模型部位。
    /// 负责主角色模型的加载、缩放处理和挂点缓存刷新。
    /// </summary>
    public class MainUnitModelPart : UnitModelPart
    {
        private readonly UnitDisplayComponent m_owner;

        /// <summary>
        /// 当前主模型配置。
        /// </summary>
        public ModelConfig ModelConfig { get; private set; }

        /// <summary>
        /// 主模型部位类型。
        /// </summary>
        public override UnitModelType ModelType => UnitModelType.MainModelType;

        /// <summary>
        /// 创建主模型部位。
        /// </summary>
        /// <param name="owner">所属显示组件。</param>
        /// <param name="onCreate">模型创建回调。</param>
        /// <param name="onDestroy">模型销毁回调。</param>
        /// <param name="onBeforeDestroy">模型销毁前回调。</param>
        public MainUnitModelPart(UnitDisplayComponent owner, Action<GameObject, UnitModelType> onCreate, Action<UnitModelType> onDestroy, Action<UnitModelType> onBeforeDestroy)
        {
            m_owner = owner;
            m_onCreate = onCreate;
            m_onDestroy = onDestroy;
            m_onBeforeDestroy = onBeforeDestroy;
        }
        
        /// <summary>
        /// 根据配置刷新主模型。
        /// </summary>
        /// <param name="modelConfig">主模型配置。</param>
        /// <param name="parent">主模型挂载父节点。</param>
        /// <returns>刷新成功返回 <see langword="true"/>。</returns>
        public async UniTask<bool> RefreshModel(ModelConfig modelConfig, Transform parent)
        {
            ModelConfig = modelConfig;
            if (ModelConfig == null || string.IsNullOrEmpty(ModelConfig.ModelLocation))
            {
                Destroy();
                m_owner?.UnitDummy?.Clear();
                return false;
            }

            SetParent(parent);
            return await LoadModelAsync(ModelConfig.ModelLocation, parent);
        }
        
        /// <summary>
        /// 主模型加载完成后的处理。
        /// </summary>
        protected override void OnModelLoaded()
        {
            if (Transform != null && ModelConfig != null)
            {
                var scale = ModelConfig.ModelScale <= 0f ? 1f : ModelConfig.ModelScale;
                Transform.localScale = Vector3.one * scale;
            }

            m_owner?.UnitDummy?.Refresh(Transform);
        }
        
        /// <summary>
        /// 主模型销毁前的处理。
        /// </summary>
        protected override void OnBeforeDestroy()
        {
            m_owner?.UnitDummy?.Clear();
            ModelConfig = null;
        }
    }
}
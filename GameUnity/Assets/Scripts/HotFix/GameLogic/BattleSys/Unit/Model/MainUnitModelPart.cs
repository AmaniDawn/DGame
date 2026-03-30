using System;
using Cysharp.Threading.Tasks;
using GameProto;
using UnityEngine;

namespace GameLogic
{
    public class MainUnitModelPart : UnitModelPart
    {
        private readonly UnitDisplayComponent m_owner;

        public ModelConfig ModelConfig { get; private set; }

        public override UnitModelType ModelType => UnitModelType.MainModelType;

        public MainUnitModelPart(UnitDisplayComponent owner, Action<GameObject, UnitModelType> onCreate, Action<UnitModelType> onDestroy, Action<UnitModelType> onBeforeDestroy)
        {
            m_owner = owner;
            base.m_onCreate = onCreate;
            base.m_onDestroy = onDestroy;
            base.m_onBeforeDestroy = onBeforeDestroy;
        }
        
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
        
        protected override void OnModelLoaded()
        {
            if (Transform != null && ModelConfig != null)
            {
                var scale = ModelConfig.ModelScale <= 0f ? 1f : ModelConfig.ModelScale;
                Transform.localScale = Vector3.one * scale;
            }

            m_owner?.UnitDummy?.Refresh(Transform);
        }
        
        protected override void OnBeforeDestroy()
        {
            m_owner?.UnitDummy?.Clear();
            ModelConfig = null;
        }
    }
}
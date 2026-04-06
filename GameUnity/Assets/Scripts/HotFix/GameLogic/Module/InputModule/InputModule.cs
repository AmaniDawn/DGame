#if ENABLE_INPUT_SYSTEM

using System.Collections.Generic;

namespace GameLogic
{
    internal sealed class InputModule : DGame.Module, IInputModule, DGame.IUpdateModule
    {
        /// <summary>
        /// 输入组件列表
        /// </summary>
        private List<IInputComponent> m_inputComponents; // 输入组件列表

        /// <summary>
        /// 实体ID映射输入上下文解析器
        /// </summary>
        private Dictionary<int, List<IInputContextLayer>> m_inputContextLayerDict; // 实体ID映射的输入上下文层

        /// <summary>
        /// 输入轴值字典
        /// </summary>
        private readonly Dictionary<InputAxisType, float> m_inputAxisDict = new Dictionary<InputAxisType, float>(); // 当前输入轴值缓存

        public override void OnCreate()
        {
            InputDefine.Initialize();
            m_inputComponents = new List<IInputComponent>(8);
            m_inputContextLayerDict = new Dictionary<int, List<IInputContextLayer>>(8);
        }

        public override void OnDestroy()
        {
            InputDefine.Dispose();
            Clear();
        }

        private void Clear()
        {
            m_inputAxisDict.Clear();

            foreach (var layers in m_inputContextLayerDict.Values)
            {
                foreach (var layer in layers)
                {
                    DGame.MemoryPool.Release(layer);
                }
                layers.Clear();
            }

            foreach (var component in m_inputComponents)
            {
                DGame.MemoryPool.Release(component);
            }
            m_inputComponents.Clear();
            m_inputContextLayerDict.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        public T AddInputComponent<T>() where T : class, IInputComponent, new()
        {
            var component = DGame.MemoryPool.Spawn<T>();
            m_inputComponents.Add(component);
            return component;
        }

        public void RemoveInputComponent<T>() where T : class, IInputComponent, new()
        {
            for (int i = 0; i < m_inputComponents.Count; i++)
            {
                var component = m_inputComponents[i];
                if (component is T)
                {
                    m_inputComponents.RemoveAt(i);
                    DGame.MemoryPool.Release(component);
                    break;
                }
            }
        }

        public T AddInputContextLayer<T>(int entityID) where T : class, IInputContextLayer, new()
        {
            var contextLayer = DGame.MemoryPool.Spawn<T>();
            if (m_inputContextLayerDict.TryGetValue(entityID, out var contextLayers))
            {
                InsertContextLayer(contextLayers, contextLayer);
            }
            else
            {
                m_inputContextLayerDict[entityID] = new List<IInputContextLayer>() { contextLayer };
            }
            return contextLayer;
        }

        private void InsertContextLayer(List<IInputContextLayer> layers, IInputContextLayer contextLayer)
        {
            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer.Priority < contextLayer.Priority)
                {
                    layers.Insert(i, contextLayer);
                    return;
                }
            }
            layers.Add(contextLayer);
        }

        public void RemoveInputContextLayer<T>(int entityID) where T : class, IInputContextLayer, new()
        {
            if (m_inputContextLayerDict.TryGetValue(entityID, out var contextLayers))
            {
                for (int i = 0; i < contextLayers.Count; i++)
                {
                    var contextLayer = contextLayers[i];

                    if (contextLayer is T)
                    {
                        DGame.MemoryPool.Release(contextLayer);
                        contextLayers.RemoveAt(i);
                        break;
                    }
                }

                if (contextLayers.Count == 0)
                {
                    m_inputContextLayerDict.Remove(entityID);
                }
            }
        }

        public void ReceiveInputAction(InputEventType actionType, InputState inputState, double time)
        {
            foreach (var component in m_inputComponents)
            {
                component.ReceiveInputEvent(actionType, inputState, time);
            }
        }

        public void ReceiveInputAxis(InputAxisType axisType, float value)
        {
            m_inputAxisDict[axisType] = value;

            foreach (var component in m_inputComponents)
            {
                component.ReceiveInputAxis(axisType, value);
            }
        }

        public float GetInputAxis(InputAxisType axisType) => m_inputAxisDict.GetValueOrDefault(axisType, 0f);

        public List<IInputContextLayer> GetInputContextLayers(int entityID)
            => m_inputContextLayerDict.GetValueOrDefault(entityID, null);

        public void Enable() => InputDefine.Enable();

        public void Disable() => InputDefine.Disable();

        public bool IsRebinding => InputDefine.IsRebinding;

        public List<InputBindingInfo> GetActionBindings(string actionName, bool includeComposite = false)
            => InputDefine.GetActionBindings(actionName, includeComposite);

        public string GetBindingDisplayString(string actionName, int bindingIndex)
            => InputDefine.GetBindingDisplayString(actionName, bindingIndex);

        public bool StartInteractiveRebind(string actionName, int bindingIndex, System.Action onComplete = null,
            System.Action onCancel = null, bool excludeMouse = true)
            => InputDefine.StartInteractiveRebind(actionName, bindingIndex, onComplete, onCancel, excludeMouse);

        public void CancelInteractiveRebind() => InputDefine.CancelInteractiveRebind();

        public void ResetBindingOverride(string actionName, int bindingIndex)
            => InputDefine.ResetBindingOverride(actionName, bindingIndex);

        public void ResetAllBindingOverrides() => InputDefine.ResetAllBindingOverrides();
    }
}

#endif
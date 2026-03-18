using System.Collections.Generic;
using DGame;

namespace GameLogic
{
    internal sealed class InputModule : Module, IInputModule, IUpdateModule
    {
        /// <summary>
        /// 输入组件列表
        /// </summary>
        private List<IInputComponent> m_inputComponents;

        /// <summary>
        /// 实体ID映射输入上下文解析器
        /// </summary>
        private Dictionary<int, List<IInputContextLayer>> m_inputContextLayerDict;

        /// <summary>
        /// 输入轴值字典
        /// </summary>
        private readonly Dictionary<InputAxisType, float> m_inputAxisDict = new Dictionary<InputAxisType, float>();

        public override void OnCreate()
        {
            InputDefine.Initialize();
            m_inputComponents = new List<IInputComponent>(8);
            m_inputContextLayerDict = new Dictionary<int, List<IInputContextLayer>>(8);
        }

        public override void OnDestroy()
        {
            Clear();
        }

        private void Clear()
        {
            m_inputAxisDict.Clear();

            foreach (var layers in m_inputContextLayerDict.Values)
            {
                foreach (var layer in layers)
                {
                    MemoryPool.Release(layer);
                }
                layers.Clear();
            }

            foreach (var component in m_inputComponents)
            {
                MemoryPool.Release(component);
            }
            m_inputComponents.Clear();
            m_inputContextLayerDict.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        public T AddInputComponent<T>() where T : class, IInputComponent, new()
        {
            var component = MemoryPool.Spawn<T>();
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
                    MemoryPool.Release(component);
                    break;
                }
            }
        }

        public T AddInputContextLayer<T>(int entityID) where T : class, IInputContextLayer, new()
        {
            var contextLayer = MemoryPool.Spawn<T>();

            if (m_inputContextLayerDict.TryGetValue(entityID, out var contextLayers))
            {
                contextLayers.Add(contextLayer);
            }
            else
            {
                m_inputContextLayerDict[entityID] = new List<IInputContextLayer>() { contextLayer };
            }
            return contextLayer;
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
                        MemoryPool.Release(contextLayer);
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

        public float GetInputAxis(InputAxisType axisType)
            => m_inputAxisDict.TryGetValue(axisType, out var value) ? value : 0f;

        public List<IInputContextLayer> GetInputContextLayers(int entityID)
            => m_inputContextLayerDict.TryGetValue(entityID, out var contextLayers)
                ? contextLayers : null;

        public void Enable() => InputDefine.Enable();

        public void Disable() => InputDefine.Disable();
    }
}
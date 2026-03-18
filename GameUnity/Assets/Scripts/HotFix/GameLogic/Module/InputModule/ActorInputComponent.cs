using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class ActorInputComponent : IInputComponent
    {
        /// <summary>
        /// 绑定EntityID
        /// </summary>
        public int EntityID { get; set; }

        /// <summary>
        /// 输入指令缓存过期时间
        /// </summary>
        public readonly Dictionary<(InputEventType EventType, InputState State), double> InputExpireConfig = new();
        
        /// <summary>
        /// 输入指令执行优先级
        /// </summary>
        public readonly Dictionary<(InputEventType EventType, InputState State), int> InputPriorityDict = new();
        
        private readonly List<InputEvent> m_inputEvents = new();
        private readonly List<InputCache> m_inputCaches = new();
        private readonly List<InputContextCommand> m_inputContextCommands = new();
        
        private readonly Dictionary<InputAxisType, float> m_inputAxisValueDict = new();
        private readonly Dictionary<InputAxisType, AxisCache> m_inputAxisCacheDict = new();
        private const float AXIS_CACHE_DURATION = 0.1f; // 轴输入缓存时间：100毫秒缓存时间

        public void OnRelease()
        {
            EntityID = 0;
            m_inputEvents.Clear();
            m_inputCaches.Clear();
            m_inputAxisValueDict.Clear();
            m_inputAxisCacheDict.Clear();
            m_inputContextCommands.Clear();
            InputExpireConfig.Clear();
            InputPriorityDict.Clear();
        }

        public void ReceiveInputEvent(InputEventType actionType, InputState state, double time)
        {
            m_inputEvents.Add(new InputEvent(actionType, state, time));
            var layers = GameModule.Input.GetInputContextLayers(EntityID);
            switch (state)
            {
                case InputState.Started:
                    foreach (var layer in layers)
                    {
                        layer.OnStarted();
                    }
                    break;

                case InputState.Performed:
                    foreach (var layer in layers)
                    {
                        layer.OnPerformed();
                    }
                    break;

                case InputState.Canceled:
                    foreach (var layer in layers)
                    {
                        layer.OnCanceled();
                    }
                    break;
            }
        }

        public void ReceiveInputAxis(InputAxisType axisType, float value)
        {
            m_inputAxisValueDict[axisType] = value;

            if (Mathf.Abs(value) > 0.001f)
            {
                var time = m_inputAxisCacheDict.TryGetValue(axisType, out var cache) ? cache.AccumulateTime : 0f;
                m_inputAxisCacheDict[axisType] = new AxisCache(value, time);
            }
        }

        public float GetInputAxis(InputAxisType axisType) => m_inputAxisValueDict.GetValueOrDefault(axisType, 0);

        public void PostProcessInput(float elapseSeconds, float realElapseSeconds)
        {
            UpdateMoveCache(realElapseSeconds);
            
            // 累积输入缓存时间
            for (var i = 0; i < m_inputCaches.Count; i++)
            {
                var inputCache = m_inputCaches[i];
                inputCache.AccumulatedTime += realElapseSeconds;
                m_inputCaches[i] = inputCache;
            }
            
            ClearExpiredInputCache();
            
            m_inputContextCommands.Clear();

            // 先处理输入缓存
            for (int i = 0; i < m_inputCaches.Count; i++)
            {
                var inputCache = m_inputCaches[i];
                var command = GetCommand(inputCache.ActionType, inputCache.InputState, inputCache.EventTime);
                if (command.Command.CommandType != InputCommandType.None)
                {
                    m_inputContextCommands.Add(command);
                }
            }
            
            // 处理输入
            for (int i = 0; i < m_inputEvents.Count; i++)
            {
                var inputEvent = m_inputEvents[i];
                var command = GetCommand(inputEvent.ActionType, inputEvent.InputState, inputEvent.EventTime);
                if (command.Command.CommandType != InputCommandType.None)
                {
                    m_inputContextCommands.Add(command);
                }
            }
            
            var bestCommand = GetMaxPriorityCommand(m_inputContextCommands);
            m_inputContextCommands.Remove(bestCommand);
            m_inputEvents.Clear();
            CacheInput(m_inputContextCommands);
            ExecuteCommand(bestCommand);
        }

        private void CacheInput(List<InputContextCommand> inputContextCommands)
        {
            foreach (var command in inputContextCommands)
            {
                m_inputCaches.Add(
                    new InputCache(command.ActionType, command.InputState, command.EventTime, DateTime.UtcNow.Ticks, 0));
            }
        }

        private InputContextCommand GetCommand(InputEventType eventType, InputState state, double time)
        {
            var layers = GameModule.Input.GetInputContextLayers(EntityID);

            switch (state)
            {
                case InputState.Started:
                    for (var i = 0; i < layers.Count; i++)
                    {
                        var layer = layers[i];
                        var commandStarted = layer.ResolveStarted();
                        if (commandStarted.CommandType != InputCommandType.None)
                        {
                            return new InputContextCommand(eventType, state, commandStarted, time, i);
                        }
                    }
                    break;

                case InputState.Performed:
                    for (var i = 0; i < layers.Count; i++)
                    {
                        var layer = layers[i];
                        var commandPerformed = layer.ResolvePerformed();
                        if (commandPerformed.CommandType != InputCommandType.None)
                        {
                            return new InputContextCommand(eventType, state, commandPerformed, time, i);
                        }
                    }
                    break;

                case InputState.Canceled:
                    for (var i = 0; i < layers.Count; i++)
                    {
                        var layer = layers[i];
                        var commandCanceled = layer.ResolveCanceled();
                        if (commandCanceled.CommandType != InputCommandType.None)
                        {
                            return new InputContextCommand(eventType, state, commandCanceled, time, i);
                        }
                    }
                    break;
            }
            return new InputContextCommand(eventType, state, 
                new GameplayCommand(InputCommandType.None), time, 0);
        }

        private void ClearExpiredInputCache()
        {
            // 清理过期缓存
            for (var i = 0; i < m_inputCaches.Count; i++)
            {
                var cache = m_inputCaches[i];
                var key = (cache.ActionType, cache.InputState);
                if (InputExpireConfig.ContainsKey(key) && cache.AccumulatedTime > InputExpireConfig[key])
                {
                    m_inputCaches.RemoveAt(i);
                }
            }
        }

        private InputContextCommand GetMaxPriorityCommand(List<InputContextCommand> commands)
        {
            var maxPriorityCmd = commands[0];
            for (int i = 1; i < commands.Count; i++)
            {
                var command = commands[i];
                var commandKey = (command.ActionType, command.InputState);
                var maxPriorityKey = (maxPriorityCmd.ActionType, maxPriorityCmd.InputState);
                if (InputPriorityDict.TryGetValue(commandKey, out var commandPriority)
                    && InputPriorityDict.TryGetValue(maxPriorityKey, out var maxPriority)
                    && commandPriority > maxPriority)
                {
                    maxPriorityCmd = command;
                }
            }
            return maxPriorityCmd;
        }

        private void UpdateMoveCache(float realElapseSeconds)
        {
            // 更新轴缓存 并 维护 m_inputAxisValueDict
            var toRemove = new List<InputAxisType>();
            foreach (var kvp in m_inputAxisCacheDict)
            {
                var axisType = kvp.Key;
                var cache = kvp.Value;
                // 累积缓存时间
                var newCache = new AxisCache(cache.LasNonZeroValue, cache.AccumulateTime + realElapseSeconds);
                m_inputAxisCacheDict[axisType] = newCache;
                bool isExist = m_inputAxisValueDict.TryGetValue(axisType, out var curValue);
                
                if (Mathf.Abs(curValue) < 0.001f && newCache.AccumulateTime <= AXIS_CACHE_DURATION)
                {
                    // 如果值为0 且缓存未过期 在字典中保持方向值
                    m_inputAxisValueDict[axisType] = Mathf.Sign(cache.LasNonZeroValue);
                }
                else if(newCache.AccumulateTime > AXIS_CACHE_DURATION)
                {
                    // 如果过期 清理掉
                    toRemove.Add(axisType);
                    if (m_inputAxisValueDict.ContainsKey(axisType) && Mathf.Abs(m_inputAxisValueDict[axisType]) < 0.001f)
                    {
                        m_inputAxisValueDict[axisType] = 0f;
                    }
                }
            }
            foreach (var axisType in toRemove)
            {
                m_inputAxisCacheDict.Remove(axisType);
            }
        }

        /// <summary>
        /// 执行具体的命令操作
        /// </summary>
        /// <param name="command"></param>
        public void ExecuteCommand(InputContextCommand command)
        {
            switch (command.ActionType)
            {
                case InputEventType.Attack:
                    break;

                case InputEventType.Jump:
                    break;

                case InputEventType.Cancel:
                    break;

                case InputEventType.Aim:
                    break;
            }
        }
    }
}
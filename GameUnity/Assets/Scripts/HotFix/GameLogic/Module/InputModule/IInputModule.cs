#if ENABLE_INPUT_SYSTEM

using System;
using System.Collections.Generic;

namespace GameLogic
{
    public interface IInputModule
    {
        /// <summary>
        /// 添加输入处理器实例
        /// </summary>
        /// <typeparam name="T">输入处理器类型</typeparam>
        /// <returns>新创建的输入处理器实例</returns>
        T AddInputComponent<T>() where T : class, IInputComponent, new();

        /// <summary>
        /// 移除指定类型的输入处理器
        /// </summary>
        /// <typeparam name="T">要移除的输入处理器类型</typeparam>
        void RemoveInputComponent<T>() where T : class, IInputComponent, new();

        /// <summary>
        /// 为指定实体添加输入上下文解析器
        /// </summary>
        /// <param name="entityID"></param>
        /// <typeparam name="T">输入上下文解析器</typeparam>
        /// <returns></returns>
        T AddInputContextLayer<T>(int entityID) where T : class, IInputContextLayer, new();

        /// <summary>
        /// 移除指定实体的输入上下文解析器
        /// </summary>
        /// <param name="entityID"></param>
        /// <typeparam name="T">输入上下文解析器</typeparam>
        void RemoveInputContextLayer<T>(int entityID) where T : class, IInputContextLayer, new();

        /// <summary>
        /// 接收来自引擎或者UI的动作输入
        /// </summary>
        /// <param name="actionType">输入动作类型</param>
        /// <param name="inputState">输入状态</param>
        /// <param name="time">事件发生时间</param>
        void ReceiveInputAction(InputEventType actionType, InputState inputState, double time);

        /// <summary>
        /// 接收来自引擎或者UI的轴输入
        /// </summary>
        /// <param name="axisType">输入轴类型</param>
        /// <param name="value">轴输入值</param>
        void ReceiveInputAxis(InputAxisType axisType, float value);

        /// <summary>
        /// 获取指定输入轴的当前值
        /// </summary>
        /// <param name="axisType">输入轴类型</param>
        /// <returns>轴输入值</returns>
        float GetInputAxis(InputAxisType axisType);

        /// <summary>
        /// 获取指定实体的所有输入上下文解析器
        /// </summary>
        /// <param name="entityID">实体ID</param>
        /// <returns>下文解析器列表</returns>
        List<IInputContextLayer> GetInputContextLayers(int entityID);

        /// <summary>
        /// 启用输入系统
        /// </summary>
        void Enable();

        /// <summary>
        /// 禁用输入系统
        /// </summary>
        void Disable();

        /// <summary>
        /// 当前是否处于改键中
        /// </summary>
        bool IsRebinding { get; }

        /// <summary>
        /// 获取指定动作的所有绑定信息
        /// </summary>
        /// <param name="actionName">输入动作名称，例如 <c>Jump</c> 或 <c>Move</c></param>
        /// <param name="includeComposite">是否包含组合绑定本身，例如 <c>2DVector</c> 或 <c>Dpad</c> 节点</param>
        /// <returns>动作当前的绑定信息列表</returns>
        List<InputBindingInfo> GetActionBindings(string actionName, bool includeComposite = false);

        /// <summary>
        /// 获取指定绑定的人类可读名称
        /// </summary>
        /// <param name="actionName">输入动作名称</param>
        /// <param name="bindingIndex">动作内的绑定索引</param>
        /// <returns>显示给玩家的按键名称。若绑定无效则返回空字符串</returns>
        string GetBindingDisplayString(string actionName, int bindingIndex);

        /// <summary>
        /// 开始交互式改键
        /// </summary>
        /// <param name="actionName">输入动作名称</param>
        /// <param name="bindingIndex">动作内的绑定索引</param>
        /// <param name="onComplete">改键完成后的回调</param>
        /// <param name="onCancel">改键取消后的回调</param>
        /// <param name="excludeMouse">是否排除鼠标输入，避免误绑定鼠标移动</param>
        /// <returns>是否成功开始改键。若动作或索引无效，或当前已有改键进行中，则返回 <see langword="false"/></returns>
        bool StartInteractiveRebind(string actionName, int bindingIndex, Action onComplete = null,
            Action onCancel = null, bool excludeMouse = true);

        /// <summary>
        /// 取消当前改键
        /// </summary>
        void CancelInteractiveRebind();

        /// <summary>
        /// 重置单个绑定覆盖
        /// </summary>
        /// <param name="actionName">输入动作名称</param>
        /// <param name="bindingIndex">动作内的绑定索引</param>
        void ResetBindingOverride(string actionName, int bindingIndex);

        /// <summary>
        /// 重置所有绑定覆盖
        /// </summary>
        void ResetAllBindingOverrides();
    }
}

#endif
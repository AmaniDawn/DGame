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
    }
}
using DGame;

namespace GameLogic
{
    public interface IInputComponent : IMemory
    {
        /// <summary>
        /// 接收输入事件
        /// </summary>
        /// <param name="actionType">输入动作类型</param>
        /// <param name="state">输入阶段</param>
        /// <param name="time">事件发生时间</param>
        void ReceiveInputEvent(InputEventType actionType, InputState state, double time);

        /// <summary>
        /// 接收输入轴变化
        /// </summary>
        /// <param name="axisType">输入轴类型</param>
        /// <param name="value">轴值</param>
        void ReceiveInputAxis(InputAxisType axisType, float value);

        /// <summary>
        /// 获取输入轴当前值
        /// </summary>
        /// <param name="axisType">输入轴类型</param>
        /// <returns>轴值</returns>
        float GetInputAxis(InputAxisType axisType);

        /// <summary>
        /// 执行输入后处理
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间（秒）</param>
        /// <param name="realElapseSeconds">真实流逝时间（秒）</param>
        void PostProcessInput(float elapseSeconds, float realElapseSeconds);
    }
}
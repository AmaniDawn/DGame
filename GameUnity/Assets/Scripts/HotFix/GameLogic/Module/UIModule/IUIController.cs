// ReSharper disable InconsistentNaming
namespace GameLogic
{
    /// <summary>
    /// UI 控制器接口，用于定义 UI 消息注册行为。
    /// </summary>
    /// <remarks>
    /// 控制器注册代码由 UIControllerGenerator 在编译期生成，无需手写注册逻辑。
    /// 业务类在块级命名空间 <c>namespace GameLogic { ... }</c> 中直接实现 <c>IUIController</c>，
    /// 保留公共无参构造函数，并实现 <c>RegUIMessage</c> 即可。
    /// <c>UIModule.RegisterAllController</c>、<c>AddUIController</c> 和 <c>m_uiControllers</c>
    /// 位于生成的 <c>UIController_Gen.g.cs</c>，通常不落盘到 Assets，无需查找或补写常规 .cs 实现。
    /// 仅排查生成失败或修改注册机制时，才查看 Tools/Generate Tools/SourceGenerator/UIControllerGenerator。
    /// </remarks>
    public interface IUIController
    {
        /// <summary>
        /// 注册 UI 消息处理器。
        /// 由业务控制器实现，生成的注册代码会自动调用此方法。
        /// </summary>
        void RegUIMessage();
    }
}
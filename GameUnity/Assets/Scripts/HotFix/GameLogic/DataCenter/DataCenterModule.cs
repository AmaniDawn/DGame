namespace GameLogic
{
    /// <summary>
    /// 数据中心模块接口，定义模块生命周期回调。
    /// </summary>
    public interface IDataCenterModule
    {
        /// <summary>
        /// 模块初始化时调用。
        /// </summary>
        void OnInit();

        /// <summary>
        /// 角色登录成功后调用。
        /// </summary>
        void OnRoleLogin();

        /// <summary>
        /// 角色登出时调用。
        /// </summary>
        void OnRoleLogout();

        /// <summary>
        /// 每帧更新时调用。
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// 主玩家地图切换时调用。
        /// </summary>
        void OnMainPlayerMapChange();
    }

    /// <summary>
    /// 数据中心模块基类，提供单例模式和生命周期虚方法。
    /// </summary>
    /// <remarks>
    /// 模块由 DataCenterModuleGenerator 在编译期自动注册，无需手写注册逻辑。
    /// 业务类在块级命名空间 <c>namespace GameLogic { ... }</c> 中直接继承
    /// <c>DataCenterModule&lt;自身类型&gt;</c>，按需重写生命周期回调即可。
    /// <c>DataCenterSys.InitModule</c> 和 <c>RegisterModule</c> 的实现位于生成的
    /// <c>DataCenterModule_Gen.g.cs</c>，通常不落盘到 Assets，无需查找或补写常规 .cs 实现。
    /// 仅排查生成失败或修改注册机制时，才查看 Tools/Generate Tools/SourceGenerator/DataCenterModuleGenerator。
    /// </remarks>
    /// <typeparam name="T">模块类型</typeparam>
    public abstract class DataCenterModule<T> : IDataCenterModule where T : new()
    {
        private static T m_instance;

        /// <summary>
        /// 获取模块单例实例。
        /// </summary>
        public static T Instance => m_instance != null ? m_instance : m_instance = new T();

        /// <summary>
        /// 模块初始化时调用。
        /// </summary>
        public virtual void OnInit() { }

        /// <summary>
        /// 角色登录成功后调用。
        /// </summary>
        public virtual void OnRoleLogin() { }

        /// <summary>
        /// 角色登出时调用。
        /// </summary>
        public virtual void OnRoleLogout() { }

        /// <summary>
        /// 每帧更新时调用。
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// 主玩家地图切换时调用。
        /// </summary>
        public virtual void OnMainPlayerMapChange() { }
    }
}
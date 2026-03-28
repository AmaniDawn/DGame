using System;
using System.Collections.Generic;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位工厂组件，负责管理和创建逻辑层单位。
    /// <remarks>使用注册表模式，支持外部注册新的单位类型。</remarks>
    /// </summary>
    public sealed class LogicUnitFactoryComponent : Entity
    {
        /// <summary>
        /// 获取逻辑单位创建器字典。
        /// <remarks>键为单位类型枚举，值为创建委托</remarks>
        /// </summary>
        internal readonly Dictionary<UnitType, Func<LogicUnit>> LogicUnitCreators = new();
    }
}
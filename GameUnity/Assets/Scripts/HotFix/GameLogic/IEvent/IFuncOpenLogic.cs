using DGame;
using GameProto;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IFuncOpenLogic
    {
        /// <summary>
        /// 系统功能开放数据变更
        /// </summary>
        void OnFuncOpenDataChange();
        
        /// <summary>
        /// 新功能开启
        /// </summary>
        /// <param name="funcType">新功能类型</param>
        void OnFuncOpen(FuncType funcType);
    }
}
using System;

namespace DGame
{
    public interface IProcedureModule
    {
        /// <summary>
        /// 当前流程
        /// </summary>
        ProcedureBase CurProcedure { get; }

        /// <summary>
        /// 当前流程持续时间
        /// </summary>
        float CurProcedureTime { get; }

        /// <summary>
        /// 初始化流程管理器
        /// </summary>
        /// <param name="fsmModule"></param>
        /// <param name="procedures"></param>
        void Initialize(IFsmModule fsmModule, params ProcedureBase[] procedures);

        /// <summary>
        /// 开始流程
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void StartProcedure<T>() where T : ProcedureBase;

        /// <summary>
        /// 开始流程
        /// </summary>
        /// <param name="procedureType"></param>
        void StartProcedure(Type procedureType);

        /// <summary>
        /// 是否存在流程
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        bool ContainsProcedure<T>() where T : ProcedureBase;

        /// <summary>
        /// 是否存在流程
        /// </summary>
        /// <param name="procedureType"></param>
        /// <returns></returns>
        bool ContainsProcedure(Type procedureType);

        /// <summary>
        /// 获取流程
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ProcedureBase GetProcedure<T>() where T : ProcedureBase;

        /// <summary>
        /// 获取流程
        /// </summary>
        /// <param name="procedureType"></param>
        /// <returns></returns>
        ProcedureBase GetProcedure(Type procedureType);

        /// <summary>
        /// 重启流程
        /// </summary>
        /// <param name="procedures"></param>
        /// <returns></returns>
        bool RestartProcedure(params ProcedureBase[] procedures);
    }
}
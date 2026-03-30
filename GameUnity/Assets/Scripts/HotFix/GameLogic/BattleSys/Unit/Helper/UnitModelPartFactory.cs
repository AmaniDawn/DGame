using System;
using DGame;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 单位模型部位工厂。
    /// 负责按部位类型创建对应的模型部位实例。
    /// </summary>
    public static class UnitModelPartFactory
    {
        /// <summary>
        /// 创建指定类型的模型部位。
        /// </summary>
        /// <param name="owner">所属显示组件。</param>
        /// <param name="unitModelType">部位类型。</param>
        /// <param name="onCreate">模型创建完成回调。</param>
        /// <param name="onDestroy">模型销毁完成回调。</param>
        /// <param name="onBeforeDestroy">模型销毁前回调。</param>
        /// <returns>创建出的模型部位；不支持的类型返回 <see langword="null"/>。</returns>
        public static UnitModelPart Create(UnitDisplayComponent owner, UnitModelType unitModelType,
            Action<GameObject, UnitModelType> onCreate, Action<UnitModelType> onDestroy, Action<UnitModelType> onBeforeDestroy)
        {
            switch (unitModelType)
            {
                case UnitModelType.MainModelType:
                    return new MainUnitModelPart(owner, onCreate, onDestroy, onBeforeDestroy);
                
                default:
                    DLogger.Warning($"UnitModelPartFactory Error ModelType: {unitModelType.ToString()}");
                    return null;
            }
        }
    }
}
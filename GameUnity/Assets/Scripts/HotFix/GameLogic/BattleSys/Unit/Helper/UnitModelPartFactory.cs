using System;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public static class UnitModelPartFactory
    {
        public static UnitModelPart Create(UnitDisplayComponent owner, UnitModelType unitModelType,
            Action<GameObject, UnitModelType> onCreate, Action<UnitModelType> onDestroy, Action<UnitModelType> onBeforeDestroy)
        {
            switch (unitModelType)
            {
                case UnitModelType.MainModelType:
                    return new MainUnitModelPart(owner, onCreate, onDestroy, onBeforeDestroy);
                
                default:
                    DLogger.Warning("ActorModelFactory Error ModelType");
                    return null;
            }
        }
    }
}
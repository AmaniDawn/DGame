using System;
using System.Collections.Generic;
using Fantasy.Entitas;
using GameBattle;

namespace GameLogic
{
    public class RenderUnitFactoryComponent : Entity, IRenderUnitFactory
    {
        internal readonly Dictionary<Type, Func<LogicUnit, IRenderUnit>> RenderUnitCreators = new();

        public IRenderUnit Create(in LogicUnit logicUnit)
        {
            var type = logicUnit.GetType();

            if (RenderUnitCreators.TryGetValue(type, out var creator))
            {
                return creator(logicUnit);
            }

            throw new NotSupportedException($"Unsupported logic unit: {type.Name}");
        }
    }
}
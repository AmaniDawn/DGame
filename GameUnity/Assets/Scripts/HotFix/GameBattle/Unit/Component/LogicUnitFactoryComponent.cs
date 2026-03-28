using System;
using System.Collections.Generic;
using Fantasy.Entitas;

namespace GameBattle
{
    public class LogicUnitFactoryComponent : Entity
    {
        internal readonly Dictionary<UnitType, Func<LogicUnit>> LogicUnitCreators = new();
    }
}
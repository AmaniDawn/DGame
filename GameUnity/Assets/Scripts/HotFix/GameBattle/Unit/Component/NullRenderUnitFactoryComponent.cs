using Fantasy.Entitas;

namespace GameBattle
{
    public class NullRenderUnitFactoryComponent : Entity, IRenderUnitFactory
    {
        public IRenderUnit Create(in LogicUnit logicUnit) => null;
    }
}
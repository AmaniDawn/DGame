using Fantasy.Entitas;

namespace GameBattle
{
    public interface IRenderUnitFactory : IEntity
    {
        IRenderUnit Create(in LogicUnit logicUnit);
    }
}
namespace GameBattle
{
    [System.Flags]
    public enum UnitStateDirtyFlags
    {
        None = 0,
        Transform = 1 << 0,
        Attr = 1 << 1,
        State = 1 << 2,
        Target = 1 << 3,
        Buff = 1 << 4,
        Skill = 1 << 5,
    }
}
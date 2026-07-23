
public enum EStateTyep
{
    None = 0,
    Idle,
    Move,
    Attack,
}

public abstract class StateBase
{
    private UnitLogicBase ub;
    public UnitLogicBase UnitLogic => ub;

    public StateBase(UnitLogicBase ub)
    {
        this.ub = ub;
    }

    public abstract EStateTyep StateType { get; }

    public abstract void EnterState(params object[] objects);

    public abstract void UpdateState(float dt);

    public abstract void ExitState();
}

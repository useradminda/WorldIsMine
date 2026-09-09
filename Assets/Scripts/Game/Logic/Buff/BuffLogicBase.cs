
public abstract class BuffLogicBase
{
    protected BuffLogicMachine mBuffLogicMachie;

    private float time;

    protected UnitLogicBase mUnitLogic;
    public BuffLogicBase(UnitLogicBase unitLoigc, BuffLogicMachine buffLogicMachie, int cfgId)
    {
        this.mUnitLogic = unitLoigc;
        this.mBuffLogicMachie = buffLogicMachie;
    }

    public abstract void Enter();

    public void Update(float dt)
    {
        if (time < 0)
            mBuffLogicMachie.ExitBuff(this);
        time -= dt;
    }

    public abstract void Exit();
}

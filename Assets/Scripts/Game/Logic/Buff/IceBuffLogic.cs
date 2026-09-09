
public class IceBuffLogic : BuffLogicBase
{
    private int value = 0;
    public IceBuffLogic(UnitLogicBase unitLoigc, BuffLogicMachine buffLogicMachie, int cfgId) : base(unitLoigc, buffLogicMachie, cfgId)
    {
    }

    public override void Enter()
    {
        value = mUnitLogic.LogicRatio - System.Convert.ToInt32(mUnitLogic.LogicRatio * 0.1f);
        mUnitLogic.SetLogicRatio(-value);
    }

    public override void Exit()
    {
        mUnitLogic.SetLogicRatio(value);
    }
}

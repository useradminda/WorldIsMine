
public class IceBuffLogic : BuffLogicBase
{
    private float speedDelta;

    public IceBuffLogic(UnitLogicBase unitLoigc, BuffLogicMachine buffLogicMachie, int cfgId) : base(unitLoigc, buffLogicMachie, cfgId)
    {
    }

    /// <summary>按照配置百分比降低单位逻辑速度。</summary>
    public override void Enter()
    {
        speedDelta = BuffCfg.value;
        mUnitLogic.ChangeLogicRatio(speedDelta);
    }

    /// <summary>移除冰冻时恢复本Buff降低的逻辑速度。</summary>
    public override void Exit()
    {
        mUnitLogic.ChangeLogicRatio(-speedDelta);
        speedDelta = 0f;
    }
}

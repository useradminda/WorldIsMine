
public class IceBuffLogic : BuffLogicBase
{
    private int value = 0;

    public IceBuffLogic(UnitLogicBase unitLoigc, BuffLogicMachine buffLogicMachie, int cfgId) : base(unitLoigc, buffLogicMachie, cfgId)
    {
    }

    /// <summary>按照配置百分比降低单位逻辑速度。</summary>
    public override void Enter()
    {
        value = UnityEngine.Mathf.Clamp(BuffCfg.value, 0, 90);
        mUnitLogic.SetLogicRatio(-value);
    }

    /// <summary>移除冰冻时恢复本Buff降低的逻辑速度。</summary>
    public override void Exit()
    {
        mUnitLogic.SetLogicRatio(value);
    }
}

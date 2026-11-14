/// <summary>
/// 加速 Buff，按配置值调整单位的逻辑速度倍率。
/// </summary>
public class SpeedUpBuffLogic : BuffLogicBase
{
    public SpeedUpBuffLogic(UnitLogicBase unitLogic, BuffLogicMachine buffLogicMachine, int cfgId)
        : base(unitLogic, buffLogicMachine, cfgId)
    {
    }

    public override void Enter()
    {
        mUnitLogic.ChangeLogicRatio(BuffCfg.value);
        if (mUnitLogic.UnitView != null)
        {
            mUnitLogic.UnitView.ActionFlowComponent.SetAnimationSpeed(mUnitLogic.LogicRatio);
        }
    }

    public override void Exit()
    {
        mUnitLogic.ChangeLogicRatio(-BuffCfg.value);
        if (mUnitLogic.UnitView != null)
        {
            mUnitLogic.UnitView.ActionFlowComponent.SetAnimationSpeed(mUnitLogic.LogicRatio);
        }
    }
}

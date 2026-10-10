public class ParalysisBuffLogic : BuffLogicBase
{
    public ParalysisBuffLogic(UnitLogicBase unitLogic, BuffLogicMachine buffLogicMachine, int cfgId) : base(unitLogic, buffLogicMachine, cfgId)
    {
    }

    /// <summary>进入麻痹Buff时修改逻辑速度，并暂停ActionFlow动作。</summary>
    public override void Enter()
    {
        mUnitLogic.ChangeLogicRatio(BuffCfg.value);
        if (mUnitLogic.UnitView != null)
        {
            mUnitLogic.UnitView.ParalysisComp.SetParalysis(mUnitLogic.LogicRatio);
        }
    }

    /// <summary>退出麻痹Buff时恢复逻辑速度，并按剩余Buff重设ActionFlow速度。</summary>
    public override void Exit()
    {
        mUnitLogic.ChangeLogicRatio(-BuffCfg.value);
        if (mUnitLogic.UnitView != null)
        {
            mUnitLogic.UnitView.ParalysisComp.ExitParalysis(mUnitLogic.LogicRatio);
        }
    }
}


public class IceBuffLogic : BuffLogicBase
{
    public IceBuffLogic(UnitLogicBase unitLoigc, BuffLogicMachine buffLogicMachie, int cfgId) : base(unitLoigc, buffLogicMachie, cfgId)
    {
    }

    /// <summary>进入冰冻Buff时直接启用单位的冰冻表现。</summary>
    public override void Enter()
    {
        mUnitLogic.ChangeLogicRatio(BuffCfg.value);
        if (mUnitLogic.UnitView != null)
        {
            mUnitLogic.UnitView.FreezeComp.SetFreeze(mUnitLogic.LogicRatio);
        }
    }

    /// <summary>退出冰冻Buff时恢复单位的正常表现。</summary>
    public override void Exit()
    {
        mUnitLogic.ChangeLogicRatio(-BuffCfg.value);
        if (mUnitLogic.UnitView != null)
        {
            mUnitLogic.UnitView.FreezeComp.ExitFreeze(mUnitLogic.LogicRatio);
        }
    }
}

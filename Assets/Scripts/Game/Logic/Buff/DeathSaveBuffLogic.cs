public class DeathSaveBuffLogic : BuffLogicBase
{
    private bool triggered;

    public DeathSaveBuffLogic(UnitLogicBase unitLogic, BuffLogicMachine buffLogicMachine, int cfgId)
        : base(unitLogic, buffLogicMachine, cfgId)
    {
    }

    public override void Enter()
    {
        triggered = false;
    }

    public override bool TryPreventDeath()
    {
        if (triggered || mUnitLogic == null || mUnitLogic.UnitType != EUnitType.Solider || mUnitLogic.SoliderCfg == null || !UnitFactory.IsHero(mUnitLogic.SoliderCfg.id) || BuffCfg == null)
        {
            return false;
        }

        triggered = true;
        mUnitLogic.Prop.RestoreMaxHpPercent(BuffCfg.value);
        return true;
    }

    public override void Exit()
    {
    }
}

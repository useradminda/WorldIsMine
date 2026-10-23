public class LastStandBuffLogic : BuffLogicBase
{
    private bool triggered;

    public LastStandBuffLogic(
        UnitLogicBase unitLogic,
        BuffLogicMachine buffLogicMachine,
        int cfgId)
        : base(unitLogic, buffLogicMachine, cfgId)
    {
    }

    public override void Enter()
    {
        triggered = false;
    }

    public override void ModifyDamage(ref int damage)
    {
        if (damage >= 0 || mUnitLogic == null || mUnitLogic.IsDead ||
            mUnitLogic.UnitType != EUnitType.Solider ||
            mUnitLogic.SoliderCfg == null ||
            !UnitFactory.IsHero(mUnitLogic.SoliderCfg.id))
        {
            return;
        }

        if (triggered)
        {
            damage = 0;
            return;
        }

        int hpAfterDamage = mUnitLogic.Prop.Hp + damage;
        if (hpAfterDamage <= 1)
        {
            damage = 1 - mUnitLogic.Prop.Hp;
            triggered = true;
        }
    }

    public override void Exit()
    {
    }
}

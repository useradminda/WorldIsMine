public class TriggerBeforeDieSkill : SkillLogicBase
{
    public override bool IsTriggerBeforeDie => true;

    public TriggerBeforeDieSkill(UnitLogicBase unitLogic, SkillCfg skillCfg)
        : base(unitLogic, skillCfg)
    {
    }

    public override bool TryTriggerBeforeDie()
    {
        if (CurCD > 0f || UnitLogic == null || UnitLogic.IsDead)
        {
            return false;
        }
        SkillDoEffect(UnitLogic, null);
        return true;
    }

    public override void OnSkillDoEffect(UnitLogicBase target, System.Collections.Generic.List<int> resultUnitIndexList)
    {
        ApplyBuffs(UnitLogic);
    }
}

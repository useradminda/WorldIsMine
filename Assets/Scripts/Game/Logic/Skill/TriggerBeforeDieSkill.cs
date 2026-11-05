/// <summary>
/// 死亡前触发技能类型。
/// 适用于单位即将死亡时自动触发一次技能效果，例如给自身添加 Buff 或产生特殊反击。
/// </summary>
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

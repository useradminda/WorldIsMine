
public class SkillCloseSkill : SkillLogicBase
{
    public SkillCloseSkill(UnitLogicBase ulb, SkillCfg skillCfg) : base(ulb, skillCfg)
    {
    }

    /// <summary>近战命中时上报双方接触位置，包含本次攻击导致目标死亡的情况。</summary>
    public override void OnSkillDoEffect()
    {
        UnitLogicBase target = SkillSearchTarget;
        if (target == null || target.IsDead || UnitLogic.IsDead)
        {
            return;
        }

        UnityEngine.Vector3 contact = (UnitLogic.CurPos + target.GetClosestPoint(UnitLogic.CurPos)) * 0.5f;
        BattleClashEffectManager.Instance.ReportContact(contact);
        base.OnSkillDoEffect();
    }
}

using System.Globalization;
using UnityEngine;

public class KillNowSkill : SkillLogicBase
{
    private const float DefaultKillThreshold = 0.15f;

    public KillNowSkill(UnitLogicBase unitLogic, SkillCfg skillCfg)
        : base(unitLogic, skillCfg)
    {
    }

    public override bool CanUseWithTarget(UnitLogicBase target)
    {
        return IsKillableHero(target);
    }

    public override void OnSkillDoEffect(UnitLogicBase target, System.Collections.Generic.List<int> resultUnitIndexList)
    {
        if (!IsKillableHero(target))
        {
            return;
        }

        target.KillNow(SkillCfg.dieType, target.GetClosestPoint(UnitLogic.CurPos));
    }

    private bool IsKillableHero(UnitLogicBase target)
    {
        if (target == null || target.IsDead || target.SoliderCfg == null || !UnitFactory.IsHero(target.SoliderCfg.id))
        {
            return false;
        }

        int maxHp = target.Prop.MaxHp;
        if (maxHp <= 0)
        {
            return false;
        }

        float threshold = GetKillThreshold();
        return target.Prop.Hp < maxHp * threshold;
    }

    private float GetKillThreshold()
    {
        if (float.TryParse(SkillCfg.skillParam, NumberStyles.Float, CultureInfo.InvariantCulture, out float threshold))
        {
            return Mathf.Clamp01(threshold);
        }

        return DefaultKillThreshold;
    }
}

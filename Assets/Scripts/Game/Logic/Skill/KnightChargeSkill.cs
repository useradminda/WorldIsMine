using System.Collections.Generic;

/// <summary>
/// 骑士出生冲锋的伤害技能，负责对一次冲锋命中的全部敌人结算伤害和 Buff。
/// </summary>
public class KnightChargeSkill : SkillLogicBase
{
    private readonly HashSet<int> damagedUnitIds = new HashSet<int>();

    public KnightChargeSkill(UnitLogicBase unitLogic, SkillCfg skillCfg)
        : base(unitLogic, skillCfg)
    {
    }

    /// <summary>
    /// 对本次冲锋范围内的全部敌人各结算一次伤害，返回是否命中目标。
    /// </summary>
    public bool ApplyChargeDamage(List<UnitLogicBase> targets)
    {
        if (UnitLogic == null || UnitLogic.IsDead || SkillCfg == null || targets == null || targets.Count == 0)
        {
            return false;
        }

        damagedUnitIds.Clear();
        int attackDamage = GetAttackDamageSnapshot();
        bool hitAnyTarget = false;
        for (int i = 0; i < targets.Count; i++)
        {
            UnitLogicBase target = targets[i];
            if (target == null || target.IsDead || target.CampType == UnitLogic.CampType || !damagedUnitIds.Add(target.UId))
            {
                continue;
            }

            BattleLogicDamageTools.DoDamage(
                UnitLogic,
                target,
                GetFinalDamage(attackDamage, target),
                target.UId,
                SkillCfg.dieType,
                target.CurPos);
            ApplyBuffs(target);
            hitAnyTarget = true;
        }

        return hitAnyTarget;
    }
}

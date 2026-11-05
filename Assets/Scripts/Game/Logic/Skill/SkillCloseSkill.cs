using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 普通近战技能类型。
/// 适用于近距离单目标攻击，由基类完成伤害、范围追加伤害和 Buff 处理。
/// </summary>
public class SkillCloseSkill : SkillLogicBase
{
    private float delayedEffectTime;
    private UnitLogicBase delayedTarget;
    private List<int> delayedResultUnitIndexList;

    public SkillCloseSkill(UnitLogicBase ulb, SkillCfg skillCfg) : base(ulb, skillCfg)
    {
    }

    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        float triggerDelay = GetTriggerSkillTime();
        if (triggerDelay <= 0f)
        {
            base.OnSkillDoEffect(target, resultUnitIndexList);
            return;
        }

        delayedEffectTime = triggerDelay;
        delayedTarget = target;
        delayedResultUnitIndexList = resultUnitIndexList != null
            ? new List<int>(resultUnitIndexList)
            : null;
    }

    public override void SkillUpdate(float dt)
    {
        base.SkillUpdate(dt);
        if (delayedEffectTime <= 0f)
        {
            return;
        }

        if (UnitLogic == null || UnitLogic.IsDead)
        {
            ClearDelayedEffect();
            return;
        }

        delayedEffectTime -= dt * UnitLogic.LogicRatio;
        if (delayedEffectTime <= 0f)
        {
            UnitLogicBase target = delayedTarget;
            List<int> resultUnitIndexList = delayedResultUnitIndexList;
            ClearDelayedEffect();
            base.OnSkillDoEffect(target, resultUnitIndexList);
        }
    }

    private void ClearDelayedEffect()
    {
        delayedEffectTime = 0f;
        delayedTarget = null;
        delayedResultUnitIndexList = null;
    }

    private float GetTriggerSkillTime()
    {
        if (SkillCfg == null)
        {
            return 0f;
        }
        return SkillCfg.triigerSkillTime;
    }
}

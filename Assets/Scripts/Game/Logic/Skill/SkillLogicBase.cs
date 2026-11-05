using System.Collections.Generic;

/// <summary>
/// 所有技能逻辑的基类。
/// 负责保存技能配置、施法者引用，并提供目标判定、伤害和 Buff 结算等通用能力。
/// </summary>
public class SkillLogicBase
{
    public float SkillSearchRange => SkillCfg.searchRange;

    private readonly UnitLogicBase unitLogic;
    public UnitLogicBase UnitLogic => unitLogic;

    private readonly SkillCfg skillCfg;
    public SkillCfg SkillCfg => skillCfg;

    public bool BNormalSkill => skillCfg.normal == 1;

    /// <summary>技能是否需要战斗目标后才能释放。</summary>
    public bool RequiresTarget => skillCfg != null && skillCfg.needTarget != 0;
    public bool TargetsSelf => skillCfg != null && skillCfg.needTarget == 2;
    public bool RequiresEnemyTarget => skillCfg != null && (skillCfg.needTarget == 1 || skillCfg.needTarget == 3);
    public bool RequiresHeroTarget => skillCfg != null && skillCfg.needTarget == 3;
    public virtual bool IsTriggerBeforeDie => false;

    public virtual bool CanUseWithTarget(UnitLogicBase target)
    {
        return true;
    }

    /// <summary>
    /// 是否需要为范围技能执行一次地图初筛。
    /// </summary>
    public virtual bool RequiresAreaSearch => skillCfg != null && skillCfg.skillArea > 0f;

    /// <summary>
    /// 范围初筛中心点，默认使用施法者当前位置。
    /// </summary>
    public virtual UnityEngine.Vector3 GetAreaSearchPosition()
    {
        return unitLogic.CurPos;
    }

    /// <summary>
    /// 范围初筛半径，默认使用技能表中的范围。
    /// </summary>
    public virtual float GetAreaSearchRadius()
    {
        return skillCfg != null ? skillCfg.skillArea : 0f;
    }

    private float curCD;
    public float CurCD => curCD;

    private readonly List<UnitLogicBase> targetList = new List<UnitLogicBase>();
    public List<UnitLogicBase> TargetList => targetList;

    public string skillGUID;

    public SkillLogicBase(UnitLogicBase ulb, SkillCfg skillCfg)
    {
        skillGUID = System.Guid.NewGuid().ToString();
        unitLogic = ulb;
        this.skillCfg = skillCfg;
    }

    public virtual void SkillUpdate(float dt)
    {
        if (unitLogic.IsDead)
        {
            return;
        }
        if (curCD > 0f)
        {
            curCD -= dt * unitLogic.LogicRatio;
        }
    }

    public void SkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        PlayStartEffect();
        SkillResetCD();
        OnSkillDoEffect(target, resultUnitIndexList);
    }

    public virtual void SkillRelease()
    {
        targetList.Clear();
    }

    public virtual bool TryTriggerBeforeDie()
    {
        return false;
    }

    public virtual void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (unitLogic == null || unitLogic.IsDead || skillCfg == null)
        {
            return;
        }

        if (skillCfg.needTarget == 2)
        {
            // needTarget=2 表示技能目标固定为施法者自身，直接使用施法者，避免外部传入错误目标。
            ApplyBuffs(unitLogic);
            return;
        }

        if (target == null || target.IsDead)
        {
            return;
        }
        int attackDamage = GetAttackDamageSnapshot();
        BattleLogicDamageTools.DoDamage(unitLogic, target, GetFinalDamage(attackDamage, target), target.UId, skillCfg.dieType, UnityEngine.Vector3.zero);
        ApplyBuffs(target);
        if (skillCfg.skillArea > 0 && resultUnitIndexList != null)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                int unitIndex = resultUnitIndexList[i];
                if (unitIndex < 0 || unitIndex >= UnitManager.Instance.UnitList.Count || unitIndex == target.Index)
                {
                    continue;
                }
                UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[unitIndex];
                if (tarUnitLogic == null || tarUnitLogic.IsDead || tarUnitLogic.CampTypeInt == unitLogic.CampTypeInt)
                {
                    continue;
                }
                BattleLogicDamageTools.DoDamage(unitLogic, tarUnitLogic, GetFinalDamage(attackDamage, tarUnitLogic), tarUnitLogic.UId, skillCfg.dieType, UnityEngine.Vector3.zero);
                ApplyBuffs(tarUnitLogic);
            }
        }
    }

    public void ApplyBuffs(UnitLogicBase targetUnit)
    {
        if (targetUnit == null || targetUnit.IsDead || skillCfg.buffId == null)
        {
            return;
        }
        for (int i = 0; i < skillCfg.buffId.Length; i++)
        {
            int buffId = skillCfg.buffId[i];
            if (buffId > 0)
            {
                targetUnit.AddBuff(buffId);
            }
        }
    }

    public void SkillResetCD()
    {
        curCD = skillCfg.cd;
    }

    public void SkillClearCD()
    {
        curCD = 0f;
    }

    public int GetAttackDamageSnapshot()
    {
        return unitLogic.GetAttackDamage(skillCfg.skillDamageRate);
    }

    protected int GetFinalDamage(int attackDamage, UnitLogicBase target)
    {
        return -BattleLogicDamageTools.CalcFinalDamage(unitLogic.SoliderCfg.unitType, target.SoliderCfg.unitType, attackDamage, unitLogic.SoliderCfg.restrainValue);
    }

    protected virtual void PlayStartEffect()
    {
        unitLogic.UnitView.PlayEffect(skillCfg.startEffect, unitLogic.CurPos, 5f);
    }
}

using System.Collections.Generic;

public class SkillLogicBase
{
    public float SkillSearchRange => SkillCfg.searchRange;

    private readonly UnitLogicBase unitLogic;
    public UnitLogicBase UnitLogic => unitLogic;

    private readonly SkillCfg skillCfg;
    public SkillCfg SkillCfg => skillCfg;

    public bool BNormalSkill => skillCfg.normal == 1;

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

    public void SkillUpdate(float dt)
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
        playStartEffect();
        SkillResetCD();
        OnSkillDoEffect(target, resultUnitIndexList);
    }

    public virtual void SkillRelease()
    {
        targetList.Clear();
    }

    public virtual void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (unitLogic == null || unitLogic.IsDead || target == null || target.IsDead)
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

    private int GetFinalDamage(int attackDamage, UnitLogicBase target)
    {
        return -BattleLogicDamageTools.CalcFinalDamage(unitLogic.SoliderCfg.unitType, target.SoliderCfg.unitType, attackDamage, unitLogic.SoliderCfg.restrainValue);
    }

    private void playStartEffect()
    {
        unitLogic.UnitView.PlayEffect(skillCfg.startEffect, unitLogic.CurPos, UnityEngine.Vector3.forward, 5f);
    }
}

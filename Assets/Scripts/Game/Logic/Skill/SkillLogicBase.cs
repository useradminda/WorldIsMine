
using System.Collections.Generic;

public class SkillLogicBase
{
    public float SkillSearchRange => SkillCfg.searchRange;

    private UnitLogicBase unitLogic;
    public UnitLogicBase UnitLogic => unitLogic;

    private SkillCfg skillCfg;
    public SkillCfg SkillCfg => skillCfg;

    public bool BNormalSkill => this.skillCfg.normal == 1;

    private float curCD;
    public float CurCD => curCD;

    private List<UnitLogicBase> targetList = new List<UnitLogicBase>();
    public List<UnitLogicBase> TargetList => targetList;

    protected UnitLogicBase skillSearchTarget;
    public UnitLogicBase SkillSearchTarget => skillSearchTarget;

    public string skillGUID;

    public SkillLogicBase(UnitLogicBase ulb, SkillCfg skillCfg)
    {
        skillGUID = System.Guid.NewGuid().ToString();
        unitLogic = ulb;
        this.skillCfg = skillCfg;
        if (BNormalSkill != true)
        {
            SkillResetCD();
        }
    }

    // 更新
    public void SkillUpdate(float dt)
    {
        if (unitLogic.IsDead == true)
            return;
        if (curCD > 0)
        {
            curCD -= dt;
        }
    }

    public void SkillDoEffect(List<int> resultUnitIndexList)
    {
        playStartEffect();
        SkillResetCD();
        OnSkillDoEffect(resultUnitIndexList);
    }

    /// <summary>结束当前技能流程，清理本次搜索结果和待处理状态。</summary>
    public void SkillRelease()
    {
        skillSearchTarget = null;
        searchReqIndex = -1;
        neastIndex = -1;
        randomIndex = -1;
        resultUnitIndexList.Clear();
        targetList.Clear();
    }

    // 执行
    public virtual void OnSkillDoEffect(List<int> resultUnitIndexList)
    {
        UnitLogicBase target = SkillSearchTarget;
        if (unitLogic == null || unitLogic.IsDead || target == null || target.IsDead)
        {
            return;
        }
        int attackDamage = GetAttackDamageSnapshot();
        BattleLogicDamageTools.DoDamage(unitLogic, target, GetFinalDamage(attackDamage, target), target.UId, SkillCfg.dieType, UnityEngine.Vector3.zero);
        ApplyBuffs(target);
        if (SkillCfg.skillArea > 0)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                int unitIndex = resultUnitIndexList[i];
                if (unitIndex != target.Index)
                {
                    UnitLogicBase tarUnitLogic = UnitManager.Instance.UnitList[unitIndex];
                    if (tarUnitLogic == null || tarUnitLogic.IsDead || tarUnitLogic.CampTypeInt == unitLogic.CampTypeInt)
                    {
                        continue;
                    }
                    BattleLogicDamageTools.DoDamage(unitLogic, tarUnitLogic, GetFinalDamage(attackDamage, tarUnitLogic), tarUnitLogic.UId, SkillCfg.dieType, UnityEngine.Vector3.zero);
                    ApplyBuffs(tarUnitLogic);
                }
            }
        }

    }

    /// <summary>把当前技能配置携带的Buff添加到命中目标。</summary>
    public void ApplyBuffs(UnitLogicBase targetUnit)
    {
        if (targetUnit == null || targetUnit.IsDead || SkillCfg.buffId == null)
        {
            return;
        }

        for (int i = 0; i < SkillCfg.buffId.Length; i++)
        {
            int buffId = SkillCfg.buffId[i];
            if (buffId > 0)
            {
                targetUnit.AddBuff(buffId);
            }
        }
    }

    // 重置CD
    public void SkillResetCD()
    {
        curCD = skillCfg.cd;
    }

    /// <summary>
    /// 清除当前技能冷却，使技能可以立即重新搜索并释放。
    /// </summary>
    public void SkillClearCD()
    {
        curCD = 0f;
    }

    protected int searchReqIndex = -1;
    protected int neastIndex = -1;
    protected int randomIndex = -1;
    protected List<int> resultUnitIndexList = new List<int>();

    public void SearchTargetFunc()
    {
        if (curCD <= 0f)
        {
            searchReqIndex = MapCellManager.Instance.RequestSearch(
                unitLogic.CurPos,
                SkillSearchRange,
                unitLogic.OtherCampTypeInt);
        }
    }

    public virtual UnitLogicBase GetSkillSearchTargetSingleResult()
    {
        skillSearchTarget = null;
        if (searchReqIndex < 0)
            return skillSearchTarget;

        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex, ref randomIndex);
        if (resultUnitIndexList.Count > 0)
        {
            skillSearchTarget = UnitManager.Instance.UnitList[neastIndex];
            if (skillSearchTarget == UnitLogic ||
                skillSearchTarget.CampType == UnitLogic.CampType)
            {
                UnityEngine.Debug.LogError("严重错误搜索到自己了!!");
                skillSearchTarget = null;
            }
            searchReqIndex = -1;
            return skillSearchTarget;
        }
        skillSearchTarget = null;
        searchReqIndex = -1;
        return null;
    }

    public int GetAttackDamageSnapshot()
    {
        return unitLogic.GetAttackDamage(SkillCfg.damage);
    }

    private int GetFinalDamage(int attackDamage, UnitLogicBase target)
    {
        return -BattleLogicDamageTools.CalcFinalDamage(unitLogic.SoliderCfg.unitType, target.SoliderCfg.unitType, attackDamage, unitLogic.SoliderCfg.restrainValue);
    }

    public void SetSkillSearchTarget(UnitLogicBase target)
    {
        skillSearchTarget = target;
    }

    private void playStartEffect()
    {
        UnitLogic.UnitView.PlayEffect(skillCfg.startEffect, UnitLogic.CurPos, new UnityEngine.Vector3(0, 0, 1), 1);
    }
}

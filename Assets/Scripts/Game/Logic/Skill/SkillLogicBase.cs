
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

    public void SkillDoEffect()
    {
        playStartEffect();
        SkillResetCD();
        OnSkillDoEffect();
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
    public virtual void OnSkillDoEffect()
    {
       BattleLogicDamageTools.DoDamage(unitLogic, SkillSearchTarget, GetDamage(), SkillSearchTarget.UId, SkillCfg.dieType, UnityEngine.Vector3.zero);
       ApplyBuffs(SkillSearchTarget);
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

    protected int searchReqIndex = -1;
    protected int neastIndex = -1;
    protected int randomIndex = -1;
    protected List<int> resultUnitIndexList = new List<int>();

    public void SearchTargetFunc()
    {
        if (curCD <= 0)
        {
            searchReqIndex = MapCellManager.Instance.RequestSearch(unitLogic.CurPos, SkillSearchRange, unitLogic.OtherCampTypeInt);
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
            if (skillSearchTarget == UnitLogic)
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

    public int GetDamage()
    {
        int attackDamage = unitLogic.GetAttackDamage(SkillCfg.damage);
        int damage = -BattleLogicDamageTools.CalcFinalDamage(unitLogic.SoliderCfg.unitType, SkillSearchTarget.SoliderCfg.unitType, attackDamage, unitLogic.SoliderCfg.restrainValue);
        return damage;
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

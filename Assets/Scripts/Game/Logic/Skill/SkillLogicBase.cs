
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
        if (curCD > 0)
        {
            curCD -= dt;
            //if (curCD < 0)
            //{
            //    if (BNormalSkill)
            //    {
            //        if (SearchTarget != null && SearchTarget.IsDead == false)
            //        {
            //            SkillDoEffect();
            //            SkillResetCD();
            //            playStartEffect();
            //        }
            //    }
            //}
        }
    }

    public void SkillDoEffect()
    {
        playStartEffect();
        SkillResetCD();
        OnSkillDoEffect();
    }

    // 执行
    public virtual void OnSkillDoEffect()
    {
       BattleLogicDamageTools.DoDamage(unitLogic, SkillSearchTarget, GetDamage(), SkillSearchTarget.UId, SkillCfg.dieType, UnityEngine.Vector3.zero);
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
        if (curCD < 0)
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
        int damage = -BattleLogicDamageTools.CalcFinalDamage(unitLogic.SoliderCfg.unitType, SkillSearchTarget.SoliderCfg.unitType, SkillCfg.damage, unitLogic.SoliderCfg.restrainValue);
        return damage;
    }

    private void playStartEffect()
    {
        UnitLogic.UnitView.PlayEffect(skillCfg.startEffect, UnitLogic.CurPos, new UnityEngine.Vector3(0, 0, 1), 1);
    }
}

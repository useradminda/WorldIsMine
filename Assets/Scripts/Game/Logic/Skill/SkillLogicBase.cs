
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class SkillLogicBase
{
    public float SkillSearchRange => SkillCfg.searchRange;

    private UnitLogicBase unitLogic;
    public UnitLogicBase UnitLogic => unitLogic;

    private SkillCfg skillCfg;
    public SkillCfg SkillCfg => skillCfg;

    public bool BNormalSkill => this.skillCfg.normal == 1;

    private float curCD;

    private List<UnitLogicBase> targetList = new List<UnitLogicBase>();
    public List<UnitLogicBase> TargetList => targetList;

    public string skillGUID;

    public SkillLogicBase(UnitLogicBase ulb, SkillCfg skillCfg)
    {
        skillGUID = System.Guid.NewGuid().ToString();
        unitLogic = ulb;
        this.skillCfg = skillCfg;
        SkillResetCD();
    }

    // 进入
    public void SkillEnter()
    {
        SkillDoEffect();
        SkillResetCD();
    }

    // 更新
    public void SkillDoEffectUpdate(float dt)
    {
        if (curCD > 0)
        {
            curCD -= dt;
            if (curCD < 0)
            {
                if (BNormalSkill)
                {
                    SkillDoEffect();
                    SkillResetCD();
                }
            }
        }
    }

    // 执行
    public virtual void SkillDoEffect()
    {
        BattleLogicDamageTools.DoDamage(unitLogic, targetList, this);
    }

    // 重置CD
    public void SkillResetCD()
    {
        curCD = skillCfg.cd;
    }

    // 清理
    public void SkillRefuse()
    {
        SkillResetCD();    
    }

    private int searchReqId = -1;
    List<int> resultUnitIndexList = new List<int>();
    public void SkillSearchTarget()
    {
        targetList.Clear();
        searchReqId = MapCellManager.Instance.RequestSearch(unitLogic.Index, SkillSearchRange, unitLogic.OtherCampTypeInt);
    }

    //public List<UnitLogicBase> SkillSearchTargetBYKd()
    //{
    //    targetList.Clear();
    //    targetList.AddRange(BattleLogicTools.SearchNotMyCampUnits(UnitLogic.CurPos.x, UnitLogic.CurPos.z, SkillSearchRange, UnitLogic.CampType, false));
    //    targetList.Sort((UnitLogicBase a, UnitLogicBase b) =>
    //    {
    //        if ((UnitLogic.CurPos - a.CurPos).sqrMagnitude < (UnitLogic.CurPos - b.CurPos).sqrMagnitude)
    //        {
    //            return 0;
    //        }
    //        else
    //        {
    //            return 1;
    //        }
    //    });
    //    return targetList;
    //}

    public List<UnitLogicBase> GetSkillSearchTargetResult()
    {
        targetList.Clear();
        if (searchReqId < 0)
            return targetList;
        
        resultUnitIndexList.Clear();
        int neastIndex = -1;
        MapCellManager.Instance.GetResult(searchReqId, resultUnitIndexList, ref neastIndex);
       
        if (resultUnitIndexList.Count > 0)
        {
            int index = neastIndex;//resultUnitIndexList[0];
            targetList.Add(UnitManager.Instance.UnitList[index]);
        }
        return targetList;
    }
}

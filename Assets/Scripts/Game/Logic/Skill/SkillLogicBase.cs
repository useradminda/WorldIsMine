
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

    private UnitLogicBase searchTarget;
    public UnitLogicBase SearchTarget => searchTarget;

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
                    if (targetList.Count > 0 && targetList[0].IsDead == false)
                    {
                        SkillDoEffect();
                        SkillResetCD();
                    }
                }
            }
        }
    }

    // 执行
    public virtual void SkillDoEffect()
    {
       
        BattleLogicDamageTools.DoDamage(unitLogic, SearchTarget, GetDamage(), SearchTarget.UId, this);
    }

    // 重置CD
    public void SkillResetCD()
    {
        curCD = skillCfg.cd;
    }

    // 清理
    public void Refuse()
    {
        SkillResetCD();    
    }

    private int searchReqId = -1;
    List<int> resultUnitIndexList = new List<int>();
    private int neastIndex = -1;
    public void SkillSearchTarget()
    {
        targetList.Clear();
        searchReqId = MapCellManager.Instance.RequestSearch(unitLogic.Index, SkillSearchRange, unitLogic.OtherCampTypeInt);
    }

    public UnitLogicBase GetSkillSearchTargetSingleResult()
    {
        searchTarget = null;
        targetList.Clear();
        if (searchReqId < 0)
            return searchTarget;
       
        resultUnitIndexList.Clear();
        neastIndex = -1;
        MapCellManager.Instance.GetResult(searchReqId, resultUnitIndexList, ref neastIndex);
        if (resultUnitIndexList.Count > 0)
        {
            for (int i = 0; i < resultUnitIndexList.Count; i++)
            {
                int unitIndex = resultUnitIndexList[i];
                targetList.Add(UnitManager.Instance.UnitList[unitIndex]);
            }
            searchTarget = UnitManager.Instance.UnitList[neastIndex];// targetList[0];
            searchReqId = -1;
            return searchTarget;
        }
        searchReqId = -1;
        return null;
    }

    public int GetDamage()
    {
        int damage = -BattleLogicDamageTools.CalcFinalDamage(unitLogic.SoliderCfg.unitType, SearchTarget.SoliderCfg.unitType, SkillCfg.damage);
        return damage;
    }
}

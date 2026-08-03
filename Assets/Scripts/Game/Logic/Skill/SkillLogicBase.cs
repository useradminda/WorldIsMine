
using System;
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

    private List<UnitLogicBase> targetList = new List<UnitLogicBase>();
    public List<UnitLogicBase> TargetList => targetList;

    public SkillLogicBase(UnitLogicBase ulb, SkillCfg skillCfg)
    {
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

    public List<UnitLogicBase> SkillSearchTarget()
    {
        targetList.Clear();
        targetList = BattleLogicTools.SearchNotMyCampUnits(UnitLogic.Agenter.pos.x, UnitLogic.Agenter.pos.z, SkillSearchRange, UnitLogic.CampType, true);
        return targetList;
    }

    
}

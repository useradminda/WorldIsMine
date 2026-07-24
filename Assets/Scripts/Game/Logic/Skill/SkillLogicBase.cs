
using System.Collections.Generic;

public class SkillLogicBase
{
    private float skillRange = 0f;
    public float SkillRange => skillRange;

    private UnitLogicBase unitLogic;
    public UnitLogicBase UnitLogic => unitLogic;

    private SkillCfg skillCfg;
    public SkillCfg SkillCfg => skillCfg;

    public bool BNormalSkill => this.skillCfg.normal == 1;

    private float curCD;

    private List<UnitLogicBase> targetList = new List<UnitLogicBase>();

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
    public void SkillDoEffect()
    {
        BattleLogicDamageTools.DoDamage(unitLogic, targetList, this);
    }

    public void SkillResetCD()
    {
        curCD = skillCfg.cd;
    }

    public void SkillRefuse()
    {
        SkillResetCD();    
    }

    public List<UnitLogicBase> SkillSearchTarget()
    {
        targetList.Clear();
        targetList = BattleLogicTools.SearchNotMyCampUnits(UnitLogic.Agenter.pos.x, UnitLogic.Agenter.pos.z, SkillRange, UnitLogic.CampType, true);
        return targetList;
    }
}

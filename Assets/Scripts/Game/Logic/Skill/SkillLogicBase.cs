
public class SkillLogicBase
{
    private float skillRange = 0f;
    public float SkillRange => skillRange;

    private UnitLogicBase unitLogic;
    public UnitLogicBase UnitLogic => unitLogic;

    private SkillCfg skillCfg;

    public bool BNormalSkill => this.skillCfg.normal == 1;

    private float curCD;

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
    public void SkillUpdate(float dt)
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

    // 执行
    public void SkillDoEffect()
    {
        
    }

    public void SkillResetCD()
    {
        curCD = skillCfg.cd;
    }

    public void Refuse()
    {
        
    }
}

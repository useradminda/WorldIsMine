using UnityEngine;

public class SkillRemoteSkill : SkillLogicBase
{
    public SkillRemoteSkill(UnitLogicBase ulb, SkillCfg skillCfg) : base(ulb, skillCfg)
    {
    }

    public override void SkillDoEffect()
    {
        createFlyObject();
    }

    private void createFlyObject()
    {
        int flyCfgId = SkillCfg.flyObjectId;
        Vector3 oriPos = UnitLogic.CurPos;
        Vector3 tarPos = SearchTarget.CurPos;
        UnitFactory.CreateFlyObjectLogic(flyCfgId, oriPos, tarPos, UnitLogic, TargetList, SearchTarget, this, GetDamage());
    }
}

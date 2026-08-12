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
        int flyId = SkillCfg.flyObjectId;
        Vector3 oriPos = UnitLogic.CurPos + new Vector3(0, 0.5f, 0);
        Vector3 tarPos = TargetList[0].CurPos;

        FlyObjectLogicBase flyObjectLogic = UnitFactory.CreateFlyObjectLogic(flyId, oriPos, tarPos, UnitLogic, TargetList, this);
    }
}

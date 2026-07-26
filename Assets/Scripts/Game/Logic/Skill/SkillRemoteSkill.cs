using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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
        float3 oriPos = UnitLogic.CurPos + new float3(0, 0.5f, 0);
        float3 tarPos = mTargetList[0].CurPos;

        FlyObjectLogicBase flyObjectLogic = UnitFactory.CreateFlyObjectLogic(flyId, oriPos, tarPos, UnitLogic, mTargetList, this);

    }
}

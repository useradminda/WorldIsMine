using System.Collections.Generic;
using UnityEngine;

public class SkillRemoteSkill : SkillLogicBase
{
    public SkillRemoteSkill(UnitLogicBase ulb, SkillCfg skillCfg) : base(ulb, skillCfg)
    {
    }

    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (target == null || target.IsDead)
        {
            return;
        }
        int flyCfgId = SkillCfg.flyObjectId;
        Vector3 oriPos = UnitLogic.CurPos;
        Vector3 tarPos = target.GetClosestPoint(UnitLogic.CurPos);
        UnitFactory.CreateFlyObjectLogic(flyCfgId, oriPos, tarPos, UnitLogic, TargetList, target, this, GetAttackDamageSnapshot());
    }
}

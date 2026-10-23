using System.Collections.Generic;
using UnityEngine;

public class SkillRemoteSkill : SkillLogicBase
{
    public SkillRemoteSkill(UnitLogicBase ulb, SkillCfg skillCfg) : base(ulb, skillCfg)
    {
    }

    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (SkillCfg.needTarget == 2)
        {
            // 自身目标技能不创建飞行物，直接给施法者添加Buff。
            ApplyBuffs(UnitLogic);
            return;
        }

        if (target == null || target.IsDead)
        {
            return;
        }
        int flyCfgId = SkillCfg.flyObjectId;
        Vector3 oriPos = UnitLogic.CurPos;
        Vector3 tarPos = target.GetClosestPoint(UnitLogic.CurPos);
        FlyObjectFactory.CreateFlyObjectLogic(flyCfgId, oriPos, tarPos, UnitLogic, TargetList, target, this, GetAttackDamageSnapshot());
    }
}

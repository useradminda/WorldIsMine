using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates a rectangular runtime RVO obstacle in front of the caster.
/// skillParam format: length,width,forwardDistance. The visual and lifetime come from flyObjectId 14.
/// </summary>
public class CreateWallSkill : SkillLogicBase
{
    public CreateWallSkill(UnitLogicBase unitLogic, SkillCfg skillCfg)
        : base(unitLogic, skillCfg)
    {
    }

    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (UnitLogic == null || UnitLogic.IsDead)
        {
            return;
        }
        FlyObjectFactory.CreateFlyObjectLogic(
            14,
            UnitLogic.CurPos,
            UnitLogic.CurPos,
            UnitLogic,
            null,
            target,
            this,
            0);
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 创建临时矩形 RVO 障碍的技能类型。
/// 适用于堵路、分割战场和保护阵地；城墙尺寸与位置参数由关联飞行器的 flyParam 提供。
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
            SkillCfg.flyObjectId,
            UnitLogic.CurPos,
            UnitLogic.CurPos,
            UnitLogic,
            null,
            target,
            this,
            0);
    }
}

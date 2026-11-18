using System.Collections.Generic;

/// <summary>
/// 创建圆形RVO区域的技能类型。
/// 负责给释放者添加技能Buff，并创建圆形伤害飞行器。
/// </summary>
public class CreateCircleSkill : SkillLogicBase
{
    public CreateCircleSkill(UnitLogicBase unitLogic, SkillCfg skillCfg)
        : base(unitLogic, skillCfg)
    {
    }

    /// <summary>
    /// 给释放者添加配置Buff，并创建以释放者为圆心的圆形飞行器。
    /// </summary>
    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (UnitLogic == null || UnitLogic.IsDead)
        {
            return;
        }

        ApplyBuffs(UnitLogic);
        FlyObjectFactory.CreateFlyObjectLogic(
            SkillCfg.flyObjectId,
            UnitLogic.CurPos,
            UnitLogic.CurPos,
            UnitLogic,
            null,
            target,
            this,
            GetAttackDamageSnapshot());
    }
}

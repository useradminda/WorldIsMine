using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Locks the current enemy target's direction, then keeps charging for the duration
/// configured by the selected aroundArea fly object.
/// </summary>
public class PushSkill : SkillLogicBase
{
    private const float DefaultSpeedMultiplier = 1.5f;

    public float Duration { get; }
    public float SpeedMultiplier { get; }

    public PushSkill(UnitLogicBase unitLogic, SkillCfg skillCfg)
        : base(unitLogic, skillCfg)
    {
        FlyObjectCfg flyObjectCfg = skillCfg != null
            ? FlyObjectCfgConfig.Ins.SearchById(skillCfg.flyObjectId)
            : null;
        Duration = flyObjectCfg != null ? Mathf.Max(0f, flyObjectCfg.liveTime) : 0f;
        SpeedMultiplier = DefaultSpeedMultiplier;
    }

    public override bool CanUseWithTarget(UnitLogicBase target)
    {
        return target != null
            && !target.IsDead
            && target.CampType != UnitLogic.CampType;
    }

    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (UnitLogic == null || UnitLogic.IsDead || !CanUseWithTarget(target))
        {
            return;
        }

        UnitLogic.StateMachine.ChangeState(EStateTyep.Push, this, target);
        StartPushDamage(target);
    }

    public Vector3 GetChargeDirection(UnitLogicBase lockedTarget)
    {
        Vector3 direction = lockedTarget != null
            ? lockedTarget.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos
            : UnitLogic.TargetForward;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = UnitLogic.TargetForward;
            direction.y = 0f;
        }

        return direction.sqrMagnitude <= 0.0001f
            ? Vector3.forward
            : direction.normalized;
    }

    private void StartPushDamage(UnitLogicBase lockedTarget)
    {
        if (SkillCfg == null || SkillCfg.flyObjectId <= 0 || UnitLogic == null || UnitLogic.IsDead)
        {
            return;
        }

        FlyObjectCfg flyObjectCfg = FlyObjectCfgConfig.Ins.SearchById(SkillCfg.flyObjectId);
        if (flyObjectCfg == null || flyObjectCfg.flyType != "aroundArea")
        {
            Debug.LogError($"Push skill requires an aroundArea fly object. SkillId={SkillCfg.id}, FlyObjectCfgId={SkillCfg.flyObjectId}");
            return;
        }

        FlyObjectFactory.CreateFlyObjectLogic(
            SkillCfg.flyObjectId,
            UnitLogic.CurPos,
            lockedTarget != null ? lockedTarget.CurPos : UnitLogic.CurPos,
            UnitLogic,
            TargetList,
            lockedTarget,
            this,
            GetAttackDamageSnapshot());
    }

}

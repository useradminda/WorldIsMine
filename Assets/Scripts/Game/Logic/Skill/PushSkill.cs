using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 锁定当前敌方目标方向并进入持续冲撞的技能类型。
/// 适用于英雄向敌方阵营突进；移动由 PushState 负责，持续范围伤害由关联的 aroundArea 飞行器负责。
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
            && target.CampType != UnitLogic.CampType
            && target.UnitType != EUnitType.Wall;
    }

    /// <summary>
    /// 执行冲撞技能的实际效果，延迟由技能基类统一处理。
    /// </summary>
    public override void OnSkillDoEffect(UnitLogicBase target, List<int> resultUnitIndexList)
    {
        if (UnitLogic == null || UnitLogic.IsDead)
        {
            return;
        }

        // 延迟释放期间主目标死亡或被回收时，保留冲锋技能本身，方向交给角色当前正方向。
        // 目标仍然有效时继续锁定目标方向；目标失效时传入空目标，让 PushState 使用 TargetForward。
        if (target != null && !target.IsDead
            && (target.CampType == UnitLogic.CampType || target.UnitType == EUnitType.Wall))
        {
            return;
        }

        if (!CanUseWithTarget(target))
        {
            target = null;
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

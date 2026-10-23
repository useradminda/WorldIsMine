using UnityEngine;

public class PushState : StateBase
{
    public override EStateTyep StateType => EStateTyep.Push;

    private PushSkill pushSkill;
    private Vector3 chargeDirection;
    private float remainTime;
    private bool wallBlocked;

    public PushState(UnitLogicBase unitLogic) : base(unitLogic)
    {
    }

    public override void EnterState(params object[] objects)
    {
        pushSkill = objects != null && objects.Length > 0
            ? objects[0] as PushSkill
            : null;
        UnitLogicBase lockedTarget = objects != null && objects.Length > 1
            ? objects[1] as UnitLogicBase
            : null;

        if (pushSkill == null || UnitLogic.IsDead)
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }

        chargeDirection = pushSkill.GetChargeDirection(lockedTarget);
        UnitLogic.SetSearchTarget(null);
        UnitLogic.SetMoveForward(chargeDirection);
        UnitLogic.TriggerMove();
        UnitLogic.UnitView.EnterState(EStateTyep.Move);
        UpdatePushAnimationSpeed();

        remainTime = pushSkill.Duration;
        wallBlocked = false;
    }

    public override void UpdateState(float dt)
    {
        if (UnitLogic.IsDead || pushSkill == null)
        {
            return;
        }

        remainTime -= dt;
        if (remainTime <= 0f)
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }

        UpdatePushAnimationSpeed();

        if (!wallBlocked)
        {
            UnitLogic.AgentMoveForward(chargeDirection, pushSkill.SpeedMultiplier);
        }
        else
        {
            KeepMovingAnimationWithoutDisplacement();
        }

    }

    public override void LateUpdateState(float dt)
    {
        if (UnitLogic.IsDead || pushSkill == null)
        {
            return;
        }

        ClampAtWallIfNeeded();
    }

    public override void ExitState()
    {
        if (UnitLogic.Agenter != null && !UnitLogic.IsDead)
        {
            UnitLogic.Agenter.prefVelocity = Vector3.zero;
            UnitLogic.Agenter.velocity = Vector3.zero;
            UnitLogic.Agenter.maxSpeed = UnitLogic.Agenter.saveMaxSpeed;

            if (UnitLogic.UnitView != null)
            {
                UnitLogic.UnitView.ActionFlowComponent.SetAnimationSpeed(UnitLogic.LogicRatio);
            }
        }

        pushSkill = null;
        chargeDirection = Vector3.zero;
        remainTime = 0f;
        wallBlocked = false;
    }

    private void KeepMovingAnimationWithoutDisplacement()
    {
        if (UnitLogic.Agenter == null)
        {
            return;
        }

        UnitLogic.Agenter.navigationEnabled = true;
        UnitLogic.Agenter.collisionEnabled = true;
        UnitLogic.Agenter.maxSpeed = UnitLogic.Agenter.saveMaxSpeed
            * pushSkill.SpeedMultiplier
            * UnitLogic.LogicRatio;
        UnitLogic.Agenter.prefVelocity = Vector3.zero;
        UnitLogic.Agenter.velocity = Vector3.zero;
    }

    private void UpdatePushAnimationSpeed()
    {
        if (UnitLogic.UnitView == null || pushSkill == null)
        {
            return;
        }

        UnitLogic.UnitView.ActionFlowComponent.SetAnimationSpeed(
            pushSkill.SpeedMultiplier * UnitLogic.LogicRatio);
    }

    private void ClampAtWallIfNeeded()
    {
        if (wallBlocked)
        {
            KeepMovingAnimationWithoutDisplacement();
            return;
        }

        UnitLogicBase redWall = UnitManager.Instance.GetWallLogic(ECampType.Red);
        UnitLogicBase blueWall = UnitManager.Instance.GetWallLogic(ECampType.Blue);
        if (TryClampAtWall(redWall) || TryClampAtWall(blueWall))
        {
            wallBlocked = true;
            KeepMovingAnimationWithoutDisplacement();
        }
    }

    private bool TryClampAtWall(UnitLogicBase wall)
    {
        if (wall == null || wall.IsDead)
        {
            return false;
        }

        Vector3 wallPoint = wall.GetClosestPoint(UnitLogic.CurPos);
        Vector3 toWall = wallPoint - UnitLogic.CurPos;
        toWall.y = 0f;
        if (toWall.sqrMagnitude > 0.0001f
            && Vector3.Dot(toWall, chargeDirection) < 0f)
        {
            return false;
        }

        if (!UnitLogic.IsTargetInAttackRange(wall, 0f))
        {
            return false;
        }

        Vector3 safePosition = wallPoint
            - chargeDirection * Mathf.Max(0f, UnitLogic.Prop.Radius);
        safePosition.y = UnitLogic.CurPos.y;
        UnitLogic.Agenter.pos = safePosition;
        return true;
    }
}

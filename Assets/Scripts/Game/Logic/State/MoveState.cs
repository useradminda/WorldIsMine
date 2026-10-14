using System.Collections.Generic;
using UnityEngine;
public class MoveState : StateBase
{
    private const float StuckCheckDuration = 0.8f;
    private const float StuckMoveThreshold = 0.05f;
    private const float EscapeDuration = 0.45f;
    private const float EscapeForwardWeight = 0.35f;

    private int searchReqIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private readonly List<int> searchResultList = new List<int>();
    private Vector3 lastCheckPosition;
    private float stuckTime;
    private float escapeTime;
    private Vector3 escapeDirection;

    public override EStateTyep StateType { get { return EStateTyep.Move; } }

    public MoveState(UnitLogicBase ulb) : base(ulb)
    {
        UnitLogic.TriggerMove();
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerMove();
        UnitLogic.SetSearchTarget(null);
        searchReqIndex = -1;
        lastCheckPosition = UnitLogic.CurPos;
        stuckTime = 0f;
        escapeTime = 0f;
        escapeDirection = Vector3.zero;
        UnitLogic.UnitView.ClearEscapeForward();
        UnitLogic.UnitView.EnterState(EStateTyep.Move);
    }

    public override void UpdateState(float dt)
    {
        if (escapeTime > 0f)
        {
            escapeTime -= dt * UnitLogic.LogicRatio;
            UnitLogic.Agenter.maxSpeed = UnitLogic.Agenter.saveMaxSpeed;
            UnitLogic.Agenter.prefVelocity = escapeDirection * UnitLogic.SoliderCfg.moveSpeed * UnitLogic.LogicRatio;
            if (escapeTime <= 0f)
            {
                escapeTime = 0f;
                stuckTime = 0f;
                lastCheckPosition = UnitLogic.CurPos;
                UnitLogic.UnitView.ClearEscapeForward();
            }
        }
        else
        {
            UnitLogic.AgentMoveForward();
            UpdateStuckState(dt);
        }
        RequestTargetSearch();
    }

    public override void LateUpdateState(float dt)
    {
        if (UnitLogic.IsDead || searchReqIndex < 0)
        {
            return;
        }

        MapCellManager.Instance.GetResult(searchReqIndex, searchResultList, ref nearestIndex, ref randomIndex);
        searchReqIndex = -1;
        UnitLogicBase target = nearestIndex >= 0 && nearestIndex < UnitManager.Instance.UnitList.Count ? UnitManager.Instance.UnitList[nearestIndex] : null;
        if (target == null || target.IsDead || target.CampType == UnitLogic.CampType)
        {
            target = UnitManager.Instance.GetOtherWallLogic(UnitLogic.CampType);
            if (target == null || target.IsDead)
            {
                UnitLogic.SetSearchTarget(null);
                return;
            }
        }

        UnitLogic.SetSearchTarget(target);
        if (UnitLogic.NormalSkill != null)
        {
            if (UnitLogic.IsTargetInAttackRange(target, UnitLogic.NormalSkill.SkillCfg.atkRange))
            {
                Vector3 attackDirection = target.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos;
                UnitLogic.UnitView.SetForwardForce(attackDirection);
                UnitLogic.StateMachine.ChangeState(HasReadySkillInRange(target) ? EStateTyep.Skill : EStateTyep.Attack);
            }
        }
    }

    public override void ExitState()
    {
        searchReqIndex = -1;
        UnitLogic.UnitView.ClearEscapeForward();
    }

    private void RequestTargetSearch()
    {
        if (UnitLogic.NormalSkill == null || searchReqIndex >= 0)
        {
            return;
        }
        searchReqIndex = MapCellManager.Instance.RequestSearch(UnitLogic.CurPos, UnitLogic.NormalSkill.SkillSearchRange, UnitLogic.OtherCampTypeInt);
    }

    private void UpdateStuckState(float dt)
    {
        UpdateStuckMovement(dt);
        if (stuckTime >= StuckCheckDuration)
        {
            StartEscapeMove();
        }
    }

    private void UpdateStuckMovement(float dt)
    {
        float movedDistanceSqr = (UnitLogic.CurPos - lastCheckPosition).sqrMagnitude;
        lastCheckPosition = UnitLogic.CurPos;
        float stuckMoveThreshold = StuckMoveThreshold;
        if (movedDistanceSqr <= stuckMoveThreshold * stuckMoveThreshold)
        {
            stuckTime += dt;
        }
        else
        {
            stuckTime = 0f;
        }
    }

    private void StartEscapeMove()
    {
        stuckTime = 0f;
        escapeTime = EscapeDuration;
        Vector3 battleForward = UnitLogic.CampType == ECampType.Red ? Vector3.forward : Vector3.back;
        Vector3 battleRight = Vector3.Cross(Vector3.up, battleForward).normalized;
        if ((UnitLogic.UId & 1) == 0)
        {
            battleRight = -battleRight;
        }
        escapeDirection = (battleForward * EscapeForwardWeight + battleRight).normalized;
        UnitLogic.UnitView.SetEscapeForward(escapeDirection);
    }

    private bool HasReadySkillInRange(UnitLogicBase target)
    {
        for (int i = 0; i < UnitLogic.SkillList.Count; i++)
        {
            SkillLogicBase skill = UnitLogic.SkillList[i];
            if (skill.BNormalSkill == false && skill.CurCD <= 0f && UnitLogic.IsTargetInAttackRange(target, skill.SkillCfg.atkRange))
            {
                return true;
            }
        }
        return false;
    }
}

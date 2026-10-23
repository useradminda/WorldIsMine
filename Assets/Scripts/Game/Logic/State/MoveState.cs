using System.Collections.Generic;
using UnityEngine;
public class MoveState : StateBase
{
    private int searchReqIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private float stuckCheckTime;
    private float escapeTime;
    private float escapeCooldownTime;
    private Vector3 stuckCheckPosition;
    private Vector3 escapeDirection;
    private readonly List<int> searchResultList = new List<int>();

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
        stuckCheckTime = 0f;
        escapeTime = 0f;
        escapeCooldownTime = 0f;
        stuckCheckPosition = UnitLogic.CurPos;
        escapeDirection = Vector3.zero;
        UnitLogic.UnitView.EnterState(EStateTyep.Move);
        RequestTargetSearch();
    }

    public override void UpdateState(float dt)
    {
        if (HasReadySkillWithoutTarget())
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Skill);
            return;
        }

        RequestTargetSearch();
        UpdateEscape(dt);
        UnitLogic.AgentMoveForward(escapeDirection);
    }

    public override void LateUpdateState(float dt)
    {
        if (UnitLogic.IsDead || searchReqIndex < 0)
        {
            return;
        }

        MapCellManager.Instance.GetResult(searchReqIndex, searchResultList, ref nearestIndex, ref randomIndex);
        searchReqIndex = -1;
        UnitLogicBase target = nearestIndex >= 0 && nearestIndex < UnitManager.Instance.UnitList.Count
            ? UnitManager.Instance.UnitList[nearestIndex]
            : null;
        if (target == null || target.IsDead || target.CampType == UnitLogic.CampType)
        {
            target = UnitManager.Instance.GetOtherWallLogic(UnitLogic.CampType);
            if (target == null || target.IsDead)
            {
                UnitLogic.SetSearchTarget(null);
                UnitLogic.TriggerMove();
                UnitLogic.UnitView.EnterState(EStateTyep.Move);
                return;
            }
        }

        UnitLogic.SetSearchTarget(target);
        if (UnitLogic.NormalSkill != null)
        {
            if (UnitLogic.IsTargetInAttackRange(target, UnitLogic.NormalSkill.SkillCfg.atkRange))
            {
                UnitLogic.StateMachine.ChangeState(HasReadySkillInRange(target) ? EStateTyep.Skill : EStateTyep.Attack);
                return;
            }
        }

        UnitLogic.TriggerMove();
        UnitLogic.UnitView.EnterState(EStateTyep.Move);
    }

    public override void ExitState()
    {
        searchReqIndex = -1;
        stuckCheckTime = 0f;
        escapeTime = 0f;
        escapeCooldownTime = 0f;
        escapeDirection = Vector3.zero;
    }

    private void UpdateEscape(float dt)
    {
        if (escapeCooldownTime > 0f)
        {
            escapeCooldownTime = Mathf.Max(0f, escapeCooldownTime - dt);
        }
        if (escapeTime > 0f)
        {
            escapeTime = Mathf.Max(0f, escapeTime - dt);
            if (escapeTime <= 0f)
            {
                escapeDirection = Vector3.zero;
                escapeCooldownTime = RvoEscapeConfig.EscapeCooldown;
                stuckCheckTime = 0f;
                stuckCheckPosition = UnitLogic.CurPos;
            }
            return;
        }
        escapeDirection = Vector3.zero;
        stuckCheckTime += dt;
        if (stuckCheckTime < RvoEscapeConfig.CheckWindow)
        {
            return;
        }
        Vector3 displacement = UnitLogic.CurPos - stuckCheckPosition;
        displacement.y = 0f;
        bool isStuck = displacement.sqrMagnitude <= RvoEscapeConfig.MaxDisplacement * RvoEscapeConfig.MaxDisplacement;
        stuckCheckTime = 0f;
        stuckCheckPosition = UnitLogic.CurPos;
        if (!isStuck || escapeCooldownTime > 0f)
        {
            return;
        }
        Vector3 forward = UnitLogic.TargetForward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return;
        }
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        escapeDirection = (UnitLogic.UId & 1) == 0 ? right : -right;
        escapeTime = RvoEscapeConfig.EscapeDuration;
    }

    private void RequestTargetSearch()
    {
        if (UnitLogic.NormalSkill == null || searchReqIndex >= 0)
        {
            return;
        }
        searchReqIndex = MapCellManager.Instance.RequestSearch(UnitLogic.CurPos, UnitLogic.NormalSkill.SkillSearchRange, UnitLogic.OtherCampTypeInt);
    }

    private bool HasReadySkillInRange(UnitLogicBase target)
    {
        for (int i = 0; i < UnitLogic.SkillList.Count; i++)
        {
            SkillLogicBase skill = UnitLogic.SkillList[i];
            if (!skill.BNormalSkill && !skill.IsTriggerBeforeDie && skill.CurCD <= 0f &&
                (skill.TargetsSelf || !skill.RequiresEnemyTarget ||
                 (skill.CanUseWithTarget(target) && UnitLogic.IsTargetInAttackRange(target, skill.SkillCfg.atkRange))))
            {
                return true;
            }
        }
        return false;
    }

    private bool HasReadySkillWithoutTarget()
    {
        for (int i = 0; i < UnitLogic.SkillList.Count; i++)
        {
            SkillLogicBase skill = UnitLogic.SkillList[i];
            if (!skill.BNormalSkill && !skill.IsTriggerBeforeDie && !skill.RequiresEnemyTarget && skill.CurCD <= 0f)
            {
                return true;
            }
        }

        return false;
    }
}

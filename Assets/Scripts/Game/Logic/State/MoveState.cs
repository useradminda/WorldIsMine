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
    private float lastEscapeSide = 1f;
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
        lastEscapeSide = 1f;
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
        RedirectBlockedSearchTarget();
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
        if (target != null && UnitLogic.IsTargetBlockedByEnemyWall(target))
        {
            target = UnitManager.Instance.GetOtherWallLogic(UnitLogic.CampType);
        }
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
                if (IsArcherUnit())
                {
                    UnitLogic.StateMachine.ChangeState(EStateTyep.ArcherPosition);
                }
                else
                {
                    UnitLogic.StateMachine.ChangeState(HasReadySkillInRange(target) ? EStateTyep.Skill : EStateTyep.Attack);
                }
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
        lastEscapeSide = 1f;
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
        escapeDirection = SelectEscapeDirection(forward);
        escapeTime = RvoEscapeConfig.EscapeDuration;
    }

    /// <summary>根据实际避让速度和目标相对方向选择贴边绕行方向，避免使用单位 ID 固定左右。</summary>
    private Vector3 SelectEscapeDirection(Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 actualVelocity = UnitLogic.Agenter.velocity;
        actualVelocity.y = 0f;
        float lateralVelocity = Vector3.Dot(actualVelocity, right);
        float sideSign;

        if (actualVelocity.sqrMagnitude > 0.01f && Mathf.Abs(lateralVelocity) > 0.05f
            && Vector3.Dot(actualVelocity.normalized, forward) > -0.1f)
        {
            sideSign = Mathf.Sign(lateralVelocity);
        }
        else
        {
            UnitLogicBase target = UnitLogic.SearchTarget;
            if (target != null && !target.IsDead)
            {
                Vector3 toTarget = target.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos;
                toTarget.y = 0f;
                float targetSide = Vector3.Dot(toTarget, right);
                sideSign = Mathf.Abs(targetSide) > 0.05f ? Mathf.Sign(targetSide) : lastEscapeSide;
            }
            else
            {
                sideSign = lastEscapeSide;
            }
        }

        lastEscapeSide = -sideSign;
        return right * sideSign;
    }

    private void RequestTargetSearch()
    {
        if (UnitLogic.NormalSkill == null || searchReqIndex >= 0)
        {
            return;
        }
        searchReqIndex = MapCellManager.Instance.RequestSearch(UnitLogic.CurPos, UnitLogic.NormalSkill.SkillSearchRange, UnitLogic.OtherCampTypeInt);
    }

    /// <summary>
    /// 在异步索敌结果返回前，先把已经位于敌方城门后方的旧目标切换为敌方城墙。
    /// </summary>
    private void RedirectBlockedSearchTarget()
    {
        UnitLogicBase target = UnitLogic.SearchTarget;
        if (target != null && UnitLogic.IsTargetBlockedByEnemyWall(target))
        {
            UnitLogic.SetSearchTarget(UnitManager.Instance.GetOtherWallLogic(UnitLogic.CampType));
        }
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

    /// <summary>判断当前单位是否为使用独立攻击流程的弓手。</summary>
    private bool IsArcherUnit()
    {
        return UnitLogic.SoliderCfg != null
            && (UnitLogic.SoliderCfg.id == 102 || UnitLogic.SoliderCfg.id == 202);
    }
}

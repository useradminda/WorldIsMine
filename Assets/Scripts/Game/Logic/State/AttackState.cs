using System.Collections.Generic;
using UnityEngine;

public class AttackState : StateBase
{
    private const float AttackExitRangeBuffer = 0.3f;
    private const float AreaSearchForwardOffset = 1f;

    public override EStateTyep StateType { get { return EStateTyep.Attack; } }

    private SkillLogicBase normalSkill;
    private float attackTime;
    private float attackEffectTime;
    private bool attackEffectTriggered;
    private bool attackEffectPending;
    private int effectSearchReqIndex = -1;
    private int targetSearchReqIndex = -1;
    private int targetNearestIndex = -1;
    private int targetRandomIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private Vector3 attackDirection;
    private readonly List<int> resultUnitIndexList = new List<int>();
    private readonly List<int> targetSearchResultList = new List<int>();

    public AttackState(UnitLogicBase ulb) : base(ulb)
    {
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerMoveStop();
        UnitLogic.SetCombatRadius();
        UnitLogic.UnitView.EnterState(EStateTyep.Attack);
        normalSkill = UnitLogic.NormalSkill;
        effectSearchReqIndex = -1;
        targetSearchReqIndex = -1;
        targetSearchResultList.Clear();
        resultUnitIndexList.Clear();
        StartNormalAttack();
    }

    public override void UpdateState(float dt)
    {
        if (UnitLogic.IsDead || normalSkill == null)
        {
            return;
        }

        if (attackTime > 0f)
        {
            float deltaTime = dt * UnitLogic.LogicRatio;
            attackTime -= deltaTime;
            attackEffectTime -= deltaTime;
            if (!attackEffectTriggered && attackEffectTime <= 0f)
            {
                RequestNormalAttackEffect();
            }
            if (attackTime <= 0f)
            {
                attackTime = 0f;
                UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
            }
            return;
        }

        if (attackEffectPending)
        {
            return;
        }

        if (UnitLogic.SearchTarget == null || !UnitLogic.IsTargetInAttackRange(UnitLogic.SearchTarget, normalSkill.SkillCfg.atkRange, AttackExitRangeBuffer))
        {
            RequestTargetSearch();
            return;
        }

        if (HasReadySkillInRange())
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Skill);
            return;
        }

        if (normalSkill.CurCD <= 0f)
        {
            StartNormalAttack();
        }
    }

    public override void LateUpdateState(float dt)
    {
        if (UnitLogic.IsDead || normalSkill == null)
        {
            return;
        }

        if (targetSearchReqIndex >= 0)
        {
            ResolveTargetSearch();
            return;
        }

        if (!attackEffectPending)
        {
            return;
        }

        if (normalSkill.SkillCfg.skillArea > 0)
        {
            if (effectSearchReqIndex < 0)
            {
                return;
            }
            resultUnitIndexList.Clear();
            MapCellManager.Instance.GetResult(effectSearchReqIndex, resultUnitIndexList, ref nearestIndex, ref randomIndex);
            effectSearchReqIndex = -1;
        }

        ExecuteNormalAttackEffect();
    }

    public override void ExitState()
    {
        UnitLogic.RestoreAgentRadius();
        if (normalSkill != null)
        {
            normalSkill.SkillRelease();
        }
        normalSkill = null;
        attackTime = 0f;
        attackEffectTime = 0f;
        attackEffectTriggered = false;
        attackEffectPending = false;
        effectSearchReqIndex = -1;
        targetSearchReqIndex = -1;
        targetSearchResultList.Clear();
        resultUnitIndexList.Clear();
    }

    private void RequestTargetSearch()
    {
        if (normalSkill == null || targetSearchReqIndex >= 0)
        {
            return;
        }
        targetSearchReqIndex = MapCellManager.Instance.RequestSearch(UnitLogic.CurPos, GetTargetSearchRange(), UnitLogic.OtherCampTypeInt);
        UnitLogic.TriggerMoveStop();
        UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
    }

    private float GetTargetSearchRange()
    {
        float searchRange = normalSkill.SkillSearchRange;
        for (int i = 0; i < UnitLogic.SkillList.Count; i++)
        {
            SkillLogicBase skill = UnitLogic.SkillList[i];
            if (skill.BNormalSkill == false)
            {
                searchRange = Mathf.Max(searchRange, skill.SkillSearchRange);
            }
        }
        return searchRange;
    }

    private void ResolveTargetSearch()
    {
        targetSearchResultList.Clear();
        MapCellManager.Instance.GetResult(targetSearchReqIndex, targetSearchResultList, ref targetNearestIndex, ref targetRandomIndex);
        targetSearchReqIndex = -1;
        UnitLogicBase target = targetNearestIndex >= 0 && targetNearestIndex < UnitManager.Instance.UnitList.Count ? UnitManager.Instance.UnitList[targetNearestIndex] : null;
        if (target == null || target.IsDead || target.CampType == UnitLogic.CampType)
        {
            UnitLogic.SetSearchTarget(null);
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }
        UnitLogic.SetSearchTarget(target);
        if (HasReadySkillInRange())
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Skill);
            return;
        }
        if (!UnitLogic.IsTargetInAttackRange(target, normalSkill.SkillCfg.atkRange, AttackExitRangeBuffer))
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }
        if (normalSkill.CurCD <= 0f)
        {
            StartNormalAttack();
        }
        else
        {
            UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
        }
    }

    private void StartNormalAttack()
    {
        if (normalSkill == null || UnitLogic.SearchTarget == null || UnitLogic.SearchTarget.IsDead)
        {
            attackTime = 0f;
            return;
        }

        if (normalSkill.CurCD > 0f)
        {
            UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
            attackTime = 0f;
            return;
        }

        UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.attack);
        attackTime = UnitLogic.UnitView.ActionFlowComponent.GetAnimLen(EActionType.attack);
        attackEffectTime = attackTime * 0.5f;
        attackEffectTriggered = false;
        attackEffectPending = false;
        effectSearchReqIndex = -1;
        resultUnitIndexList.Clear();
        attackDirection = UnitLogic.SearchTarget.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos;
        attackDirection.y = 0f;
        if (attackDirection.sqrMagnitude <= 0.0001f)
        {
            attackDirection = UnitLogic.TargetForward;
            attackDirection.y = 0f;
        }
        attackDirection.Normalize();
        UnitLogic.UnitView.SetForwardForce(attackDirection);
        if (attackTime <= 0f)
        {
            RequestNormalAttackEffect();
        }
    }

    private void RequestNormalAttackEffect()
    {
        attackEffectTriggered = true;
        attackEffectPending = true;
        if (normalSkill.SkillCfg.skillArea > 0)
        {
            Vector3 searchPosition = UnitLogic.CurPos + attackDirection * AreaSearchForwardOffset;
            float searchRadius = normalSkill.SkillCfg.skillArea + Mathf.Max(0f, UnitLogic.Prop.Radius);
            effectSearchReqIndex = MapCellManager.Instance.RequestSearch(searchPosition, searchRadius, UnitLogic.OtherCampTypeInt);
        }
    }

    private void ExecuteNormalAttackEffect()
    {
        if (UnitLogic.SearchTarget != null && !UnitLogic.SearchTarget.IsDead)
        {
            normalSkill.SkillDoEffect(UnitLogic.SearchTarget, resultUnitIndexList);
        }
        attackEffectPending = false;
    }

    private bool HasReadySkillInRange()
    {
        for (int i = 0; i < UnitLogic.SkillList.Count; i++)
        {
            SkillLogicBase skill = UnitLogic.SkillList[i];
            if (skill.BNormalSkill == false && skill.CurCD <= 0f && UnitLogic.IsTargetInAttackRange(UnitLogic.SearchTarget, skill.SkillCfg.atkRange))
            {
                return true;
            }
        }
        return false;
    }

}

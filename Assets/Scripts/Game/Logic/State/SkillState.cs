using System.Collections.Generic;
using UnityEngine;

public class SkillState : StateBase
{
    private SkillLogicBase skillLogic;
    private readonly List<int> resultUnitIndexList = new List<int>();
    private int searchReqIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
    private bool executePending;
    private bool executed;
    private float skillTime;

    public override EStateTyep StateType { get { return EStateTyep.Skill; } }

    public SkillState(UnitLogicBase unitLogic) : base(unitLogic)
    {
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerMoveStop();
        UnitLogic.SetCombatRadius();
        UnitLogic.UnitView.EnterState(EStateTyep.Skill);
        skillLogic = GetReadySkill();
        executePending = skillLogic != null;
        executed = false;
        skillTime = 0f;
        searchReqIndex = -1;
        resultUnitIndexList.Clear();
    }

    public override void UpdateState(float dt)
    {
        if (UnitLogic.IsDead || skillLogic == null || (skillLogic.RequiresEnemyTarget && UnitLogic.SearchTarget == null) || (!executed && skillLogic.CurCD > 0f))
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
            return;
        }

        if (!executed)
        {
            if (searchReqIndex < 0 && executePending && skillLogic.RequiresEnemyTarget && skillLogic.SkillCfg.skillArea > 0)
            {
                searchReqIndex = MapCellManager.Instance.RequestSearch(UnitLogic.CurPos, skillLogic.SkillCfg.skillArea, UnitLogic.OtherCampTypeInt);
            }
            return;
        }

        skillTime -= dt * UnitLogic.LogicRatio;
        if (skillTime <= 0f)
        {
            UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.wait);
            UnitLogic.StateMachine.ChangeState(EStateTyep.Attack);
        }
    }

    public override void LateUpdateState(float dt)
    {
        if (UnitLogic.IsDead || executed || skillLogic == null || !executePending)
        {
            return;
        }

        if (skillLogic.RequiresEnemyTarget && skillLogic.SkillCfg.skillArea > 0)
        {
            if (searchReqIndex < 0)
            {
                return;
            }
            MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref nearestIndex, ref randomIndex);
            searchReqIndex = -1;
        }
        ExecuteSkill();
    }

    public override void ExitState()
    {
        UnitLogic.RestoreAgentRadius();
        skillLogic = null;
        searchReqIndex = -1;
        executePending = false;
        executed = false;
        resultUnitIndexList.Clear();
    }

    private SkillLogicBase GetReadySkill()
    {
        for (int i = 0; i < UnitLogic.SkillList.Count; i++)
        {
            SkillLogicBase skill = UnitLogic.SkillList[i];
            if (skill.BNormalSkill || skill.IsTriggerBeforeDie || skill.CurCD > 0f)
            {
                continue;
            }

            if (skill.TargetsSelf || !skill.RequiresEnemyTarget ||
                (UnitLogic.SearchTarget != null &&
                 skill.CanUseWithTarget(UnitLogic.SearchTarget) &&
                 UnitLogic.IsTargetInAttackRange(UnitLogic.SearchTarget, skill.SkillCfg.atkRange)))
            {
                return skill;
            }
        }
        return null;
    }

    private void ExecuteSkill()
    {
        if (executed || skillLogic == null || skillLogic.CurCD > 0f ||
            (skillLogic.RequiresEnemyTarget &&
             (UnitLogic.SearchTarget == null || !skillLogic.CanUseWithTarget(UnitLogic.SearchTarget))))
        {
            return;
        }

        UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.skill);
        skillTime = UnitLogic.UnitView.ActionFlowComponent.GetAnimLen(EActionType.skill);
        UnitLogicBase skillTarget = skillLogic.TargetsSelf ? UnitLogic : UnitLogic.SearchTarget;
        Vector3 attackDirection = skillTarget != null && skillTarget != UnitLogic
            ? skillTarget.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos
            : UnitLogic.TargetForward;
        if (attackDirection.sqrMagnitude <= 0.0001f)
        {
            attackDirection = Vector3.forward;
        }
        UnitLogic.UnitView.SetForwardForce(attackDirection);
        skillLogic.SkillDoEffect(skillTarget, resultUnitIndexList);
        executed = true;
        executePending = false;
    }
}

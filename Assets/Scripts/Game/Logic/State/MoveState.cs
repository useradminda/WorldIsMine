using System.Collections.Generic;
using UnityEngine;
public class MoveState : StateBase
{
    private int searchReqIndex = -1;
    private int nearestIndex = -1;
    private int randomIndex = -1;
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
        UnitLogic.UnitView.EnterState(EStateTyep.Move);
    }

    public override void UpdateState(float dt)
    {
        UnitLogic.AgentMoveForward();
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
            if (skill.BNormalSkill == false && skill.CurCD <= 0f && UnitLogic.IsTargetInAttackRange(target, skill.SkillCfg.atkRange))
            {
                return true;
            }
        }
        return false;
    }
}

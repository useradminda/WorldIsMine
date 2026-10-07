using UnityEngine;
using System.Collections.Generic;
public class AttackState : StateBase
{
    private const float AttackExitRangeBuffer = 0.3f;

    public override EStateTyep StateType { get { return EStateTyep.Attack; } }

    private SkillLogicBase useSkill;
    private SkillLogicBase superSkill;
    private float attackTime = 0;

    public AttackState(UnitLogicBase ulb) : base(ulb)
    {
        attackTime = 0;
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerMoveStop();
        UnitLogic.UnitView.EnterState(EStateTyep.Attack);
        useSkill = UnitLogic.NormalSkill;
        useSkillPlay();

    }

    public override void UpdateState(float dt)
    {  
        if (UnitLogic.IsDead)
            return;

        judgeTargetBeDead();
        if (UnitLogic.StateMachine.GetCurrentState() != this)
            return;

        reqSearchTar();
        setSearchTar();

        updateSuprerSkill();
        if (attackTime > 0)
        {
            attackTime -= dt;
        }
        else
        {
            if(superSkill != null)
            {
                useSkill = superSkill;
                useSkillPlay();
            }
            else
            {
                useSkill = UnitLogic.NormalSkill;
                useSkillPlay();
            }
        }
    }

    public override void ExitState()
    {
        if (useSkill != null)
            useSkill.SkillRelease();
        if (superSkill != null && superSkill != useSkill)
            superSkill.SkillRelease();
        attackTime = 0;
        useSkill = null;
        superSkill = null;

        if (UnitLogic.IsDead)
        {
            UnitLogic.SetSearchTarget(null);
        }
    }

    private void judgeTargetBeDead()
    {
        if (useSkill.BNormalSkill)
        {
            if (useSkill.SkillSearchTarget != null)
            {
                if (useSkill.SkillSearchTarget.IsDead)
                {
                    if (attackTime <= 0f)
                    {
                        UnitLogic.NormalSkill.SkillClearCD();
                        UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
                    }
                    return;
                }
                if (!UnitLogic.IsTargetInAttackRange(
                        useSkill.SkillSearchTarget,
                        UnitLogic.NormalSkill.SkillCfg.atkRange,
                        AttackExitRangeBuffer))
                {
                    UnitLogic.NormalSkill.SkillClearCD();
                    UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
                    return;
                }
            }
        }  
    }

    private void updateSuprerSkill()
    {
        superSkill = UnitLogic.GetSuperSkillBySearchTarget();
    }

    private void useSkillPlay()
    {
        if (UnitLogic.IsDead || useSkill == null || useSkill.SkillSearchTarget == null)
            return;

        // 攻击动画结束后可能早于技能冷却结束，冷却期间不能重复释放技能。
        if (useSkill.CurCD > 0f)
            return;

        if (useSkill.BNormalSkill == true)
        {
            UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.attack);
            attackTime = UnitLogic.UnitView.ActionFlowComponent.GetAnimLen(EActionType.attack);         
        }
        else
        {
            UnitLogic.UnitView.ActionFlowComponent.PlayAction(EActionType.skill);
            attackTime = UnitLogic.UnitView.ActionFlowComponent.GetAnimLen(EActionType.skill);
        }
        Vector3 atkDir =  useSkill.SkillSearchTarget.GetClosestPoint(UnitLogic.CurPos) - UnitLogic.CurPos;
        UnitLogic.UnitView.SetForwardForce(atkDir);
        useSkill.SkillDoEffect(resultUnitIndexList);
    }

    private int searchReqIndex = -1;
    List<int> resultUnitIndexList = new List<int>();
    private int neastIndex = -1;
    private int randomIndex = -1;
    private void reqSearchTar()
    {
        if (UnitLogic.IsDead || useSkill == null || useSkill.BNormalSkill == false || useSkill.SkillCfg.skillArea == 0)
            return;
        searchReqIndex = MapCellManager.Instance.RequestSearch(UnitLogic.CurPos, useSkill.SkillCfg.skillArea, UnitLogic.OtherCampTypeInt);
    }

    private void setSearchTar()
    { 
        if (UnitLogic.IsDead || useSkill == null || useSkill.BNormalSkill == false || useSkill.SkillCfg.skillArea == 0)
            return;
        MapCellManager.Instance.GetResult(searchReqIndex, resultUnitIndexList, ref neastIndex, ref randomIndex);
    }

}

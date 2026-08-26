using UnityEngine;

public class MoveState : StateBase
{


    public override EStateTyep StateType { get { return EStateTyep.Move; } }

    public MoveState(UnitLogicBase ulb):base(ulb)
    {
        UnitLogic.TriggerMove();
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerMove();
        UnitLogic.UnitView.EnterState(EStateTyep.Move);
       
    }

    public override void UpdateState(float dt)
    {
        searchTargetUnits();
        getTargetUnits();
        updateMove();
    }

    public override void ExitState()
    {
    }

    private void searchTargetUnits()
    {
        UnitLogic.NormalSkill.SkillSearchTarget();
    }

    private void getTargetUnits()
    {
        UnitLogicBase ulb = UnitLogic.NormalSkill.GetSkillSearchTargetSingleResult();
        if (ulb != null && ulb.IsDead == false)
        {
            float sqrDistance = (UnitLogic.CurPos - ulb.CurPos).sqrMagnitude;
            if (sqrDistance <= UnitLogic.NormalSkill.SkillCfg.atkRange * UnitLogic.NormalSkill.SkillCfg.atkRange)
            {
                UnitLogic.StateMachine.ChangeState(EStateTyep.Attack, ulb, UnitLogic.NormalSkill);
            }
        }
    }

    private void updateMove()
    {
        UnitLogic.MoveForward();
    }
}

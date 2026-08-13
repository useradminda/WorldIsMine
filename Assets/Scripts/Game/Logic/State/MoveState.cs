using UnityEngine;

public class MoveState : StateBase
{

    private int nextSearchFrame;
    public override EStateTyep StateType { get { return EStateTyep.Move; } }

    public MoveState(UnitLogicBase ulb):base(ulb)
    {
        nextSearchFrame =
        Time.frameCount +
        (UnitLogic.Index % BattleDefine.SearchInterval);
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerMove();
        UnitLogic.UnitView.EnterState(EStateTyep.Move);
        nextSearchFrame =
       Time.frameCount +
       (UnitLogic.Index % BattleDefine.SearchInterval);
    }

    public override void UpdateState(float dt)
    {
        //if (Time.frameCount >= nextSearchFrame)
        {
            searchTargetUnits();
            getTargetUnits();
            nextSearchFrame = nextSearchFrame + BattleDefine.SearchInterval;
        }
        updateMove();
    }


    public override void ExitState()
    {
    }

    private void searchTargetUnits()
    {
        SkillLogicBase normalSkill = UnitLogic.NormalSkill;
        normalSkill.SkillSearchTarget();
    }

    private void getTargetUnits()
    {
        UnitLogicBase ulb = UnitLogic.NormalSkill.GetSkillSearchTargetSingleResult();
        if (ulb != null)
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

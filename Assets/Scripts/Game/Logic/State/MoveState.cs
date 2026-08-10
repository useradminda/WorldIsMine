
using System.Collections.Generic;


public class MoveState : StateBase
{
    public override EStateTyep StateType { get { return EStateTyep.Move; } }

    public MoveState(UnitLogicBase ulb):base(ulb)
    {

    }

    public override void EnterState(params object[] objects)
    {
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

    private List<UnitLogicBase> targetUnits = new List<UnitLogicBase>();
    private void searchTargetUnits()
    {
        SkillLogicBase normalSkill = UnitLogic.NormalSkill;
        normalSkill.SkillSearchTarget();
        //normalSkill.SkillSearchTargetBYKd();
        //if (targetUnits != null && targetUnits.Count > 0)
        //{
        //    UnitLogicBase ulb = targetUnits[0];
        //    float sqrDistance = (UnitLogic.CurPos - ulb.CurPos).sqrMagnitude;
        //    if (sqrDistance <= UnitLogic.NormalSkill.SkillCfg.atkRange * UnitLogic.NormalSkill.SkillCfg.atkRange)
        //    {
        //        UnitLogic.StateMachine.ChangeState(EStateTyep.Attack, targetUnits[0], UnitLogic.NormalSkill);
        //    }
        //}
    }

    private void getTargetUnits()
    {
        targetUnits.Clear();
        targetUnits.AddRange(UnitLogic.NormalSkill.GetSkillSearchTargetResult());
        if (targetUnits.Count > 0)
        {
            UnitLogicBase ulb = targetUnits[0];
            float sqrDistance = (UnitLogic.CurPos - ulb.CurPos).sqrMagnitude;
            if (sqrDistance <= UnitLogic.NormalSkill.SkillCfg.atkRange * UnitLogic.NormalSkill.SkillCfg.atkRange)
            {
               // UnitLogic.StateMachine.ChangeState(EStateTyep.Attack, targetUnits[0], UnitLogic.NormalSkill);
            }
        }
    }

    private void updateMove()
    {
        UnitLogic.MoveForward();
    }
}

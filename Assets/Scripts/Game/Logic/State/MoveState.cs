
using System.Collections.Generic;


public class MoveState : StateBase
{
    public override EStateTyep StateType { get { return EStateTyep.Move; } }

    public MoveState(UnitLogicBase ulb):base(ulb)
    {

    }

    public override void EnterState(params object[] objects)
    {

    }

    public override void UpdateState(float dt)
    {
        searchTargetUnits();
    }

    public override void ExitState()
    {
        updateMove();
        searchTargetUnits();
    }

    private void searchTargetUnits()
    {
        SkillLogicBase normalSkill = UnitLogic.NormalSkill;
        List<UnitLogicBase> targetUnits = normalSkill.SkillSearchTarget();
        if (targetUnits != null && targetUnits.Count > 0)
        {
            UnitLogicBase ulb = targetUnits[0];
            float sqrDistance = (UnitLogic.CurPos - ulb.CurPos).sqrMagnitude;
            if (sqrDistance <= UnitLogic.NormalSkill.SkillCfg.atkRange * UnitLogic.NormalSkill.SkillCfg.atkRange)
            {
                UnitLogic.StateMachine.ChangeState(EStateTyep.Attack, targetUnits[0], UnitLogic.NormalSkill);
            }
        }
    }

    private void updateMove()
    {
        float agentSpeed = UnitLogic.Agenter.saveMaxSpeed;
        UnitLogic.Agenter.maxSpeed = agentSpeed;
        UnitLogic.Agenter.prefVelocity = UnitLogic.TargetForward;
    }
}

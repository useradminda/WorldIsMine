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
        getTargetUnits();
        updateMove();
    }

    public override void ExitState()
    {
    }

    private void getTargetUnits()
    {
        SkillLogicBase skillLogic = UnitLogic.GetNormalSkillBySearchTarget();
        if (skillLogic != null)
        {
            UnitLogic.SetSearchTarget(skillLogic.SkillSearchTarget);
            float sqrDistance = (UnitLogic.CurPos - skillLogic.SkillSearchTarget.CurPos).sqrMagnitude;
            if (sqrDistance <= skillLogic.SkillCfg.atkRange * skillLogic.SkillCfg.atkRange)
            {
                UnitLogic.StateMachine.ChangeState(EStateTyep.Attack, skillLogic.SkillSearchTarget, skillLogic);
            }
        }
        else
        {
            UnitLogic.SetSearchTarget(null);
        }
    }

    private void updateMove()
    {
        UnitLogic.MoveForward();
    }
}

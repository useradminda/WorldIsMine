
public class AttackState : StateBase
{
    public override EStateTyep StateType { get { return EStateTyep.Attack; } }

    private UnitLogicBase targetUnit;
    private SkillLogicBase useSkill;

    public AttackState(UnitLogicBase ulb) : base(ulb)
    {
       
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerMoveStop();
        UnitLogic.UnitView.EnterState(EStateTyep.Attack);
        targetUnit = (UnitLogicBase)objects[0];
        useSkill = (SkillLogicBase)objects[1];
        useSkill.SkillEnter();
    }

    public override void UpdateState(float dt)
    {  
        skillUpdate(dt);
        judgeTargetBeDead();
    }

    public override void ExitState()
    {

    }

    private void judgeTargetBeDead()
    {
        if (targetUnit != null)
        {
            if (targetUnit.IsDead)
            {
                if (UnitLogic.NormalSkill.CurCD <= 0)
                {
                    UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
                    return;
                }
            }
            float sqrDistance = (UnitLogic.CurPos - targetUnit.CurPos).sqrMagnitude;
            if (sqrDistance > UnitLogic.NormalSkill.SkillCfg.atkRange * UnitLogic.NormalSkill.SkillCfg.atkRange)
            {
                UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
                return;
            }
        }
    }

    private void skillUpdate(float dt)
    {
        useSkill.SkillUpdate(dt);
    }
}


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
        if (targetUnit != null && targetUnit.IsDead)
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
        }
    }

    private void skillUpdate(float dt)
    {
        useSkill.SkillDoEffectUpdate(dt);
    }
}

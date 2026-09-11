
public class AttackState : StateBase
{
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
        judgeTargetBeDead();
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

    }

    private void judgeTargetBeDead()
    {
        if (useSkill.BNormalSkill)
        {
            if (useSkill.SkillSearchTarget != null)
            {
                if (useSkill.SkillSearchTarget.IsDead)
                {
                    if (UnitLogic.NormalSkill.CurCD <= 0)
                    {
                        UnitLogic.StateMachine.ChangeState(EStateTyep.Move);
                        return;
                    }
                }
                float sqrDistance = (UnitLogic.CurPos - useSkill.SkillSearchTarget.CurPos).sqrMagnitude;
                if (sqrDistance > UnitLogic.NormalSkill.SkillCfg.atkRange * UnitLogic.NormalSkill.SkillCfg.atkRange)
                {
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
        useSkill.SkillDoEffect();
    }
}

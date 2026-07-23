
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
        searchEnemyUnits();
    }

    public override void ExitState()
    {

    }

    private void searchEnemyUnits()
    {
        SkillLogicBase normalSkill = UnitLogic.NormalSkill;
        List<UnitLogicBase> enemyUnits = normalSkill.SkillSearchTarget();
        if (enemyUnits != null && enemyUnits.Count > 0)
        {
            UnitLogic.StateMachine.ChangeState(EStateTyep.Attack, enemyUnits[0], UnitLogic.NormalSkill);
        }
    }
}

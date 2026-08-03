
using System.Collections.Generic;
public class DieState : StateBase
{
    public override EStateTyep StateType { get { return EStateTyep.Die; } }

    public DieState(UnitLogicBase ulb) : base(ulb)
    {

    }

    public override void EnterState(params object[] objects)
    {

    }

    public override void UpdateState(float dt)
    {
    }

    public override void ExitState()
    {

    }
}

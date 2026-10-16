
public class DieState : StateBase
{
    private float dieActionTime = 0;
    public override EStateTyep StateType { get { return EStateTyep.Die; } }

    public DieState(UnitLogicBase ulb) : base(ulb)
    {
      
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerDie();
        UnitLogic.UnitView.EnterState(EStateTyep.Die, objects);
    }

    public override void UpdateState(float dt)
    {
       
    }

    public override void ExitState()
    {

    }

    public override void LateUpdateState(float dt)
    {
       
    }
}

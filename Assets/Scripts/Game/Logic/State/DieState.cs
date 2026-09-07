
public class DieState : StateBase
{
    private float dieActionTime = 0;
    public override EStateTyep StateType { get { return EStateTyep.Die; } }

    public DieState(UnitLogicBase ulb) : base(ulb)
    {
        dieActionTime = 0;
    }

    public override void EnterState(params object[] objects)
    {
        UnitLogic.TriggerDie();
        UnitFactory.RecycleId(UnitLogic.CampType, UnitLogic.Index);


       
        UnitLogic.UnitView.EnterState(EStateTyep.Die, objects);
    }

    public override void UpdateState(float dt)
    {
        if (dieActionTime > 0)
        {
            dieActionTime -= dt;
        }
        if (dieActionTime < 0)
        { 
            UnitViewFactory.RemoveUnitView(UnitLogic.UnitView);
            dieActionTime = 0;
        }
    }

    public override void ExitState()
    {

    }
}

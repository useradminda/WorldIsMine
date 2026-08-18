
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
        UnitLogic.UnitView.EnterState(EStateTyep.Die);
        dieActionTime = UnitLogic.UnitView.GetDieTime();
    }

    public override void UpdateState(float dt)
    {
        if (dieActionTime > 0)
        {
            dieActionTime -= dt;
        }
        if (dieActionTime < 0)
        { 
            UnitFactory.RecycleId(UnitLogic.CampType, UnitLogic.Index);
            UnitViewFactory.RemoveUnitView(UnitLogic.UnitView);
            dieActionTime = 0;
        }
    }

    public override void ExitState()
    {

    }
}


using UnityEngine;
public class UnitView : IView
{
    private UnitLogicBase unitLogic;

    private ActionFlow actionFlow;
    
    public ActionFlow ActionFlowComponent
    {
        get
        {
            if (actionFlow == null)
            {
                actionFlow = gameObject.GetOrAddComponentInChild<ActionFlow>();
            }
            return actionFlow;
        }
    }

    public void Init(UnitLogicBase unit)
    {
        this.unitLogic = unit;
    }

    public override void ViewInit()
    {

    }

    public override void ViewUpdate()
    {
        updatePos();
        updateRot();
        updateState();
    }

    public override void ViewDestroy()
    {

    }

    public override void ViewRefuse()
    {

    }

    private void updatePos()
    {
        if (unitLogic != null)
        {
            transform.position = Vector3.Lerp(transform.position, unitLogic.Agenter.pos, Time.deltaTime * 3);
        }
    }

    private void updateRot()
    {
        if (unitLogic != null)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(unitLogic.TargetForward), Time.deltaTime * 3);
        }
    }

    private void updateState()
    {
        if (unitLogic != null)
        {
            if (unitLogic.StateMachine.StateDirty)
            {
                StateBase state = unitLogic.StateMachine.GetCurrentState();
                if (state != null)
                {
                    if (state.StateType == EStateTyep.Move)
                    {
                        ActionFlowComponent.PlayAction(EActionType.wait);
                    }
                    else if (state.StateType == EStateTyep.Attack)
                    {
                        ActionFlowComponent.PlayAction(EActionType.attack);
                    }
                    else if (state.StateType == EStateTyep.Die)
                    {
                        ActionFlowComponent.PlayAction(EActionType.die);
                    }
                }
                unitLogic.StateMachine.ClearStateDirty();
            }
        }
    }
}

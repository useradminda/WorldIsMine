using System.Collections;
using System.Collections.Generic;
public class StateMachine 
{
    // 当前状态
    private StateBase currentState;
    // 所有状态
    private List<StateBase> states;


    public StateMachine(UnitLogicBase unitLogic)
    {
        states = new List<StateBase>();
        states.Add(new IdleState(unitLogic));
        states.Add(new MoveState(unitLogic));
        states.Add(new AttackState(unitLogic));
        currentState = states[0];
    }

    public void ChangeState(EStateTyep stateType, params object[] objects)
    {
        if (currentState != null)
        {
            currentState.ExitState();
        }
        currentState = GetState(stateType);
        currentState?.EnterState(objects);
    }

    public void UpdateState(float dt)
    {
        if (currentState != null)
        {
            currentState.UpdateState(dt);
        }
    }

    public void ExitState()
    {
        if (currentState != null)
        {
            currentState.ExitState();
        }
    }

    public StateBase GetCurrentState()
    {
        return currentState;
    }

    public StateBase GetState(EStateTyep stateType)
    {
        return states.Find(state => state.StateType == stateType);
    }
}
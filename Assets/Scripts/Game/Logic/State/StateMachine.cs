using System.Collections;
using System.Collections.Generic;
using UnityEditor;
public class StateMachine 
{
    // 当前状态
    private StateBase currentState;
    // 所有状态
    private List<StateBase> states;

    /// <summary>
    ///  状态切换脏标记
    /// </summary>
    private bool stateDirty = true;
    public bool StateDirty => stateDirty;


    public StateMachine(UnitLogicBase unitLogic)
    {
        states = new List<StateBase>();
        states.Add(new IdleState(unitLogic));
        states.Add(new MoveState(unitLogic));
        states.Add(new AttackState(unitLogic));
        currentState = states[0];
    }

    public void ChangeState(EStateTyep enterStateType, params object[] objects)
    {
        if (currentState != null)
        {
            currentState.ExitState();
        }
        StateBase enterState = GetState(enterStateType);
        if (enterState != currentState)
        {
            stateDirty = true;
            currentState = enterState;
            currentState?.EnterState(objects);
        }
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

    public void ClearStateDirty()
    {
        stateDirty = false; 
    }
}
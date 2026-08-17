using System.Collections;
using System.Collections.Generic;

public class StateMachine 
{
    // 当前状态
    private StateBase currentState;
    // 所有状态
    private List<StateBase> states;

    private Dictionary<EStateTyep, StateBase> statesByEState = new Dictionary<EStateTyep, StateBase>();

    /// <summary>
    ///  状态切换脏标记
    /// </summary>
    private bool stateDirty = true;
    public bool StateDirty => stateDirty;

    public StateMachine(UnitLogicBase unitLogic)
    {
        stateDirty = false;
        states = new List<StateBase>();

        statesByEState.Add(EStateTyep.Idle, new IdleState(unitLogic));
        statesByEState.Add(EStateTyep.Move, new MoveState(unitLogic));
        statesByEState.Add(EStateTyep.Attack, new AttackState(unitLogic));
        statesByEState.Add(EStateTyep.Die, new DieState(unitLogic));
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
        return statesByEState[stateType];
        //return states.Find(state => state.StateType == stateType);
    }

    public void ClearStateDirty()
    {
        stateDirty = false; 
    }
}
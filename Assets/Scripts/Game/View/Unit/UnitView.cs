
using UnityEngine;

public class UnitView : IView
{
    private UnitLogicBase unitLogic;

    private string prefabName;
    public string PrefabName => prefabName;

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

    private SlashComponent slashComp;
    public SlashComponent SlachComp
    {
        get
        {
            if(slashComp == null)
                slashComp = gameObject.GetOrAddComponent<SlashComponent>();
            return slashComp;
        }
    }

    private FreezeComponent freezeComp;
    public FreezeComponent FreezeComp
    {
        get
        {
            if (freezeComp == null)
                freezeComp = gameObject.GetOrAddComponent<FreezeComponent>();
            return freezeComp;
        }
    }

    public void Init(UnitLogicBase unit, string prefabName)
    {
        this.prefabName = prefabName;
        this.unitLogic = unit;
        SlachComp.ExitSlash();
    }

    public override void ViewInit()
    {

    }

    public override void ViewUpdate(float dt)
    {
        updatePos(dt);
        updateRot(dt);

    }

    public override void ViewDestroy()
    {

    }

    public override void ViewRefuse()
    {

    }

    public void BeHitSlash()
    {
        SlachComp.SetSlash();
    }

    public void EnterState(EStateTyep stateType)
    {
        if (unitLogic != null)
        {
            if (unitLogic.StateMachine.StateDirty)
            {
                if (stateType == EStateTyep.Move)
                {
                    ActionFlowComponent.PlayAction(EActionType.run);
                }
                else if (stateType == EStateTyep.Attack)
                {
                    ActionFlowComponent.PlayAction(EActionType.attack);
                }
                else if (stateType == EStateTyep.Die)
                {
                    ActionFlowComponent.PlayAction(EActionType.die);
                }
                unitLogic.StateMachine.ClearStateDirty();
            }
        }
    }

    public float GetDieTime()
    {
        return ActionFlowComponent.GetAnimLen(EActionType.die);
    }

    private Vector3 tarPos;
    private Vector3 transPos;
    private void updatePos(float dt)
    {
        if (unitLogic.Agenter.DirtyPos == true)
        {
            tarPos = unitLogic.Agenter.pos;
            unitLogic.Agenter.DirtyPos = false;
            transPos = transform.position;
        }
        if (tarPos != transPos)
        {
            if ((tarPos - transform.position).sqrMagnitude < 0.0004f)
            {
                transform.position = tarPos;
                transPos = transform.position;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, unitLogic.Agenter.pos, dt * 3);
                transPos = transform.position;
            }
        }
    }
    private Quaternion targetQ;
    private Quaternion transQ;
    private void updateRot(float dt)
    {
        if (unitLogic.DirtyForward == true)
        {
            targetQ = Quaternion.LookRotation(unitLogic.TargetForward);
            unitLogic.DirtyForward = false;
            transQ = transform.rotation;
        }
        if (targetQ != transQ)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, transQ, dt * 3);
            transQ = transform.rotation;
        }
    }
}

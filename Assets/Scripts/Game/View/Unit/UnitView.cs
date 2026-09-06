using UnityEngine;

public class UnitView : IView
{
    private UnitLogicBase unitLogic;

    private string prefabName;
    public string PrefabName => prefabName;

    private EStateTyep stateType;

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

    public void EnterState(EStateTyep stateType, params object[] paramsInfo)
    {
        if (unitLogic != null)
        {
            if (unitLogic.StateMachine.StateDirty)
            {
                this.stateType = stateType;
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
                    enterDieState(System.Convert.ToSingle(paramsInfo[0]));
                    ActionFlowComponent.PlayAction(EActionType.die);
                }
                unitLogic.StateMachine.ClearStateDirty();
            }
        }
    }

    // get dead time
    public float GetDieTime()
    {
        return ActionFlowComponent.GetAnimLen(EActionType.die);
    }

    public void PlayEffect(string prefabName, Vector3 pos, Vector3 forward, float time)
    {
        if (prefabName == "")
            return;
        GameObject go = UnitViewFactory.CreateGob(prefabName, pos, forward);
        go.GetOrAddComponent<RecycleGobComponent>().SetRecycleGobTime(time, prefabName);
    }

    private Vector3 tarPos;
    private Vector3 transPos;
    private void updatePos(float dt)
    {
        if (stateType == EStateTyep.Die)
        {
            updateDiePos(dt);
            return;
        }
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
        if (stateType == EStateTyep.Die)
            return;
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

    private Vector3 startPosition;
    private Vector3 backwardDirection;
    private float startTime;
    private float duration;
    private float backwardDistance = 5;
    private float maxHeight = 3f;
    private bool playingDie;
    // 进入
    private void enterDieState(float duration)
    {
        this.duration = duration;
        startPosition = transform.position;
        backwardDirection = -transform.forward;
        startTime = Time.time;
        playingDie = true;
    }

    private void updateDiePos(float dt)
    {
        if (!playingDie || transform == null)
            return;
        float t = Mathf.Clamp01((Time.time - startTime) / duration);
        Vector3 horizontalOffset = backwardDirection * backwardDistance * t;
        float height = 4f * maxHeight * t * (1f - t);
        transform.position = startPosition + horizontalOffset + Vector3.up * height;

        if (t >= 1f)
        {
            transform.position = startPosition + backwardDirection * backwardDistance;
            playingDie = false;
            return;
        }
    }
}
